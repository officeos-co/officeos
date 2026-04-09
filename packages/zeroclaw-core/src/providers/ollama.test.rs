use super::*;

#[test]
fn default_url() {
    let p = OllamaProvider::new(None, None);
    assert_eq!(p.base_url, "http://localhost:11434");
}

#[test]
fn custom_url_trailing_slash() {
    let p = OllamaProvider::new(Some("http://192.168.1.100:11434/"), None);
    assert_eq!(p.base_url, "http://192.168.1.100:11434");
}

#[test]
fn custom_url_no_trailing_slash() {
    let p = OllamaProvider::new(Some("http://myserver:11434"), None);
    assert_eq!(p.base_url, "http://myserver:11434");
}

#[test]
fn custom_url_strips_api_suffix() {
    let p = OllamaProvider::new(Some("https://ollama.com/api/"), None);
    assert_eq!(p.base_url, "https://ollama.com");
}

#[test]
fn custom_url_strips_api_chat_suffix() {
    let p = OllamaProvider::new(Some("http://172.30.30.50:11434/api/chat"), None);
    assert_eq!(p.base_url, "http://172.30.30.50:11434");
}

#[test]
fn empty_url_uses_empty() {
    let p = OllamaProvider::new(Some(""), None);
    assert_eq!(p.base_url, "");
}

#[test]
fn cloud_suffix_strips_model_name() {
    let p = OllamaProvider::new(Some("https://ollama.com"), Some("ollama-key"));
    let (model, should_auth) = p.resolve_request_details("qwen3:cloud").unwrap();
    assert_eq!(model, "qwen3");
    assert!(should_auth);
}

#[test]
fn cloud_suffix_with_local_endpoint_errors() {
    let p = OllamaProvider::new(None, Some("ollama-key"));
    let error = p
        .resolve_request_details("qwen3:cloud")
        .expect_err("cloud suffix should fail on local endpoint");
    assert!(
        error
            .to_string()
            .contains("requested cloud routing, but Ollama endpoint is local")
    );
}

#[test]
fn cloud_suffix_without_api_key_errors() {
    let p = OllamaProvider::new(Some("https://ollama.com"), None);
    let error = p
        .resolve_request_details("qwen3:cloud")
        .expect_err("cloud suffix should require API key");
    assert!(
        error
            .to_string()
            .contains("requested cloud routing, but no API key is configured")
    );
}

#[test]
fn remote_endpoint_auth_enabled_when_key_present() {
    let p = OllamaProvider::new(Some("https://ollama.com"), Some("ollama-key"));
    let (_model, should_auth) = p.resolve_request_details("qwen3").unwrap();
    assert!(should_auth);
}

#[test]
fn remote_endpoint_with_api_suffix_still_allows_cloud_models() {
    let p = OllamaProvider::new(Some("https://ollama.com/api"), Some("ollama-key"));
    let (model, should_auth) = p.resolve_request_details("qwen3:cloud").unwrap();
    assert_eq!(model, "qwen3");
    assert!(should_auth);
}

#[test]
fn local_endpoint_auth_disabled_even_with_key() {
    let p = OllamaProvider::new(None, Some("ollama-key"));
    let (_model, should_auth) = p.resolve_request_details("llama3").unwrap();
    assert!(!should_auth);
}

#[test]
fn request_omits_think_when_reasoning_not_configured() {
    let provider = OllamaProvider::new(None, None);
    let request = provider.build_chat_request(
        vec![Message {
            role: "user".to_string(),
            content: Some("hello".to_string()),
            images: None,
            tool_calls: None,
            tool_name: None,
        }],
        "llama3",
        0.7,
        None,
    );

    let json = serde_json::to_value(request).unwrap();
    assert!(json.get("think").is_none());
}

#[test]
fn request_includes_think_when_reasoning_configured() {
    let provider = OllamaProvider::new_with_reasoning(None, None, Some(false));
    let request = provider.build_chat_request(
        vec![Message {
            role: "user".to_string(),
            content: Some("hello".to_string()),
            images: None,
            tool_calls: None,
            tool_name: None,
        }],
        "llama3",
        0.7,
        None,
    );

    let json = serde_json::to_value(request).unwrap();
    assert_eq!(json.get("think"), Some(&serde_json::json!(false)));
}

