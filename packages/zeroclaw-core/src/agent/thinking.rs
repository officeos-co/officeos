//! Thinking/Reasoning Level Control
//!
//! Allows users to control how deeply the model reasons per message,
//! trading speed for depth. Levels range from `Off` (fastest, most concise)
//! to `Max` (deepest reasoning, slowest).
//!
//! Users can set the level via:
//! - Inline directive: `/think:high` at the start of a message
//! - Agent config: `[agent.thinking]` section with `default_level`
//!
//! Resolution hierarchy (highest priority first):
//! 1. Inline directive (`/think:<level>`)
//! 2. Session override (reserved for future use)
//! 3. Agent config (`agent.thinking.default_level`)
//! 4. Global default (`Medium`)

use schemars::JsonSchema;
use serde::{Deserialize, Serialize};

/// How deeply the model should reason for a given message.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize, Default, JsonSchema)]
#[serde(rename_all = "lowercase")]
pub enum ThinkingLevel {
    /// No chain-of-thought. Fastest, most concise responses.
    Off,
    /// Minimal reasoning. Brief, direct answers.
    Minimal,
    /// Light reasoning. Short explanations when needed.
    Low,
    /// Balanced reasoning (default). Moderate depth.
    #[default]
    Medium,
    /// Deep reasoning. Thorough analysis and step-by-step thinking.
    High,
    /// Maximum reasoning depth. Exhaustive analysis.
    Max,
}

impl ThinkingLevel {
    /// Parse a thinking level from a string (case-insensitive).
    pub fn from_str_insensitive(s: &str) -> Option<Self> {
        match s.to_lowercase().as_str() {
            "off" | "none" => Some(Self::Off),
            "minimal" | "min" => Some(Self::Minimal),
            "low" => Some(Self::Low),
            "medium" | "med" | "default" => Some(Self::Medium),
            "high" => Some(Self::High),
            "max" | "maximum" => Some(Self::Max),
            _ => None,
        }
    }
}

/// Configuration for thinking/reasoning level control.
#[derive(Debug, Clone, Serialize, Deserialize, JsonSchema)]
pub struct ThinkingConfig {
    /// Default thinking level when no directive is present.
    #[serde(default)]
    pub default_level: ThinkingLevel,
}

impl Default for ThinkingConfig {
    fn default() -> Self {
        Self {
            default_level: ThinkingLevel::Medium,
        }
    }
}

/// Parameters derived from a thinking level, applied to the LLM request.
#[derive(Debug, Clone, PartialEq)]
pub struct ThinkingParams {
    /// Temperature adjustment (added to the base temperature, clamped to 0.0..=2.0).
    pub temperature_adjustment: f64,
    /// Maximum tokens adjustment (added to any existing max_tokens setting).
    pub max_tokens_adjustment: i64,
    /// Optional system prompt prefix injected before the existing system prompt.
    pub system_prompt_prefix: Option<String>,
}

/// Parse a `/think:<level>` directive from the start of a message.
///
/// Returns `Some((level, remaining_message))` if a directive is found,
/// or `None` if no directive is present. The remaining message has
/// leading whitespace after the directive trimmed.
pub fn parse_thinking_directive(message: &str) -> Option<(ThinkingLevel, String)> {
    let trimmed = message.trim_start();
    if !trimmed.starts_with("/think:") {
        return None;
    }

    // Extract the level token (everything between `/think:` and the next whitespace or end).
    let after_prefix = &trimmed["/think:".len()..];
    let level_end = after_prefix
        .find(|c: char| c.is_whitespace())
        .unwrap_or(after_prefix.len());
    let level_str = &after_prefix[..level_end];

    let level = ThinkingLevel::from_str_insensitive(level_str)?;

    let remaining = after_prefix[level_end..].trim_start().to_string();
    Some((level, remaining))
}

/// Convert a `ThinkingLevel` into concrete parameters for the LLM request.
pub fn apply_thinking_level(level: ThinkingLevel) -> ThinkingParams {
    match level {
        ThinkingLevel::Off => ThinkingParams {
            temperature_adjustment: -0.2,
            max_tokens_adjustment: -1000,
            system_prompt_prefix: Some(
                "Be extremely concise. Give direct answers without explanation \
                 unless explicitly asked. No preamble."
                    .into(),
            ),
        },
        ThinkingLevel::Minimal => ThinkingParams {
            temperature_adjustment: -0.1,
            max_tokens_adjustment: -500,
            system_prompt_prefix: Some(
                "Be concise and fast. Keep explanations brief. \
                 Prioritize speed over thoroughness."
                    .into(),
            ),
        },
        ThinkingLevel::Low => ThinkingParams {
            temperature_adjustment: -0.05,
            max_tokens_adjustment: 0,
            system_prompt_prefix: Some("Keep reasoning light. Explain only when helpful.".into()),
        },
        ThinkingLevel::Medium => ThinkingParams {
            temperature_adjustment: 0.0,
            max_tokens_adjustment: 0,
            system_prompt_prefix: None,
        },
        ThinkingLevel::High => ThinkingParams {
            temperature_adjustment: 0.05,
            max_tokens_adjustment: 1000,
            system_prompt_prefix: Some(
                "Think step by step. Provide thorough analysis and \
                 consider edge cases before answering."
                    .into(),
            ),
        },
        ThinkingLevel::Max => ThinkingParams {
            temperature_adjustment: 0.1,
            max_tokens_adjustment: 2000,
            system_prompt_prefix: Some(
                "Think very carefully and exhaustively. Break down the problem \
                 into sub-problems, consider all angles, verify your reasoning, \
                 and provide the most thorough analysis possible."
                    .into(),
            ),
        },
    }
}

/// Resolve the effective thinking level using the priority hierarchy:
/// 1. Inline directive (if present)
/// 2. Session override (reserved, currently always `None`)
/// 3. Agent config default
/// 4. Global default (`Medium`)
pub fn resolve_thinking_level(
    inline_directive: Option<ThinkingLevel>,
    session_override: Option<ThinkingLevel>,
    config: &ThinkingConfig,
) -> ThinkingLevel {
    inline_directive
        .or(session_override)
        .unwrap_or(config.default_level)
}

/// Clamp a temperature value to the valid range `[0.0, 2.0]`.
pub fn clamp_temperature(temp: f64) -> f64 {
    temp.clamp(0.0, 2.0)
}


#[cfg(test)]
#[path = "thinking.test.rs"]
mod tests;
