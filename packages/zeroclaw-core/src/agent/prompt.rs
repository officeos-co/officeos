//! Composable system-prompt assembly via [`PromptSection`] objects.
//!
//! System prompts in zeroclaw-core are not a single template string. They are
//! built by composing `Box<dyn PromptSection>` instances, where each section
//! owns a slice of the final prompt and renders itself from a shared
//! [`PromptContext`]. This makes prompts extensible (tests and downstream
//! crates can `add_section(Box::new(MySection))`) and keeps section logic
//! local to its own type.
//!
//! [`SystemPromptBuilder::with_defaults`] (line 43) returns a builder
//! pre-loaded with the 9 default sections that ship with the runtime, in
//! order: `DateTimeSection`, `IdentitySection`, `ToolHonestySection`,
//! `ToolsSection`, `SafetySection`, `SkillsSection`, `WorkspaceSection`,
//! `RuntimeSection`, `ChannelMediaSection`. Sections that produce empty
//! output are silently dropped from the final prompt.
//!
//! The system prompt is rebuilt **every turn** by `Agent::turn`, so sections
//! must be cheap and resilient. In particular, [`IdentitySection`] uses the
//! **lenient** `personality::load_personality()` loader — the **strict** gate
//! runs only once at boot in `Agent::from_config`. This split ensures a
//! missing personality file fails fast at startup but never crashes a
//! mid-conversation turn.
//!
//! ## Key types
//! - [`SystemPromptBuilder`] — composes sections into a final prompt string.
//! - [`PromptSection`] — trait each section implements.
//! - [`PromptContext`] — per-turn inputs (workspace, model, tools, skills,
//!   dispatcher instructions, security summary, autonomy level).
//!
//! ## Related
//! - `src/agent/agent.rs` — calls `build()` once per turn.
//! - `src/agent/personality.rs` — strict vs lenient personality loaders.
//! - `docs/architecture/overview.md` — the big picture.

use crate::agent::personality;
use crate::i18n::ToolDescriptions;
use crate::security::AutonomyLevel;
use crate::skills::Skill;
use crate::tools::Tool;
use anyhow::Result;
use chrono::{Datelike, Local, Timelike};
use std::fmt::Write;
use std::path::Path;

pub struct PromptContext<'a> {
    pub workspace_dir: &'a Path,
    pub model_name: &'a str,
    pub tools: &'a [Box<dyn Tool>],
    pub skills: &'a [Skill],
    pub skills_prompt_mode: crate::config::SkillsPromptInjectionMode,
    pub dispatcher_instructions: &'a str,
    /// Locale-aware tool descriptions. When present, tool descriptions in
    /// prompts are resolved from the locale file instead of hardcoded values.
    pub tool_descriptions: Option<&'a ToolDescriptions>,
    /// Pre-rendered security policy summary for inclusion in the Safety
    /// prompt section.  When present, the LLM sees the concrete constraints
    /// (allowed commands, forbidden paths, autonomy level) so it can plan
    /// tool calls without trial-and-error.  See issue #2404.
    pub security_summary: Option<String>,
    /// Autonomy level from config. Controls whether the safety section
    /// includes "ask before acting" instructions. Full autonomy omits them
    /// so the model executes tools directly without simulating approval.
    pub autonomy_level: AutonomyLevel,
}

pub trait PromptSection: Send + Sync {
    fn name(&self) -> &str;
    fn build(&self, ctx: &PromptContext<'_>) -> Result<String>;
}

#[derive(Default)]
pub struct SystemPromptBuilder {
    sections: Vec<Box<dyn PromptSection>>,
}

impl SystemPromptBuilder {
    pub fn with_defaults() -> Self {
        Self {
            sections: vec![
                Box::new(DateTimeSection),
                Box::new(IdentitySection),
                Box::new(ToolHonestySection),
                Box::new(ToolsSection),
                Box::new(SafetySection),
                Box::new(SkillsSection),
                Box::new(WorkspaceSection),
                Box::new(RuntimeSection),
                Box::new(ChannelMediaSection),
            ],
        }
    }

    pub fn add_section(mut self, section: Box<dyn PromptSection>) -> Self {
        self.sections.push(section);
        self
    }

    pub fn build(&self, ctx: &PromptContext<'_>) -> Result<String> {
        let mut output = String::new();
        for section in &self.sections {
            let part = section.build(ctx)?;
            if part.trim().is_empty() {
                continue;
            }
            output.push_str(part.trim_end());
            output.push_str("\n\n");
        }
        Ok(output)
    }
}

pub struct IdentitySection;
pub struct ToolHonestySection;
pub struct ToolsSection;
pub struct SafetySection;
pub struct SkillsSection;
pub struct WorkspaceSection;
pub struct RuntimeSection;
pub struct DateTimeSection;
pub struct ChannelMediaSection;

impl PromptSection for IdentitySection {
    fn name(&self) -> &str {
        "identity"
    }

    fn build(&self, ctx: &PromptContext<'_>) -> Result<String> {
        // Identity is sourced entirely from the per-agent Obsidian vault,
        // which the dashboard backend materializes into the workspace
        // directory via a mounted ConfigMap before the agent container
        // starts. See Phase 3 in STRIP_DOWN.md. The boot path in agent.rs
        // uses `personality::load_personality_strict` to fail loudly if any
        // required file is missing; here we use the lenient loader because
        // prompt building is a read-only rendering step and must stay
        // resilient to partial test workspaces.
        let mut prompt = String::from("## Project Context\n\n");
        prompt.push_str(
            "The following workspace files define your identity, behavior, and context.\n\n",
        );

        let profile = personality::load_personality(ctx.workspace_dir);
        prompt.push_str(&profile.render());

        Ok(prompt)
    }
}

impl PromptSection for ToolHonestySection {
    fn name(&self) -> &str {
        "tool_honesty"
    }