#[test]
fn response_deserializes() {
    let json = r#"{"message":{"role":"assistant","content":"Hello from Ollama!"}}"#;
    let resp: ApiChatResponse = serde_json::from_str(json).unwrap();
    assert_eq!(resp.message.content, "Hello from Ollama!");
}

#[test]
fn response_with_empty_content() {
    let json = r#"{"message":{"role":"assistant","content":""}}"#;
    let resp: ApiChatResponse = serde_json::from_str(json).unwrap();
    assert!(resp.message.content.is_empty());
}

#[test]
fn normalize_response_text_rejects_whitespace_only_content() {
    assert_eq!(
        OllamaProvider::normalize_response_text("\n \t".to_string()),
        None
    );
    assert_eq!(
        OllamaProvider::normalize_response_text(" hello ".to_string()),
        Some("hello".to_string())
    );
}

#[test]
fn normalize_response_text_strips_think_tags() {
    assert_eq!(
        OllamaProvider::normalize_response_text("<think>reasoning</think> hello".to_string()),
        Some("hello".to_string())
    );
}

#[test]
fn normalize_response_text_rejects_think_only_content() {
    assert_eq!(
        OllamaProvider::normalize_response_text("<think>only thinking here</think>".to_string()),
        None
    );
}

#[test]
fn fallback_text_for_empty_content_without_thinking_is_generic() {
    let text = OllamaProvider::fallback_text_for_empty_content("qwen3-coder", None);
    assert!(text.contains("couldn't get a complete response from Ollama"));
}

#[test]
fn response_with_missing_content_defaults_to_empty() {
    let json = r#"{"message":{"role":"assistant"}}"#;
    let resp: ApiChatResponse = serde_json::from_str(json).unwrap();
    assert!(resp.message.content.is_empty());
}

#[test]
fn response_with_thinking_field_extracts_content() {
    let json =
        r#"{"message":{"role":"assistant","content":"hello","thinking":"internal reasoning"}}"#;
    let resp: ApiChatResponse = serde_json::from_str(json).unwrap();
    assert_eq!(resp.message.content, "hello");
}

#[test]
fn response_with_tool_calls_parses_correctly() {
    let json = r#"{"message":{"role":"assistant","content":"","tool_calls":[{"id":"call_123","function":{"name":"shell","arguments":{"command":"date"}}}]}}"#;
    let resp: ApiChatResponse = serde_json::from_str(json).unwrap();
    assert!(resp.message.content.is_empty());
    assert_eq!(resp.message.tool_calls.len(), 1);
    assert_eq!(resp.message.tool_calls[0].function.name, "shell");
}

#[test]
fn extract_tool_name_handles_nested_tool_call() {
    let provider = OllamaProvider::new(None, None);
    let tc = OllamaToolCall {
        id: Some("call_123".into()),
        function: OllamaFunction {
            name: "tool_call".into(),
            arguments: serde_json::json!({
                "name": "shell",
                "arguments": {"command": "date"}
            }),
        },
    };
    let (name, args) = provider.extract_tool_name_and_args(&tc);
    assert_eq!(name, "shell");
    assert_eq!(args.get("command").unwrap(), "date");
}

#[test]
fn extract_tool_name_handles_prefixed_name() {
    let provider = OllamaProvider::new(None, None);
    let tc = OllamaToolCall {
        id: Some("call_123".into()),
        function: OllamaFunction {
            name: "tool.shell".into(),
            arguments: serde_json::json!({"command": "ls"}),
        },
    };
    let (name, args) = provider.extract_tool_name_and_args(&tc);
    assert_eq!(name, "shell");
    assert_eq!(args.get("command").unwrap(), "ls");
}

#[test]
fn extract_tool_name_handles_normal_call() {
    let provider = OllamaProvider::new(None, None);
    let tc = OllamaToolCall {
        id: Some("call_123".into()),
        function: OllamaFunction {
            name: "file_read".into(),
            arguments: serde_json::json!({"path": "/tmp/test"}),
        },
    };
    let (name, args) = provider.extract_tool_name_and_args(&tc);
    assert_eq!(name, "file_read");
    assert_eq!(args.get("path").unwrap(), "/tmp/test");
}

