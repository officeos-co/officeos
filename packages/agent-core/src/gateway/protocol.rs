//! WebSocket wire protocol — client/server message enums. See API.md §8.

use serde::{Deserialize, Serialize};

/// Client → Server messages.
#[derive(Debug, Clone, Deserialize)]
#[serde(tag = "type", rename_all = "snake_case")]
pub enum WsInbound {
    /// Start a new turn.
    UserMessage {
        text: String,
        #[serde(default)]
        id: Option<String>,
    },
    /// Cancel the current turn.
    Cancel {
        #[serde(default)]
        id: Option<String>,
    },
}

/// Server → Client messages.
#[derive(Debug, Clone, Serialize)]
#[serde(tag = "type", rename_all = "snake_case")]
pub enum WsOutbound {
    /// Incremental assistant text.
    AssistantDelta { turn_id: String, text: String },
    /// A tool is about to run.
    ToolCallStart {
        turn_id: String,
        call_id: String,
        tool: String,
        args: serde_json::Value,
    },
    /// Tool finished.
    ToolCallResult {
        turn_id: String,
        call_id: String,
        tool: String,
        success: bool,
        output: String,
        #[serde(skip_serializing_if = "Option::is_none")]
        error: Option<String>,
    },
    /// End of turn.
    TurnComplete { turn_id: String, cancelled: bool },
    /// Runtime error.
    Error {
        #[serde(skip_serializing_if = "Option::is_none")]
        turn_id: Option<String>,
        code: String,
        message: String,
    },
}
