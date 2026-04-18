// Rust guideline compliant 2026-02-21

//! WebSocket gateway — Axum app serving `/ws` and `/health`.
//!
//! See API.md §8.

pub mod protocol;
pub mod ws;

use std::net::SocketAddr;
use std::sync::Arc;

use axum::extract::Query;
use axum::response::IntoResponse;
use axum::routing::get;
use axum::Router;

use crate::config::RuntimeConfig;

/// Query params on the WS upgrade URL.
#[derive(Debug, serde::Deserialize)]
struct WsQuery {
    token: Option<String>,
}

/// Start the Axum HTTP server and block until shutdown.
///
/// Binds to `cfg.gateway_host:cfg.gateway_port` with a `/ws` route
/// that upgrades to WebSocket and a `/health` route.
///
/// # Errors
///
/// Returns `Error::Gateway` if the bind address is invalid or the
/// listener fails to start.
pub async fn serve(
    cfg: Arc<RuntimeConfig>,
    agent: Arc<crate::agent::Agent>,
) -> crate::error::Result<()> {
    let cfg_for_ws = Arc::clone(&cfg);
    let agent_for_ws = Arc::clone(&agent);

    let app = Router::new()
        .route(
            "/ws",
            get(
                move |ws_upgrade: axum::extract::WebSocketUpgrade, Query(query): Query<WsQuery>| {
                    let cfg = Arc::clone(&cfg_for_ws);
                    let agent = Arc::clone(&agent_for_ws);
                    async move {
                        // Auth: reject if token missing or wrong.
                        let expected = cfg.agent_id.to_string();
                        match query.token {
                            Some(ref t) if t == &expected => {
                                tracing::info!(name: "gateway.ws.accepted", "ws connection accepted");
                            }
                            _ => {
                                tracing::info!(name: "gateway.ws.rejected", "ws connection rejected: unauthorized");
                                return (axum::http::StatusCode::UNAUTHORIZED, "unauthorized")
                                    .into_response();
                            }
                        }

                        ws_upgrade
                            .on_upgrade(move |socket| ws::handle_ws(cfg, agent, socket))
                            .into_response()
                    }
                },
            ),
        )
        .route("/health", get(|| async { "ok" }));

    let addr: SocketAddr = format!("{}:{}", cfg.gateway_host, cfg.gateway_port)
        .parse()
        .map_err(|e| crate::error::Error::Gateway(format!("invalid bind address: {e}")))?;

    tracing::info!(name: "gateway.listen.start", server_address = %addr, "gateway listening on {{server_address}}");

    let listener = tokio::net::TcpListener::bind(addr)
        .await
        .map_err(|e| crate::error::Error::Gateway(format!("bind failed: {e}")))?;

    axum::serve(listener, app)
        .await
        .map_err(|e| crate::error::Error::Gateway(format!("server error: {e}")))?;

    Ok(())
}
