//! `content_search` — search file contents by regex. See API.md §10.12.

use async_trait::async_trait;
use std::path::PathBuf;

use super::traits::{Tool, ToolResult};

pub struct ContentSearchTool {
    memory_dir: PathBuf,
}

impl ContentSearchTool {
    pub fn new(memory_dir: PathBuf) -> Self {
        Self { memory_dir }
    }
}

#[async_trait]
impl Tool for ContentSearchTool {
    fn name(&self) -> &str {
        "content_search"
    }

    fn description(&self) -> &str {
        "Search file contents by regex pattern within the workspace."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        serde_json::json!({
            "type": "object",
            "properties": {
                "pattern": {"type": "string", "description": "Regex pattern to search for"},
                "path": {"type": "string", "description": "Directory to search in", "default": "."},
                "glob": {"type": "string", "description": "File glob filter"},
                "case_insensitive": {"type": "boolean", "description": "Case-insensitive search"},
                "line_numbers": {"type": "boolean", "description": "Show line numbers", "default": true},
                "max_results": {"type": "integer", "description": "Max results to return", "default": 1000}
            },
            "required": ["pattern"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let _ = (&self.memory_dir, args);
        todo!("Phase 3: shell out to rg or grep, cap output at 1 MiB")
    }
}
