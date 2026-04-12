//! Native tool: find Obsidian notes by category frontmatter property.
//!
//! Calls the obsidian skill via the skill runtime HTTP endpoint with
//! `action = "find_by_category"`. Returns a JSON array of matching notes
//! with their path, title, category, and tags.

use super::traits::{Tool, ToolResult};
use async_trait::async_trait;
use serde_json::json;
use std::time::Duration;

const MAX_RESPONSE_BYTES: usize = 1_048_576;
const HTTP_TIMEOUT_SECS: u64 = 30;
const DEFAULT_LIMIT: u64 = 50;

/// Find notes in the Obsidian knowledge graph by their `category` frontmatter.
pub struct ObsidianFindByCategoryTool {
    /// Base URL of the skill runtime (e.g. `http://localhost:3001`).
    skill_runtime_url: String,
}

impl ObsidianFindByCategoryTool {
    pub fn new(skill_runtime_url: impl Into<String>) -> Self {
        Self {
            skill_runtime_url: skill_runtime_url.into(),
        }
    }

    fn endpoint(&self) -> String {
        format!("{}/skills/obsidian/execute", self.skill_runtime_url)
    }
}

#[async_trait]
impl Tool for ObsidianFindByCategoryTool {
    fn name(&self) -> &str {
        "obsidian_find_by_category"
    }

    fn description(&self) -> &str {
        "Find notes in the knowledge graph by their category frontmatter property. \
         Returns path, title, category, and tags for each match."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        json!({
            "type": "object",
            "properties": {
                "category": {
                    "type": "string",
                    "description": "The category frontmatter value to search for (exact match)"
                },
                "limit": {
                    "type": "number",
                    "description": "Maximum number of results to return (default 50)"
                }
            },
            "required": ["category"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let category = args
            .get("category")
            .and_then(|v| v.as_str())
            .ok_or_else(|| anyhow::anyhow!("Missing 'category' parameter"))?;

        let limit = args
            .get("limit")
            .and_then(|v| v.as_u64())
            .unwrap_or(DEFAULT_LIMIT);

        let client = match reqwest::Client::builder()
            .timeout(Duration::from_secs(HTTP_TIMEOUT_SECS))
            .build()
        {
            Ok(c) => c,
            Err(e) => {
                return Ok(ToolResult {
                    success: false,
                    output: String::new(),
                    error: Some(format!("Failed to build HTTP client: {e}")),
                });
            }
        };

        let payload = json!({
            "action": "find_by_category",
            "category": category,
            "limit": limit,
        });

        let response = match client
            .post(self.endpoint())
            .header("content-type", "application/json")
            .json(&payload)
            .send()
            .await
        {
            Ok(r) => r,
            Err(e) => {
                return Ok(ToolResult {
                    success: false,
                    output: String::new(),
                    error: Some(format!("Obsidian skill request failed: {e}")),
                });
            }
        };

        let status = response.status();
        let body = match response.bytes().await {
            Ok(bytes) => {
                let mut text = String::from_utf8_lossy(&bytes).to_string();
                if text.len() > MAX_RESPONSE_BYTES {
                    let mut b = MAX_RESPONSE_BYTES.min(text.len());
                    while b > 0 && !text.is_char_boundary(b) {
                        b -= 1;
                    }
                    text.truncate(b);
                    text.push_str("\n... [response truncated at 1MB]");
                }
                text
            }
            Err(e) => {
                return Ok(ToolResult {
                    success: false,
                    output: String::new(),
                    error: Some(format!("Failed to read skill response: {e}")),
                });
            }
        };

        Ok(ToolResult {
            success: status.is_success(),
            output: body,
            error: if status.is_success() {
                None
            } else {
                Some(format!("Obsidian skill HTTP {status}"))
            },
        })
    }
}

#[cfg(test)]
#[path = "obsidian_find_by_category.test.rs"]
mod tests;
