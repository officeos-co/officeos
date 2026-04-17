//! OpenAI-compatible chat completions client. See API.md §7.
//!
//! Endpoint: `POST {backend_url}/v1/chat/completions`.
//! Bearer: `{agent_id}`.
//! SSE response, OpenAI `data:` frame shape.

use std::pin::Pin;
use std::sync::Arc;

use eventsource_stream::Eventsource;
use futures::stream::StreamExt;
use futures::Stream;
use serde::{Deserialize, Serialize};

use crate::config::RuntimeConfig;
use crate::error::{Error, Result};

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
        let url = format!("{}/v1/chat/completions", self.cfg.backend_url);

        let mut body = serde_json::json!({
            "model": "default",
            "messages": messages,
            "stream": true,
        });
        if !tools.is_empty() {
            body["tools"] = serde_json::to_value(&tools)
                .map_err(|e| Error::Llm(format!("serialize tools: {e}")))?;
        }

        let client = reqwest::Client::new();
        let resp = client
            .post(&url)
            .header("Authorization", format!("Bearer {}", self.cfg.backend_token))
            .json(&body)
            .send()
            .await
            .map_err(|e| Error::Llm(format!("request failed: {e}")))?;

        if !resp.status().is_success() {
            let status = resp.status();
            let text = resp.text().await.unwrap_or_default();
            return Err(Error::Llm(format!(
                "llm HTTP {status}: {text}"
            )));
        }

        let byte_stream = resp.bytes_stream();
        let event_stream = byte_stream.eventsource();

        let mapped = event_stream.filter_map(|result| async move {
            match result {
                Ok(event) => {
                    let data = event.data;
                    if data == "[DONE]" {
                        return None;
                    }
                    Some(parse_sse_chunk(&data))
                }
                Err(e) => Some(ChatEvent::Error {
                    message: format!("SSE error: {e}"),
                }),
            }
        });

        Ok(Box::pin(mapped))
    }
}

/// Deserialization structs for OpenAI streaming chunks.
#[derive(Deserialize)]
struct SseChunk {
    choices: Vec<SseChoice>,
}

#[derive(Deserialize)]
struct SseChoice {
    delta: SseDelta,
    #[serde(default)]
    finish_reason: Option<String>,
}

#[derive(Deserialize)]
struct SseDelta {
    #[serde(default)]
    content: Option<String>,
    #[serde(default)]
    tool_calls: Option<Vec<SseToolCallDelta>>,
}

#[derive(Deserialize)]
struct SseToolCallDelta {
    index: usize,
    #[serde(default)]
    id: Option<String>,
    #[serde(default)]
    function: Option<SseFunctionDelta>,
}

#[derive(Deserialize)]
struct SseFunctionDelta {
    #[serde(default)]
    name: Option<String>,
    #[serde(default)]
    arguments: Option<String>,
}

/// Parse a single SSE `data: {...}` JSON chunk into a `ChatEvent`.
fn parse_sse_chunk(data: &str) -> ChatEvent {
    let chunk: SseChunk = match serde_json::from_str(data) {
        Ok(c) => c,
        Err(e) => {
            return ChatEvent::Error {
                message: format!("parse chunk: {e}"),
            };
        }
    };

    let Some(choice) = chunk.choices.first() else {
        return ChatEvent::Error {
            message: "empty choices array".to_string(),
        };
    };

    // Check finish_reason first.
    if let Some(reason) = &choice.finish_reason {
        let fr = match reason.as_str() {
            "stop" => FinishReason::Stop,
            "tool_calls" => FinishReason::ToolCalls,
            "length" => FinishReason::Length,
            other => FinishReason::Other(other.to_string()),
        };
        return ChatEvent::Finish { reason: fr };
    }

    // Check tool_calls delta.
    if let Some(tool_calls) = &choice.delta.tool_calls {
        if let Some(tc) = tool_calls.first() {
            if let Some(func) = &tc.function {
                if let Some(name) = &func.name {
                    // This is a ToolCallStart — has id + name.
                    return ChatEvent::ToolCallStart {
                        index: tc.index,
                        id: tc.id.clone().unwrap_or_default(),
                        name: name.clone(),
                    };
                }
                if let Some(args) = &func.arguments {
                    return ChatEvent::ToolCallArgsDelta {
                        index: tc.index,
                        args_chunk: args.clone(),
                    };
                }
            }
        }
    }

    // Check content delta.
    if let Some(content) = &choice.delta.content {
        return ChatEvent::ContentDelta {
            text: content.clone(),
        };
    }

    // Empty delta (e.g. role-only chunk) — skip by returning an error we can filter,
    // but since we use filter_map only for [DONE], let's just return a content delta
    // with empty string. Actually, better to return an error that's benign.
    // In practice OpenAI sends empty deltas for the initial role chunk.
    ChatEvent::ContentDelta {
        text: String::new(),
    }
}
