//! Bootstrap flow — `GET {backend_url}/api/agents/{id}` with retry,
//! mapped into a `RuntimeConfig`. See API.md §3.
//!
//! Retry budget: 10 attempts, 1→2→4→8→16→30→30... (capped at 30s).

use std::collections::HashMap;
use std::path::PathBuf;

use crate::config::{Permission, RuntimeConfig, SkillSummary};
use crate::error::{Error, Result};

/// Max retry attempts on 5xx / network errors before giving up.
pub const MAX_BOOTSTRAP_ATTEMPTS: u32 = 10;

/// Upper bound on the exponential backoff between retries.
pub const MAX_BACKOFF_SECS: u64 = 30;

/// Wire types matching the backend's camelCase JSON. These are internal to
/// this module — the rest of the crate consumes `RuntimeConfig`.
#[derive(Debug, Clone, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
struct AgentBootstrapPayload {
    agent_id: uuid::Uuid,
    display_name: String,
    system_prompt: Option<String>,
    #[allow(dead_code)]
    provider: AgentProviderBootstrap,
    #[allow(dead_code)]
    proxy: AgentProxyBootstrap,
    gateway: AgentGatewayBootstrap,
    skills: Vec<AgentInstalledSkillSummary>,
    tool_permissions: AgentToolPermissionsBootstrap,
}

#[derive(Debug, Clone, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
struct AgentProviderBootstrap {
    #[allow(dead_code)]
    name: String,
    #[allow(dead_code)]
    model: String,
    #[allow(dead_code)]
    api_url: String,
    #[allow(dead_code)]
    token_ref: Option<String>,
}

#[derive(Debug, Clone, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
struct AgentProxyBootstrap {
    #[allow(dead_code)]
    url: String,
    #[allow(dead_code)]
    token: Option<String>,
}

#[derive(Debug, Clone, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
struct AgentGatewayBootstrap {
    host: String,
    port: i32,
    #[allow(dead_code)]
    tls_cert_ref: Option<String>,
}

#[derive(Debug, Clone, serde::Deserialize)]
struct AgentInstalledSkillSummary {
    name: String,
}

#[derive(Debug, Clone, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
struct AgentToolPermissionsBootstrap {
    entries: Vec<AgentBootstrapToolPermission>,
}

#[derive(Debug, Clone, serde::Deserialize)]
struct AgentBootstrapToolPermission {
    skill: String,
    tool: String,
    mode: String,
}

/// Compute the sleep duration for a retry. In test builds (detected via
/// `ZEROCLAW_TEST_FAST_BACKOFF` env var), we use milliseconds instead of
/// seconds so integration tests complete quickly.
fn backoff_duration(secs: u64) -> std::time::Duration {
    if std::env::var("ZEROCLAW_TEST_FAST_BACKOFF").is_ok() {
        std::time::Duration::from_millis(secs)
    } else {
        std::time::Duration::from_secs(secs)
    }
}

/// Fetch the bootstrap payload from the backend, retrying on transient
/// failure, and assemble a `RuntimeConfig`.
pub async fn bootstrap(agent_id: String, backend_url: String) -> Result<RuntimeConfig> {
    let url = format!("{backend_url}/api/agents/{agent_id}");
    let client = reqwest::Client::new();

    let mut last_err: Option<Error> = None;

    for attempt in 0..MAX_BOOTSTRAP_ATTEMPTS {
        if attempt > 0 {
            let delay_secs = std::cmp::min(1u64 << (attempt - 1), MAX_BACKOFF_SECS);
            tokio::time::sleep(backoff_duration(delay_secs)).await;
        }

        let resp = match client
            .get(&url)
            .header("Authorization", format!("Bearer {agent_id}"))
            .header("Accept", "application/json")
            .send()
            .await
        {
            Ok(r) => r,
            Err(e) => {
                tracing::warn!("bootstrap attempt {}: network error: {e}", attempt + 1);
                last_err = Some(Error::BootstrapHttp(e));
                continue;
            }
        };

        let status = resp.status();

        if status == reqwest::StatusCode::UNAUTHORIZED || status == reqwest::StatusCode::FORBIDDEN {
            return Err(Error::BootstrapUnauthorized);
        }
        if status == reqwest::StatusCode::NOT_FOUND {
            return Err(Error::BootstrapNotFound);
        }
        if status.is_server_error() {
            tracing::warn!("bootstrap attempt {}: server returned {status}", attempt + 1);
            last_err = Some(Error::BootstrapPayload(format!("server returned {status}")));
            continue;
        }

        let payload: AgentBootstrapPayload = resp
            .json()
            .await
            .map_err(|e| Error::BootstrapPayload(format!("invalid JSON: {e}")))?;

        return build_config(payload, backend_url);
    }

    Err(last_err.unwrap_or_else(|| Error::BootstrapPayload("exhausted retries".into())))
}

fn build_config(payload: AgentBootstrapPayload, backend_url: String) -> Result<RuntimeConfig> {
    // Validate gateway port.
    if payload.gateway.port <= 0 || payload.gateway.port > 65535 {
        return Err(Error::BootstrapPayload(format!(
            "gateway.port must be 1..=65535, got {}",
            payload.gateway.port
        )));
    }

    // Validate system prompt non-empty.
    let system_prompt = payload.system_prompt.unwrap_or_default();
    if system_prompt.is_empty() {
        return Err(Error::BootstrapPayload(
            "systemPrompt is required and must not be empty".into(),
        ));
    }

    // Build tool permissions map with lowercased keys.
    let mut tool_permissions = HashMap::new();
    for entry in &payload.tool_permissions.entries {
        let key = (
            entry.skill.to_ascii_lowercase(),
            entry.tool.to_ascii_lowercase(),
        );
        let perm = match entry.mode.as_str() {
            "allow" => Permission::Allow,
            "deny" => Permission::Deny,
            other => {
                tracing::warn!(
                    "unknown permission mode '{}' for {}:{}, treating as Deny",
                    other, entry.skill, entry.tool
                );
                Permission::Deny
            }
        };
        tool_permissions.insert(key, perm);
    }

    let skills = payload
        .skills
        .into_iter()
        .map(|s| SkillSummary { name: s.name })
        .collect();

    Ok(RuntimeConfig {
        agent_id: payload.agent_id,
        backend_url,
        backend_token: payload.agent_id.to_string(),
        memory_dir: PathBuf::from("/zeroclaw-data/memory"),
        gateway_host: payload.gateway.host,
        gateway_port: payload.gateway.port as u16,
        system_prompt,
        display_name: payload.display_name,
        skills,
        tool_permissions,
    })
}
