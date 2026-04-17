//! WebSocket upgrade handler with query-param auth. See API.md §8.2.

use std::sync::Arc;

use crate::config::RuntimeConfig;

/// Handle a WebSocket connection after upgrade. Per-connection lifecycle:
/// parse inbound frames, drive the agent turn loop, emit outbound events.
pub async fn handle_ws(
    _cfg: Arc<RuntimeConfig>,
    _agent: Arc<crate::agent::Agent>,
) {
    todo!("Phase 3: WS frame loop — parse WsInbound, drive turn loop, send WsOutbound")
}