    fn build(&self, _ctx: &PromptContext<'_>) -> Result<String> {
        Ok(
            "## CRITICAL: Tool Honesty\n\n\
             - NEVER fabricate, invent, or guess tool results. If a tool returns empty results, say \"No results found.\"\n\
             - If a tool call fails, report the error — never make up data to fill the gap.\n\
             - When unsure whether a tool call succeeded, ask the user rather than guessing."
                .into(),
        )
    }
}

impl PromptSection for ToolsSection {
    fn name(&self) -> &str {
        "tools"
    }

    fn build(&self, ctx: &PromptContext<'_>) -> Result<String> {
        let mut out = String::from("## Tools\n\n");
        for tool in ctx.tools {
            let desc = ctx
                .tool_descriptions
                .and_then(|td: &ToolDescriptions| td.get(tool.name()))
                .unwrap_or_else(|| tool.description());
            let _ = writeln!(
                out,
                "- **{}**: {}\n  Parameters: `{}`",
                tool.name(),
                desc,
                tool.parameters_schema()
            );
        }
        if !ctx.dispatcher_instructions.is_empty() {
            out.push('\n');
            out.push_str(ctx.dispatcher_instructions);
        }
        Ok(out)
    }
}

impl PromptSection for SafetySection {
    fn name(&self) -> &str {
        "safety"
    }

    fn build(&self, ctx: &PromptContext<'_>) -> Result<String> {
        let mut out = String::from("## Safety\n\n- Do not exfiltrate private data.\n");

        // Omit "ask before acting" instructions when autonomy is Full —
        // mirrors build_system_prompt_with_mode_and_autonomy. See #3952.
        if ctx.autonomy_level != AutonomyLevel::Full {
            out.push_str(
                "- Do not run destructive commands without asking.\n\
                 - Do not bypass oversight or approval mechanisms.\n",
            );
        }

        out.push_str("- Prefer `trash` over `rm`.\n");
        out.push_str(match ctx.autonomy_level {
            AutonomyLevel::Full => {
                "- Execute tools and actions directly — no extra approval needed.\n\
                 - You have full access to all configured tools. Use them confidently to accomplish tasks.\n\
                 - Only refuse an action if the runtime explicitly rejects it — do not preemptively decline."
            }
            AutonomyLevel::ReadOnly => {
                "- This runtime is read-only. Write operations will be rejected by the runtime if attempted.\n\
                 - Use read-only tools freely and confidently."
            }
            AutonomyLevel::Supervised => {
                "- Ask for approval when the runtime policy requires it for the specific action.\n\
                 - Do not preemptively refuse actions — attempt them and let the runtime enforce restrictions.\n\
                 - Use available tools confidently; the security policy will enforce boundaries."
            }
        });

        // Append concrete security policy constraints when available (#2404).
        // This tells the LLM exactly what commands are allowed, which paths
        // are off-limits, etc. — preventing wasteful trial-and-error.
        if let Some(ref summary) = ctx.security_summary {
            out.push_str("\n\n### Active Security Policy\n\n");
            out.push_str(summary);
        }

        Ok(out)
    }
}

impl PromptSection for SkillsSection {
    fn name(&self) -> &str {
        "skills"
    }

    fn build(&self, ctx: &PromptContext<'_>) -> Result<String> {
        Ok(crate::skills::skills_to_prompt_with_mode(
            ctx.skills,
            ctx.workspace_dir,
            ctx.skills_prompt_mode,
        ))
    }
}

impl PromptSection for WorkspaceSection {
    fn name(&self) -> &str {
        "workspace"
    }

    fn build(&self, ctx: &PromptContext<'_>) -> Result<String> {
        Ok(format!(
            "## Workspace\n\nWorking directory: `{}`",
            ctx.workspace_dir.display()
        ))
    }
}

impl PromptSection for RuntimeSection {
    fn name(&self) -> &str {
        "runtime"
    }

    fn build(&self, ctx: &PromptContext<'_>) -> Result<String> {
        let host =
            hostname::get().map_or_else(|_| "unknown".into(), |h| h.to_string_lossy().to_string());
        Ok(format!(
            "## Runtime\n\nHost: {host} | OS: {} | Model: {}",
            std::env::consts::OS,
            ctx.model_name
        ))
    }
}

impl PromptSection for DateTimeSection {
    fn name(&self) -> &str {
        "datetime"
    }

    fn build(&self, _ctx: &PromptContext<'_>) -> Result<String> {
        let now = Local::now();
        // Force Gregorian year to avoid confusion with local calendars (e.g. Buddhist calendar).
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

impl PromptSection for ChannelMediaSection {
    fn name(&self) -> &str {
        "channel_media"
    }

    fn build(&self, _ctx: &PromptContext<'_>) -> Result<String> {
        Ok("## Channel Media Markers\n\n\
            Messages from channels may contain media markers:\n\
            - `[Voice] <text>` — The user sent a voice/audio message that has already been transcribed to text. Respond to the transcribed content directly.\n\
            - `[IMAGE:<path>]` — An image attachment, processed by the vision pipeline.\n\
            - `[Document: <name>] <path>` — A file attachment saved to the workspace."
            .into())
    }
}


#[cfg(test)]
#[path = "prompt.test.rs"]
mod tests;
