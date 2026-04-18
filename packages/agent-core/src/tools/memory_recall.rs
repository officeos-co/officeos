// Rust guideline compliant 2026-02-21

//! `memory_recall` — search long-term memory. See API.md §10.3.
//!
//! Scans all `.md` files under `memory_dir`, applies a simple keyword match
//! (case-insensitive substring), and returns the top results sorted by
//! relevance (number of keyword hits).

use async_trait::async_trait;
use serde_json::json;
use std::fmt::Write;
use std::path::PathBuf;

use super::traits::{Tool, ToolResult};

#[derive(Debug)]
pub struct MemoryRecallTool {
    memory_dir: PathBuf,
}

impl MemoryRecallTool {
    #[must_use]
    pub fn new(memory_dir: PathBuf) -> Self {
        Self { memory_dir }
    }
}

/// Simple scored entry from a memory file.
struct MemoryEntry {
    key: String,
    category: String,
    content: String,
    score: usize,
}

#[async_trait]
impl Tool for MemoryRecallTool {
    fn name(&self) -> &'static str {
        "memory_recall"
    }

    fn description(&self) -> &'static str {
        "Search long-term memory for relevant facts. Returns scored results ranked by relevance."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        json!({
            "type": "object",
            "properties": {
                "query": {"type": "string", "description": "Search query text"},
                "limit": {"type": "integer", "description": "Max results (default: 5)", "default": 5}
            }
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let query = args.get("query").and_then(|v| v.as_str()).unwrap_or("");

        if query.trim().is_empty() {
            return Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some("Provide a 'query' to search for.".into()),
            });
        }

        #[expect(
            clippy::cast_possible_truncation,
            reason = "limit is always small enough to fit in usize"
        )]
        let limit = args
            .get("limit")
            .and_then(serde_json::Value::as_u64)
            .map_or(5, |v| v as usize);

        let query_lower = query.to_lowercase();
        let keywords: Vec<&str> = query_lower.split_whitespace().collect();

        let mut entries = Vec::new();
        collect_memories(&self.memory_dir, &keywords, &mut entries).await;

        if entries.is_empty() {
            return Ok(ToolResult {
                success: true,
                output: "No memories found.".into(),
                error: None,
            });
        }

        // Sort by score descending
        entries.sort_by(|a, b| b.score.cmp(&a.score));
        entries.truncate(limit);

        let mut output = format!("Found {} memories:\n", entries.len());
        for entry in &entries {
            let _ = writeln!(
                output,
                "- [{}] {}: {}",
                entry.category, entry.key, entry.content
            );
        }

        Ok(ToolResult {
            success: true,
            output,
            error: None,
        })
    }
}

/// Recursively scan `dir` for `.md` files and score them against keywords.
async fn collect_memories(dir: &PathBuf, keywords: &[&str], entries: &mut Vec<MemoryEntry>) {
    let Ok(mut read_dir) = tokio::fs::read_dir(dir).await else {
        return;
    };

    while let Ok(Some(entry)) = read_dir.next_entry().await {
        let path = entry.path();
        if path.is_dir() {
            Box::pin(collect_memories(&path, keywords, entries)).await;
            continue;
        }
        if path.extension().and_then(|e| e.to_str()) != Some("md") {
            continue;
        }

        let Ok(text) = tokio::fs::read_to_string(&path).await else {
            continue;
        };

        // Parse YAML front-matter
        let (key, category, body) = parse_memory_file(&text, &path);

        let text_lower = text.to_lowercase();
        let score: usize = keywords
            .iter()
            .filter(|kw| text_lower.contains(*kw))
            .count();

        if score > 0 {
            entries.push(MemoryEntry {
                key,
                category,
                content: body,
                score,
            });
        }
    }
}

/// Extract key, category, and body from a memory markdown file.
fn parse_memory_file(text: &str, path: &std::path::Path) -> (String, String, String) {
    let default_key = path
        .file_stem()
        .and_then(|s| s.to_str())
        .unwrap_or("unknown")
        .to_string();
    let default_category = path
        .parent()
        .and_then(|p| p.file_name())
        .and_then(|s| s.to_str())
        .unwrap_or("unknown")
        .to_string();

    // Try to parse YAML front-matter between ---
    if !text.starts_with("---") {
        return (default_key, default_category, text.trim().to_string());
    }

    if let Some(end) = text[3..].find("---") {
        let front_matter = &text[3..3 + end];
        let body = text[3 + end + 3..].trim().to_string();

        let mut key = default_key;
        let mut category = default_category;

        for line in front_matter.lines() {
            if let Some(v) = line.strip_prefix("key:") {
                key = v.trim().to_string();
            } else if let Some(v) = line.strip_prefix("category:") {
                category = v.trim().to_string();
            }
        }

        (key, category, body)
    } else {
        (default_key, default_category, text.trim().to_string())
    }
}
