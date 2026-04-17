//! `memory_store` — persist a fact to long-term memory. See API.md §10.2.
//!
//! Writes a markdown file at `{memory_dir}/{category}/{key}.md` containing
//! YAML front-matter (key, category, timestamp) followed by the content body.

use async_trait::async_trait;
use serde_json::json;
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
        json!({
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
        let key = args
            .get("key")
            .and_then(|v| v.as_str())
            .ok_or_else(|| anyhow::anyhow!("Missing 'key' parameter"))?;

        let content = args
            .get("content")
            .and_then(|v| v.as_str())
            .ok_or_else(|| anyhow::anyhow!("Missing 'content' parameter"))?;

        let category = args
            .get("category")
            .and_then(|v| v.as_str())
            .unwrap_or("core");

        let dir = self.memory_dir.join(category);
        tokio::fs::create_dir_all(&dir).await?;

        let file_path = dir.join(format!("{key}.md"));
        let now = chrono::Utc::now().to_rfc3339();
        let file_content = format!(
            "---\nkey: {key}\ncategory: {category}\ncreated: {now}\n---\n{content}\n"
        );

        match tokio::fs::write(&file_path, &file_content).await {
            Ok(()) => Ok(ToolResult {
                success: true,
                output: format!("Stored memory: {key}"),
                error: None,
            }),
            Err(e) => Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some(format!("Failed to store memory: {e}")),
            }),
        }
    }
}
