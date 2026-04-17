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
/// Parses and validates both vars, strips the trailing slash from the
/// URL, returns the pair. Missing or malformed → `Error::Env`.
pub fn load_env() -> Result<(String, String)> {
    let agent_id = std::env::var(AGENT_ID_VAR)
        .ok()
        .filter(|s| !s.is_empty())
        .ok_or_else(|| missing(AGENT_ID_VAR))?;

    // Validate UUID format.
    uuid::Uuid::parse_str(&agent_id)
        .map_err(|e| Error::Env(format!("{AGENT_ID_VAR} is not a valid UUID: {e}")))?;

    let backend_url = std::env::var(BACKEND_URL_VAR)
        .ok()
        .filter(|s| !s.is_empty())
        .ok_or_else(|| missing(BACKEND_URL_VAR))?;

    // Validate URL format.
    reqwest::Url::parse(&backend_url)
        .map_err(|e| Error::Env(format!("{BACKEND_URL_VAR} is not a valid URL: {e}")))?;

    // Strip trailing slash.
    let backend_url = backend_url.trim_end_matches('/').to_string();

    tracing::info!("environment validated");
    Ok((agent_id, backend_url))
}

/// Convenience wrapper: emit a consistent error when a required env var
/// is missing. Real (not `todo!()`) — this is a data helper.
pub fn missing(var: &str) -> Error {
    Error::Env(format!("{var} is required but not set or empty"))
}
