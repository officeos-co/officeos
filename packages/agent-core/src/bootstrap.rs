// Rust guideline compliant 2026-02-21
//! Bootstrap flow — fetch agent config from the backend.
//!
//! `GET {backend_url}/api/agents/{id}` with exponential-backoff retry,
//! mapped into a [`RuntimeConfig`]. See API.md §3.

use std::collections::HashMap;
use std::path::PathBuf;

use crate::config::{Permission, RuntimeConfig, SkillSummary};
use crate::error::{Error, Result};

/// Maximum retry attempts on 5xx / network errors before giving up.
///
/// Chosen to balance availability against stalling indefinitely during
/// infrastructure outages. At the max backoff of 30 s this gives ~4 min
/// of total wait time before the pod crashes and restarts.
pub const MAX_BOOTSTRAP_ATTEMPTS: u32 = 10;

/// Upper bound (seconds) on exponential backoff between retries.
///
/// Caps the doubling sequence at 30 s to avoid excessive delays while
/// still giving the backend time to recover.
pub const MAX_BACKOFF_SECS: u64 = 30;

/// Wire types matching the backend's camelCase JSON. These are internal to
/// this module — the rest of the crate consumes `RuntimeConfig`.
#[derive(Debug, Clone, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
struct AgentBootstrapPayload {
    agent_id: uuid::Uuid,
    display_name: String,
    system_prompt: Option<String>,
    #[expect(
        dead_code,
        reason = "field required for deserialization but not used in 1.0"
    )]
    provider: AgentProviderBootstrap,
    #[expect(
        dead_code,
        reason = "field required for deserialization but not used in 1.0"
    )]
    proxy: AgentProxyBootstrap,
    gateway: AgentGatewayBootstrap,
    skills: Vec<AgentInstalledSkillSummary>,
    tool_permissions: AgentToolPermissionsBootstrap,
}

#[derive(Debug, Clone, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
struct AgentProviderBootstrap {
    #[expect(
        dead_code,
        reason = "field required for deserialization but not used in 1.0"
    )]
    name: String,
    #[expect(
        dead_code,
        reason = "field required for deserialization but not used in 1.0"
    )]
    model: String,
    #[expect(
        dead_code,
        reason = "field required for deserialization but not used in 1.0"
    )]
    api_url: String,
    #[expect(
        dead_code,
        reason = "field required for deserialization but not used in 1.0"
    )]
    token_ref: Option<String>,
}

#[derive(Debug, Clone, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
struct AgentProxyBootstrap {
    #[expect(
        dead_code,
        reason = "field required for deserialization but not used in 1.0"
    )]
    url: String,
    #[expect(
        dead_code,
        reason = "field required for deserialization but not used in 1.0"
    )]
    token: Option<String>,
}

#[derive(Debug, Clone, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
struct AgentGatewayBootstrap {
    host: String,
    port: i32,
    #[expect(
        dead_code,
        reason = "field required for deserialization but not used in 1.0"
    )]
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
/// # Errors
///
/// Returns an error if the bootstrap request fails after all retries,
/// or if the payload is invalid.
pub async fn bootstrap(agent_id: String, backend_url: String) -> Result<RuntimeConfig> {
    let url = format!("{backend_url}/api/agents/{agent_id}");
    tracing::info!(name: "bootstrap.request.start", url_full = %url, "bootstrap request to {{url_full}}");
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
                tracing::warn!(name: "bootstrap.attempt.network_error", bootstrap_attempt = attempt + 1, error_message = %e, "bootstrap attempt {{bootstrap_attempt}}: network error: {{error_message}}");
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
            tracing::warn!(name: "bootstrap.attempt.server_error", bootstrap_attempt = attempt + 1, http_response_status_code = %status, "bootstrap attempt {{bootstrap_attempt}}: server returned {{http_response_status_code}}");
            last_err = Some(Error::BootstrapPayload(format!("server returned {status}")));
            continue;
        }

        let payload: AgentBootstrapPayload = resp
            .json()
            .await
            .map_err(|e| Error::BootstrapPayload(format!("invalid JSON: {e}")))?;

        tracing::info!(name: "bootstrap.payload.received", "bootstrap payload received, building config");
        tracing::info!(name: "bootstrap.payload.details", skill_count = payload.skills.len(), tool_permission_count = payload.tool_permissions.entries.len(), "payload details: {{skill_count}} skills, {{tool_permission_count}} permissions");
        return build_config(payload, backend_url);
    }

    Err(last_err.unwrap_or_else(|| Error::BootstrapPayload("exhausted retries".into())))
}

#[expect(
    clippy::cast_possible_truncation,
    clippy::cast_sign_loss,
    reason = "port is validated to be 1..=65535 before this cast"
)]
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
                    name: "bootstrap.permission.unknown_mode",
                    permission_mode = %other,
                    skill_name = %entry.skill,
                    tool_name = %entry.tool,
                    "unknown permission mode {{permission_mode}} for {{skill_name}}:{{tool_name}}, treating as Deny",
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
