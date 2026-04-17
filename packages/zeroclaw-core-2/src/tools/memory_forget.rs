//! `memory_forget` — remove a memory by key. See API.md §10.4.

use async_trait::async_trait;
use std::path::PathBuf;

use super::traits::{Tool, ToolResult};

pub struct MemoryForgetTool {
    memory_dir: PathBuf,
}

impl MemoryForgetTool {
    pub fn new(memory_dir: PathBuf) -> Self {
        Self { memory_dir }
    }
}

#[async_trait]
impl Tool for MemoryForgetTool {
    fn name(&self) -> &str {
        "memory_forget"
    }

    fn description(&self) -> &str {
        "Remove a memory by key. Use to delete outdated facts or sensitive data."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        serde_json::json!({
            "type": "object",
            "properties": {
                "key": {"type": "string", "description": "Key of the memory to remove"},
                "category": {"type": "string", "description": "Category to search in. Omit to search all categories."}
            },
            "required": ["key"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let _ = (&self.memory_dir, args);
        todo!("Phase 3: find and delete memory file")
    }
}
