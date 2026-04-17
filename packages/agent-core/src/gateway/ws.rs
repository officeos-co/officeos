//! WebSocket upgrade handler with query-param auth. See API.md §8.2.

use std::sync::Arc;

use axum::extract::ws::{Message, WebSocket};
use futures::stream::SplitSink;
use futures::{SinkExt, StreamExt};
use tokio::sync::mpsc;

use crate::config::RuntimeConfig;
use crate::gateway::protocol::{WsInbound, WsOutbound};

/// Handle a WebSocket connection after upgrade. Per-connection lifecycle:
/// parse inbound frames, drive the agent turn loop, emit outbound events.
pub async fn handle_ws(
    _cfg: Arc<RuntimeConfig>,
    agent: Arc<crate::agent::Agent>,
    socket: WebSocket,
) {
    tracing::info!("ws session started");
    let (ws_sink, mut ws_stream) = socket.split();

    // Channel for sending outbound WS frames.
    let (out_tx, out_rx) = mpsc::unbounded_channel::<WsOutbound>();

    // Spawn a task that forwards outbound events to the WS sink.
    let sink_handle = tokio::spawn(forward_outbound(ws_sink, out_rx));

    // Track whether a turn is in flight. In the current sequential loop
    // this is always false at the top of each iteration, but the flag is
    // retained for structural parity with API.md §8.7 (one turn per conn).
    let mut turn_in_flight = false;

    // Read inbound frames.
    while let Some(Ok(msg)) = ws_stream.next().await {
        let text = match msg {
            Message::Text(t) => t.to_string(),
            Message::Close(_) => break,
            _ => continue,
        };

        let inbound: WsInbound = match serde_json::from_str(&text) {
            Ok(m) => m,
            Err(e) => {
                let _ = out_tx.send(WsOutbound::Error {
                    turn_id: None,
                    code: "BAD_REQUEST".to_string(),
                    message: format!("invalid JSON: {e}"),
                });
                continue;
            }
        };

        match inbound {
            WsInbound::UserMessage { text, id } => {
                let turn_id_display = id.as_deref().unwrap_or("none");
                tracing::info!(turn_id = %turn_id_display, "user message received");
                if turn_in_flight {
                    tracing::warn!("user message rejected: turn in flight");
                    let _ = out_tx.send(WsOutbound::Error {
                        turn_id: None,
                        code: "BAD_REQUEST".to_string(),
                        message: "turn in flight".to_string(),
                    });
                    continue;
                }

                // Sequential loop so the `true` is always overwritten to
                // `false` at the end of this arm, but retained per API.md §8.7.
                #[allow(unused_assignments)]
                {
                    turn_in_flight = true;
                }
                let turn_id = id.clone();

                let result = agent
                    .handle_user_message_with_ws(text, turn_id, &out_tx)
                    .await;

                if let Err(e) = result {
                    tracing::error!(turn_id = %turn_id_display, error = %e, "turn failed");
                    let _ = out_tx.send(WsOutbound::Error {
                        turn_id: id.clone(),
                        code: "INTERNAL".to_string(),
                        message: format!("{e}"),
                    });
                    let _ = out_tx.send(WsOutbound::TurnComplete {
                        turn_id: id.unwrap_or_default(),
                        cancelled: false,
                    });
                } else {
                    tracing::info!(turn_id = %turn_id_display, "turn complete");
                }

                turn_in_flight = false;
            }
            WsInbound::AskUserResponse { id: _, response: _ } => {
                // Phase 3 TODO: wire to AskUserBridge once ask_user tool is implemented.
            }
            WsInbound::Cancel { id } => {
                // Phase 3 TODO: cancel support. For now, acknowledge.
                if turn_in_flight {
                    let _ = out_tx.send(WsOutbound::TurnComplete {
                        turn_id: id.unwrap_or_default(),
                        cancelled: true,
                    });
                    turn_in_flight = false;
                }
            }
        }
    }

    // Clean up.
    tracing::info!("ws session ended");
    drop(out_tx);
    let _ = sink_handle.await;
}

/// Forward outbound events from the channel to the WebSocket sink.
async fn forward_outbound(
    mut sink: SplitSink<WebSocket, Message>,
    mut rx: mpsc::UnboundedReceiver<WsOutbound>,
) {
    while let Some(msg) = rx.recv().await {
        let json = match serde_json::to_string(&msg) {
            Ok(j) => j,
            Err(_) => continue,
        };
        if sink.send(Message::Text(json.into())).await.is_err() {
            break;
        }
    }
}
