use super::*;
use crate::providers::traits::{ChatMessage, Provider};

#[test]
fn capabilities_report_vision_support() {
    let provider = OpenRouterProvider::new(Some("openrouter-test-credential"), None);
    let caps = <OpenRouterProvider as Provider>::capabilities(&provider);
    assert!(caps.native_tool_calling);
    assert!(caps.vision);
}

#[test]
fn creates_with_key() {
    let provider = OpenRouterProvider::new(Some("openrouter-test-credential"), None);
    assert_eq!(
        provider.credential.as_deref(),
        Some("openrouter-test-credential")
    );
}

#[test]
fn creates_without_key() {
    let provider = OpenRouterProvider::new(None, None);
    assert!(provider.credential.is_none());
}

#[test]
fn uses_configured_timeout_when_provided() {
    let provider = OpenRouterProvider::new(Some("openrouter-test-credential"), Some(1200));
    assert_eq!(provider.timeout_secs, 1200);
}

#[test]
fn falls_back_to_default_timeout_for_zero() {
    let provider = OpenRouterProvider::new(Some("openrouter-test-credential"), Some(0));
    assert_eq!(provider.timeout_secs, DEFAULT_OPENROUTER_TIMEOUT_SECS);
}

#[tokio::test]
async fn warmup_without_key_is_noop() {
    let provider = OpenRouterProvider::new(None, None);
    let result = provider.warmup().await;
    assert!(result.is_ok());
}

#[tokio::test]
async fn chat_with_system_fails_without_key() {
    let provider = OpenRouterProvider::new(None, None);
    let result = provider
        .chat_with_system(Some("system"), "hello", "openai/gpt-4o", 0.2)
        .await;

    assert!(result.is_err());
    assert!(result.unwrap_err().to_string().contains("API key not set"));
}

#[tokio::test]
async fn chat_with_history_fails_without_key() {
    let provider = OpenRouterProvider::new(None, None);
    let messages = vec![
        ChatMessage {
            role: "system".into(),
            content: "be concise".into(),
        },
        ChatMessage {
            role: "user".into(),
            content: "hello".into(),
        },
    ];

    let result = provider
        .chat_with_history(&messages, "anthropic/claude-sonnet-4", 0.7)
        .await;

    assert!(result.is_err());
    assert!(result.unwrap_err().to_string().contains("API key not set"));
}

#[test]
fn chat_request_serializes_with_system_and_user() {
    let request = ChatRequest {
        model: "anthropic/claude-sonnet-4".into(),
        messages: vec![
            Message {
                role: "system".into(),
                content: MessageContent::Text("You are helpful".into()),
            },
            Message {
                role: "user".into(),
                content: MessageContent::Text("Summarize this".into()),
            },
        ],
        temperature: 0.5,
        max_tokens: None,
    };

    let json = serde_json::to_string(&request).unwrap();

    assert!(json.contains("anthropic/claude-sonnet-4"));
    assert!(json.contains("\"role\":\"system\""));
    assert!(json.contains("\"role\":\"user\""));
    assert!(json.contains("\"temperature\":0.5"));
}

#[test]
fn chat_request_serializes_history_messages() {
    let messages = [
        ChatMessage {
            role: "assistant".into(),
            content: "Previous answer".into(),
        },
        ChatMessage {
            role: "user".into(),
            content: "Follow-up".into(),
        },
    ];

    let request = ChatRequest {
        model: "google/gemini-2.5-pro".into(),
        messages: messages
            .iter()
            .map(|msg| Message {
                role: msg.role.clone(),
                content: MessageContent::Text(msg.content.clone()),
            })
            .collect(),
        temperature: 0.0,
        max_tokens: None,
    };

    let json = serde_json::to_string(&request).unwrap();
    assert!(json.contains("\"role\":\"assistant\""));
    assert!(json.contains("\"role\":\"user\""));
    assert!(json.contains("google/gemini-2.5-pro"));
}

#[test]
fn response_deserializes_single_choice() {
    let json = r#"{"choices":[{"message":{"content":"Hi from OpenRouter"}}]}"#;

    let response: ApiChatResponse = serde_json::from_str(json).unwrap();

    assert_eq!(response.choices.len(), 1);
    assert_eq!(response.choices[0].message.content, "Hi from OpenRouter");
}

#[test]
fn response_deserializes_empty_choices() {
    let json = r#"{"choices":[]}"#;

    let response: ApiChatResponse = serde_json::from_str(json).unwrap();

    assert!(response.choices.is_empty());
}

