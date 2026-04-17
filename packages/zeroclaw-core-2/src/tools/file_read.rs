//! `file_read` — read file contents with line numbers. See API.md §10.7.

use async_trait::async_trait;
use std::path::PathBuf;

use super::traits::{Tool, ToolResult};

pub struct FileReadTool {
    memory_dir: PathBuf,
}

impl FileReadTool {
    pub fn new(memory_dir: PathBuf) -> Self {
        Self { memory_dir }
    }
}

#[async_trait]
impl Tool for FileReadTool {
    fn name(&self) -> &str {
        "file_read"
    }

    fn description(&self) -> &str {
        "Read file contents with line numbers. Supports partial reading via offset and limit."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        serde_json::json!({
            "type": "object",
            "properties": {
                "path": {"type": "string", "description": "File path relative to memory_dir"},
                "offset": {"type": "integer", "description": "Starting line number (1-based)", "default": 1},
                "limit": {"type": "integer", "description": "Max lines to read"}
            },
            "required": ["path"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let _ = (&self.memory_dir, args);
        todo!("Phase 3: read file with line numbers, enforce 10 MiB limit")
    }
}
