//! `shell` — run a shell command in the agent workspace. See API.md §10.6.

use async_trait::async_trait;
use std::path::PathBuf;

use super::traits::{Tool, ToolResult};

pub struct ShellTool {
    memory_dir: PathBuf,
}

impl ShellTool {
    pub fn new(memory_dir: PathBuf) -> Self {
        Self { memory_dir }
    }
}

#[async_trait]
impl Tool for ShellTool {
    fn name(&self) -> &str {
        "shell"
    }

    fn description(&self) -> &str {
        "Run a shell command inside the agent workspace and return stdout/stderr."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        serde_json::json!({
            "type": "object",
            "properties": {
                "command": {"type": "string", "description": "Shell command to execute"},
                "timeout_secs": {"type": "integer", "description": "Timeout in seconds", "default": 60},
                "cwd": {"type": "string", "description": "Working directory relative to memory_dir"}
            },
            "required": ["command"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let _ = (&self.memory_dir, args);
        todo!("Phase 3: spawn /bin/sh -lc, capture output, enforce timeout")
    }
}
