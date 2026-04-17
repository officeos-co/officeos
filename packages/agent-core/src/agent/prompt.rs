//! System prompt composition via trait-based sections. See API.md §5.3.
//! Port of `packages/zeroclaw-core/src/agent/prompt.rs`, minus
//! ChannelMediaSection, autonomy levels, i18n, security policy.

use std::path::Path;
use std::sync::Arc;

use chrono::{Datelike, Local, Timelike};

use crate::config::SkillSummary;
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

// ── Section impls ────────────────────────────────────────────────

/// Current local date + time + ISO 8601 + timezone.
pub struct DateTimeSection;

impl PromptSection for DateTimeSection {
    fn name(&self) -> &'static str {
        "datetime"
    }
    fn build(&self, _ctx: &PromptContext<'_>) -> crate::error::Result<String> {
        let now = Local::now();
        let (year, month, day) = (now.year(), now.month(), now.day());
        let (hour, minute, second) = (now.hour(), now.minute(), now.second());
        let tz = now.format("%Z");
        Ok(format!(
            "## CRITICAL CONTEXT: CURRENT DATE & TIME\n\n\
             The following is the ABSOLUTE TRUTH regarding the current date and time. \
             Use this for all relative time calculations (e.g. \"last 7 days\").\n\n\
             Date: {year:04}-{month:02}-{day:02}\n\
             Time: {hour:02}:{minute:02}:{second:02} ({tz})\n\
             ISO 8601: {year:04}-{month:02}-{day:02}T{hour:02}:{minute:02}:{second:02}{}",
            now.format("%:z")
        ))
    }
}

const MAX_PERSONALITY_CHARS: usize = 20_000;

/// Concatenates SOUL.md, IDENTITY.md, BOOTSTRAP.md from memory_dir.
/// Truncates each at 20k chars. Missing files silently skipped (lenient).
pub struct IdentitySection;

impl PromptSection for IdentitySection {
    fn name(&self) -> &'static str {
        "identity"
    }
    fn build(&self, ctx: &PromptContext<'_>) -> crate::error::Result<String> {
        let mut prompt = String::from(
            "## Project Context\n\n\
             The following workspace files define your identity, behavior, and context.\n\n",
        );

        for name in &["SOUL.md", "IDENTITY.md", "BOOTSTRAP.md"] {
            let path = ctx.memory_dir.join(name);
            if let Ok(content) = std::fs::read_to_string(&path) {
                if content.len() > MAX_PERSONALITY_CHARS {
                    let truncated: String = content.chars().take(MAX_PERSONALITY_CHARS).collect();
                    prompt.push_str(&format!("### {name}\n\n{truncated}\n[... truncated]\n\n"));
                } else {
                    prompt.push_str(&format!("### {name}\n\n{content}\n\n"));
                }
            }
        }

        Ok(prompt)
    }
}

/// Static text warning the model never to fabricate tool results.
pub struct ToolHonestySection;

impl PromptSection for ToolHonestySection {
    fn name(&self) -> &'static str {
        "tool_honesty"
    }
    fn build(&self, _ctx: &PromptContext<'_>) -> crate::error::Result<String> {
        Ok(
            "## CRITICAL: Tool Honesty\n\n\
             - NEVER fabricate, invent, or guess tool results. If a tool returns empty results, say \"No results found.\"\n\
             - If a tool call fails, report the error — never make up data to fill the gap.\n\
             - When unsure whether a tool call succeeded, ask the user rather than guessing."
                .into(),
        )
    }
}

/// Always yields empty in 1.0 — tools are delivered via the `tools` array.
pub struct ToolsSection;

impl PromptSection for ToolsSection {
    fn name(&self) -> &'static str {
        "tools"
    }
    fn build(&self, _ctx: &PromptContext<'_>) -> crate::error::Result<String> {
        Ok(String::new())
    }
}

/// Static safety rules — no autonomy levels in 1.0.
pub struct SafetySection;

impl PromptSection for SafetySection {
    fn name(&self) -> &'static str {
        "safety"
    }
    fn build(&self, _ctx: &PromptContext<'_>) -> crate::error::Result<String> {
        Ok("## Safety\n\n\
             - Do not exfiltrate private data.\n\
             - Do not run destructive commands without asking.\n\
             - Do not bypass oversight or approval mechanisms.\n\
             - Prefer `trash` over `rm`."
            .into())
    }
}

/// Lists installed skills so the model knows what `skill_exec --help` surfaces.
pub struct SkillsSection;

impl PromptSection for SkillsSection {
    fn name(&self) -> &'static str {
        "skills"
    }
    fn build(&self, ctx: &PromptContext<'_>) -> crate::error::Result<String> {
        if ctx.skills.is_empty() {
            return Ok(String::new());
        }
        let mut out = String::from(
            "## Installed Skills\n\n\
             Use `skill_exec --help` to discover available skills and actions.\n\n",
        );
        for skill in ctx.skills {
            out.push_str(&format!("- {}\n", skill.name));
        }
        Ok(out)
    }
}

/// `Working directory: {memory_dir}`.
pub struct WorkspaceSection;

impl PromptSection for WorkspaceSection {
    fn name(&self) -> &'static str {
        "workspace"
    }
    fn build(&self, ctx: &PromptContext<'_>) -> crate::error::Result<String> {
        Ok(format!(
            "## Workspace\n\nWorking directory: `{}`",
            ctx.memory_dir.display()
        ))
    }
}

/// Hostname + OS (no model name — backend owns the model choice).
pub struct RuntimeSection;

impl PromptSection for RuntimeSection {
    fn name(&self) -> &'static str {
        "runtime"
    }
    fn build(&self, _ctx: &PromptContext<'_>) -> crate::error::Result<String> {
        let host =
            hostname::get().map_or_else(|_| "unknown".into(), |h| h.to_string_lossy().to_string());
        Ok(format!(
            "## Runtime\n\nHost: {host} | OS: {}",
            std::env::consts::OS
        ))
    }
}

// ── Builder ──────────────────────────────────────────────────────

/// Compose the full system prompt from all default sections.
/// Sections that produce empty output are dropped. Joined by `\n\n`.
pub fn compose_system_prompt(ctx: &PromptContext<'_>) -> String {
    let sections: Vec<Box<dyn PromptSection>> = vec![
        Box::new(DateTimeSection),
        Box::new(IdentitySection),
        Box::new(ToolHonestySection),
        Box::new(ToolsSection),
        Box::new(SafetySection),
        Box::new(SkillsSection),
        Box::new(WorkspaceSection),
        Box::new(RuntimeSection),
    ];

    let mut output = String::new();
    for section in &sections {
        match section.build(ctx) {
            Ok(part) => {
                if part.trim().is_empty() {
                    continue;
                }
                output.push_str(part.trim_end());
                output.push_str("\n\n");
            }
            Err(e) => {
                tracing::warn!(section = section.name(), error = %e, "prompt section failed");
            }
        }
    }
    output
}
