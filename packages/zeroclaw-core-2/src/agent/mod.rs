//! Agent — owns history, registry, llm client, config. See API.md §6.

pub mod turn_loop;
pub mod prompt;
pub mod history;

use std::sync::Arc;

use crate::config::RuntimeConfig;

/// The per-connection agent that drives the turn loop.
pub struct Agent {
    cfg: Arc<RuntimeConfig>,
}

impl Agent {
    /// Construct an agent bound to the given runtime configuration.
    pub fn new(cfg: Arc<RuntimeConfig>) -> Self {
        Self { cfg }
    }

    /// Handle an inbound user message — enters the turn loop.
    pub async fn handle_user_message(
        &self,
        text: String,
    ) -> crate::error::Result<()> {
        let _ = (&self.cfg, text, turn_loop::run_turn);
        todo!("Phase 3: push user message, run turn loop, emit turn_complete")
    }
}
