use super::*;

#[test]
fn creates_with_key() {
    let p = OpenAiProvider::new(Some("openai-test-credential"));
    assert_eq!(p.credential.as_deref(), Some("openai-test-credential"));
}

#[test]
fn creates_without_key() {
    let p = OpenAiProvider::new(None);
    assert!(p.credential.is_none());
}

#[test]
fn creates_with_empty_key() {
    let p = OpenAiProvider::new(Some(""));
    assert_eq!(p.credential.as_deref(), Some(""));
}

#[tokio::test]
async fn chat_fails_without_key() {
    let p = OpenAiProvider::new(None);
    let result = p.chat_with_system(None, "hello", "gpt-4o", 0.7).await;
    assert!(result.is_err());
    assert!(result.unwrap_err().to_string().contains("API key not set"));
}

#[tokio::test]
async fn chat_with_system_fails_without_key() {
    let p = OpenAiProvider::new(None);
    let result = p
        .chat_with_system(Some("You are ZeroClaw"), "test", "gpt-4o", 0.5)
        .await;
    assert!(result.is_err());
}

#[test]
fn request_serializes_with_system_message() {
    let req = ChatRequest {
        model: "gpt-4o".to_string(),
        messages: vec![
            Message {
                role: "system".to_string(),
                content: "You are ZeroClaw".to_string(),
            },
            Message {
                role: "user".to_string(),
                content: "hello".to_string(),
            },
        ],
        temperature: 0.7,
        max_tokens: None,
    };
    let json = serde_json::to_string(&req).unwrap();
    assert!(json.contains("\"role\":\"system\""));
    assert!(json.contains("\"role\":\"user\""));
    assert!(json.contains("gpt-4o"));
}

#[test]
fn request_serializes_without_system() {
    let req = ChatRequest {
        model: "gpt-4o".to_string(),
        messages: vec![Message {
            role: "user".to_string(),
            content: "hello".to_string(),
        }],
        temperature: 0.0,
        max_tokens: None,
    };
    let json = serde_json::to_string(&req).unwrap();
    assert!(!json.contains("system"));
    assert!(json.contains("\"temperature\":0.0"));
}

#[test]
fn response_deserializes_single_choice() {
    let json = r#"{"choices":[{"message":{"content":"Hi!"}}]}"#;
    let resp: ChatResponse = serde_json::from_str(json).unwrap();
    assert_eq!(resp.choices.len(), 1);
    assert_eq!(resp.choices[0].message.effective_content(), "Hi!");
}

#[test]
fn response_deserializes_empty_choices() {
    let json = r#"{"choices":[]}"#;
    let resp: ChatResponse = serde_json::from_str(json).unwrap();
    assert!(resp.choices.is_empty());
}

#[test]
fn response_deserializes_multiple_choices() {
    let json = r#"{"choices":[{"message":{"content":"A"}},{"message":{"content":"B"}}]}"#;
    let resp: ChatResponse = serde_json::from_str(json).unwrap();
    assert_eq!(resp.choices.len(), 2);
    assert_eq!(resp.choices[0].message.effective_content(), "A");
}

#[test]
fn response_with_unicode() {
    let json = r#"{"choices":[{"message":{"content":"Hello \u03A9"}}]}"#;
    let resp: ChatResponse = serde_json::from_str(json).unwrap();
    assert_eq!(
        resp.choices[0].message.effective_content(),
        "Hello \u{03A9}"
    );
}

