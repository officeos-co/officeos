//! Fully-hydrated runtime configuration. Built once at boot from
//! `Env` + `AgentBootstrapPayload`. Never mutated afterwards.
//!
//! See API.md §4.

use std::collections::HashMap;
use std::path::PathBuf;

/// Allow/Deny policy entries for tool dispatch. Only consulted by
/// `skill_exec` in 1.0 — all other tools are unconditionally allowed.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum Permission {
    Allow,
    Deny,
}

/// Summary of one installed skill, as reported by the backend bootstrap
/// payload. Surfaces only what the prompt builder / `skill_exec` needs.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct SkillSummary {
    pub name: String,
}

/// Everything the agent needs to run, already validated.
///
/// Keys into `tool_permissions` are stored lowercased (both skill and tool
/// component) so downstream lookups are literal.
#[derive(Debug, Clone)]
pub struct RuntimeConfig {
    pub agent_id: uuid::Uuid,
    pub backend_url: String,
    pub backend_token: String,
    pub memory_dir: PathBuf,
    pub gateway_host: String,
    pub gateway_port: u16,
    pub system_prompt: String,
    pub display_name: String,
    pub skills: Vec<SkillSummary>,
    pub tool_permissions: HashMap<(String, String), Permission>,
}
