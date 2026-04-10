//! Live capability sync — pulls the active skill tool list from the
//! EAOS backend instead of loading `SKILL.md` files from disk.
//!
//! Flow:
//!   1. On the first turn (or after the refresh TTL expires) the agent
//!      calls `CapabilityCache::refresh()`.
//!   2. `refresh()` GETs `{backend_url}/api/agents/me/capabilities` with
//!      the configured bearer token.
//!   3. Each returned tool is materialized as a `BackendSkillTool` that
//!      POSTs to the concrete backend route when the LLM invokes it.
//!   4. If the resolved list differs from the previous refresh (hashed
//!      over name + route), `refresh()` returns `Some(new_tools)` so
//!      the agent can swap its tool set and rebuild the system prompt.
//!      Otherwise it returns `None` and the cached tool set is reused.
//!
//! This module intentionally does NOT touch `Skill` / `SkillTool`
//! structs — it produces a ready-to-install `Vec<Box<dyn Tool>>` +
//! `Vec<ToolSpec>` directly. Keeping the two code paths (disk vs
//! backend) separate avoids forcing every SKILL.md consumer to care
//! about backend wire formats.

use std::collections::hash_map::DefaultHasher;
use std::hash::{Hash, Hasher};
use std::time::{Duration, Instant};

use serde::Deserialize;

use crate::tools::backend_skill_tool::BackendSkillTool;
use crate::tools::traits::{Tool, ToolSpec};

const HTTP_TIMEOUT_SECS: u64 = 10;

#[derive(Debug, Clone, Deserialize)]
pub struct BackendCapability {
    pub skill: String,
    pub name: String,
    pub description: String,
    pub parameters: serde_json::Value,
    /// Path fragment like `/api/agents/me/skills/notion/search`. The
    /// agent joins this with `backend_url` to form the concrete POST
    /// target.
    pub route: String,
}

#[derive(Debug, Clone, Deserialize)]
pub struct BackendSkillDoc {
    pub name: String,
    pub doc: String,
}

#[derive(Debug, Deserialize)]
struct CapabilitiesResponse {
    tools: Vec<BackendCapability>,
    #[serde(default)]
    skills: Vec<BackendSkillDoc>,
}

/// Output of a successful refresh: a new set of tools ready to swap in.
pub struct RefreshedCapabilities {
    pub tools: Vec<Box<dyn Tool>>,
    pub specs: Vec<ToolSpec>,
    /// Per-skill markdown documentation for system prompt injection.
    pub skill_docs: Vec<(String, String)>,
}

/// Tracks the last successful backend fetch so consecutive turns inside
/// the refresh window reuse the same tool list without hitting the wire.
pub struct CapabilityCache {
    backend_url: String,
    backend_token: Option<String>,
    refresh_interval: Duration,
    last_refresh: Option<Instant>,
    last_hash: Option<u64>,
}

impl CapabilityCache {
    pub fn new(backend_url: String, backend_token: Option<String>, refresh_seconds: u64) -> Self {
        Self {
            backend_url: backend_url.trim_end_matches('/').to_string(),
            backend_token,
            refresh_interval: Duration::from_secs(refresh_seconds.max(1)),
            last_refresh: None,
            last_hash: None,
        }
    }

    /// Force the next call to `refresh()` to hit the backend regardless
    /// of the TTL window — used on the very first turn to guarantee a
    /// live pull before the system prompt is built.
    pub fn invalidate(&mut self) {
        self.last_refresh = None;
    }

    /// Refresh the cached capability list.
    ///
    /// Returns:
    ///   * `Ok(Some(RefreshedCapabilities))` — the set changed (or this
    ///     was the first successful fetch); the caller should swap
    ///     tools, specs, and rebuild the system prompt.
    ///   * `Ok(None)` — either the TTL has not yet expired or the
    ///     backend returned the same list as before; reuse the current
    ///     tool set.
    ///   * `Err(_)` — a network / parse error. The caller should log
    ///     and continue with the previously cached tool set (fail-open
    ///     to avoid breaking agents when the backend blips).
    pub async fn refresh(&mut self) -> anyhow::Result<Option<RefreshedCapabilities>> {
        if let Some(last) = self.last_refresh {
            if last.elapsed() < self.refresh_interval {
                return Ok(None);
            }
        }

        let client = reqwest::Client::builder()
            .timeout(Duration::from_secs(HTTP_TIMEOUT_SECS))
            .build()?;

        // Agents authenticate with X-Agent-Token / bearer, so hit the
        // agent-scoped capabilities endpoint (not the user-scoped /api/capabilities).
        let url = format!("{}/api/agents/me/capabilities", self.backend_url);
        let mut req = client.get(&url);
        if let Some(token) = &self.backend_token {
            req = req.bearer_auth(token);
        }

        let resp = req.send().await?;
        if !resp.status().is_success() {
            anyhow::bail!("backend /capabilities returned HTTP {}", resp.status());
        }

        let parsed: CapabilitiesResponse = resp.json().await?;
        self.last_refresh = Some(Instant::now());

        let new_hash = hash_capabilities(&parsed.tools);
        if self.last_hash == Some(new_hash) {
            return Ok(None);
        }
        self.last_hash = Some(new_hash);

        let mut tools: Vec<Box<dyn Tool>> = Vec::with_capacity(parsed.tools.len());
        let mut specs: Vec<ToolSpec> = Vec::with_capacity(parsed.tools.len());
        for cap in parsed.tools {
            let url = format!("{}{}", self.backend_url, cap.route);
            let tool = BackendSkillTool::new(
                cap.name.clone(),
                cap.description.clone(),
                cap.parameters.clone(),
                url,
                self.backend_token.clone(),
            );
            specs.push(ToolSpec {
                name: cap.name,
                description: cap.description,
                parameters: cap.parameters,
            });
            tools.push(Box::new(tool));
        }

        let skill_docs: Vec<(String, String)> = parsed
            .skills
            .into_iter()
            .map(|s| (s.name, s.doc))
            .collect();

        Ok(Some(RefreshedCapabilities {
            tools,
            specs,
            skill_docs,
        }))
    }
}

fn hash_capabilities(caps: &[BackendCapability]) -> u64 {
    let mut hasher = DefaultHasher::new();
    for c in caps {
        c.skill.hash(&mut hasher);
        c.name.hash(&mut hasher);
        c.route.hash(&mut hasher);
        // parameters are serialized to string for a stable hash — avoids
        // depending on serde_json::Value's internal ordering semantics.
        c.parameters.to_string().hash(&mut hasher);
    }
    hasher.finish()
}
