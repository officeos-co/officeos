//! WebSocket gateway — Axum app serving `/ws` + `/health`. See API.md §8.

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
#[derive(serde::Deserialize)]
struct WsQuery {
    token: Option<String>,
}

/// Start the Axum HTTP server on `cfg.gateway_host:cfg.gateway_port` with
/// a `/ws` route that upgrades to WebSocket and a `/health` route.
/// Blocks until shutdown.
pub async fn serve(
    cfg: Arc<RuntimeConfig>,
    agent: Arc<crate::agent::Agent>,
) -> crate::error::Result<()> {
    let cfg_for_ws = cfg.clone();
    let agent_for_ws = agent.clone();

    let app = Router::new()
        .route(
            "/ws",
            get(
                move |ws_upgrade: axum::extract::WebSocketUpgrade,
                      Query(query): Query<WsQuery>| {
                    let cfg = cfg_for_ws.clone();
                    let agent = agent_for_ws.clone();
                    async move {
                        // Auth: reject if token missing or wrong.
                        let expected = cfg.agent_id.to_string();
                        match query.token {
                            Some(ref t) if t == &expected => {}
                            _ => {
                                return (
                                    axum::http::StatusCode::UNAUTHORIZED,
                                    "unauthorized",
                                )
                                    .into_response();
                            }
                        }

                        ws_upgrade
                            .on_upgrade(move |socket| {
                                ws::handle_ws(cfg, agent, socket)
                            })
                            .into_response()
                    }
                },
            ),
        )
        .route("/health", get(|| async { "ok" }));

    let addr: SocketAddr = format!("{}:{}", cfg.gateway_host, cfg.gateway_port)
        .parse()
        .map_err(|e| crate::error::Error::Gateway(format!("invalid bind address: {e}")))?;

    tracing::info!(%addr, "gateway listening");

    let listener = tokio::net::TcpListener::bind(addr)
        .await
        .map_err(|e| crate::error::Error::Gateway(format!("bind failed: {e}")))?;

    axum::serve(listener, app)
        .await
        .map_err(|e| crate::error::Error::Gateway(format!("server error: {e}")))?;

    Ok(())
}
