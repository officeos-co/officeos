//! WebSocket gateway — Axum app serving `/ws`. See API.md §8.

pub mod protocol;
pub mod ws;

use std::sync::Arc;

use crate::config::RuntimeConfig;

/// Start the Axum HTTP server on `cfg.gateway_host:cfg.gateway_port` with
/// a single `/ws` route that upgrades to WebSocket. Blocks until shutdown.
pub async fn serve(
    cfg: Arc<RuntimeConfig>,
    agent: Arc<crate::agent::Agent>,
) -> crate::error::Result<()> {
    let _ = (cfg, agent);
    todo!("Phase 3: build Axum router with /ws route, bind, serve")
}
