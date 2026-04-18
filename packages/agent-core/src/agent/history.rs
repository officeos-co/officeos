// Rust guideline compliant 2026-02-21

//! Conversation history for the turn loop.
//!
//! See API.md §6.2.

use crate::llm::ChatMessage;

/// Estimated chars-per-token ratio used by the token estimator.
///
/// The value 4.0 is the standard approximation for English text with
/// the `OpenAI` tokenizer family. The 1.2 multiplier adds headroom for
/// role/framing overhead per message.
const CHARS_PER_TOKEN: f64 = 4.0;

/// Safety multiplier applied on top of the chars-per-token estimate.
const TOKEN_OVERHEAD_MULTIPLIER: f64 = 1.2;

/// Per-message framing overhead in characters (role, delimiters).
const MESSAGE_FRAMING_CHARS: usize = 4;

/// Per-connection conversation history.
#[derive(Debug)]
pub struct ConversationHistory {
    messages: Vec<ChatMessage>,
}

impl Default for ConversationHistory {
    fn default() -> Self {
        Self::new()
    }
}

impl ConversationHistory {
    /// Create an empty history.
    #[must_use]
    pub fn new() -> Self {
        Self {
            messages: Vec::new(),
        }
    }

    /// Append a message to history.
    pub fn push(&mut self, message: ChatMessage) {
        self.messages.push(message);
    }

    /// Return a reference to all messages.
    #[must_use]
    pub fn messages(&self) -> &[ChatMessage] {
        &self.messages
    }

    /// Return a mutable reference to all messages.
    pub fn messages_mut(&mut self) -> &mut Vec<ChatMessage> {
        &mut self.messages
    }

    /// Prune history when estimated tokens exceed `max_tokens`.
    ///
    /// Phase 1: collapse old assistant+tool pairs into summaries.
    /// Phase 2: drop unprotected messages until under budget.
    /// The most recent `keep_recent` messages are always protected.
    pub fn prune(&mut self, max_tokens: usize, keep_recent: usize) {
        if self.messages.is_empty() {
            return;
        }

        // Phase 1: collapse old assistant+tool pairs into summaries.
        let mut i = 0;
        while i + 1 < self.messages.len() {
            let protected = protected_indices(&self.messages, keep_recent);
            if self.messages[i].role == "assistant"
                && self.messages[i + 1].role == "tool"
                && !protected[i]
                && !protected[i + 1]
            {
                /// Max chars of tool output to keep in the collapsed summary.
                const SUMMARY_TRUNCATE_CHARS: usize = 100;

                let tool_content = self.messages[i + 1].content.as_deref().unwrap_or("");
                let truncated: String = tool_content.chars().take(SUMMARY_TRUNCATE_CHARS).collect();
                let summary = format!("[Tool result: {truncated}...]");
                self.messages[i] = ChatMessage {
                    role: "assistant".to_string(),
                    content: Some(summary),
                    tool_calls: vec![],
                    tool_call_id: None,
                };
                self.messages.remove(i + 1);
            } else {
                i += 1;
            }
        }

        // Phase 2: drop unprotected messages until under budget.
        while estimate_tokens(&self.messages) > max_tokens {
            let protected = protected_indices(&self.messages, keep_recent);
            if let Some(idx) = protected
                .iter()
                .enumerate()
                .find(|&(_, &p)| !p)
                .map(|(i, _)| i)
            {
                self.messages.remove(idx);
            } else {
                break;
            }
        }
    }

    /// Number of messages in history.
    #[must_use]
    pub fn len(&self) -> usize {
        self.messages.len()
    }

    /// Whether history is empty.
    #[must_use]
    pub fn is_empty(&self) -> bool {
        self.messages.is_empty()
    }

    /// Clear all history.
    pub fn clear(&mut self) {
        self.messages.clear();
    }
}

/// Cheap token estimator: `total_chars / 4 * 1.2`.
///
/// See API.md §6.2.
#[expect(
    clippy::cast_possible_truncation,
    clippy::cast_sign_loss,
    clippy::cast_precision_loss,
    reason = "token estimation is inherently approximate; precision loss is acceptable"
)]
fn estimate_tokens(messages: &[ChatMessage]) -> usize {
    let total_chars: usize = messages
        .iter()
        .map(|m| {
            let content_len = m.content.as_deref().map_or(0, str::len);
            let tc_len: usize = m
                .tool_calls
                .iter()
                .map(|tc| tc.function.name.len() + tc.function.arguments.len())
                .sum();
            content_len + tc_len + MESSAGE_FRAMING_CHARS
        })
        .sum();
    (total_chars as f64 / CHARS_PER_TOKEN * TOKEN_OVERHEAD_MULTIPLIER) as usize
}

/// Identify which messages are protected from pruning.
fn protected_indices(messages: &[ChatMessage], keep_recent: usize) -> Vec<bool> {
    let len = messages.len();
    let mut protected = vec![false; len];
    for (i, msg) in messages.iter().enumerate() {
        if msg.role == "system" {
            protected[i] = true;
        }
    }
    let recent_start = len.saturating_sub(keep_recent);
    for p in protected.iter_mut().skip(recent_start) {
        *p = true;
    }
    protected
}
