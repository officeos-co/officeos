// Rust guideline compliant 2026-02-21

//! WebSocket wire protocol — client/server message enums.
//!
//! See API.md §8.

use serde::{Deserialize, Serialize};

/// Client → Server messages.
#[derive(Debug, Clone, Deserialize)]
#[serde(tag = "type", rename_all = "snake_case")]
pub enum WsInbound {
    /// Start a new turn.
    UserMessage {
        /// The user's message text.
        text: String,
        /// Optional client-supplied turn identifier.
        #[serde(default)]
        id: Option<String>,
    },
    /// Cancel the current turn.
    Cancel {
        /// Turn identifier to cancel.
        #[serde(default)]
        id: Option<String>,
    },
}

/// Server → Client messages.
#[derive(Debug, Clone, Serialize)]
#[serde(tag = "type", rename_all = "snake_case")]
pub enum WsOutbound {
    /// Incremental assistant text.
    AssistantDelta {
        /// Turn this delta belongs to.
        turn_id: String,
        /// Text fragment.
        text: String,
    },
    /// A tool is about to run.
    ToolCallStart {
        /// Turn this call belongs to.
        turn_id: String,
        /// Unique call identifier.
        call_id: String,
        /// Tool name.
        tool: String,
        /// Tool arguments.
        args: serde_json::Value,
    },
    /// Tool finished.
    ToolCallResult {
        /// Turn this result belongs to.
        turn_id: String,
        /// Call identifier this result answers.
        call_id: String,
        /// Tool name.
        tool: String,
        /// Whether the tool succeeded.
        success: bool,
        /// Tool output text.
        output: String,
        /// Error message, if any.
        #[serde(skip_serializing_if = "Option::is_none")]
        error: Option<String>,
    },
    /// End of turn.
    TurnComplete {
        /// Turn identifier.
        turn_id: String,
        /// Whether the turn was cancelled by the client.
        cancelled: bool,
    },
    /// Runtime error.
    Error {
        /// Turn identifier, if available.
        #[serde(skip_serializing_if = "Option::is_none")]
        turn_id: Option<String>,
        /// Error classification code.
        code: String,
        /// Human-readable error description.
        message: String,
    },
}
