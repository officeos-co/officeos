//! Environment-variable loading. Exactly two vars: `ZEROCLAW_AGENT_ID`
//! and `BACKEND_URL`. Both required; both validated. See API.md §2.

use crate::error::{Error, Result};

/// Env var name constants — the only runtime inputs the pod accepts.
pub const AGENT_ID_VAR: &str = "ZEROCLAW_AGENT_ID";
pub const BACKEND_URL_VAR: &str = "BACKEND_URL";

/// Read and validate the two required env vars.
///
/// Returns `(agent_id_string, backend_url_without_trailing_slash)`.
///
/// Phase 3: parse + validate both vars, strip the trailing slash from the
/// URL, return the pair. Missing or malformed → `Error::Env`.
pub fn load_env() -> Result<(String, String)> {
    let _ = (AGENT_ID_VAR, BACKEND_URL_VAR);
    todo!("Phase 3: read ZEROCLAW_AGENT_ID and BACKEND_URL, validate both")
}

/// Convenience wrapper: emit a consistent error when a required env var
/// is missing. Real (not `todo!()`) — this is a data helper.
pub fn missing(var: &str) -> Error {
    Error::Env(format!("{var} is required but not set or empty"))
}
