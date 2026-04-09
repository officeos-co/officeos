use std::fmt::Write;
use std::time::Duration;

use anyhow::Result;
use schemars::JsonSchema;
use serde::{Deserialize, Serialize};

use std::sync::Arc;

use crate::memory::traits::Memory;
use crate::providers::traits::{ChatMessage, Provider};

// ---------------------------------------------------------------------------
// Config
// ---------------------------------------------------------------------------

fn default_enabled() -> bool {
    true
}
fn default_threshold_ratio() -> f64 {
    0.50
}
fn default_protect_first_n() -> usize {
    3
}
fn default_protect_last_n() -> usize {
    4
}
fn default_max_passes() -> u32 {
    3
}
fn default_summary_max_chars() -> usize {
    4_000
}
fn default_source_max_chars() -> usize {
    50_000
}
fn default_timeout_secs() -> u64 {
    60
}
fn default_identifier_policy() -> String {
    "strict".to_string()
}
fn default_tool_result_retrim_chars() -> usize {
    2_000
}

#[derive(Debug, Clone, Serialize, Deserialize, JsonSchema)]
pub struct ContextCompressionConfig {
    /// Enable automatic context compression. Default: `true`.
    #[serde(default = "default_enabled")]
    pub enabled: bool,
    /// Fraction of context window that triggers compression (0.0–1.0). Default: `0.50`.
    #[serde(default = "default_threshold_ratio")]
    pub threshold_ratio: f64,
    /// Number of messages to protect at the start (system prompt + initial context). Default: `3`.
    #[serde(default = "default_protect_first_n")]
    pub protect_first_n: usize,
    /// Number of messages to protect at the end (recent conversation). Default: `4`.
    #[serde(default = "default_protect_last_n")]
    pub protect_last_n: usize,
    /// Maximum compression passes before giving up. Default: `3`.
    #[serde(default = "default_max_passes")]
    pub max_passes: u32,
    /// Maximum characters retained in stored compaction summary. Default: `4000`.
    #[serde(default = "default_summary_max_chars")]
    pub summary_max_chars: usize,
    /// Safety cap for compaction source transcript passed to the summarizer. Default: `50000`.
    #[serde(default = "default_source_max_chars")]
    pub source_max_chars: usize,
    /// Timeout in seconds for the summarization LLM call. Default: `60`.
    #[serde(default = "default_timeout_secs")]
    pub timeout_secs: u64,
    /// Override model for summarization (cheaper/faster). Default: same as main model.
    #[serde(default)]
    pub summary_model: Option<String>,
    /// Identifier preservation policy: `"strict"` or `"off"`. Default: `"strict"`.
    #[serde(default = "default_identifier_policy")]
    pub identifier_policy: String,
    /// Maximum chars for old tool results during fast-trim pass. Default: `2000`.
    #[serde(default = "default_tool_result_retrim_chars")]
    pub tool_result_retrim_chars: usize,
    /// Tool names exempt from result trimming. Default: `[]`.
    #[serde(default)]
    pub tool_result_trim_exempt: Vec<String>,
}

impl Default for ContextCompressionConfig {
    fn default() -> Self {
        Self {
            enabled: default_enabled(),
            threshold_ratio: default_threshold_ratio(),
            protect_first_n: default_protect_first_n(),
            protect_last_n: default_protect_last_n(),
            max_passes: default_max_passes(),
            summary_max_chars: default_summary_max_chars(),
            source_max_chars: default_source_max_chars(),
            timeout_secs: default_timeout_secs(),
            summary_model: None,
            identifier_policy: default_identifier_policy(),
            tool_result_retrim_chars: default_tool_result_retrim_chars(),
            tool_result_trim_exempt: Vec::new(),
        }
    }
}

// ---------------------------------------------------------------------------
// Result
// ---------------------------------------------------------------------------

#[derive(Debug, Clone)]
pub struct CompressionResult {
    pub compressed: bool,
    pub tokens_before: usize,
    pub tokens_after: usize,
    pub passes_used: u32,
}

// ---------------------------------------------------------------------------
// Probe tiers for unknown model context windows
// ---------------------------------------------------------------------------

const PROBE_TIERS: &[usize] = &[
    2_000_000, 1_000_000, 512_000, 200_000, 128_000, 64_000, 32_000,
];

fn next_probe_tier(current: usize) -> usize {
    PROBE_TIERS
        .iter()
        .copied()
        .find(|&tier| tier < current)
        .unwrap_or(32_000)
}

// ---------------------------------------------------------------------------
// Error message parsing
// ---------------------------------------------------------------------------

