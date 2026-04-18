// Rust guideline compliant 2026-02-21

//! Tool registry and catalog.
//!
//! See API.md §9–§10.

pub mod content_search;
pub mod file_edit;
pub mod file_read;
pub mod file_write;
pub mod glob_search;
pub mod http_request;
pub mod memory_forget;
pub mod memory_recall;
pub mod memory_store;
pub mod shell;
pub mod skill_exec;
pub mod traits;
pub mod web_fetch;

use std::sync::Arc;

use crate::config::RuntimeConfig;
use crate::llm::{ChatToolSchema, ChatToolSchemaFunction};
pub use traits::{Tool, ToolResult, ToolSpec};

/// Holds all compiled-in tools and dispatches by name.
#[derive(Debug)]
pub struct ToolRegistry {
    #[expect(dead_code, reason = "retained for future use in tool-level config")]
    cfg: Arc<RuntimeConfig>,
    tools: Vec<Arc<dyn Tool>>,
}

impl ToolRegistry {
    /// Build the registry with the full 1.0 tool set.
    #[must_use]
    pub fn new(cfg: Arc<RuntimeConfig>) -> Self {
        let workspace = cfg.memory_dir.clone();

        let tools: Vec<Arc<dyn Tool>> = vec![
            Arc::new(shell::ShellTool::new(workspace.clone())),
            Arc::new(file_read::FileReadTool::new(workspace.clone())),
            Arc::new(file_write::FileWriteTool::new(workspace.clone())),
            Arc::new(file_edit::FileEditTool::new(workspace.clone())),
            Arc::new(http_request::HttpRequestTool::new()),
            Arc::new(web_fetch::WebFetchTool::new()),
            Arc::new(content_search::ContentSearchTool::new(workspace.clone())),
            Arc::new(glob_search::GlobSearchTool::new(workspace.clone())),
            Arc::new(memory_store::MemoryStoreTool::new(workspace.clone())),
            Arc::new(memory_recall::MemoryRecallTool::new(workspace.clone())),
            Arc::new(memory_forget::MemoryForgetTool::new(workspace)),
            Arc::new(skill_exec::SkillExecTool::new(Arc::clone(&cfg))),
        ];

        Self { cfg, tools }
    }

    /// Return specs for all registered tools.
    #[must_use]
    pub fn specs(&self) -> Vec<ToolSpec> {
        self.tools.iter().map(|t| t.spec()).collect()
    }

    /// Return tool schemas in the `OpenAI` `tools` wire format.
    #[must_use]
    pub fn chat_tool_schemas(&self) -> Vec<ChatToolSchema> {
        self.tools
            .iter()
            .map(|t| ChatToolSchema {
                kind: "function".to_string(),
                function: ChatToolSchemaFunction {
                    name: t.name().to_string(),
                    description: t.description().to_string(),
                    parameters: t.parameters_schema(),
                },
            })
            .collect()
    }

    /// Return `Arc` refs to all tools (for prompt context).
    #[must_use]
    pub fn tools(&self) -> Vec<Arc<dyn Tool>> {
        self.tools.clone()
    }

    /// Look up a tool by name.
    #[must_use]
    pub fn get(&self, name: &str) -> Option<Arc<dyn Tool>> {
        self.tools.iter().find(|t| t.name() == name).cloned()
    }

    /// Dispatch a tool call by name.
    ///
    /// # Errors
    ///
    /// Returns an error if the tool's `execute` method fails.
    pub async fn dispatch(
        &self,
        name: &str,
        params: serde_json::Value,
    ) -> anyhow::Result<ToolResult> {
        match self.get(name) {
            Some(tool) => tool.execute(params).await,
            None => Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some(format!("unknown tool: {name}")),
            }),
        }
    }
}
