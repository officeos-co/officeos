// Rust guideline compliant 2026-02-21

//! Agent — owns history, registry, LLM client, and config.
//!
//! See API.md §6.

pub mod history;
pub mod loop_detector;
pub mod prompt;
pub mod turn_loop;

use std::sync::Arc;

use tokio::sync::Mutex;

use crate::config::RuntimeConfig;
use crate::gateway::protocol::WsOutbound;
use crate::llm::LlmClient;
use crate::tools::ToolRegistry;

use history::ConversationHistory;

/// Per-connection agent that drives the turn loop.
///
/// Holds the LLM client, tool registry, conversation history, and
/// the immutable runtime config. One `Agent` instance is shared
/// across all WebSocket connections.
#[derive(Debug)]
pub struct Agent {
    cfg: Arc<RuntimeConfig>,
    llm: LlmClient,
    registry: ToolRegistry,
    history: Mutex<ConversationHistory>,
}

impl Agent {
    /// Construct an agent bound to the given runtime configuration.
    #[must_use]
    pub fn new(cfg: Arc<RuntimeConfig>) -> Self {
        let llm = LlmClient::new(Arc::clone(&cfg));
        let registry = ToolRegistry::new(Arc::clone(&cfg));
        Self {
            cfg,
            llm,
            registry,
            history: Mutex::new(ConversationHistory::new()),
        }
    }

    /// Handle an inbound user message — enters the turn loop.
    ///
    /// # Errors
    ///
    /// Propagates errors from the turn loop (LLM, tool dispatch).
    pub async fn handle_user_message(&self, text: String) -> crate::error::Result<()> {
        let (tx, _rx) = tokio::sync::mpsc::unbounded_channel();
        self.handle_user_message_with_ws(text, None, &tx).await
    }

    /// Handle a user message with a WS sender for streaming events.
    ///
    /// # Errors
    ///
    /// Propagates errors from the turn loop (LLM, tool dispatch).
    pub async fn handle_user_message_with_ws(
        &self,
        text: String,
        turn_id: Option<String>,
        ws_tx: &tokio::sync::mpsc::UnboundedSender<WsOutbound>,
    ) -> crate::error::Result<()> {
        let turn_id = turn_id.unwrap_or_else(|| uuid::Uuid::new_v4().to_string());

        let mut history = self.history.lock().await;

        // Push user message.
        history.push(crate::llm::ChatMessage {
            role: "user".to_string(),
            content: Some(text),
            tool_calls: vec![],
            tool_call_id: None,
        });

        // Run the turn loop.
        turn_loop::run_turn(
            &self.cfg,
            &mut history,
            &self.llm,
            &self.registry,
            &turn_id,
            ws_tx,
        )
        .await
    }

    /// Whether the history lock is currently free.
    ///
    /// Approximate — used by the WS handler for concurrency guard.
    #[must_use]
    pub fn try_lock_history(&self) -> bool {
        self.history.try_lock().is_ok()
    }
}
