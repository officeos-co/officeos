//! The agent turn loop — drives LLM streaming, tool dispatch, WS events.
//! See API.md §6.

use crate::agent::history::ConversationHistory;
use crate::agent::loop_detector::{LoopDetectionResult, LoopDetector};
use crate::agent::prompt::{compose_system_prompt, PromptContext};
use crate::config::RuntimeConfig;
use crate::gateway::protocol::WsOutbound;
use crate::llm::{ChatEvent, ChatMessage, ChatToolCall, ChatToolCallFunction, LlmClient};
use crate::tools::ToolRegistry;

use std::sync::Arc;

use futures::StreamExt;
use tokio::sync::mpsc;

/// Max tokens before pruning kicks in.
const MAX_TOKENS: usize = 8192;
/// Number of recent messages to protect from pruning.
const KEEP_RECENT: usize = 4;

/// Accumulates streaming tool call chunks into complete calls.
struct ToolCallAccumulator {
    id: String,
    name: String,
    arguments: String,
}

/// Run a single turn: stream LLM response, dispatch tool calls, loop
/// until the assistant returns no tool calls or a termination condition.
pub(crate) async fn run_turn(
    cfg: &Arc<RuntimeConfig>,
    history: &mut ConversationHistory,
    llm: &LlmClient,
    registry: &ToolRegistry,
    turn_id: &str,
    ws_tx: &mpsc::UnboundedSender<WsOutbound>,
) -> crate::error::Result<()> {
    let mut loop_detector = LoopDetector::new();

    loop {
        // Compose system prompt fresh each iteration.
        let tools_arc = registry.tools();
        let ctx = PromptContext {
            memory_dir: &cfg.memory_dir,
            tools: &tools_arc,
            skills: &cfg.skills,
            system_prompt: &cfg.system_prompt,
        };
        let system_prompt = compose_system_prompt(&ctx);

        // Build messages: system + history.
        let mut messages = vec![ChatMessage {
            role: "system".to_string(),
            content: Some(system_prompt),
            tool_calls: vec![],
            tool_call_id: None,
        }];
        messages.extend(history.messages().iter().cloned());

        // Prune if over token budget.
        history.prune(MAX_TOKENS, KEEP_RECENT);

        // Get tool specs for LLM.
        let tool_schemas = registry.chat_tool_schemas();

        // Stream LLM response.
        tracing::info!(turn_id, "starting LLM stream");
        let mut stream = llm.chat_stream(messages, tool_schemas).await?;

        let mut assistant_text = String::new();
        let mut accumulators: Vec<ToolCallAccumulator> = Vec::new();
        let mut had_error = false;

        while let Some(event) = stream.next().await {
            match event {
                ChatEvent::ContentDelta { text } => {
                    if !text.is_empty() {
                        let _ = ws_tx.send(WsOutbound::AssistantDelta {
                            turn_id: turn_id.to_string(),
                            text: text.clone(),
                        });
                        assistant_text.push_str(&text);
                    }
                }
                ChatEvent::ToolCallStart { index, id, name } => {
                    // Ensure vec is large enough.
                    while accumulators.len() <= index {
                        accumulators.push(ToolCallAccumulator {
                            id: String::new(),
                            name: String::new(),
                            arguments: String::new(),
                        });
                    }
                    accumulators[index].id = id;
                    accumulators[index].name = name;
                }
                ChatEvent::ToolCallArgsDelta { index, args_chunk } => {
                    if index < accumulators.len() {
                        accumulators[index].arguments.push_str(&args_chunk);
                    }
                }
                ChatEvent::Finish { reason: _ } => {
                    break;
                }
                ChatEvent::Error { message } => {
                    let _ = ws_tx.send(WsOutbound::Error {
                        turn_id: Some(turn_id.to_string()),
                        code: "LLM".to_string(),
                        message,
                    });
                    had_error = true;
                    break;
                }
            }
        }

        if had_error {
            tracing::warn!(turn_id, "LLM stream error");
            let _ = ws_tx.send(WsOutbound::TurnComplete {
                turn_id: turn_id.to_string(),
                cancelled: false,
            });
            return Ok(());
        }

        // Build tool_calls for history.
        let tool_calls: Vec<ChatToolCall> = accumulators
            .iter()
            .filter(|a| !a.name.is_empty())
            .map(|a| ChatToolCall {
                id: a.id.clone(),
                kind: "function".to_string(),
                function: ChatToolCallFunction {
                    name: a.name.clone(),
                    arguments: a.arguments.clone(),
                },
            })
            .collect();

        // Push assistant message to history.
        history.push(ChatMessage {
            role: "assistant".to_string(),
            content: if assistant_text.is_empty() {
                None
            } else {
                Some(assistant_text)
            },
            tool_calls: tool_calls.clone(),
            tool_call_id: None,
        });

        tracing::info!(
            turn_id,
            tool_calls = tool_calls.len(),
            "LLM stream complete"
        );

        // No tool calls → turn complete.
        if tool_calls.is_empty() {
            break;
        }

        // Dispatch each tool call.
        for tc in &tool_calls {
            let args: serde_json::Value = serde_json::from_str(&tc.function.arguments)
                .unwrap_or(serde_json::Value::Object(serde_json::Map::new()));

            let _ = ws_tx.send(WsOutbound::ToolCallStart {
                turn_id: turn_id.to_string(),
                call_id: tc.id.clone(),
                tool: tc.function.name.clone(),
                args: args.clone(),
            });

            // Dispatch.
            tracing::info!(turn_id, tool = %tc.function.name, call_id = %tc.id, "dispatching tool");
            let result = registry.dispatch(&tc.function.name, args.clone()).await;
            let tool_result = match result {
                Ok(tr) => tr,
                Err(e) => crate::tools::ToolResult {
                    success: false,
                    output: String::new(),
                    error: Some(format!("{e}")),
                },
            };

            tracing::info!(turn_id, tool = %tc.function.name, success = tool_result.success, "tool result");

            // Loop detection.
            let detection = loop_detector.record(&tc.function.name, &args, &tool_result.output);

            let _ = ws_tx.send(WsOutbound::ToolCallResult {
                turn_id: turn_id.to_string(),
                call_id: tc.id.clone(),
                tool: tc.function.name.clone(),
                success: tool_result.success,
                output: tool_result.output.clone(),
                error: tool_result.error.clone(),
            });

            // Push tool result to history.
            history.push(ChatMessage {
                role: "tool".to_string(),
                content: Some(if tool_result.success {
                    tool_result.output.clone()
                } else {
                    tool_result
                        .error
                        .clone()
                        .unwrap_or_else(|| tool_result.output.clone())
                }),
                tool_calls: vec![],
                tool_call_id: Some(tc.id.clone()),
            });

            // Handle loop detection results.
            match detection {
                LoopDetectionResult::Break(msg) => {
                    tracing::warn!(turn_id, "loop detected: {msg}");
                    let _ = ws_tx.send(WsOutbound::Error {
                        turn_id: Some(turn_id.to_string()),
                        code: "LOOP".to_string(),
                        message: msg,
                    });
                    let _ = ws_tx.send(WsOutbound::TurnComplete {
                        turn_id: turn_id.to_string(),
                        cancelled: false,
                    });
                    return Ok(());
                }
                LoopDetectionResult::Block(msg) => {
                    tracing::warn!(turn_id, "loop detected: {msg}");
                    // Replace last tool result in history with block message.
                    if let Some(last) = history.messages_mut().last_mut() {
                        last.content = Some(format!("BLOCKED: {msg}"));
                    }
                }
                LoopDetectionResult::Warning(_msg) => {
                    // Continue — the model will see the warning in tool output context.
                }
                LoopDetectionResult::Ok => {}
            }
        }

        // Continue the loop — LLM will see tool results and respond.
    }

    tracing::info!(turn_id, "turn loop finished");
    let _ = ws_tx.send(WsOutbound::TurnComplete {
        turn_id: turn_id.to_string(),
        cancelled: false,
    });
    Ok(())
}
