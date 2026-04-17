//! `ask_user` — prompt the operator and wait for a reply. See API.md §10.5.
//!
//! Uses `tokio::sync::oneshot` channels bridged by the gateway. The gateway
//! calls `AskUserBridge::set_response()` when the operator replies; this tool
//! blocks on the receiver side with a timeout.

use async_trait::async_trait;
use serde_json::json;
use std::sync::Arc;
use std::time::Duration;
use tokio::sync::Mutex;

use super::traits::{Tool, ToolResult};

const DEFAULT_TIMEOUT_SECS: u64 = 300;

/// Bridge between the `ask_user` tool and the gateway WebSocket handler.
///
/// When the tool fires, it stores a `oneshot::Sender` in `pending`. The
/// gateway reads the prompt via `take_pending()` and later delivers the
/// user's reply via `set_response()`.
pub struct AskUserBridge {
    pending: Mutex<Option<PendingQuestion>>,
}

struct PendingQuestion {
    question: String,
    responder: tokio::sync::oneshot::Sender<String>,
}

impl Default for AskUserBridge {
    fn default() -> Self { Self::new() }
}

impl AskUserBridge {
    pub fn new() -> Self {
        Self {
            pending: Mutex::new(None),
        }
    }

    /// Called by the gateway to retrieve the pending question (if any).
    pub async fn take_pending(&self) -> Option<String> {
        let lock = self.pending.lock().await;
        lock.as_ref().map(|p| p.question.clone())
    }

    /// Called by the gateway when the operator sends a reply.
    pub async fn set_response(&self, response: String) {
        let pending = self.pending.lock().await.take();
        if let Some(p) = pending {
            // Ignore send error — tool may have timed out already.
            let _ = p.responder.send(response);
        }
    }
}

pub struct AskUserTool {
    bridge: Arc<AskUserBridge>,
}

impl AskUserTool {
    pub fn new(bridge: Arc<AskUserBridge>) -> Self {
        Self { bridge }
    }
}

#[async_trait]
impl Tool for AskUserTool {
    fn name(&self) -> &str {
        "ask_user"
    }

    fn description(&self) -> &str {
        "Ask the connected operator a question and wait for their reply. Returns the response text."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        json!({
            "type": "object",
            "properties": {
                "question": {"type": "string", "description": "The question to ask"},
                "choices": {"type": "array", "items": {"type": "string"}, "description": "Optional list of choices"},
                "timeout_secs": {"type": "integer", "description": "Seconds to wait (default: 300)"}
            },
            "required": ["question"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let question = args
            .get("question")
            .and_then(|v| v.as_str())
            .map(|s| s.trim())
            .filter(|s| !s.is_empty())
            .ok_or_else(|| anyhow::anyhow!("Missing 'question' parameter"))?
            .to_string();

        let timeout_secs = args
            .get("timeout_secs")
            .and_then(|v| v.as_u64())
            .unwrap_or(DEFAULT_TIMEOUT_SECS);

        let (tx, rx) = tokio::sync::oneshot::channel::<String>();

        {
            let mut pending = self.bridge.pending.lock().await;
            *pending = Some(PendingQuestion {
                question: question.clone(),
                responder: tx,
            });
        }

        match tokio::time::timeout(Duration::from_secs(timeout_secs), rx).await {
            Ok(Ok(response)) => Ok(ToolResult {
                success: true,
                output: response,
                error: None,
            }),
            Ok(Err(_)) => Ok(ToolResult {
                success: false,
                output: "TIMEOUT".to_string(),
                error: Some("Bridge channel closed before receiving a response".into()),
            }),
            Err(_) => Ok(ToolResult {
                success: false,
                output: "TIMEOUT".to_string(),
                error: Some(format!(
                    "No response received within {timeout_secs} seconds"
                )),
            }),
        }
    }
}
