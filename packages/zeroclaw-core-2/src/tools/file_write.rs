//! `file_write` — write contents to a file. See API.md §10.8.

use async_trait::async_trait;
use std::path::PathBuf;

use super::traits::{Tool, ToolResult};

pub struct FileWriteTool {
    memory_dir: PathBuf,
}

impl FileWriteTool {
    pub fn new(memory_dir: PathBuf) -> Self {
        Self { memory_dir }
    }
}

#[async_trait]
impl Tool for FileWriteTool {
    fn name(&self) -> &str {
        "file_write"
    }

    fn description(&self) -> &str {
        "Write contents to a file in the workspace."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        serde_json::json!({
            "type": "object",
            "properties": {
                "path": {"type": "string", "description": "File path relative to memory_dir. Absolute paths rejected."},
                "content": {"type": "string", "description": "File contents to write"}
            },
            "required": ["path", "content"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let _ = (&self.memory_dir, args);
        todo!("Phase 3: atomic write via tempfile+rename, create parent dirs")
    }
}