#[test]
fn parse_chat_response_body_reports_sanitized_snippet() {
    let body = r#"{"choices":"invalid","api_key":"sk-test-secret-value"}"#;
    let err = OpenRouterProvider::parse_response_body::<ApiChatResponse>(
        "OpenRouter",
        body,
        "chat-completions",
    )
    .expect_err("payload should fail");
    let msg = err.to_string();

    assert!(msg.contains("OpenRouter API returned an unexpected chat-completions payload"));
    assert!(msg.contains("body="));
    assert!(msg.contains("[REDACTED]"));
    assert!(!msg.contains("sk-test-secret-value"));
}

#[test]
fn parse_native_response_body_reports_sanitized_snippet() {
    let body = r#"{"choices":123,"api_key":"sk-another-secret"}"#;
    let err = OpenRouterProvider::parse_response_body::<NativeChatResponse>(
        "OpenRouter",
        body,
        "native chat",
    )
    .expect_err("payload should fail");
    let msg = err.to_string();

    assert!(msg.contains("OpenRouter API returned an unexpected native chat payload"));
    assert!(msg.contains("body="));
    assert!(msg.contains("[REDACTED]"));
    assert!(!msg.contains("sk-another-secret"));
}

#[tokio::test]
async fn chat_with_tools_fails_without_key() {
    let provider = OpenRouterProvider::new(None, None);
    let messages = vec![ChatMessage {
        role: "user".into(),
        content: "What is the date?".into(),
    }];
    let tools = vec![serde_json::json!({
        "type": "function",
        "function": {
            "name": "shell",
            "description": "Run a shell command",
            "parameters": {"type": "object", "properties": {"command": {"type": "string"}}}
        }
    })];

    let result = provider
        .chat_with_tools(&messages, &tools, "deepseek/deepseek-chat", 0.5)
        .await;

    assert!(result.is_err());
    assert!(result.unwrap_err().to_string().contains("API key not set"));
}

#[test]
fn native_response_deserializes_with_tool_calls() {
    let json = r#"{
            "choices":[{
                "message":{
                    "content":null,
                    "tool_calls":[
                        {"id":"call_123","type":"function","function":{"name":"get_price","arguments":"{\"symbol\":\"BTC\"}"}}
                    ]
                }
            }]
        }"#;

    let response: NativeChatResponse = serde_json::from_str(json).unwrap();

    assert_eq!(response.choices.len(), 1);
    let message = &response.choices[0].message;
    assert!(message.content.is_none());
    let tool_calls = message.tool_calls.as_ref().unwrap();
    assert_eq!(tool_calls.len(), 1);
    assert_eq!(tool_calls[0].id.as_deref(), Some("call_123"));
    assert_eq!(tool_calls[0].function.name, "get_price");
    assert_eq!(tool_calls[0].function.arguments, "{\"symbol\":\"BTC\"}");
}

#[test]
fn native_response_deserializes_with_text_and_tool_calls() {
    let json = r#"{
            "choices":[{
                "message":{
                    "content":"I'll get that for you.",
                    "tool_calls":[
                        {"id":"call_456","type":"function","function":{"name":"shell","arguments":"{\"command\":\"date\"}"}}
                    ]
                }
            }]
        }"#;

    let response: NativeChatResponse = serde_json::from_str(json).unwrap();

    assert_eq!(response.choices.len(), 1);
    let message = &response.choices[0].message;
    assert_eq!(message.content.as_deref(), Some("I'll get that for you."));
    let tool_calls = message.tool_calls.as_ref().unwrap();
    assert_eq!(tool_calls.len(), 1);
    assert_eq!(tool_calls[0].function.name, "shell");
}

#[test]
fn parse_native_response_converts_to_chat_response() {
    let message = NativeResponseMessage {
        content: Some("Here you go.".into()),
        reasoning_content: None,
        tool_calls: Some(vec![NativeToolCall {
            id: Some("call_789".into()),
            kind: Some("function".into()),
            function: NativeFunctionCall {
                name: "file_read".into(),
                arguments: r#"{"path":"test.txt"}"#.into(),
            },
        }]),
    };

    let response = OpenRouterProvider::parse_native_response(message);

    assert_eq!(response.text.as_deref(), Some("Here you go."));
    assert_eq!(response.tool_calls.len(), 1);
    assert_eq!(response.tool_calls[0].id, "call_789");
    assert_eq!(response.tool_calls[0].name, "file_read");
}

