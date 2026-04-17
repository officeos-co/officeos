//! `memory_forget` — remove a memory by key. See API.md §10.4.
//!
//! Scans all subdirectories of `memory_dir` for `{key}.md` and deletes the
//! first match found.

use async_trait::async_trait;
use serde_json::json;
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
        json!({
            "type": "object",
            "properties": {
                "key": {"type": "string", "description": "Key of the memory to remove"}
            },
            "required": ["key"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let key = args
            .get("key")
            .and_then(|v| v.as_str())
            .ok_or_else(|| anyhow::anyhow!("Missing 'key' parameter"))?;

        let filename = format!("{key}.md");

        // Scan all subdirectories for the file
        if let Some(path) = find_memory_file(&self.memory_dir, &filename).await {
            match tokio::fs::remove_file(&path).await {
                Ok(()) => Ok(ToolResult {
                    success: true,
                    output: format!("Forgot memory: {key}"),
                    error: None,
                }),
                Err(e) => Ok(ToolResult {
                    success: false,
                    output: String::new(),
                    error: Some(format!("Failed to delete memory file: {e}")),
                }),
            }
        } else {
            Ok(ToolResult {
                success: true,
                output: format!("No memory found with key: {key}"),
                error: None,
            })
        }
    }
}

/// Recursively search for a file by name under `dir`.
async fn find_memory_file(dir: &PathBuf, filename: &str) -> Option<PathBuf> {
    let mut read_dir = tokio::fs::read_dir(dir).await.ok()?;
    while let Ok(Some(entry)) = read_dir.next_entry().await {
        let path = entry.path();
        if path.is_dir() {
            if let Some(found) = Box::pin(find_memory_file(&path, filename)).await {
                return Some(found);
            }
        } else if path.file_name().and_then(|n| n.to_str()) == Some(filename) {
            return Some(path);
        }
    }
    None
}
