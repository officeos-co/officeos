//! Bootstrap flow — `GET {backend_url}/api/agents/{id}` with retry,
//! mapped into a `RuntimeConfig`. See API.md §3.
//!
//! Retry budget: 10 attempts, 1→2→4→8→16→30→30... (capped at 30s).

use crate::config::RuntimeConfig;
use crate::error::Result;

/// Max retry attempts on 5xx / network errors before giving up.
pub const MAX_BOOTSTRAP_ATTEMPTS: u32 = 10;

/// Upper bound on the exponential backoff between retries.
pub const MAX_BACKOFF_SECS: u64 = 30;

/// Fetch the bootstrap payload from the backend, retrying on transient
/// failure, and assemble a `RuntimeConfig`.
///
/// Phase 3: implement retry loop, deserialize the payload, validate
/// gateway.port != 0, validate systemPrompt non-empty, fold permissions
/// into the HashMap keyed by lowercased `(skill, tool)`.
pub async fn bootstrap(agent_id: String, backend_url: String) -> Result<RuntimeConfig> {
    let _ = (agent_id, backend_url);
    todo!("Phase 3: retry GET /api/agents/{{id}}, parse payload, build RuntimeConfig")
}
