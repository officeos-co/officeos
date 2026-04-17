//! System prompt composition via trait-based sections. See API.md §5.3.

use std::path::Path;
use std::sync::Arc;

use crate::config::{RuntimeConfig, SkillSummary};
use crate::tools::Tool;

/// Context available to each prompt section at build time.
pub struct PromptContext<'a> {
    pub memory_dir: &'a Path,
    pub tools: &'a [Arc<dyn Tool>],
    pub skills: &'a [SkillSummary],
    pub system_prompt: &'a str,
}

/// A composable section of the system prompt. Sections that return an empty
/// string are dropped from the final output.
pub trait PromptSection: Send + Sync {
    fn name(&self) -> &'static str;
    fn build(&self, ctx: &PromptContext<'_>) -> crate::error::Result<String>;
}

// --- Concrete section structs (one per API.md §5.3 default) ---

/// Current local date + time + ISO 8601 + timezone.
pub struct DateTimeSection;

impl PromptSection for DateTimeSection {
    fn name(&self) -> &'static str { "datetime" }
    fn build(&self, _ctx: &PromptContext<'_>) -> crate::error::Result<String> {
        todo!("Phase 3: render current date/time")
    }
}

/// Concatenates SOUL.md, IDENTITY.md, BOOTSTRAP.md from memory_dir.
pub struct IdentitySection;

impl PromptSection for IdentitySection {
    fn name(&self) -> &'static str { "identity" }
    fn build(&self, _ctx: &PromptContext<'_>) -> crate::error::Result<String> {
        todo!("Phase 3: load and concat personality files, truncate at 20k chars each")
    }
}

/// Static text warning the model never to fabricate tool results.
pub struct ToolHonestySection;

impl PromptSection for ToolHonestySection {
    fn name(&self) -> &'static str { "tool_honesty" }
    fn build(&self, _ctx: &PromptContext<'_>) -> crate::error::Result<String> {
        todo!("Phase 3: return static honesty text")
    }
}

/// Always yields empty in 1.0 — tools are delivered via the `tools` array.
pub struct ToolsSection;

impl PromptSection for ToolsSection {
    fn name(&self) -> &'static str { "tools" }
    fn build(&self, _ctx: &PromptContext<'_>) -> crate::error::Result<String> {
        Ok(String::new())
    }
}

/// Static safety rules.
pub struct SafetySection;

impl PromptSection for SafetySection {
    fn name(&self) -> &'static str { "safety" }
    fn build(&self, _ctx: &PromptContext<'_>) -> crate::error::Result<String> {
        todo!("Phase 3: return static safety rules")
    }
}

/// Lists installed skills so the model knows what `skill_exec --help` surfaces.
pub struct SkillsSection;

impl PromptSection for SkillsSection {
    fn name(&self) -> &'static str { "skills" }
    fn build(&self, _ctx: &PromptContext<'_>) -> crate::error::Result<String> {
        todo!("Phase 3: list skill names")
    }
}

/// `Working directory: {memory_dir}`.
pub struct WorkspaceSection;

impl PromptSection for WorkspaceSection {
    fn name(&self) -> &'static str { "workspace" }
    fn build(&self, _ctx: &PromptContext<'_>) -> crate::error::Result<String> {
        todo!("Phase 3: render working directory")
    }
}

/// Hostname + OS.
pub struct RuntimeSection;

impl PromptSection for RuntimeSection {
    fn name(&self) -> &'static str { "runtime" }
    fn build(&self, _ctx: &PromptContext<'_>) -> crate::error::Result<String> {
        todo!("Phase 3: render hostname + OS")
    }
}

/// Compose the full system prompt from all default sections.
/// Sections that produce empty output are dropped. Joined by `\n\n`.
pub fn compose_system_prompt(memory_dir: &Path, cfg: &RuntimeConfig) -> String {
    let _ = (memory_dir, cfg);
    todo!("Phase 3: build PromptContext, iterate sections, join non-empty results")
}
