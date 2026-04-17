//! Tool registry and catalog. See API.md §9–§10.

pub mod traits;
pub mod skill_exec;
pub mod memory_store;
pub mod memory_recall;
pub mod memory_forget;
pub mod ask_user;
pub mod shell;
pub mod file_read;
pub mod file_write;
pub mod file_edit;
pub mod http_request;
pub mod web_fetch;
pub mod content_search;
pub mod glob_search;

use std::sync::Arc;

use crate::config::RuntimeConfig;
pub use traits::{Tool, ToolResult, ToolSpec};

/// Holds all compiled-in tools and dispatches by name.
pub struct ToolRegistry {
    cfg: Arc<RuntimeConfig>,
    tools: Vec<Arc<dyn Tool>>,
}

impl ToolRegistry {
    /// Build the registry with the full 1.0 tool set.
    pub fn new(cfg: Arc<RuntimeConfig>) -> Self {
        let _ = &cfg;
        // Phase 3: construct all 13 tools and push into the vec.
        Self {
            cfg,
            tools: Vec::new(),
        }
    }

    /// Return specs for all registered tools (sent in the LLM `tools` array).
    pub fn specs(&self) -> Vec<ToolSpec> {
        self.tools.iter().map(|t| t.spec()).collect()
    }

    /// Look up a tool by name.
    pub fn get(&self, name: &str) -> Option<Arc<dyn Tool>> {
        self.tools.iter().find(|t| t.name() == name).cloned()
    }

    /// Dispatch a tool call by name. Returns an error if the tool is unknown.
    pub async fn dispatch(
        &self,
        _name: &str,
        _params: serde_json::Value,
    ) -> anyhow::Result<ToolResult> {
        let _ = (&self.cfg, &_params);
        todo!("Phase 3: look up tool by name, call execute, handle unknown tool")
    }
}
