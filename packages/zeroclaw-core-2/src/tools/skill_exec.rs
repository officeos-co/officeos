//! `skill_exec` — execute a skill command via the backend GraphQL API.
//! See API.md §10.1.

use async_trait::async_trait;
use std::sync::Arc;

use crate::config::RuntimeConfig;
use super::traits::{Tool, ToolResult};

/// Executes skill commands by sending GraphQL queries to the backend.
pub struct SkillExecTool {
    cfg: Arc<RuntimeConfig>,
}

impl SkillExecTool {
    pub fn new(cfg: Arc<RuntimeConfig>) -> Self {
        Self { cfg }
    }
}

#[async_trait]
impl Tool for SkillExecTool {
    fn name(&self) -> &str {
        "skill_exec"
    }

    fn description(&self) -> &str {
        "Execute a skill command. Works like a CLI: use --help at any level to discover skills, actions, and arguments. Examples: \"--help\", \"notion --help\", \"notion search --query meetings\", \"github repos --visibility PRIVATE\"."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        serde_json::json!({
            "type": "object",
            "properties": {
                "command": {
                    "type": "string",
                    "description": "CLI-style command. Use --help to discover available skills and actions."
                }
            },
            "required": ["command"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let _ = (&self.cfg, args);
        todo!("Phase 3: parse command, check permissions, execute GraphQL query")
    }
}