#[test]
fn format_tool_calls_produces_valid_json() {
    let provider = OllamaProvider::new(None, None);
    let tool_calls = vec![OllamaToolCall {
        id: Some("call_abc".into()),
        function: OllamaFunction {
            name: "shell".into(),
            arguments: serde_json::json!({"command": "date"}),
        },
    }];

    let formatted = provider.format_tool_calls_for_loop(&tool_calls);
    let parsed: serde_json::Value = serde_json::from_str(&formatted).unwrap();

    assert!(parsed.get("tool_calls").is_some());
    let calls = parsed.get("tool_calls").unwrap().as_array().unwrap();
    assert_eq!(calls.len(), 1);

    let func = calls[0].get("function").unwrap();
    assert_eq!(func.get("name").unwrap(), "shell");
    // arguments should be a string (JSON-encoded)
    assert!(func.get("arguments").unwrap().is_string());
}

#[test]
fn convert_messages_parses_native_assistant_tool_calls() {
    let provider = OllamaProvider::new(None, None);
    let messages = vec![ChatMessage {
            role: "assistant".into(),
            content: r#"{"content":null,"tool_calls":[{"id":"call_1","name":"shell","arguments":"{\"command\":\"ls\"}"}]}"#.into(),
        }];

    let converted = provider.convert_messages(&messages);

    assert_eq!(converted.len(), 1);
    assert_eq!(converted[0].role, "assistant");
    assert!(converted[0].content.is_none());
    let calls = converted[0]
        .tool_calls
        .as_ref()
        .expect("tool calls expected");
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].kind, "function");
    assert_eq!(calls[0].function.name, "shell");
    assert_eq!(calls[0].function.arguments.get("command").unwrap(), "ls");
}

#[test]
fn convert_messages_maps_tool_result_call_id_to_tool_name() {
    let provider = OllamaProvider::new(None, None);
    let messages = vec![
            ChatMessage {
                role: "assistant".into(),
                content: r#"{"content":null,"tool_calls":[{"id":"call_7","name":"file_read","arguments":"{\"path\":\"README.md\"}"}]}"#.into(),
            },
            ChatMessage {
                role: "tool".into(),
                content: r#"{"tool_call_id":"call_7","content":"ok"}"#.into(),
            },
        ];

    let converted = provider.convert_messages(&messages);

    assert_eq!(converted.len(), 2);
    assert_eq!(converted[1].role, "tool");
    assert_eq!(converted[1].tool_name.as_deref(), Some("file_read"));
    assert_eq!(converted[1].content.as_deref(), Some("ok"));
    assert!(converted[1].tool_calls.is_none());
}

#[test]
fn convert_messages_extracts_images_from_user_marker() {
    let provider = OllamaProvider::new(None, None);
    let messages = vec![ChatMessage {
        role: "user".into(),
        content: "Inspect this screenshot [IMAGE:data:image/png;base64,abcd==]".into(),
    }];

    let converted = provider.convert_messages(&messages);
    assert_eq!(converted.len(), 1);
    assert_eq!(converted[0].role, "user");
    assert_eq!(
        converted[0].content.as_deref(),
        Some("Inspect this screenshot")
    );
    let images = converted[0]
        .images
        .as_ref()
        .expect("images should be present");
    assert_eq!(images, &vec!["abcd==".to_string()]);
}

#[test]
fn capabilities_disable_native_tools_and_enable_vision() {
    let provider = OllamaProvider::new(None, None);
    let caps = <OllamaProvider as Provider>::capabilities(&provider);
    assert!(
        !caps.native_tool_calling,
        "Ollama should default to prompt-guided tool calling"
    );
    assert!(caps.vision);
}

#[test]
fn api_response_parses_eval_counts() {
    let json = r#"{
            "message": {"content": "Hello", "tool_calls": []},
            "prompt_eval_count": 50,
            "eval_count": 25
        }"#;
    let resp: ApiChatResponse = serde_json::from_str(json).unwrap();
    assert_eq!(resp.prompt_eval_count, Some(50));
    assert_eq!(resp.eval_count, Some(25));
}

#[test]
fn api_response_parses_without_eval_counts() {
    let json = r#"{"message": {"content": "Hello", "tool_calls": []}}"#;
    let resp: ApiChatResponse = serde_json::from_str(json).unwrap();
    assert!(resp.prompt_eval_count.is_none());
    assert!(resp.eval_count.is_none());
}

