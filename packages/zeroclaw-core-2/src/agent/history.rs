//! Conversation history for the turn loop. See API.md §6.2.

use crate::llm::ChatMessage;

/// Per-connection conversation history.
pub struct ConversationHistory {
    _messages: Vec<ChatMessage>,
}

impl ConversationHistory {
    pub fn new() -> Self {
        Self {
            _messages: Vec::new(),
        }
    }

    /// Append a message to history.
    pub fn push(&mut self, _message: ChatMessage) {
        todo!("Phase 3: push message")
    }

    /// Return a reference to all messages.
    pub fn messages(&self) -> &[ChatMessage] {
        todo!("Phase 3: return messages slice")
    }

    /// Prune history when estimated tokens exceed threshold.
    pub fn prune(&mut self, max_tokens: usize, keep_recent: usize) {
        let _ = (max_tokens, keep_recent);
        todo!("Phase 3: token estimation + pruning algorithm")
    }

    /// Number of messages in history.
    pub fn len(&self) -> usize {
        todo!("Phase 3: return messages.len()")
    }

    /// Whether history is empty.
    pub fn is_empty(&self) -> bool {
        todo!("Phase 3: return messages.is_empty()")
    }

    /// Clear all history.
    pub fn clear(&mut self) {
        todo!("Phase 3: clear messages")
    }
}
