//! `memory_store` — persist a fact to long-term memory. See API.md §10.2.

use async_trait::async_trait;
use std::path::PathBuf;

use super::traits::{Tool, ToolResult};

pub struct MemoryStoreTool {
    memory_dir: PathBuf,
}

impl MemoryStoreTool {
    pub fn new(memory_dir: PathBuf) -> Self {
        Self { memory_dir }
    }
}

#[async_trait]
impl Tool for MemoryStoreTool {
    fn name(&self) -> &str {
        "memory_store"
    }

    fn description(&self) -> &str {
        "Store a fact, preference, or note in long-term memory. Use category 'core' for permanent facts, 'daily' for session notes, 'conversation' for chat context, or a custom category name."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        serde_json::json!({
            "type": "object",
            "properties": {
                "key": {"type": "string", "description": "Unique key for this memory"},
                "content": {"type": "string", "description": "The information to remember"},
                "category": {"type": "string", "description": "'core' | 'daily' | 'conversation' | custom. Defaults to 'core'."}
            },
            "required": ["key", "content"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let _ = (&self.memory_dir, args);
        todo!("Phase 3: write memory file with YAML front-matter")
    }
}
