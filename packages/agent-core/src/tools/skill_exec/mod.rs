// Rust guideline compliant 2026-02-21

//! `skill_exec` — CLI-like interface to backend skills via GraphQL.
//!
//! See API.md §10.1.

pub mod parser;
pub mod query_builder;
pub mod schema_cache;

use std::sync::Arc;
use std::time::Duration;

use async_trait::async_trait;
use serde_json::json;
use tokio::sync::Mutex;

use super::traits::{Tool, ToolResult};
use crate::config::{Permission, RuntimeConfig};
use parser::ParsedCommand;
use schema_cache::SchemaCache;

/// Maximum response body size (1 MiB) before truncation.
const MAX_RESPONSE_BYTES: usize = 1_048_576;
/// HTTP request timeout for GraphQL calls (seconds).
const HTTP_TIMEOUT_SECS: u64 = 30;

#[derive(Debug)]
pub struct SkillExecTool {
    cfg: Arc<RuntimeConfig>,
    schema_cache: Arc<Mutex<SchemaCache>>,
}

impl SkillExecTool {
    #[must_use]
    pub fn new(cfg: Arc<RuntimeConfig>) -> Self {
        let graphql_url = format!("{}/api/graphql", cfg.backend_url.trim_end_matches('/'));
        let bearer_token = Some(cfg.backend_token.clone());
        let cache = SchemaCache::new(graphql_url, bearer_token);
        Self {
            cfg,
            schema_cache: Arc::new(Mutex::new(cache)),
        }
    }
}

#[async_trait]
impl Tool for SkillExecTool {
    fn name(&self) -> &'static str {
        "skill_exec"
    }

    fn description(&self) -> &'static str {
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
        let command = args.get("command").and_then(|v| v.as_str()).unwrap_or("");

        let parsed = parser::parse(command);

        // Ensure schema is loaded
        {
            let mut cache = self.schema_cache.lock().await;
            if let Err(e) = cache.ensure_loaded().await {
                tracing::warn!(name: "skill_exec.schema.load_failed", error_message = %e, "Failed to load GraphQL schema: {{error_message}}");
                return Ok(ToolResult {
                    success: false,
                    output: String::new(),
                    error: Some(format!(
                        "Could not load skill schema from backend: {e}. \
                         Is the backend reachable at {}?",
                        self.cfg.backend_url
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
                tracing::info!(name: "skill_exec.execute.start", skill_name = %skill, skill_action = %action, "executing skill {{skill_name}} action {{skill_action}}");
                // Enforce per-tool allow/deny before dispatch.
                let key = (skill.to_ascii_lowercase(), action.clone());
                if let Some(Permission::Deny) = self.cfg.tool_permissions.get(&key) {
                    return Ok(ToolResult {
                        success: false,
                        output: String::new(),
                        error: Some(format!(
                            "tool denied by policy: {skill}:{action} is set to deny for this agent"
                        )),
                    });
                }

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

                let result = self.execute_graphql(&gql_query).await;
                if let Ok(ref tr) = result {
                    if !tr.success {
                        tracing::warn!(name: "skill_exec.execute.failed", skill_name = %skill, skill_action = %action, "skill {{skill_name}} action {{skill_action}} failed");
                    }
                }
                result
            }
        }
    }
}

impl SkillExecTool {
    async fn execute_graphql(&self, query: &str) -> anyhow::Result<ToolResult> {
        let graphql_url = format!("{}/api/graphql", self.cfg.backend_url.trim_end_matches('/'));

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
        let req = client
            .post(&graphql_url)
            .header("content-type", "application/json")
            .bearer_auth(&self.cfg.backend_token)
            .json(&body);

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