#[test]
fn response_with_long_content() {
    let long = "x".repeat(100_000);
    let json = format!(r#"{{"choices":[{{"message":{{"content":"{long}"}}}}]}}"#);
    let resp: ChatResponse = serde_json::from_str(&json).unwrap();
    assert_eq!(
        resp.choices[0].message.content.as_ref().unwrap().len(),
        100_000
    );
}

#[tokio::test]
async fn warmup_without_key_is_noop() {
    let provider = OpenAiProvider::new(None);
    let result = provider.warmup().await;
    assert!(result.is_ok());
}

// ----------------------------------------------------------
// Reasoning model fallback tests (reasoning_content)
// ----------------------------------------------------------

#[test]
fn reasoning_content_fallback_empty_content() {
    let json = r#"{"choices":[{"message":{"content":"","reasoning_content":"Thinking..."}}]}"#;
    let resp: ChatResponse = serde_json::from_str(json).unwrap();
    assert_eq!(resp.choices[0].message.effective_content(), "Thinking...");
}

#[test]
fn reasoning_content_fallback_null_content() {
    let json = r#"{"choices":[{"message":{"content":null,"reasoning_content":"Thinking..."}}]}"#;
    let resp: ChatResponse = serde_json::from_str(json).unwrap();
    assert_eq!(resp.choices[0].message.effective_content(), "Thinking...");
}

#[test]
fn reasoning_content_not_used_when_content_present() {
    let json = r#"{"choices":[{"message":{"content":"Hello","reasoning_content":"Ignored"}}]}"#;
    let resp: ChatResponse = serde_json::from_str(json).unwrap();
    assert_eq!(resp.choices[0].message.effective_content(), "Hello");
}

#[test]
fn native_response_reasoning_content_fallback() {
    let json = r#"{"choices":[{"message":{"content":"","reasoning_content":"Native thinking"}}]}"#;
    let resp: NativeChatResponse = serde_json::from_str(json).unwrap();
    let msg = &resp.choices[0].message;
    assert_eq!(msg.effective_content(), Some("Native thinking".to_string()));
}

#[test]
fn native_response_reasoning_content_ignored_when_content_present() {
    let json =
        r#"{"choices":[{"message":{"content":"Real answer","reasoning_content":"Ignored"}}]}"#;
    let resp: NativeChatResponse = serde_json::from_str(json).unwrap();
    let msg = &resp.choices[0].message;
    assert_eq!(msg.effective_content(), Some("Real answer".to_string()));
}

#[tokio::test]
async fn chat_with_tools_fails_without_key() {
    let p = OpenAiProvider::new(None);
    let messages = vec![ChatMessage::user("hello".to_string())];
    let tools = vec![serde_json::json!({
        "type": "function",
        "function": {
            "name": "shell",
            "description": "Run a shell command",
            "parameters": {
                "type": "object",
                "properties": {
                    "command": { "type": "string" }
                },
                "required": ["command"]
            }
        }
    })];
    let result = p.chat_with_tools(&messages, &tools, "gpt-4o", 0.7).await;
    assert!(result.is_err());
    assert!(result.unwrap_err().to_string().contains("API key not set"));
}

#[tokio::test]
async fn chat_with_tools_rejects_invalid_tool_shape() {
    let p = OpenAiProvider::new(Some("openai-test-credential"));
    let messages = vec![ChatMessage::user("hello".to_string())];
    let tools = vec![serde_json::json!({
        "type": "function",
        "function": {
            "name": "shell",
            "parameters": {
                "type": "object",
                "properties": {
                    "command": { "type": "string" }
                },
                "required": ["command"]
            }
        }
    })];

    let result = p.chat_with_tools(&messages, &tools, "gpt-4o", 0.7).await;
    assert!(result.is_err());
    assert!(
        result
            .unwrap_err()
            .to_string()
            .contains("Invalid OpenAI tool specification")
    );
}

#[test]
fn native_tool_spec_deserializes_from_openai_format() {
    let json = serde_json::json!({
        "type": "function",
        "function": {
            "name": "shell",
            "description": "Run a shell command",
            "parameters": {
                "type": "object",
                "properties": {
                    "command": { "type": "string" }
                },
                "required": ["command"]
            }
        }
    });
    let spec = parse_native_tool_spec(json).unwrap();
    assert_eq!(spec.kind, "function");
    assert_eq!(spec.function.name, "shell");
}

#[test]
fn native_response_parses_usage() {
    let json = r#"{
            "choices": [{"message": {"content": "Hello"}}],
            "usage": {"prompt_tokens": 100, "completion_tokens": 50}
        }"#;
    let resp: NativeChatResponse = serde_json::from_str(json).unwrap();
    let usage = resp.usage.unwrap();
    assert_eq!(usage.prompt_tokens, Some(100));
    assert_eq!(usage.completion_tokens, Some(50));
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
    let json = r#"{"choices":[{"message":{
            "content":"answer",
            "reasoning_content":"thinking step",
            "tool_calls":[{"id":"call_1","type":"function","function":{"name":"shell","arguments":"{}"}}]
        }}]}"#;
    let resp: NativeChatResponse = serde_json::from_str(json).unwrap();
    let message = resp.choices.into_iter().next().unwrap().message;
    let parsed = OpenAiProvider::parse_native_response(message);
    assert_eq!(parsed.reasoning_content.as_deref(), Some("thinking step"));
    assert_eq!(parsed.tool_calls.len(), 1);
}

