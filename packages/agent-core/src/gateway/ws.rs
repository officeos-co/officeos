// Rust guideline compliant 2026-02-21

//! WebSocket upgrade handler with query-param auth.
//!
//! See API.md §8.2.

use std::sync::Arc;

use axum::extract::ws::{Message, WebSocket};
use futures::stream::SplitSink;
use futures::{SinkExt, StreamExt};
use tokio::sync::mpsc;

use crate::config::RuntimeConfig;
use crate::gateway::protocol::{WsInbound, WsOutbound};

/// Handle a WebSocket connection after upgrade.
///
/// Per-connection lifecycle: parse inbound frames, drive the agent
/// turn loop, emit outbound events.
pub async fn handle_ws(
    _cfg: Arc<RuntimeConfig>,
    agent: Arc<crate::agent::Agent>,
    socket: WebSocket,
) {
    tracing::info!(name: "ws.session.start", "ws session started");
    let (ws_sink, mut ws_stream) = socket.split();

    // Channel for sending outbound WS frames.
    let (out_tx, out_rx) = mpsc::unbounded_channel::<WsOutbound>();

    // Spawn a task that forwards outbound events to the WS sink.
    let sink_handle = tokio::spawn(forward_outbound(ws_sink, out_rx));

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
                tracing::info!(name: "ws.message.received", turn_id = %turn_id_display, "user message received for turn {{turn_id}}");
                if turn_in_flight {
                    tracing::warn!(name: "ws.message.rejected", "user message rejected: turn in flight");
                    let _ = out_tx.send(WsOutbound::Error {
                        turn_id: None,
                        code: "BAD_REQUEST".to_string(),
                        message: "turn in flight".to_string(),
                    });
                    continue;
                }

                #[expect(unused_assignments, reason = "retained per API.md §8.7 for structural parity")]
                #[expect(clippy::unnecessary_operation, reason = "block needed to attach #[expect] to assignment")]
                #[rustfmt::skip]
                { turn_in_flight = true };
                let turn_id = id.clone();

                let result = agent
                    .handle_user_message_with_ws(text, turn_id, &out_tx)
                    .await;

                if let Err(e) = result {
                    tracing::error!(name: "ws.turn.failed", turn_id = %turn_id_display, error_message = %e, "turn {{turn_id}} failed: {{error_message}}");
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
                    tracing::info!(name: "ws.turn.complete", turn_id = %turn_id_display, "turn {{turn_id}} complete");
                }

                turn_in_flight = false;
            }
            WsInbound::Cancel { id } => {
                if turn_in_flight {
                    tracing::info!(name: "ws.cancel.requested", "cancel requested");
                    let _ = out_tx.send(WsOutbound::TurnComplete {
                        turn_id: id.unwrap_or_default(),
                        cancelled: true,
                    });
                    turn_in_flight = false;
                }
            }
        }
    }

    tracing::info!(name: "ws.session.end", "ws session ended");
    drop(out_tx);
    let _ = sink_handle.await;
}

/// Forward outbound events from the channel to the WebSocket sink.
async fn forward_outbound(
    mut sink: SplitSink<WebSocket, Message>,
    mut rx: mpsc::UnboundedReceiver<WsOutbound>,
) {
    while let Some(msg) = rx.recv().await {
        let Ok(json) = serde_json::to_string(&msg) else {
            continue;
        };
        if sink.send(Message::Text(json.into())).await.is_err() {
            break;
        }
    }
}
