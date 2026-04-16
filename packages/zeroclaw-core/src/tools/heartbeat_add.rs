use super::traits::{Tool, ToolResult};
use crate::heartbeat::engine::{
    HeartbeatEngine, HeartbeatTask, TaskPriority, TaskStatus, parse_duration_str,
};
use async_trait::async_trait;
use serde_json::json;
use std::path::PathBuf;

/// Tool that lets the agent add a new heartbeat task.
pub struct HeartbeatAddTool {
    workspace_dir: PathBuf,
}

impl HeartbeatAddTool {
    pub fn new(workspace_dir: PathBuf) -> Self {
        Self { workspace_dir }
    }
}

#[async_trait]
impl Tool for HeartbeatAddTool {
    fn name(&self) -> &str {
        "heartbeat_add"
    }

    fn description(&self) -> &str {
        "Add a new heartbeat task. The agent will check this task periodically."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        json!({
            "type": "object",
            "properties": {
                "name": {
                    "type": "string",
                    "description": "Unique name for this task (e.g. 'inbox-triage')"
                },
                "prompt": {
                    "type": "string",
                    "description": "The prompt/instruction for this task"
                },
                "interval": {
                    "type": "string",
                    "description": "How often to run (e.g. '30m', '6h', '1d'). Omit for every tick."
                },
                "priority": {
                    "type": "string",
                    "enum": ["high", "medium", "low"],
                    "description": "Task priority. Default: medium"
                }
            },
            "required": ["name", "prompt"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let name = args
            .get("name")
            .and_then(|v| v.as_str())
            .ok_or_else(|| anyhow::anyhow!("Missing 'name' parameter"))?;

        let prompt = args
            .get("prompt")
            .and_then(|v| v.as_str())
            .ok_or_else(|| anyhow::anyhow!("Missing 'prompt' parameter"))?;

        let interval = args
            .get("interval")
            .and_then(|v| v.as_str())
            .map(parse_duration_str);

        let priority = match args.get("priority").and_then(|v| v.as_str()) {
            Some("high") => TaskPriority::High,
            Some("low") => TaskPriority::Low,
            _ => TaskPriority::Medium,
        };

        let mut tasks = HeartbeatEngine::read_tasks_from_file(&self.workspace_dir)?;

        // Check for duplicate name
        if tasks.iter().any(|t| t.name.as_deref() == Some(name)) {
            return Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some(format!("Task with name '{name}' already exists")),
            });
        }

        tasks.push(HeartbeatTask {
            text: prompt.to_string(),
            priority,
            status: TaskStatus::Active,
            name: Some(name.to_string()),
            interval_secs: interval,
            prompt: Some(prompt.to_string()),
        });

        HeartbeatEngine::write_tasks_to_file(&self.workspace_dir, &tasks)?;

        Ok(ToolResult {
            success: true,
            output: format!("Added heartbeat task '{name}'"),
            error: None,
        })
    }
}

#[cfg(test)]
#[path = "heartbeat_add.test.rs"]
mod tests;