/// Try to extract the actual context window limit from a provider error message.
pub fn parse_context_limit_from_error(msg: &str) -> Option<usize> {
    // Match patterns like "maximum context length is 128000" or "limit of 200000 tokens"
    // or "context window of 131072" or "available context size (8448 tokens)"
    let re_patterns: &[&str] = &[
        // "maximum context length is 128000"
        r"(?:max(?:imum)?|limit)\s*(?:context\s*)?(?:length|size|window)?\s*(?:is|of|:)?\s*(\d{4,})",
        // "context length is 128000" / "context window of 131072"
        r"context\s*(?:length|size|window)\s*(?:is|of|:)?\s*(\d{4,})",
        // "128000 token context" / "128000 limit"
        r"(\d{4,})\s*(?:tokens?\s*)?(?:context|limit)",
        // "available context size (8448 tokens)"
        r"available context size\s*\(\s*(\d{4,})",
        // "> 128000 maximum context length" (Anthropic-style)
        r">\s*(\d{4,})\s*(?:maximum|max)?\s*(?:context)?\s*(?:length|size|window|tokens?)",
    ];
    let lower = msg.to_lowercase();
    for pattern in re_patterns {
        if let Ok(re) = regex::Regex::new(pattern) {
            if let Some(caps) = re.captures(&lower) {
                if let Some(m) = caps.get(1) {
                    if let Ok(limit) = m.as_str().parse::<usize>() {
                        if (1024..=10_000_000).contains(&limit) {
                            return Some(limit);
                        }
                    }
                }
            }
        }
    }
    None
}

// ---------------------------------------------------------------------------
// Token estimation
// ---------------------------------------------------------------------------

/// Estimate token count for a message history using ~4 chars/token heuristic
/// with a 1.2x safety margin.
pub fn estimate_tokens(messages: &[ChatMessage]) -> usize {
    let raw: usize = messages
        .iter()
        .map(|m| m.content.len().div_ceil(4) + 4)
        .sum();
    // 1.2x safety margin to account for underestimation
    #[allow(clippy::cast_possible_truncation, clippy::cast_sign_loss)]
    {
        (raw as f64 * 1.2) as usize
    }
}

// ---------------------------------------------------------------------------
// Summarizer prompt
// ---------------------------------------------------------------------------

const SUMMARIZER_SYSTEM: &str = "\
You are a conversation compaction engine. Summarize the conversation segment below into concise context.

PRESERVE exactly:
- All identifiers (UUIDs, hashes, file paths, URLs, tokens, IPs)
- Actions taken (tool calls, file operations, commands run)
- Key information obtained (data, results, error messages)
- Decisions made and user preferences expressed
- Current task status and unresolved items
- Constraints and requirements mentioned

OMIT:
- Verbose tool output (keep only key results)
- Repeated greetings or filler
- Redundant information already stated

Output concise bullet points. Be thorough but brief.";

// ---------------------------------------------------------------------------
// ContextCompressor
// ---------------------------------------------------------------------------

pub struct ContextCompressor {
    config: ContextCompressionConfig,
    context_window: usize,
    memory: Option<Arc<dyn Memory>>,
}

impl ContextCompressor {
    pub fn new(config: ContextCompressionConfig, context_window: usize) -> Self {
        Self {
            config,
            context_window,
            memory: None,
        }
    }

    /// Attach a memory handle so compression summaries are persisted before
    /// old messages are discarded. Without this, compressed facts are lost.
    pub fn with_memory(mut self, memory: Arc<dyn Memory>) -> Self {
        self.memory = Some(memory);
        self
    }

    /// Update the context window size (e.g. after error-driven probing).
    pub fn set_context_window(&mut self, window: usize) {
        self.context_window = window;
    }

    /// Fast-path: trim oversized tool results in non-protected messages.
    /// Returns total characters saved. No LLM call needed.
    fn fast_trim_tool_results(&self, history: &mut [ChatMessage]) -> usize {
        let max = self.config.tool_result_retrim_chars;
        if max == 0 {
            return 0;
        }
        let mut saved = 0;
        let protect_start = self.config.protect_first_n.min(history.len());
        let protect_end = history.len().saturating_sub(self.config.protect_last_n);

        if protect_start >= protect_end {
            return 0;
        }

        for msg in &mut history[protect_start..protect_end] {
            if msg.role != "tool" {
                continue;
            }
            if msg.content.len() <= max {
                continue;
            }
            // Skip exempt tools
            if self
                .config
                .tool_result_trim_exempt
                .iter()
                .any(|t| msg.content.contains(t.as_str()))
            {
                continue;
            }
            // Skip base64 images
            if msg.content.contains("data:image/") {
                continue;
            }
            let original_len = msg.content.len();
            msg.content = crate::agent::loop_::truncate_tool_result(&msg.content, max);
            saved += original_len - msg.content.len();
        }
        saved
    }

