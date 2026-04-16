//! Narrow runtime-config bridge.
//!
//! [`AgentRuntimeConfig`] is a small, typed slice of [`crate::config::Config`]
//! containing only the fields the agent runtime actually needs at the
//! leaf-consumer level (memory backends, security policy, skill_exec, etc.).
//!
//! This exists to decouple leaf subsystems from the giant TOML [`Config`]
//! struct. New consumers should accept `&AgentRuntimeConfig` (or even narrower
//! slices) rather than `&Config`, and call sites should construct one via
//! [`AgentRuntimeConfig::from_config`] at the boundary.
//!
//! Named `AgentRuntimeConfig` to avoid colliding with the existing
//! `[runtime]` TOML section type [`crate::config::RuntimeConfig`].

use std::path::PathBuf;

/// Narrow runtime-config slice consumed by leaf subsystems.
#[derive(Debug, Clone)]
pub struct AgentRuntimeConfig {
    /// Agent UUID. In pod mode this is sourced from `ZEROCLAW_AGENT_ID`;
    /// empty string in local-dev/CLI mode where no agent identity exists.
    pub agent_id: String,
    /// Backend base URL (e.g. `https://api.officeos.co`). Empty when not set.
    pub backend_url: String,
    /// Bearer token for backend calls. Empty when not set.
    pub backend_token: String,
    /// Default model routed through the selected provider.
    pub model: String,
    /// Gateway bind host.
    pub gateway_host: String,
    /// Gateway bind port.
    pub gateway_port: u16,
    /// Root of the on-disk workspace / memory directory.
    pub memory_dir: PathBuf,
}

impl AgentRuntimeConfig {
    /// Project a [`crate::config::Config`] down to the narrow runtime slice.
    pub fn from_config(cfg: &crate::config::Config) -> Self {
        Self {
            // Config has no `agent_id` field; the agent UUID is injected via
            // the `ZEROCLAW_AGENT_ID` env var at boot. Read it here so leaf
            // consumers don't have to.
            agent_id: std::env::var("ZEROCLAW_AGENT_ID").unwrap_or_default(),
            backend_url: cfg.skills.backend_url.clone().unwrap_or_default(),
            backend_token: cfg.skills.backend_token.clone().unwrap_or_default(),
            model: cfg.default_model.clone().unwrap_or_default(),
            gateway_host: cfg.gateway.host.clone(),
            gateway_port: cfg.gateway.port,
            memory_dir: cfg.workspace_dir.clone(),
        }
    }
}

impl From<&crate::config::Config> for AgentRuntimeConfig {
    fn from(cfg: &crate::config::Config) -> Self {
        Self::from_config(cfg)
    }
}