// ═══════════════════════════════════════════════════════════════════════
// <think> tag stripping tests
// ═══════════════════════════════════════════════════════════════════════

#[test]
fn strip_think_tags_removes_single_block() {
    let input = "<think>internal reasoning</think>Hello world";
    assert_eq!(OllamaProvider::strip_think_tags(input), "Hello world");
}

#[test]
fn strip_think_tags_removes_multiple_blocks() {
    let input = "<think>first</think>A<think>second</think>B";
    assert_eq!(OllamaProvider::strip_think_tags(input), "AB");
}

#[test]
fn strip_think_tags_handles_unclosed_block() {
    let input = "visible<think>hidden tail";
    assert_eq!(OllamaProvider::strip_think_tags(input), "visible");
}

#[test]
fn strip_think_tags_preserves_text_without_tags() {
    let input = "plain text response";
    assert_eq!(
        OllamaProvider::strip_think_tags(input),
        "plain text response"
    );
}

#[test]
fn strip_think_tags_returns_empty_for_think_only() {
    let input = "<think>only thinking</think>";
    assert_eq!(OllamaProvider::strip_think_tags(input), "");
}

// ═══════════════════════════════════════════════════════════════════════
// effective_content tests
// ═══════════════════════════════════════════════════════════════════════

#[test]
fn effective_content_strips_think_and_returns_rest() {
    let result = OllamaProvider::effective_content(
        "<think>reasoning</think>\n<tool_call>{\"name\":\"shell\",\"arguments\":{\"command\":\"ls\"}}</tool_call>",
        None,
    );
    assert!(result.is_some());
    let text = result.unwrap();
    assert!(text.contains("<tool_call>"));
    assert!(!text.contains("<think>"));
}

#[test]
fn effective_content_falls_back_to_thinking_field() {
    let result = OllamaProvider::effective_content(
        "",
        Some("<tool_call>{\"name\":\"shell\",\"arguments\":{\"command\":\"date\"}}</tool_call>"),
    );
    assert!(result.is_some());
    assert!(result.unwrap().contains("<tool_call>"));
}

#[test]
fn effective_content_returns_none_when_both_empty() {
    assert!(OllamaProvider::effective_content("", None).is_none());
    assert!(OllamaProvider::effective_content("", Some("")).is_none());
    assert!(
        OllamaProvider::effective_content(
            "<think>only thinking</think>",
            Some("<think>also only thinking</think>")
        )
        .is_none()
    );
}

#[test]
fn effective_content_prefers_content_over_thinking() {
    let result = OllamaProvider::effective_content("content text", Some("thinking text"));
    assert_eq!(result, Some("content text".to_string()));
}

#[test]
fn effective_content_uses_thinking_when_content_is_think_only() {
    let result = OllamaProvider::effective_content(
        "<think>just reasoning</think>",
        Some("actual useful text from thinking field"),
    );
    assert_eq!(
        result,
        Some("actual useful text from thinking field".to_string())
    );
}

// ═══════════════════════════════════════════════════════════════════════
// Qwen tool-call regression scenario tests
// ═══════════════════════════════════════════════════════════════════════

#[test]
fn qwen_think_with_tool_call_in_content_preserved() {
    // Qwen produces <think> tags followed by <tool_call> in content,
    // with no structured tool_calls. The <tool_call> tags must survive
    // for downstream parse_tool_calls to extract them.
    let content = "<think>I should list files</think>\n<tool_call>\n{\"name\":\"shell\",\"arguments\":{\"command\":\"ls\"}}\n</tool_call>";
    let result = OllamaProvider::effective_content(content, None);
    assert!(result.is_some());
    let text = result.unwrap();
    assert!(text.contains("<tool_call>"));
    assert!(text.contains("shell"));
    assert!(!text.contains("<think>"));
}

#[test]
fn qwen_thinking_field_with_tool_call_xml_extracted() {
    // When think=true, Ollama separates thinking, but Qwen may put tool
    // call XML in the thinking field with empty content.
    let content = "";
    let thinking = "I need to check the date\n<tool_call>\n{\"name\":\"shell\",\"arguments\":{\"command\":\"date\"}}\n</tool_call>";
    let result = OllamaProvider::effective_content(content, Some(thinking));
    assert!(result.is_some());
    let text = result.unwrap();
    assert!(text.contains("<tool_call>"));
    assert!(text.contains("date"));
}
