// Rust guideline compliant 2026-02-21

//! Fully-hydrated runtime configuration.
//!
//! Built once at boot from `Env` + `AgentBootstrapPayload`. Never
//! mutated afterwards. See API.md §4.

use std::collections::HashMap;
use std::fmt;
use std::path::PathBuf;

/// Allow or deny policy for tool dispatch.
///
/// Only consulted by `skill_exec` in 1.0 — all other tools are
/// unconditionally allowed.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum Permission {
    /// The tool may execute.
    Allow,
    /// The tool is blocked by policy.
    Deny,
}

impl fmt::Display for Permission {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            Self::Allow => f.write_str("allow"),
            Self::Deny => f.write_str("deny"),
        }
    }
}

/// Summary of one installed skill from the bootstrap payload.
///
/// Surfaces only what the prompt builder / `skill_exec` needs.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct SkillSummary {
    /// Skill identifier as registered in the backend.
    pub name: String,
}

/// Everything the agent needs to run, already validated.
///
/// Keys into `tool_permissions` are stored lowercased (both skill and
/// tool component) so downstream lookups are literal.
#[derive(Debug, Clone)]
pub struct RuntimeConfig {
    /// Agent UUID from the backend.
    pub agent_id: uuid::Uuid,
    /// Base URL of the backend (no trailing slash).
    pub backend_url: String,
    /// Bearer token for backend requests (currently == `agent_id`).
    pub backend_token: String,
    /// Filesystem root for personality files and agent memory.
    pub memory_dir: PathBuf,
    /// Bind address for the WebSocket gateway.
    pub gateway_host: String,
    /// Bind port for the WebSocket gateway.
    pub gateway_port: u16,
    /// Operator-supplied system prompt from the bootstrap payload.
    pub system_prompt: String,
    /// Human-readable agent name.
    pub display_name: String,
    /// Installed skills reported by the backend.
    pub skills: Vec<SkillSummary>,
    /// Per-(skill, tool) allow/deny overrides.
    pub tool_permissions: HashMap<(String, String), Permission>,
}
