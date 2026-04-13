use super::traits::{Tool, ToolResult};
use crate::heartbeat::engine::HeartbeatEngine;
use async_trait::async_trait;
use serde_json::json;
use std::path::PathBuf;

/// Tool that lets the agent remove a heartbeat task by name.
pub struct HeartbeatRemoveTool {
    workspace_dir: PathBuf,
}

impl HeartbeatRemoveTool {
    pub fn new(workspace_dir: PathBuf) -> Self {
        Self { workspace_dir }
    }
}

#[async_trait]
impl Tool for HeartbeatRemoveTool {
    fn name(&self) -> &str {
        "heartbeat_remove"
    }

    fn description(&self) -> &str {
        "Remove a heartbeat task by name."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        json!({
            "type": "object",
            "properties": {
                "name": {
                    "type": "string",
                    "description": "Name of the task to remove"
                }
            },
            "required": ["name"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let name = args
            .get("name")
            .and_then(|v| v.as_str())
            .ok_or_else(|| anyhow::anyhow!("Missing 'name' parameter"))?;

        let mut tasks = HeartbeatEngine::read_tasks_from_file(&self.workspace_dir)?;
        let original_len = tasks.len();

        tasks.retain(|t| {
            t.name.as_deref() != Some(name) && t.text != name
        });

        if tasks.len() == original_len {
            return Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some(format!("Task '{name}' not found")),
            });
        }

        HeartbeatEngine::write_tasks_to_file(&self.workspace_dir, &tasks)?;

        Ok(ToolResult {
            success: true,
            output: format!("Removed heartbeat task '{name}'"),
            error: None,
        })
    }
}

#[cfg(test)]
#[path = "heartbeat_remove.test.rs"]
mod tests;
