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

use std::collections::HashMap;
use std::time::Duration;

use serde::Deserialize;

use crate::config::Config;

/// Per-tool allow/deny decision set by the operator on the dashboard
/// and enforced by `SkillExecTool` before dispatch. There is no "ask"
/// mode — the agent pod runs unattended.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum ToolPermission {
    Allow,
    Deny,
}

/// Subset of the backend's `AgentBootstrapPayload` relevant to the pod.
/// Credentials never appear here — provider keys stay behind the LLM
/// proxy and skill credentials stay behind the GraphQL gateway.
#[derive(Debug, Clone)]
pub struct Bootstrap {
    pub model: String,
    pub skills: Vec<String>,
    pub tool_permissions: HashMap<(String, String), ToolPermission>,
}

#[derive(Deserialize)]
struct RawPayload {
    #[serde(default)]
    provider: Option<RawProvider>,
    #[serde(default)]
    skills: Vec<RawSkill>,
    #[serde(default, rename = "toolPermissions")]
    tool_permissions: Vec<RawToolPermission>,
}

#[derive(Deserialize)]
struct RawProvider {
    #[serde(default)]
    model: Option<String>,
}

#[derive(Deserialize)]
struct RawSkill {
    name: String,
}

#[derive(Deserialize)]
struct RawToolPermission {
    skill: String,
    tool: String,
    mode: String,
}

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

    // Skills gateway (legacy REST capabilities + new GraphQL)
    config.skills.backend_url = Some(backend_url.clone());
    config.skills.backend_token = Some(agent_id.clone());
    config.skills.backend_refresh_seconds = 30;
    config.skills.graphql_url = Some(format!("{backend_url}/api/graphql"));

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
    let _ = std::env::var(ENV_AGENT_ID)
        .ok()
        .filter(|s| !s.trim().is_empty())?;
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

/// Fetch the pod-facing bootstrap payload from
/// `GET {backend_url}/api/agents/{id}` and overlay the parts the pod
/// can use onto `config`. Never fatal: on any failure the existing
/// config (env vars / config.toml) remains authoritative so local-dev
/// workflows keep working.
///
/// Returns the per-tool permission map so `SkillExecTool` can enforce
/// allow/deny before dispatch.
pub async fn fetch_and_overlay(
    config: &mut Config,
    agent_id: &str,
    backend_url: &str,
) -> HashMap<(String, String), ToolPermission> {
    let url = format!("{backend_url}/api/agents/{agent_id}");

    let client = match reqwest::Client::builder()
        .timeout(Duration::from_secs(10))
        .build()
    {
        Ok(c) => c,
        Err(e) => {
            tracing::warn!(error = %e, "Failed to build bootstrap HTTP client");
            return HashMap::new();
        }
    };

    let response = match client.get(&url).bearer_auth(agent_id).send().await {
        Ok(r) => r,
        Err(e) => {
            tracing::warn!(error = %e, url = %url, "Bootstrap fetch failed; using env/config fallback");
            return HashMap::new();
        }
    };

    if !response.status().is_success() {
        tracing::warn!(status = %response.status(), url = %url, "Bootstrap returned non-2xx; using env/config fallback");
        return HashMap::new();
    }

    let payload: RawPayload = match response.json().await {
        Ok(p) => p,
        Err(e) => {
            tracing::warn!(error = %e, "Bootstrap response was not valid JSON");
            return HashMap::new();
        }
    };

    // Overlay model from bootstrap (overrides the "backend-managed"
    // placeholder set by `apply`). The provider stays as `custom:<url>/v1`.
    if let Some(provider) = payload.provider {
        if let Some(model) = provider.model {
            let m = model.trim();
            if !m.is_empty() {
                config.default_model = Some(m.to_string());
            }
        }
    }

    // Installed-skills list can be surfaced elsewhere in the future;
    // for now it's just logged for observability.
    let skill_names: Vec<String> = payload.skills.into_iter().map(|s| s.name).collect();
    tracing::info!(
        skills = ?skill_names,
        permissions = payload.tool_permissions.len(),
        "Bootstrap payload applied"
    );

    let mut perms: HashMap<(String, String), ToolPermission> = HashMap::new();
    for tp in payload.tool_permissions {
        let mode = match tp.mode.to_ascii_lowercase().as_str() {
            "allow" => ToolPermission::Allow,
            "deny" => ToolPermission::Deny,
            other => {
                tracing::warn!(mode = %other, "Unknown tool permission mode — skipping");
                continue;
            }
        };
        perms.insert(
            (
                tp.skill.trim().to_ascii_lowercase(),
                tp.tool.trim().to_string(),
            ),
            mode,
        );
    }
    perms
}

/// Fetch bootstrap with the current env-var agent_id/backend_url, or
/// return an empty permission map if gateway bootstrap is not active.
pub async fn fetch_and_overlay_from_env(
    config: &mut Config,
) -> HashMap<(String, String), ToolPermission> {
    let Some(id) = agent_id() else {
        return HashMap::new();
    };
    let Some(backend) = backend_url() else {
        return HashMap::new();
    };
    fetch_and_overlay(config, &id, &backend).await
}
