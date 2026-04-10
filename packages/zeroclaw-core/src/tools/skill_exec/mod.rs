//! `skill_exec` — single tool that gives the agent a CLI-like interface
//! to all backend skills, powered by GraphQL under the hood.
//!
//! The agent sees:
//!   skill_exec("notion search --query meetings")
//!   skill_exec("github repos --visibility PRIVATE")
//!   skill_exec("--help")
//!
//! Under the hood:
//!   CLI string → deterministic parser → GraphQL query → POST /api/graphql
//!
//! `--help` at any level uses a cached GraphQL introspection result
//! (no HTTP roundtrip after the initial schema fetch).

pub mod parser;
pub mod query_builder;
pub mod schema_cache;

use std::sync::Arc;
use std::time::Duration;

use async_trait::async_trait;
use serde_json::json;
use tokio::sync::Mutex;

use super::traits::{Tool, ToolResult};
use parser::ParsedCommand;
use schema_cache::SchemaCache;

const MAX_RESPONSE_BYTES: usize = 1_048_576;
const HTTP_TIMEOUT_SECS: u64 = 30;

pub struct SkillExecTool {
    graphql_url: String,
    bearer_token: Option<String>,
    schema_cache: Arc<Mutex<SchemaCache>>,
}

impl SkillExecTool {
    pub fn new(graphql_url: String, bearer_token: Option<String>) -> Self {
        let cache = SchemaCache::new(graphql_url.clone(), bearer_token.clone());
        Self {
            graphql_url,
            bearer_token,
            schema_cache: Arc::new(Mutex::new(cache)),
        }
    }
}

#[async_trait]
impl Tool for SkillExecTool {
    fn name(&self) -> &str {
        "skill_exec"
    }

    fn description(&self) -> &str {
        "Execute a skill command. Works like a CLI: use --help at any level to discover skills, \
         actions, and arguments. Examples: \"--help\", \"notion --help\", \
         \"notion search --query meetings\", \"github repos --visibility PRIVATE\"."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        json!({
            "type": "object",
            "properties": {
                "command": {
                    "type": "string",
                    "description": "CLI-style command. Use --help to discover available skills and actions."
                }
            },
            "required": ["command"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let command = args
            .get("command")
            .and_then(|v| v.as_str())
            .unwrap_or("");

        let parsed = parser::parse(command);

        // Ensure schema is loaded for both help and query paths
        {
            let mut cache = self.schema_cache.lock().await;
            if let Err(e) = cache.ensure_loaded().await {
                tracing::warn!(error = %e, "Failed to load GraphQL schema for skill_exec");
                return Ok(ToolResult {
                    success: false,
                    output: String::new(),
                    error: Some(format!(
                        "Could not load skill schema from backend: {e}. \
                         Is the backend reachable at {}?",
                        self.graphql_url
                    )),
                });
            }
        }

        match parsed {
            ParsedCommand::Help(level) => {
                let cache = self.schema_cache.lock().await;
                let help = cache.help_text(&level);
                Ok(ToolResult {
                    success: true,
                    output: help,
                    error: None,
                })
            }
            ParsedCommand::Query {
                skill,
                action,
                args: cli_args,
            } => {
                let gql_query = {
                    let cache = self.schema_cache.lock().await;
                    match query_builder::build_query(&skill, &action, &cli_args, &cache) {
                        Ok(q) => q,
                        Err(e) => {
                            return Ok(ToolResult {
                                success: false,
                                output: e,
                                error: None,
                            });
                        }
                    }
                };

                self.execute_graphql(&gql_query).await
            }
        }
    }
}

impl SkillExecTool {
    async fn execute_graphql(&self, query: &str) -> anyhow::Result<ToolResult> {
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

        let body = json!({ "query": query });
        let mut req = client
            .post(&self.graphql_url)
            .header("content-type", "application/json")
            .json(&body);
        if let Some(token) = &self.bearer_token {
            req = req.bearer_auth(token);
        }

        let response = match req.send().await {
            Ok(r) => r,
            Err(e) => {
                return Ok(ToolResult {
                    success: false,
                    output: String::new(),
                    error: Some(format!("GraphQL request failed: {e}")),
                });
            }
        };

        let status = response.status();
        let bytes = match response.bytes().await {
            Ok(b) => b,
            Err(e) => {
                return Ok(ToolResult {
                    success: false,
                    output: String::new(),
                    error: Some(format!("Failed to read response: {e}")),
                });
            }
        };

        let mut text = String::from_utf8_lossy(&bytes).to_string();
        if text.len() > MAX_RESPONSE_BYTES {
            let mut b = MAX_RESPONSE_BYTES.min(text.len());
            while b > 0 && !text.is_char_boundary(b) {
                b -= 1;
            }
            text.truncate(b);
            text.push_str("\n... [response truncated at 1MB]");
        }

        // Parse the GraphQL response to extract data or errors
        if let Ok(parsed) = serde_json::from_str::<serde_json::Value>(&text) {
            if let Some(errors) = parsed.get("errors") {
                let err_text = serde_json::to_string_pretty(errors).unwrap_or(text.clone());
                return Ok(ToolResult {
                    success: false,
                    output: err_text,
                    error: Some("GraphQL query returned errors".to_string()),
                });
            }
            if let Some(data) = parsed.get("data") {
                let pretty = serde_json::to_string_pretty(data).unwrap_or(text);
                return Ok(ToolResult {
                    success: true,
                    output: pretty,
                    error: None,
                });
            }
        }

        Ok(ToolResult {
            success: status.is_success(),
            output: text,
            error: if status.is_success() {
                None
            } else {
                Some(format!("GraphQL HTTP {status}"))
            },
        })
    }
}
