//! OpenAI-compatible chat completions client. See API.md §7.
//!
//! Endpoint: `POST {backend_url}/v1/chat/completions`.
//! Bearer: `{agent_id}`.
//! SSE response, OpenAI `data:` frame shape.

use std::pin::Pin;
use std::sync::Arc;

use futures::Stream;
use serde::{Deserialize, Serialize};

use crate::config::RuntimeConfig;
use crate::error::Result;

/// One event emitted by the SSE parser for the turn loop to consume.
#[derive(Debug, Clone, PartialEq, Eq)]
pub enum ChatEvent {
    /// A chunk of assistant text.
    ContentDelta { text: String },
    /// The start of a tool call — carries the name + call id. Further
    /// `arguments` string chunks arrive as `ToolCallArgsDelta`.
    ToolCallStart {
        index: usize,
        id: String,
        name: String,
    },
    /// A chunk of the `function.arguments` JSON string for the tool call
    /// at `index`. Chunks are concatenated in arrival order.
    ToolCallArgsDelta { index: usize, args_chunk: String },
    /// The stream's terminal `finish_reason`.
    Finish { reason: FinishReason },
    /// Malformed chunk or transport error captured inline in the stream.
    Error { message: String },
}

/// OpenAI `finish_reason` values we care about.
#[derive(Debug, Clone, PartialEq, Eq)]
pub enum FinishReason {
    Stop,
    ToolCalls,
    Length,
    Other(String),
}

/// A role-tagged chat message, OpenAI shape. `tool_calls` is populated on
/// assistant turns that issued calls; `tool_call_id` on tool-result turns.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ChatMessage {
    pub role: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub content: Option<String>,
    #[serde(default, skip_serializing_if = "Vec::is_empty")]
    pub tool_calls: Vec<ChatToolCall>,
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub tool_call_id: Option<String>,
}

/// One tool call as carried in the assistant message history.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ChatToolCall {
    pub id: String,
    #[serde(rename = "type")]
    pub kind: String,
    pub function: ChatToolCallFunction,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ChatToolCallFunction {
    pub name: String,
    pub arguments: String,
}

/// Tool schema entry as it goes over the wire in the `tools` array.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ChatToolSchema {
    #[serde(rename = "type")]
    pub kind: String,
    pub function: ChatToolSchemaFunction,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ChatToolSchemaFunction {
    pub name: String,
    pub description: String,
    pub parameters: serde_json::Value,
}

/// Handle to the backend's OpenAI-compatible chat completions endpoint.
pub struct LlmClient {
    cfg: Arc<RuntimeConfig>,
}

impl LlmClient {
    /// Construct a client bound to this agent's backend URL + token.
    pub fn new(cfg: Arc<RuntimeConfig>) -> Self {
        Self { cfg }
    }

    /// Backend URL in use. Helper for tests + debugging.
    pub fn backend_url(&self) -> &str {
        &self.cfg.backend_url
    }

    /// POST `/v1/chat/completions` with `stream: true` and an OpenAI-shape
    /// `tools` array. Returns an async stream of `ChatEvent`s.
    ///
    /// Phase 3: build the body, send the request, parse the SSE stream
    /// via `eventsource-stream`, map each `data: {...}` into a
    /// `ChatEvent`. `[DONE]` terminates cleanly without emitting an event.
    pub async fn chat_stream(
        &self,
        messages: Vec<ChatMessage>,
        tools: Vec<ChatToolSchema>,
    ) -> Result<Pin<Box<dyn Stream<Item = ChatEvent> + Send>>> {
        let _ = (messages, tools);
        todo!("Phase 3: POST /v1/chat/completions and return the SSE-parsed stream")
    }
}
