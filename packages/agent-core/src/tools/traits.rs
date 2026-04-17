//! The `Tool` trait — the only extension point in zeroclaw-agent.
//! See API.md §9.

use async_trait::async_trait;
use serde::{Deserialize, Serialize};

/// Result of a tool invocation, returned over the WS as `tool_call_result`.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ToolResult {
    pub success: bool,
    pub output: String,
    pub error: Option<String>,
}

/// Wire-format tool spec sent in the LLM `tools` array.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ToolSpec {
    pub name: String,
    pub description: String,
    pub parameters: serde_json::Value,
}

/// Every compiled-in tool implements this trait. Implementations live in
/// sibling files (`shell.rs`, `file_read.rs`, etc.). The `execute` method
/// returns `anyhow::Result<ToolResult>` for trait-compat; surface errors
/// come back as `ToolResult { success: false, error: Some(...) }`.
#[async_trait]
pub trait Tool: Send + Sync {
    fn name(&self) -> &str;
    fn description(&self) -> &str;
    fn parameters_schema(&self) -> serde_json::Value;
    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult>;

    fn spec(&self) -> ToolSpec {
        ToolSpec {
            name: self.name().to_string(),
            description: self.description().to_string(),
            parameters: self.parameters_schema(),
        }
    }
}