    /// Main entry point. Compresses history in-place if over threshold.
    pub async fn compress_if_needed(
        &self,
        history: &mut Vec<ChatMessage>,
        provider: &dyn Provider,
        model: &str,
    ) -> Result<CompressionResult> {
        if !self.config.enabled {
            let tokens = estimate_tokens(history);
            return Ok(CompressionResult {
                compressed: false,
                tokens_before: tokens,
                tokens_after: tokens,
                passes_used: 0,
            });
        }

        let tokens_before = estimate_tokens(history);
        #[allow(clippy::cast_possible_truncation, clippy::cast_sign_loss)]
        let threshold = (self.context_window as f64 * self.config.threshold_ratio) as usize;

        if tokens_before <= threshold {
            return Ok(CompressionResult {
                compressed: false,
                tokens_before,
                tokens_after: tokens_before,
                passes_used: 0,
            });
        }

        // Fast-trim pass — may resolve overflow without an LLM call
        let chars_saved = self.fast_trim_tool_results(history);
        if chars_saved > 0 {
            tracing::info!(chars_saved, "Fast-trim saved chars from old tool results");
            let recheck = estimate_tokens(history);
            if recheck <= threshold {
                return Ok(CompressionResult {
                    compressed: true,
                    tokens_before,
                    tokens_after: recheck,
                    passes_used: 0,
                });
            }
        }

        let mut passes_used = 0;
        for _ in 0..self.config.max_passes {
            let did_compress = self.compress_once(history, provider, model).await?;
            if did_compress {
                passes_used += 1;
            }
            if estimate_tokens(history) <= threshold || !did_compress {
                break;
            }
        }

        let tokens_after = estimate_tokens(history);
        Ok(CompressionResult {
            compressed: passes_used > 0,
            tokens_before,
            tokens_after,
            passes_used,
        })
    }

    /// Reactive compression triggered by a context_length_exceeded error.
    /// Parses the actual limit from the error, steps down probe tiers, and re-compresses.
    pub async fn compress_on_error(
        &mut self,
        history: &mut Vec<ChatMessage>,
        provider: &dyn Provider,
        model: &str,
        error_msg: &str,
    ) -> Result<bool> {
        // Try to extract actual limit from error message
        if let Some(limit) = parse_context_limit_from_error(error_msg) {
            self.context_window = limit;
        } else {
            // Step down to next probe tier
            self.context_window = next_probe_tier(self.context_window);
        }

        tracing::info!(
            context_window = self.context_window,
            "Context limit adjusted, re-compressing"
        );

        let result = self.compress_if_needed(history, provider, model).await?;
        Ok(result.compressed)
    }

    /// Single compression pass: protect head/tail, summarize middle.
    async fn compress_once(
        &self,
        history: &mut Vec<ChatMessage>,
        provider: &dyn Provider,
        model: &str,
    ) -> Result<bool> {
        let n = history.len();
        let protected_total = self.config.protect_first_n + self.config.protect_last_n;
        if n <= protected_total {
            return Ok(false);
        }

        let mut start = self.config.protect_first_n.min(n);
        let mut end = n.saturating_sub(self.config.protect_last_n);

        // Align boundaries to avoid orphaning tool_call/tool_result pairs
        start = align_boundary_forward(history, start);
        end = align_boundary_backward(history, end);

        if start >= end {
            return Ok(false);
        }

        // Build transcript from the middle section
        let middle = &history[start..end];
        let transcript = build_transcript(middle, self.config.source_max_chars);

        if transcript.is_empty() {
            return Ok(false);
        }

        let message_count = end - start;
        let summary_model = self.config.summary_model.as_deref().unwrap_or(model);

        let identifier_note = if self.config.identifier_policy == "strict" {
            "\nIMPORTANT: Preserve all identifiers exactly as they appear."
        } else {
            ""
        };

        let user_prompt = format!(
            "Summarize the following conversation history ({message_count} messages) for context preservation. \
             Keep it concise (max 20 bullet points).{identifier_note}\n\n{transcript}"
        );

        // LLM summarization with safety timeout
        let timeout = Duration::from_secs(self.config.timeout_secs);
        let summary_raw = match tokio::time::timeout(
            timeout,
            provider.chat_with_system(Some(SUMMARIZER_SYSTEM), &user_prompt, summary_model, 0.1),
        )
        .await
        {
            Ok(Ok(s)) => s,
            Ok(Err(e)) => {
                tracing::warn!(error = %e, "Summarization LLM call failed, using transcript truncation");
                truncate_chars(&transcript, self.config.summary_max_chars)
            }
            Err(_) => {
                tracing::warn!(
                    "Summarization timed out after {}s, using transcript truncation",
                    self.config.timeout_secs
                );
                truncate_chars(&transcript, self.config.summary_max_chars)
            }
        };

        let summary = truncate_chars(&summary_raw, self.config.summary_max_chars);

        // Persist the compression summary to memory before discarding old messages.
        // This ensures facts from compressed turns remain retrievable via memory recall.
        if let Some(ref memory) = self.memory {
            let facts_key = format!("compressed_context_{}", uuid::Uuid::new_v4());
            if let Err(e) = memory
                .store(
                    &facts_key,
                    &summary,
                    crate::memory::traits::MemoryCategory::Daily,
                    None,
                )
                .await
            {
                tracing::debug!("Failed to save compression summary to memory: {e}");
            } else {
                tracing::debug!(
                    "Saved compression summary to memory before discarding {message_count} messages"
                );
            }
        }

        // Splice: head + [SUMMARY] + tail
        let summary_msg = ChatMessage::assistant(format!(
            "[CONTEXT SUMMARY \u{2014} {message_count} earlier messages compressed]\n\n{summary}"
        ));
        history.splice(start..end, std::iter::once(summary_msg));

        // Repair orphaned tool pairs
        repair_tool_pairs(history);

        Ok(true)
    }
}

