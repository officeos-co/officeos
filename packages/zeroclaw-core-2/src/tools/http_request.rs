//! `http_request` — make an HTTP request to an API. See API.md §10.10.

use async_trait::async_trait;

use super::traits::{Tool, ToolResult};

pub struct HttpRequestTool;

impl HttpRequestTool {
    pub fn new() -> Self {
        Self
    }
}

#[async_trait]
impl Tool for HttpRequestTool {
    fn name(&self) -> &str {
        "http_request"
    }

    fn description(&self) -> &str {
        "Make an HTTP request to an API endpoint."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        serde_json::json!({
            "type": "object",
            "properties": {
                "url": {"type": "string", "description": "Request URL"},
                "method": {"type": "string", "enum": ["GET", "POST", "PUT", "DELETE", "PATCH"], "default": "GET"},
                "headers": {"type": "object", "description": "Request headers as key-value pairs"},
                "body": {"type": "string", "description": "Request body"},
                "timeout_secs": {"type": "integer", "description": "Timeout in seconds", "default": 30}
            },
            "required": ["url"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let _ = args;
        todo!("Phase 3: send HTTP request, deny private IPs, cap response at 5 MiB")
    }
}
