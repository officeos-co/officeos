// Rust guideline compliant 2026-02-21

//! `content_search` — search file contents by regex pattern. See API.md §10.12.

use async_trait::async_trait;
use serde_json::json;
use std::path::PathBuf;
use std::process::Stdio;

use super::traits::{Tool, ToolResult};

const MAX_RESULTS: usize = 1000;
const MAX_OUTPUT_BYTES: usize = 1_048_576;
const TIMEOUT_SECS: u64 = 30;

#[derive(Debug)]
pub struct ContentSearchTool {
    workspace_dir: PathBuf,
    has_rg: bool,
}

impl ContentSearchTool {
    #[must_use]
    pub fn new(workspace_dir: PathBuf) -> Self {
        let has_rg = which::which("rg").is_ok();
        Self {
            workspace_dir,
            has_rg,
        }
    }
}

#[async_trait]
impl Tool for ContentSearchTool {
    fn name(&self) -> &'static str {
        "content_search"
    }

    fn description(&self) -> &'static str {
        "Search file contents by regex pattern within the workspace. \
         Uses ripgrep (rg) with grep fallback."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        json!({
            "type": "object",
            "properties": {
                "pattern": {"type": "string", "description": "Regex pattern to search for"},
                "path": {"type": "string", "description": "Directory relative to workspace (default: '.')", "default": "."},
                "include": {"type": "string", "description": "File glob filter, e.g. '*.rs'"},
                "case_sensitive": {"type": "boolean", "description": "Case-sensitive matching (default: true)", "default": true},
                "max_results": {"type": "integer", "description": "Max results (default: 1000)", "default": 1000}
            },
            "required": ["pattern"]
        })
    }

    #[expect(
        clippy::too_many_lines,
        reason = "search tool with rg/grep fallback is inherently verbose"
    )]
    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let pattern = args
            .get("pattern")
            .and_then(|v| v.as_str())
            .ok_or_else(|| anyhow::anyhow!("Missing 'pattern' parameter"))?;

        if pattern.is_empty() {
            return Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some("Empty pattern is not allowed.".into()),
            });
        }

        let search_path = args.get("path").and_then(|v| v.as_str()).unwrap_or(".");
        let include = args.get("include").and_then(|v| v.as_str());
        let case_sensitive = args
            .get("case_sensitive")
            .and_then(serde_json::Value::as_bool)
            .unwrap_or(true);

        #[expect(
            clippy::cast_possible_truncation,
            reason = "max_results is always small enough to fit in usize"
        )]
        let max_results = args
            .get("max_results")
            .and_then(serde_json::Value::as_u64)
            .map_or(MAX_RESULTS, |v| v as usize)
            .min(MAX_RESULTS);

        let resolved_path = self.workspace_dir.join(search_path);

        if !resolved_path.exists() {
            return Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some(format!("Path does not exist: {search_path}")),
            });
        }

        let mut cmd = if self.has_rg {
            let mut c = std::process::Command::new("rg");
            c.arg("--no-heading")
                .arg("--line-number")
                .arg("--with-filename");
            if !case_sensitive {
                c.arg("-i");
            }
            if let Some(glob) = include {
                c.arg("--glob").arg(glob);
            }
            c.arg("--").arg(pattern).arg(&resolved_path);
            c
        } else {
            let mut c = std::process::Command::new("grep");
            c.arg("-r").arg("-n").arg("-E");
            if !case_sensitive {
                c.arg("-i");
            }
            if let Some(glob) = include {
                c.arg("--include").arg(glob);
            }
            c.arg("--").arg(pattern).arg(&resolved_path);
            c
        };

        cmd.env_clear();
        for key in &["PATH", "HOME", "LANG", "LC_ALL", "LC_CTYPE"] {
            if let Ok(val) = std::env::var(key) {
                cmd.env(key, val);
            }
        }
        cmd.stdout(Stdio::piped());
        cmd.stderr(Stdio::piped());

        let output = match tokio::time::timeout(
            std::time::Duration::from_secs(TIMEOUT_SECS),
            tokio::process::Command::from(cmd).output(),
        )
        .await
        {
            Ok(Ok(out)) => out,
            Ok(Err(e)) => {
                return Ok(ToolResult {
                    success: false,
                    output: String::new(),
                    error: Some(format!("Failed to execute search command: {e}")),
                });
            }
            Err(_) => {
                return Ok(ToolResult {
                    success: false,
                    output: String::new(),
                    error: Some(format!("Search timed out after {TIMEOUT_SECS} seconds.")),
                });
            }
        };

        let exit_code = output.status.code().unwrap_or(-1);
        if exit_code >= 2 {
            let stderr = String::from_utf8_lossy(&output.stderr);
            return Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some(format!("Search error: {}", stderr.trim())),
            });
        }

        let raw_stdout = String::from_utf8_lossy(&output.stdout);

        if raw_stdout.trim().is_empty() {
            return Ok(ToolResult {
                success: true,
                output: "No matches found.".into(),
                error: None,
            });
        }

        // Relativize paths and limit results
        let workspace_prefix = self
            .workspace_dir
            .canonicalize()
            .unwrap_or_else(|_| self.workspace_dir.clone())
            .to_string_lossy()
            .to_string();

        let mut lines: Vec<String> = Vec::new();
        let mut truncated = false;
        for line in raw_stdout.lines() {
            if line.is_empty() {
                continue;
            }
            let relativized = if let Some(rest) = line.strip_prefix(&workspace_prefix) {
                rest.strip_prefix('/').unwrap_or(rest).to_string()
            } else {
                line.to_string()
            };
            lines.push(relativized);
            if lines.len() >= max_results {
                truncated = true;
                break;
            }
        }

        let mut formatted = lines.join("\n");
        if truncated {
            use std::fmt::Write;
            let _ = write!(
                formatted,
                "\n\n[Results truncated: showing first {max_results} results]"
            );
        }

        // Truncate total output size
        if formatted.len() > MAX_OUTPUT_BYTES {
            let mut end = MAX_OUTPUT_BYTES;
            while end > 0 && !formatted.is_char_boundary(end) {
                end -= 1;
            }
            formatted.truncate(end);
            formatted.push_str("\n\n[Output truncated: exceeded 1 MB limit]");
        }

        Ok(ToolResult {
            success: true,
            output: formatted,
            error: None,
        })
    }
}