// ---------------------------------------------------------------------------
// Boundary alignment
// ---------------------------------------------------------------------------

/// Move boundary forward past any orphaned tool results at the start.
fn align_boundary_forward(messages: &[ChatMessage], idx: usize) -> usize {
    let mut i = idx;
    while i < messages.len() && messages[i].role == "tool" {
        i += 1;
    }
    i
}

/// Move boundary backward past any tool_call-bearing assistant messages at the end
/// so their results stay in the protected tail.
fn align_boundary_backward(messages: &[ChatMessage], idx: usize) -> usize {
    let mut i = idx;
    // If the message just before the boundary is an assistant message that likely
    // contains tool calls (heuristic: followed by a tool result), pull the boundary back.
    while i > 0 && i < messages.len() && messages[i].role == "tool" {
        // The tool result at `i` belongs to a tool_call before it — move boundary past it
        i -= 1;
    }
    i
}

// ---------------------------------------------------------------------------
// Tool pair repair
// ---------------------------------------------------------------------------

/// Remove orphaned tool_results and add stubs for orphaned tool_calls.
///
/// After compression, some tool results may reference tool_calls that were
/// summarized away, and vice versa. This function cleans up the history
/// so every tool_result has a matching assistant message and every
/// tool_call-bearing assistant message has results.
fn repair_tool_pairs(messages: &mut Vec<ChatMessage>) {
    // Heuristic: tool messages whose content references a call ID that no longer
    // exists in any assistant message should be removed. Since ChatMessage is a
    // simple role+content struct (no structured tool_call_id field), we use a
    // simpler approach: remove any "tool" message that immediately follows the
    // [CONTEXT SUMMARY] message (it's orphaned by definition).
    let mut i = 0;
    while i < messages.len() {
        if messages[i].content.contains("[CONTEXT SUMMARY") {
            // Remove any immediately following orphaned tool results
            while i + 1 < messages.len() && messages[i + 1].role == "tool" {
                messages.remove(i + 1);
            }
        }
        i += 1;
    }

    // Also check for tool results at the very start (after system prompt) that
    // are orphaned because their assistant message was compressed.
    let start = if messages.first().map_or(false, |m| m.role == "system") {
        1
    } else {
        0
    };
    while start < messages.len() && messages[start].role == "tool" {
        messages.remove(start);
    }
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

fn build_transcript(messages: &[ChatMessage], max_chars: usize) -> String {
    let mut transcript = String::new();
    for msg in messages {
        let role = msg.role.to_uppercase();
        let _ = writeln!(transcript, "{role}: {}", msg.content.trim());
    }

    if transcript.len() > max_chars {
        truncate_chars(&transcript, max_chars)
    } else {
        transcript
    }
}

fn truncate_chars(s: &str, max: usize) -> String {
    if s.len() <= max {
        return s.to_string();
    }
    // Find a safe char boundary
    let mut end = max;
    while end > 0 && !s.is_char_boundary(end) {
        end -= 1;
    }
    let mut result = s[..end].to_string();
    result.push_str("...");
    result
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

#[cfg(test)]
#[path = "context_compressor.test.rs"]
mod tests;
