//! Gateway bootstrap — when `ZEROCLAW_AGENT_ID` is set, the agent
//! derives ALL configuration from that single identity value plus a
//! hardcoded backend URL. No other env vars are needed.
//!
//! The agent's UUID doubles as its bearer token for backend calls
//! (LLM proxy, skills gateway, vault proxy). The backend validates
//! it by looking up the UUID in the Agents table.
//!
//! ## What gets configured
//!
//! - **Provider**: `custom:{backend_url}/v1` — all LLM calls route
//!   through the backend's `/v1/chat/completions` proxy. The backend
//!   resolves the real provider + API key from the agent record.
//! - **API key**: the agent UUID itself (used as `Authorization: Bearer`).
//! - **Model**: `"backend-managed"` placeholder — the backend overrides
//!   the model from the agent record, so the pod doesn't need to know.
//! - **Skills**: `backend_url` + agent UUID as token.
//! - **Gateway**: 0.0.0.0:42617, no pairing, public bind.
//! - **Workspace**: `/zeroclaw-data` (PVC mount path).
//!
//! ## Vault
//!
//! When gateway bootstrap is active, `vault_bootstrap::hydrate` fetches
//! vault files from the backend's memory proxy
//! (`GET /api/agents/{id}/memory/{file}`) instead of CouchDB directly.
//! The agent never knows CouchDB exists.

use crate::config::Config;

/// Default backend URL for in-cluster pods. The k8s Service
/// `eaos-backend-prod` resolves to the backend Deployment.
const DEFAULT_BACKEND_URL: &str = "http://eaos-backend-prod:8000";

const ENV_AGENT_ID: &str = "ZEROCLAW_AGENT_ID";
const ENV_BACKEND_URL: &str = "ZEROCLAW_BACKEND_URL";

/// If `ZEROCLAW_AGENT_ID` is set, override the config to route
/// everything through the backend. Returns `Some(agent_id)` if
/// gateway bootstrap is active, `None` otherwise.
pub fn apply(config: &mut Config) -> Option<String> {
    let agent_id = match std::env::var(ENV_AGENT_ID) {
        Ok(id) if !id.trim().is_empty() => id.trim().to_string(),
        _ => return None,
    };

    let backend_url = std::env::var(ENV_BACKEND_URL)
        .ok()
        .filter(|s| !s.trim().is_empty())
        .map(|s| s.trim().trim_end_matches('/').to_string())
        .unwrap_or_else(|| DEFAULT_BACKEND_URL.to_string());

    tracing::info!(
        agent_id = %agent_id,
        backend_url = %backend_url,
        "Gateway bootstrap: agent derives all config from backend"
    );

    // Provider: custom proxy → backend handles routing to real provider
    config.default_provider = Some(format!("custom:{backend_url}/v1"));
    // API key = agent UUID, used as bearer token for all backend calls
    config.api_key = Some(agent_id.clone());
    // Model: placeholder — backend overrides from agent record per-request
    config.default_model = Some("backend-managed".to_string());

    // Skills gateway
    config.skills.backend_url = Some(backend_url.clone());
    config.skills.backend_token = Some(agent_id.clone());
    config.skills.backend_refresh_seconds = 30;

    // Gateway: bind to all interfaces for k8s service routing
    config.gateway.port = 42617;
    config.gateway.host = "0.0.0.0".to_string();
    config.gateway.require_pairing = false;
    config.gateway.allow_public_bind = true;

    // Workspace on PVC
    config.workspace_dir = std::path::PathBuf::from("/zeroclaw-data/workspace");

    Some(agent_id)
}

/// Returns the backend URL for vault proxy calls, or `None` if
/// gateway bootstrap is not active.
pub fn backend_url() -> Option<String> {
    let _ = std::env::var(ENV_AGENT_ID).ok().filter(|s| !s.trim().is_empty())?;
    Some(
        std::env::var(ENV_BACKEND_URL)
            .ok()
            .filter(|s| !s.trim().is_empty())
            .map(|s| s.trim().trim_end_matches('/').to_string())
            .unwrap_or_else(|| DEFAULT_BACKEND_URL.to_string()),
    )
}

/// Returns the agent ID if gateway bootstrap is active.
pub fn agent_id() -> Option<String> {
    std::env::var(ENV_AGENT_ID)
        .ok()
        .filter(|s| !s.trim().is_empty())
        .map(|s| s.trim().to_string())
}
