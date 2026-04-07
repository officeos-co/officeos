//! Generic tool wrappers for crosscutting concerns.
//!
//! Each wrapper implements [`Tool`] by delegating to an inner tool while
//! applying one crosscutting concern around the `execute` call.  Wrappers
//! compose: stack them at construction time in `tools/mod.rs` rather than
//! repeating the same guard blocks inside every tool's `execute` method.
//!
//! # Composition order (outermost first)
//!
//! ```text
//! RateLimitedTool
//!   └─ PathGuardedTool
//!        └─ <concrete tool>
//! ```
//!
//! # Example
//!
//! ```rust,ignore
//! let tool = RateLimitedTool::new(
//!     PathGuardedTool::new(ShellTool::new(security.clone(), runtime), security.clone()),
//!     security.clone(),
//! );
//! ```

use super::traits::{Tool, ToolResult};
use crate::security::SecurityPolicy;
use async_trait::async_trait;
use std::sync::Arc;

/// Type alias for a path-extraction closure used by [`PathGuardedTool`].
type PathExtractor = dyn Fn(&serde_json::Value) -> Option<String> + Send + Sync;

// ── RateLimitedTool ───────────────────────────────────────────────────────────

/// Wraps any [`Tool`] and enforces the [`SecurityPolicy`] rate limit.
///
/// Replaces the repeated `is_rate_limited()` / `record_action()` guard blocks
/// previously inlined in every tool's `execute` method (~30 files, ~50 call
/// sites).  The inner tool receives the call only when the rate limit allows it.
pub struct RateLimitedTool<T: Tool> {
    inner: T,
    security: Arc<SecurityPolicy>,
}

impl<T: Tool> RateLimitedTool<T> {
    pub fn new(inner: T, security: Arc<SecurityPolicy>) -> Self {
        Self { inner, security }
    }
}

#[async_trait]
impl<T: Tool> Tool for RateLimitedTool<T> {
    fn name(&self) -> &str {
        self.inner.name()
    }

    fn description(&self) -> &str {
        self.inner.description()
    }

    fn parameters_schema(&self) -> serde_json::Value {
        self.inner.parameters_schema()
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        if self.security.is_rate_limited() {
            return Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some("Rate limit exceeded: too many actions in the last hour".into()),
            });
        }

        if !self.security.record_action() {
            return Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some("Rate limit exceeded: action budget exhausted".into()),
            });
        }

        self.inner.execute(args).await
    }
}

// ── PathGuardedTool ───────────────────────────────────────────────────────────

/// Wraps any [`Tool`] and blocks calls whose arguments contain a forbidden path.
///
/// Replaces the `forbidden_path_argument()` guard blocks previously inlined in
/// tools that accept a path-like argument (`shell`, `file_read`, `file_write`,
/// `file_edit`, `pdf_read`, `content_search`, `glob_search`, `image_info`).
///
/// Path extraction is argument-name-driven: the wrapper inspects the `"path"`,
/// `"command"`, `"pattern"`, and `"query"` fields of the JSON argument object.
/// Tools whose path argument uses a different field name can pass a custom
/// extractor at construction via [`PathGuardedTool::with_extractor`].
pub struct PathGuardedTool<T: Tool> {
    inner: T,
    security: Arc<SecurityPolicy>,
    /// Optional override: extract a path string from the args JSON.
    extractor: Option<Box<PathExtractor>>,
}

impl<T: Tool> PathGuardedTool<T> {
    pub fn new(inner: T, security: Arc<SecurityPolicy>) -> Self {
        Self {
            inner,
            security,
            extractor: None,
        }
    }

    /// Supply a custom path-extraction closure for tools with non-standard arg names.
    pub fn with_extractor<F>(mut self, f: F) -> Self
    where
        F: Fn(&serde_json::Value) -> Option<String> + Send + Sync + 'static,
    {
        self.extractor = Some(Box::new(f));
        self
    }

    fn extract_path_string(&self, args: &serde_json::Value) -> Option<String> {
        if let Some(ref f) = self.extractor {
            return f(args);
        }
        // Default: check common argument names used across ZeroClaw tools.
        for field in &["path", "command", "pattern", "query", "file"] {
            if let Some(s) = args.get(field).and_then(|v| v.as_str()) {
                return Some(s.to_string());
            }
        }
        None
    }
}

#[async_trait]
impl<T: Tool> Tool for PathGuardedTool<T> {
    fn name(&self) -> &str {
        self.inner.name()
    }

    fn description(&self) -> &str {
        self.inner.description()
    }

    fn parameters_schema(&self) -> serde_json::Value {
        self.inner.parameters_schema()
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        if let Some(arg) = self.extract_path_string(&args) {
            // For shell command arguments, use the full token-aware scanner.
            // For plain path values (e.g. "path" or custom extractor), fall back
            // to the direct path check.
            let blocked = if self.extractor.is_none()
                && args.get("command").and_then(|v| v.as_str()).is_some()
            {
                self.security.forbidden_path_argument(&arg)
            } else if !self.security.is_path_allowed(&arg) {
                Some(arg.clone())
            } else {
                None
            };

            if let Some(path) = blocked {
                return Ok(ToolResult {
                    success: false,
                    output: String::new(),
                    error: Some(format!("Path blocked by security policy: {path}")),
                });
            }
        }

        self.inner.execute(args).await
    }
}

// ── Tests ─────────────────────────────────────────────────────────────────────


#[cfg(test)]
#[path = "wrappers.test.rs"]
mod tests;
