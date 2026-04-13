use super::traits::{Tool, ToolResult};
use crate::heartbeat::engine::{HeartbeatEngine, HeartbeatState, format_duration_secs};
use async_trait::async_trait;
use serde_json::json;
use std::path::PathBuf;

/// Tool that lets the agent list its own heartbeat tasks with intervals and last-run times.
pub struct HeartbeatListTool {
    workspace_dir: PathBuf,
}

impl HeartbeatListTool {
    pub fn new(workspace_dir: PathBuf) -> Self {
        Self { workspace_dir }
    }
}

#[async_trait]
impl Tool for HeartbeatListTool {
    fn name(&self) -> &str {
        "heartbeat_list"
    }

    fn description(&self) -> &str {
        "List all heartbeat tasks with their intervals, priorities, statuses, and last-run times."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        json!({
            "type": "object",
            "properties": {},
            "required": []
        })
    }

    async fn execute(&self, _args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let tasks = HeartbeatEngine::read_tasks_from_file(&self.workspace_dir)?;
        let state = HeartbeatState::load(&self.workspace_dir);

        if tasks.is_empty() {
            return Ok(ToolResult {
                success: true,
                output: "No heartbeat tasks defined.".to_string(),
                error: None,
            });
        }

        let mut lines = Vec::new();
        for (i, task) in tasks.iter().enumerate() {
            let name = task.name.as_deref().unwrap_or("(unnamed)");
            let interval = task
                .interval_secs
                .map(format_duration_secs)
                .unwrap_or_else(|| "default".to_string());
            let key = task.name.as_deref().unwrap_or(&task.text);
            let last_run = state
                .tasks
                .get(key)
                .and_then(|s| s.last_run_at)
                .map(|t| t.to_rfc3339())
                .unwrap_or_else(|| "never".to_string());
            let run_count = state
                .tasks
                .get(key)
                .map(|s| s.run_count)
                .unwrap_or(0);

            lines.push(format!(
                "{}. [{}|{}] {} — interval: {}, last_run: {}, runs: {}{}",
                i + 1,
                task.priority,
                task.status,
                name,
                interval,
                last_run,
                run_count,
                task.prompt
                    .as_deref()
                    .map(|p| format!("\n   prompt: {p}"))
                    .unwrap_or_default(),
            ));
        }

        Ok(ToolResult {
            success: true,
            output: lines.join("\n"),
            error: None,
        })
    }
}

#[cfg(test)]
#[path = "heartbeat_list.test.rs"]
mod tests;
