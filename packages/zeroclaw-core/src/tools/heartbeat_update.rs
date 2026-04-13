use super::traits::{Tool, ToolResult};
use crate::heartbeat::engine::{HeartbeatEngine, TaskPriority, TaskStatus, parse_duration_str};
use async_trait::async_trait;
use serde_json::json;
use std::path::PathBuf;

/// Tool that lets the agent update an existing heartbeat task.
pub struct HeartbeatUpdateTool {
    workspace_dir: PathBuf,
}

impl HeartbeatUpdateTool {
    pub fn new(workspace_dir: PathBuf) -> Self {
        Self { workspace_dir }
    }
}

#[async_trait]
impl Tool for HeartbeatUpdateTool {
    fn name(&self) -> &str {
        "heartbeat_update"
    }

    fn description(&self) -> &str {
        "Update a heartbeat task's prompt, interval, priority, or status."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        json!({
            "type": "object",
            "properties": {
                "name": {
                    "type": "string",
                    "description": "Name of the task to update"
                },
                "prompt": {
                    "type": "string",
                    "description": "New prompt/instruction for this task"
                },
                "interval": {
                    "type": "string",
                    "description": "New interval (e.g. '30m', '6h', '1d')"
                },
                "priority": {
                    "type": "string",
                    "enum": ["high", "medium", "low"],
                    "description": "New priority"
                },
                "status": {
                    "type": "string",
                    "enum": ["active", "paused", "completed"],
                    "description": "New status"
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

        let task = tasks.iter_mut().find(|t| {
            t.name.as_deref() == Some(name) || t.text == name
        });

        let task = match task {
            Some(t) => t,
            None => {
                return Ok(ToolResult {
                    success: false,
                    output: String::new(),
                    error: Some(format!("Task '{name}' not found")),
                });
            }
        };

        if let Some(prompt) = args.get("prompt").and_then(|v| v.as_str()) {
            task.prompt = Some(prompt.to_string());
            task.text = prompt.to_string();
        }

        if let Some(interval) = args.get("interval").and_then(|v| v.as_str()) {
            task.interval_secs = Some(parse_duration_str(interval));
        }

        if let Some(priority) = args.get("priority").and_then(|v| v.as_str()) {
            task.priority = match priority {
                "high" => TaskPriority::High,
                "low" => TaskPriority::Low,
                _ => TaskPriority::Medium,
            };
        }

        if let Some(status) = args.get("status").and_then(|v| v.as_str()) {
            task.status = match status {
                "paused" | "pause" => TaskStatus::Paused,
                "completed" | "complete" | "done" => TaskStatus::Completed,
                _ => TaskStatus::Active,
            };
        }

        HeartbeatEngine::write_tasks_to_file(&self.workspace_dir, &tasks)?;

        Ok(ToolResult {
            success: true,
            output: format!("Updated heartbeat task '{name}'"),
            error: None,
        })
    }
}

#[cfg(test)]
#[path = "heartbeat_update.test.rs"]
mod tests;