#[test]
fn parse_native_response_none_reasoning_content_for_normal_model() {
    let json = r#"{"choices":[{"message":{"content":"hello"}}]}"#;
    let resp: NativeChatResponse = serde_json::from_str(json).unwrap();
    let message = resp.choices.into_iter().next().unwrap().message;
    let parsed = OpenAiProvider::parse_native_response(message);
    assert!(parsed.reasoning_content.is_none());
}

#[test]
fn convert_messages_round_trips_reasoning_content() {
    use crate::providers::ChatMessage;

    let history_json = serde_json::json!({
        "content": "I will check",
        "tool_calls": [{
            "id": "tc_1",
            "name": "shell",
            "arguments": "{}"
        }],
        "reasoning_content": "Let me think..."
    });

    let messages = vec![ChatMessage::assistant(history_json.to_string())];
    let native = OpenAiProvider::convert_messages(&messages);
    assert_eq!(native.len(), 1);
    assert_eq!(
        native[0].reasoning_content.as_deref(),
        Some("Let me think...")
    );
}

#[test]
fn convert_messages_no_reasoning_content_when_absent() {
    use crate::providers::ChatMessage;

    let history_json = serde_json::json!({
        "content": "I will check",
        "tool_calls": [{
            "id": "tc_1",
            "name": "shell",
            "arguments": "{}"
        }]
    });

    let messages = vec![ChatMessage::assistant(history_json.to_string())];
    let native = OpenAiProvider::convert_messages(&messages);
    assert_eq!(native.len(), 1);
    assert!(native[0].reasoning_content.is_none());
}

#[test]
fn native_message_omits_reasoning_content_when_none() {
    let msg = NativeMessage {
        role: "assistant".to_string(),
        content: Some("hi".to_string()),
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
        content: Some("hi".to_string()),
        tool_call_id: None,
        tool_calls: None,
        reasoning_content: Some("thinking...".to_string()),
    };
    let json = serde_json::to_string(&msg).unwrap();
    assert!(json.contains("reasoning_content"));
    assert!(json.contains("thinking..."));
}

// ═══════════════════════════════════════════════════════════════════════
// Temperature adjustment tests
// ═══════════════════════════════════════════════════════════════════════

#[test]
fn adjust_temperature_for_o1_models() {
    assert_eq!(OpenAiProvider::adjust_temperature_for_model("o1", 0.7), 1.0);
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("o1-2024-12-17", 0.5),
        1.0
    );
}

#[test]
fn adjust_temperature_for_o3_models() {
    assert_eq!(OpenAiProvider::adjust_temperature_for_model("o3", 0.7), 1.0);
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("o3-2025-04-16", 0.5),
        1.0
    );
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("o3-mini", 0.3),
        1.0
    );
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("o3-mini-2025-01-31", 0.8),
        1.0
    );
}

#[test]
fn adjust_temperature_for_o4_models() {
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("o4-mini", 0.7),
        1.0
    );
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("o4-mini-2025-04-16", 0.5),
        1.0
    );
}

#[test]
fn adjust_temperature_for_gpt5_models() {
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("gpt-5", 0.7),
        1.0
    );
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("gpt-5-2025-08-07", 0.5),
        1.0
    );
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("gpt-5-mini", 0.3),
        1.0
    );
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("gpt-5-mini-2025-08-07", 0.8),
        1.0
    );
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("gpt-5-nano", 0.6),
        1.0
    );
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("gpt-5-nano-2025-08-07", 0.4),
        1.0
    );
}

#[test]
fn adjust_temperature_for_gpt5_chat_latest_models() {
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("gpt-5.1-chat-latest", 0.7),
        1.0
    );
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("gpt-5.2-chat-latest", 0.5),
        1.0
    );
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("gpt-5.3-chat-latest", 0.3),
        1.0
    );
}

#[test]
fn adjust_temperature_preserves_for_standard_models() {
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("gpt-4o", 0.7),
        0.7
    );
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("gpt-4-turbo", 0.5),
        0.5
    );
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("gpt-3.5-turbo", 0.3),
        0.3
    );
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("gpt-4", 1.0),
        1.0
    );
}

#[test]
fn adjust_temperature_handles_edge_cases() {
    // Temperature 0.0 should be preserved for standard models
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("gpt-4o", 0.0),
        0.0
    );
    // Temperature 1.0 should be preserved for all models
    assert_eq!(OpenAiProvider::adjust_temperature_for_model("o1", 1.0), 1.0);
    assert_eq!(
        OpenAiProvider::adjust_temperature_for_model("gpt-4o", 1.0),
        1.0
    );
}
