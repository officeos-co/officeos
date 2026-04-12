//! Native tool: query Obsidian notes by any frontmatter property.
//!
//! Calls the obsidian skill via the skill runtime HTTP endpoint with
//! `action = "query_by_property"`. Supports `eq`, `contains`, and `exists`
//! operators — equivalent to Obsidian Bases filtered views.

use super::traits::{Tool, ToolResult};
use async_trait::async_trait;
use serde_json::json;
use std::time::Duration;

const MAX_RESPONSE_BYTES: usize = 1_048_576;
const HTTP_TIMEOUT_SECS: u64 = 30;
const DEFAULT_LIMIT: u64 = 50;

/// Query notes in the Obsidian knowledge graph by any frontmatter property.
pub struct ObsidianQueryByPropertyTool {
    /// Base URL of the skill runtime (e.g. `http://localhost:3001`).
    skill_runtime_url: String,
}

impl ObsidianQueryByPropertyTool {
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
impl Tool for ObsidianQueryByPropertyTool {
    fn name(&self) -> &str {
        "obsidian_query_by_property"
    }

    fn description(&self) -> &str {
        "Query notes in the knowledge graph by any frontmatter property. \
         Supports eq, contains, and exists operators — equivalent to Obsidian Bases filtered views."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        json!({
            "type": "object",
            "properties": {
                "property": {
                    "type": "string",
                    "description": "The frontmatter property name to filter on (e.g. 'status', 'project', 'category')"
                },
                "value": {
                    "type": "string",
                    "description": "The value to compare against (not required when operator is 'exists')"
                },
                "operator": {
                    "type": "string",
                    "enum": ["eq", "contains", "exists"],
                    "description": "Comparison operator: 'eq' (exact match, default), 'contains' (substring), 'exists' (property present)"
                },
                "limit": {
                    "type": "number",
                    "description": "Maximum number of results to return (default 50)"
                }
            },
            "required": ["property"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let property = args
            .get("property")
            .and_then(|v| v.as_str())
            .ok_or_else(|| anyhow::anyhow!("Missing 'property' parameter"))?;

        let operator = args
            .get("operator")
            .and_then(|v| v.as_str())
            .unwrap_or("eq");

        if !matches!(operator, "eq" | "contains" | "exists") {
            return Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some(format!(
                    "Invalid operator '{operator}'. Must be one of: eq, contains, exists"
                )),
            });
        }

        let value = args.get("value").and_then(|v| v.as_str());
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

        let mut payload = json!({
            "action": "query_by_property",
            "property": property,
            "operator": operator,
            "limit": limit,
        });

        if let Some(v) = value {
            payload["value"] = json!(v);
        }

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
#[path = "obsidian_query_by_property.test.rs"]
mod tests;
