//! `ask_user` — prompt the operator and wait for a reply. See API.md §10.5.

use async_trait::async_trait;

use super::traits::{Tool, ToolResult};

/// Handle injected at construction so `ask_user` can reach the active WS.
/// Phase 3: this becomes a real `AskUserBridge` from `gateway/ask_user_bridge.rs`.
pub struct AskUserBridge {
    _private: (),
}

pub struct AskUserTool {
    bridge: AskUserBridge,
}

impl AskUserTool {
    pub fn new(bridge: AskUserBridge) -> Self {
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
        serde_json::json!({
            "type": "object",
            "properties": {
                "question": {"type": "string"},
                "choices": {"type": "array", "items": {"type": "string"}},
                "timeout_secs": {"type": "integer", "default": 300}
            },
            "required": ["question"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let _ = (&self.bridge, args);
        todo!("Phase 3: send ask_user_prompt via WS bridge, await response")
    }
}
