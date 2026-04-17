//! `http_request` — make HTTP requests to external APIs. See API.md §10.10.

use async_trait::async_trait;
use serde_json::json;
use std::time::Duration;

use super::traits::{Tool, ToolResult};

const MAX_RESPONSE_BYTES: usize = 1_048_576;
const DEFAULT_TIMEOUT_SECS: u64 = 30;

pub struct HttpRequestTool;

impl Default for HttpRequestTool {
    fn default() -> Self {
        Self::new()
    }
}

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
        "Make HTTP requests to external APIs. Supports GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        json!({
            "type": "object",
            "properties": {
                "url": {"type": "string", "description": "HTTP or HTTPS URL"},
                "method": {"type": "string", "description": "HTTP method", "default": "GET"},
                "headers": {"type": "object", "description": "HTTP headers as key-value pairs"},
                "body": {"type": "string", "description": "Request body"}
            },
            "required": ["url"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let url = args
            .get("url")
            .and_then(|v| v.as_str())
            .ok_or_else(|| anyhow::anyhow!("Missing 'url' parameter"))?;

        let method_str = args.get("method").and_then(|v| v.as_str()).unwrap_or("GET");
        let headers_val = args.get("headers").cloned().unwrap_or(json!({}));
        let body = args.get("body").and_then(|v| v.as_str());

        if url.is_empty() {
            return Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some("URL cannot be empty".into()),
            });
        }

        if !url.starts_with("http://") && !url.starts_with("https://") {
            return Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some("Only http:// and https:// URLs are allowed".into()),
            });
        }

        let method = match method_str.to_uppercase().as_str() {
            "GET" => reqwest::Method::GET,
            "POST" => reqwest::Method::POST,
            "PUT" => reqwest::Method::PUT,
            "DELETE" => reqwest::Method::DELETE,
            "PATCH" => reqwest::Method::PATCH,
            "HEAD" => reqwest::Method::HEAD,
            "OPTIONS" => reqwest::Method::OPTIONS,
            _ => {
                return Ok(ToolResult {
                    success: false,
                    output: String::new(),
                    error: Some(format!("Unsupported HTTP method: {method_str}")),
                });
            }
        };

        let client = match reqwest::Client::builder()
            .timeout(Duration::from_secs(DEFAULT_TIMEOUT_SECS))
            .connect_timeout(Duration::from_secs(10))
            .redirect(reqwest::redirect::Policy::none())
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

        let mut request = client.request(method, url);

        if let Some(obj) = headers_val.as_object() {
            for (key, value) in obj {
                if let Some(str_val) = value.as_str() {
                    request = request.header(key.as_str(), str_val);
                }
            }
        }

        if let Some(body_str) = body {
            request = request.body(body_str.to_string());
        }

        match request.send().await {
            Ok(response) => {
                let status = response.status();
                let status_code = status.as_u16();

                let response_text = match response.text().await {
                    Ok(text) => {
                        if text.len() > MAX_RESPONSE_BYTES {
                            let mut truncated =
                                text.chars().take(MAX_RESPONSE_BYTES).collect::<String>();
                            truncated
                                .push_str("\n\n... [Response truncated due to size limit] ...");
                            truncated
                        } else {
                            text
                        }
                    }
                    Err(e) => format!("[Failed to read response body: {e}]"),
                };

                let output = format!(
                    "Status: {} {}\n\nResponse Body:\n{}",
                    status_code,
                    status.canonical_reason().unwrap_or("Unknown"),
                    response_text
                );

                Ok(ToolResult {
                    success: status.is_success(),
                    output,
                    error: if status.is_client_error() || status.is_server_error() {
                        Some(format!("HTTP {status_code}"))
                    } else {
                        None
                    },
                })
            }
            Err(e) => Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some(format!("HTTP request failed: {e}")),
            }),
        }
    }
}
