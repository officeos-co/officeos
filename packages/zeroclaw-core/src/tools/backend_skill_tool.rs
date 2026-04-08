//! Tool backed by a backend skill route.
//!
//! Each entry returned by the EAOS backend's `GET /api/capabilities`
//! endpoint is converted into one `BackendSkillTool`. Calling the tool
//! POSTs the LLM-provided arguments as the JSON body to
//! `{backend_url}{route}` with the configured bearer token.
//!
//! This is the Rust-side half of the "Skill Gateway" architecture:
//! agents hold no credentials and ship no skill code — the backend
//! does the real work and the agent just forwards structured calls.

use super::traits::{Tool, ToolResult};
use async_trait::async_trait;
use std::time::Duration;

const MAX_RESPONSE_BYTES: usize = 1_048_576;
const HTTP_TIMEOUT_SECS: u64 = 30;

pub struct BackendSkillTool {
    tool_name: String,
    tool_description: String,
    parameters: serde_json::Value,
    /// Absolute URL built from `backend_url` + the route fragment returned
    /// by `/api/capabilities` (e.g. `https://.../api/skills/notion/search`).
    url: String,
    bearer_token: Option<String>,
}

impl BackendSkillTool {
    pub fn new(
        tool_name: impl Into<String>,
        tool_description: impl Into<String>,
        parameters: serde_json::Value,
        url: impl Into<String>,
        bearer_token: Option<String>,
    ) -> Self {
        Self {
            tool_name: tool_name.into(),
            tool_description: tool_description.into(),
            parameters,
            url: url.into(),
            bearer_token,
        }
    }
}

#[async_trait]
impl Tool for BackendSkillTool {
    fn name(&self) -> &str {
        &self.tool_name
    }

    fn description(&self) -> &str {
        &self.tool_description
    }

    fn parameters_schema(&self) -> serde_json::Value {
        self.parameters.clone()
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
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

        let mut req = client
            .post(&self.url)
            .header("content-type", "application/json")
            .json(&args);
        if let Some(token) = &self.bearer_token {
            req = req.bearer_auth(token);
        }

        let response = match req.send().await {
            Ok(r) => r,
            Err(e) => {
                return Ok(ToolResult {
                    success: false,
                    output: String::new(),
                    error: Some(format!("Backend skill request failed: {e}")),
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
                    error: Some(format!("Failed to read backend response: {e}")),
                });
            }
        };

        Ok(ToolResult {
            success: status.is_success(),
            output: body,
            error: if status.is_success() {
                None
            } else {
                Some(format!("Backend skill HTTP {status}"))
            },
        })
    }
}