#[test]
fn convert_messages_parses_assistant_tool_call_payload() {
    let messages = vec![ChatMessage {
            role: "assistant".into(),
            content: r#"{"content":"Using tool","tool_calls":[{"id":"call_abc","name":"shell","arguments":"{\"command\":\"pwd\"}"}]}"#
                .into(),
        }];

    let converted = OpenRouterProvider::convert_messages(&messages);
    assert_eq!(converted.len(), 1);
    assert_eq!(converted[0].role, "assistant");
    assert_eq!(
        converted[0]
            .content
            .as_ref()
            .and_then(|content| match content {
                MessageContent::Text(value) => Some(value.as_str()),
                MessageContent::Parts(_) => None,
            }),
        Some("Using tool")
    );

    let tool_calls = converted[0].tool_calls.as_ref().unwrap();
    assert_eq!(tool_calls.len(), 1);
    assert_eq!(tool_calls[0].id.as_deref(), Some("call_abc"));
    assert_eq!(tool_calls[0].function.name, "shell");
    assert_eq!(tool_calls[0].function.arguments, r#"{"command":"pwd"}"#);
}

#[test]
fn convert_messages_parses_tool_result_payload() {
    let messages = vec![ChatMessage {
        role: "tool".into(),
        content: r#"{"tool_call_id":"call_xyz","content":"done"}"#.into(),
    }];

    let converted = OpenRouterProvider::convert_messages(&messages);
    assert_eq!(converted.len(), 1);
    assert_eq!(converted[0].role, "tool");
    assert_eq!(converted[0].tool_call_id.as_deref(), Some("call_xyz"));
    assert_eq!(
        converted[0]
            .content
            .as_ref()
            .and_then(|content| match content {
                MessageContent::Text(value) => Some(value.as_str()),
                MessageContent::Parts(_) => None,
            }),
        Some("done")
    );
    assert!(converted[0].tool_calls.is_none());
}

#[test]
fn to_message_content_converts_image_markers_to_openai_parts() {
    let content = "Describe this\n\n[IMAGE:data:image/png;base64,abcd]";
    let value =
        serde_json::to_value(OpenRouterProvider::to_message_content("user", content)).unwrap();
    let parts = value
        .as_array()
        .expect("multimodal content should be an array");
    assert_eq!(parts.len(), 2);
    assert_eq!(parts[0]["type"], "text");
    assert_eq!(parts[0]["text"], "Describe this");
    assert_eq!(parts[1]["type"], "image_url");
    assert_eq!(parts[1]["image_url"]["url"], "data:image/png;base64,abcd");
}

#[test]
fn native_response_parses_usage() {
    let json = r#"{
            "choices": [{"message": {"content": "Hello"}}],
            "usage": {"prompt_tokens": 42, "completion_tokens": 15}
        }"#;
    let resp: NativeChatResponse = serde_json::from_str(json).unwrap();
    let usage = resp.usage.unwrap();
    assert_eq!(usage.prompt_tokens, Some(42));
    assert_eq!(usage.completion_tokens, Some(15));
}

#[test]
fn native_response_parses_without_usage() {
    let json = r#"{"choices": [{"message": {"content": "Hello"}}]}"#;
    let resp: NativeChatResponse = serde_json::from_str(json).unwrap();
    assert!(resp.usage.is_none());
}

// ═══════════════════════════════════════════════════════════════════════
// reasoning_content pass-through tests
// ═══════════════════════════════════════════════════════════════════════

#[test]
fn parse_native_response_captures_reasoning_content() {
    let message = NativeResponseMessage {
        content: Some("answer".into()),
        reasoning_content: Some("thinking step".into()),
        tool_calls: Some(vec![NativeToolCall {
            id: Some("call_1".into()),
            kind: Some("function".into()),
            function: NativeFunctionCall {
                name: "shell".into(),
                arguments: "{}".into(),
            },
        }]),
    };
    let parsed = OpenRouterProvider::parse_native_response(message);
    assert_eq!(parsed.reasoning_content.as_deref(), Some("thinking step"));
    assert_eq!(parsed.tool_calls.len(), 1);
}

#[test]
fn parse_native_response_none_reasoning_content_for_normal_model() {
    let message = NativeResponseMessage {
        content: Some("hello".into()),
        reasoning_content: None,
        tool_calls: None,
    };
    let parsed = OpenRouterProvider::parse_native_response(message);
    assert!(parsed.reasoning_content.is_none());
}

#[test]
fn native_response_deserializes_reasoning_content() {
    let json = r#"{
            "choices":[{
                "message":{
                    "content":"answer",
                    "reasoning_content":"deep thought",
                    "tool_calls":[
                        {"id":"call_r1","type":"function","function":{"name":"shell","arguments":"{}"}}
                    ]
                }
            }]
        }"#;
    let resp: NativeChatResponse = serde_json::from_str(json).unwrap();
    let message = &resp.choices[0].message;
    assert_eq!(message.reasoning_content.as_deref(), Some("deep thought"));
}

