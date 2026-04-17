//! `web_fetch` — fetch a web page as plain text. See API.md §10.11.

use async_trait::async_trait;

use super::traits::{Tool, ToolResult};

pub struct WebFetchTool;

impl WebFetchTool {
    pub fn new() -> Self {
        Self
    }
}

#[async_trait]
impl Tool for WebFetchTool {
    fn name(&self) -> &str {
        "web_fetch"
    }

    fn description(&self) -> &str {
        "Fetch a web page and convert HTML to plain text for LLM consumption."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        serde_json::json!({
            "type": "object",
            "properties": {
                "url": {"type": "string", "description": "URL to fetch"},
                "timeout_secs": {"type": "integer", "description": "Timeout in seconds", "default": 30}
            },
            "required": ["url"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let _ = args;
        todo!("Phase 3: GET URL, convert HTML to plain text, cap at 5 MiB")
    }
}
