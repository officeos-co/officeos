//! `memory_recall` — search long-term memory. See API.md §10.3.

use async_trait::async_trait;
use std::path::PathBuf;

use super::traits::{Tool, ToolResult};

pub struct MemoryRecallTool {
    memory_dir: PathBuf,
}

impl MemoryRecallTool {
    pub fn new(memory_dir: PathBuf) -> Self {
        Self { memory_dir }
    }
}

#[async_trait]
impl Tool for MemoryRecallTool {
    fn name(&self) -> &str {
        "memory_recall"
    }

    fn description(&self) -> &str {
        "Search long-term memory for relevant facts. Returns scored results ranked by relevance. Supports keyword search, time-only query (since/until), or both."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        serde_json::json!({
            "type": "object",
            "properties": {
                "query": {"type": "string", "description": "Search query text"},
                "limit": {"type": "integer", "description": "Max results to return", "default": 5},
                "since": {"type": "string", "description": "RFC3339 timestamp lower bound"},
                "until": {"type": "string", "description": "RFC3339 timestamp upper bound"}
            }
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let _ = (&self.memory_dir, args);
        todo!("Phase 3: BM25 search over memory_dir markdown files")
    }
}
