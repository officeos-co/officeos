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
use crate::llm::{ChatToolSchema, ChatToolSchemaFunction};
pub use traits::{Tool, ToolResult, ToolSpec};

/// Holds all compiled-in tools and dispatches by name.
pub struct ToolRegistry {
    #[allow(dead_code)]
    cfg: Arc<RuntimeConfig>,
    tools: Vec<Arc<dyn Tool>>,
}

impl ToolRegistry {
    /// Build the registry with the full 1.0 tool set.
    pub fn new(cfg: Arc<RuntimeConfig>, ask_bridge: Arc<ask_user::AskUserBridge>) -> Self {
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
            Arc::new(ask_user::AskUserTool::new(ask_bridge)),
            Arc::new(skill_exec::SkillExecTool::new(cfg.clone())),
        ];

        Self { cfg, tools }
    }

    /// Return specs for all registered tools (sent in the LLM `tools` array).
    pub fn specs(&self) -> Vec<ToolSpec> {
        self.tools.iter().map(|t| t.spec()).collect()
    }

    /// Return tool schemas in the OpenAI chat completions `tools` wire format.
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

    /// Return Arc refs to all tools (for prompt context).
    pub fn tools(&self) -> Vec<Arc<dyn Tool>> {
        self.tools.clone()
    }

    /// Look up a tool by name.
    pub fn get(&self, name: &str) -> Option<Arc<dyn Tool>> {
        self.tools.iter().find(|t| t.name() == name).cloned()
    }

    /// Dispatch a tool call by name. Returns an error if the tool is unknown.
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