#[test]
fn convert_messages_round_trips_reasoning_content() {
    let history_json = serde_json::json!({
        "content": "I will check",
        "tool_calls": [{
            "id": "tc_1",
            "name": "shell",
            "arguments": "{}"
        }],
        "reasoning_content": "Let me think..."
    });

    let messages = vec![ChatMessage {
        role: "assistant".into(),
        content: history_json.to_string(),
    }];
    let native = OpenRouterProvider::convert_messages(&messages);
    assert_eq!(native.len(), 1);
    assert_eq!(
        native[0].reasoning_content.as_deref(),
        Some("Let me think...")
    );
}

#[test]
fn convert_messages_no_reasoning_content_when_absent() {
    let history_json = serde_json::json!({
        "content": "I will check",
        "tool_calls": [{
            "id": "tc_1",
            "name": "shell",
            "arguments": "{}"
        }]
    });

    let messages = vec![ChatMessage {
        role: "assistant".into(),
        content: history_json.to_string(),
    }];
    let native = OpenRouterProvider::convert_messages(&messages);
    assert_eq!(native.len(), 1);
    assert!(native[0].reasoning_content.is_none());
}

#[test]
fn native_message_omits_reasoning_content_when_none() {
    let msg = NativeMessage {
        role: "assistant".to_string(),
        content: Some(MessageContent::Text("hi".into())),
        tool_call_id: None,
        tool_calls: None,
        reasoning_content: None,
    };
    let json = serde_json::to_string(&msg).unwrap();
    assert!(!json.contains("reasoning_content"));
}

#[test]
fn native_message_includes_reasoning_content_when_some() {
    let msg = NativeMessage {
        role: "assistant".to_string(),
        content: Some(MessageContent::Text("hi".into())),
        tool_call_id: None,
        tool_calls: None,
        reasoning_content: Some("thinking...".to_string()),
    };
    let json = serde_json::to_string(&msg).unwrap();
    assert!(json.contains("reasoning_content"));
    assert!(json.contains("thinking..."));
}

// ═══════════════════════════════════════════════════════════════════════
// timeout_secs configuration tests
// ═══════════════════════════════════════════════════════════════════════

#[test]
fn default_timeout_is_120() {
    let provider = OpenRouterProvider::new(Some("key"), None);
    assert_eq!(provider.timeout_secs, 120);
}

#[test]
fn with_timeout_secs_overrides_default() {
    let provider = OpenRouterProvider::new(Some("key"), None).with_timeout_secs(300);
    assert_eq!(provider.timeout_secs, 300);
}

// ═══════════════════════════════════════════════════════════════════════
// tool name validation tests
// ═══════════════════════════════════════════════════════════════════════

#[test]
fn valid_openai_tool_names() {
    assert!(is_valid_openai_tool_name("shell"));
    assert!(is_valid_openai_tool_name("file_read"));
    assert!(is_valid_openai_tool_name("web-search"));
    assert!(is_valid_openai_tool_name("Tool123"));
    assert!(is_valid_openai_tool_name("a"));
}

#[test]
fn invalid_openai_tool_names() {
    assert!(!is_valid_openai_tool_name(""));
    assert!(!is_valid_openai_tool_name("mcp:server.tool"));
    assert!(!is_valid_openai_tool_name("node.js"));
    assert!(!is_valid_openai_tool_name("tool name"));
    assert!(!is_valid_openai_tool_name(
        "this_tool_name_is_way_too_long_and_exceeds_the_sixty_four_character_limit_xxxxx"
    ));
}

#[test]
fn convert_tools_skips_invalid_names() {
    use crate::tools::ToolSpec;

    let tools = vec![
        ToolSpec {
            name: "valid_tool".into(),
            description: "A valid tool".into(),
            parameters: serde_json::json!({"type": "object"}),
        },
        ToolSpec {
            name: "mcp:server.bad".into(),
            description: "Invalid name".into(),
            parameters: serde_json::json!({"type": "object"}),
        },
        ToolSpec {
            name: "another-valid".into(),
            description: "Also valid".into(),
            parameters: serde_json::json!({"type": "object"}),
        },
    ];

    let result = OpenRouterProvider::convert_tools(Some(&tools)).unwrap();
    assert_eq!(result.len(), 2);
    assert_eq!(result[0].function.name, "valid_tool");
    assert_eq!(result[1].function.name, "another-valid");
}

#[test]
fn convert_tools_returns_none_when_all_invalid() {
    use crate::tools::ToolSpec;

    let tools = vec![ToolSpec {
        name: "mcp:bad.name".into(),
        description: "Invalid".into(),
        parameters: serde_json::json!({"type": "object"}),
    }];

    assert!(OpenRouterProvider::convert_tools(Some(&tools)).is_none());
}
