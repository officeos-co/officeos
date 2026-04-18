// Rust guideline compliant 2026-02-21

//! Forward agent log entries to the backend.
//!
//! Endpoint: `POST /api/agents/me/logs`. See API.md §19.

use crate::error::{Error, Result};

/// HTTP client for forwarding log entries to the backend.
///
/// Fire-and-forget callers should `.ok()` the result; critical
/// callers (bootstrap message) should propagate errors.
#[derive(Debug)]
pub struct LogClient {
    backend_url: String,
    token: String,
    http: reqwest::Client,
}

impl LogClient {
    /// Create a new log client bound to the given backend.
    #[must_use]
    pub fn new(backend_url: String, token: String) -> Self {
        Self {
            backend_url,
            token,
            http: reqwest::Client::new(),
        }
    }

    /// Forward a single log entry to the backend.
    ///
    /// # Errors
    ///
    /// Returns `Error::Llm` on transport failure or non-success status.
    pub async fn forward(
        &self,
        log_type: &str,
        content: &str,
        correlation_id: Option<String>,
    ) -> Result<()> {
        let url = format!("{}/api/agents/me/logs", self.backend_url);

        let body = serde_json::json!({
            "type": log_type,
            "content": content,
            "correlationId": correlation_id,
        });

        let resp = self
            .http
            .post(&url)
            .header("Authorization", format!("Bearer {}", self.token))
            .json(&body)
            .send()
            .await
            .map_err(|e| Error::Llm(format!("log forward failed: {e}")))?;

        if !resp.status().is_success() {
            return Err(Error::Llm(format!(
                "log forward returned {}",
                resp.status()
            )));
        }

        Ok(())
    }
}
