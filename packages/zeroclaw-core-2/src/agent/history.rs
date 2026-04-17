//! Conversation history for the turn loop. See API.md §6.2.

use crate::llm::ChatMessage;

/// Per-connection conversation history.
pub struct ConversationHistory {
    messages: Vec<ChatMessage>,
}

impl Default for ConversationHistory {
    fn default() -> Self { Self::new() }
}

impl ConversationHistory {
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
    pub fn messages(&self) -> &[ChatMessage] {
        &self.messages
    }

    /// Return a mutable reference to all messages (for pruning).
    pub fn messages_mut(&mut self) -> &mut Vec<ChatMessage> {
        &mut self.messages
    }

    /// Prune history when estimated tokens exceed threshold.
    /// Port of legacy history_pruner.rs — see API.md §6.2.
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
                let tool_content = self.messages[i + 1]
                    .content
                    .as_deref()
                    .unwrap_or("");
                let truncated: String = tool_content.chars().take(100).collect();
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
    pub fn len(&self) -> usize {
        self.messages.len()
    }

    /// Whether history is empty.
    pub fn is_empty(&self) -> bool {
        self.messages.is_empty()
    }

    /// Clear all history.
    pub fn clear(&mut self) {
        self.messages.clear();
    }
}

/// Cheap token estimator: total_chars / 4 * 1.2. See API.md §6.2.
fn estimate_tokens(messages: &[ChatMessage]) -> usize {
    let total_chars: usize = messages
        .iter()
        .map(|m| {
            let content_len = m.content.as_deref().map_or(0, |c| c.len());
            let tc_len: usize = m
                .tool_calls
                .iter()
                .map(|tc| tc.function.name.len() + tc.function.arguments.len())
                .sum();
            // +4 per message for role/framing overhead
            content_len + tc_len + 4
        })
        .sum();
    (total_chars as f64 / 4.0 * 1.2) as usize
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
