// Rust guideline compliant 2026-02-21

//! The `Tool` trait — the only extension point in zeroclaw-agent.
//!
//! See API.md §9.

use async_trait::async_trait;
use serde::{Deserialize, Serialize};

/// Result of a tool invocation, returned over the WS as `tool_call_result`.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ToolResult {
    /// Whether the tool call succeeded.
    pub success: bool,
    /// Tool output text (may be empty on failure).
    pub output: String,
    /// Error message, if the tool failed.
    pub error: Option<String>,
}

/// Wire-format tool spec sent in the LLM `tools` array.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ToolSpec {
    /// Tool name as the LLM sees it.
    pub name: String,
    /// Human-readable description for the LLM.
    pub description: String,
    /// JSON Schema for the tool's parameters.
    pub parameters: serde_json::Value,
}

/// Every compiled-in tool implements this trait.
///
/// Implementations live in sibling files (`shell.rs`, `file_read.rs`,
/// etc.). The `execute` method returns `anyhow::Result<ToolResult>`
/// for trait-compat; surface errors come back as
/// `ToolResult { success: false, error: Some(...) }`.
#[async_trait]
pub trait Tool: Send + Sync + std::fmt::Debug {
    /// Tool name as registered in the LLM tools array.
    fn name(&self) -> &str;
    /// Human-readable description for the LLM.
    fn description(&self) -> &str;
    /// JSON Schema for the tool's parameters.
    fn parameters_schema(&self) -> serde_json::Value;
    /// Execute the tool with the given arguments.
    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult>;

    /// Build a wire-format spec from this tool's metadata.
    fn spec(&self) -> ToolSpec {
        ToolSpec {
            name: self.name().to_string(),
            description: self.description().to_string(),
            parameters: self.parameters_schema(),
        }
    }
}
