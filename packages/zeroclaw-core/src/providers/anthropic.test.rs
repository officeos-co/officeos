    use super::*;
    use crate::auth::anthropic_token::{AnthropicAuthKind, detect_auth_kind};

    #[test]
    fn creates_with_key() {
        let p = AnthropicProvider::new(Some("anthropic-test-credential"));
        assert!(p.credential.is_some());
        assert_eq!(p.credential.as_deref(), Some("anthropic-test-credential"));
        assert_eq!(p.base_url, "https://api.anthropic.com");
    }

    #[test]
    fn creates_without_key() {
        let p = AnthropicProvider::new(None);
        assert!(p.credential.is_none());
        assert_eq!(p.base_url, "https://api.anthropic.com");
    }

    #[test]
    fn creates_with_empty_key() {
        let p = AnthropicProvider::new(Some(""));
        assert!(p.credential.is_none());
    }

    #[test]
    fn creates_with_whitespace_key() {
        let p = AnthropicProvider::new(Some("  anthropic-test-credential  "));
        assert!(p.credential.is_some());
        assert_eq!(p.credential.as_deref(), Some("anthropic-test-credential"));
    }

    #[test]
    fn creates_with_custom_base_url() {
        let p = AnthropicProvider::with_base_url(
            Some("anthropic-credential"),
            Some("https://api.example.com"),
        );
        assert_eq!(p.base_url, "https://api.example.com");
        assert_eq!(p.credential.as_deref(), Some("anthropic-credential"));
    }

    #[test]
    fn custom_base_url_trims_trailing_slash() {
        let p = AnthropicProvider::with_base_url(None, Some("https://api.example.com/"));
        assert_eq!(p.base_url, "https://api.example.com");
    }

    #[test]
    fn default_base_url_when_none_provided() {
        let p = AnthropicProvider::with_base_url(None, None);
        assert_eq!(p.base_url, "https://api.anthropic.com");
    }

    #[tokio::test]
    async fn chat_fails_without_key() {
        let p = AnthropicProvider::new(None);
        let result = p
            .chat_with_system(None, "hello", "claude-3-opus", 0.7)
            .await;
        assert!(result.is_err());
        let err = result.unwrap_err().to_string();
        assert!(
            err.contains("credentials not set"),
            "Expected key error, got: {err}"
        );
    }

    #[test]
    fn setup_token_detection_works() {
        assert!(AnthropicProvider::is_setup_token("sk-ant-oat01-abcdef"));
        assert!(!AnthropicProvider::is_setup_token("sk-ant-api-key"));
    }

    #[test]
    fn apply_auth_uses_bearer_and_beta_for_setup_tokens() {
        let provider = AnthropicProvider::new(None);
        let request = provider
            .apply_auth(
                provider
                    .http_client()
                    .get("https://api.anthropic.com/v1/models"),
                "sk-ant-oat01-test-token",
            )
            .build()
            .expect("request should build");

        assert_eq!(
            request
                .headers()
                .get("authorization")
                .and_then(|v| v.to_str().ok()),
            Some("Bearer sk-ant-oat01-test-token")
        );
        assert_eq!(
            request
                .headers()
                .get("anthropic-beta")
                .and_then(|v| v.to_str().ok()),
            Some("claude-code-20250219,oauth-2025-04-20,interleaved-thinking-2025-05-14")
        );
        assert_eq!(
            request
                .headers()
                .get("anthropic-dangerous-direct-browser-access")
                .and_then(|v| v.to_str().ok()),
            Some("true")
        );
        assert!(request.headers().get("x-api-key").is_none());
    }

    #[test]
    fn apply_auth_uses_x_api_key_for_regular_tokens() {
        let provider = AnthropicProvider::new(None);
        let request = provider
            .apply_auth(
                provider
                    .http_client()
                    .get("https://api.anthropic.com/v1/models"),
                "sk-ant-api-key",
            )
            .build()
            .expect("request should build");

        assert_eq!(
            request
                .headers()
                .get("x-api-key")
                .and_then(|v| v.to_str().ok()),
            Some("sk-ant-api-key")
        );
        assert!(request.headers().get("authorization").is_none());
        assert!(request.headers().get("anthropic-beta").is_none());
    }

    #[tokio::test]
    async fn chat_with_system_fails_without_key() {
        let p = AnthropicProvider::new(None);
        let result = p
            .chat_with_system(Some("You are ZeroClaw"), "hello", "claude-3-opus", 0.7)
            .await;
        assert!(result.is_err());
    }

    #[test]
    fn chat_request_serializes_without_system() {
        let req = ChatRequest {
            model: "claude-3-opus".to_string(),
            max_tokens: 4096,
            system: None,
            messages: vec![Message {
                role: "user".to_string(),
                content: "hello".to_string(),
            }],
            temperature: 0.7,
        };
        let json = serde_json::to_string(&req).unwrap();
        assert!(
            !json.contains("system"),
            "system field should be skipped when None"
        );
        assert!(json.contains("claude-3-opus"));
        assert!(json.contains("hello"));
    }

    #[test]
    fn chat_request_serializes_with_system() {
        let req = ChatRequest {
            model: "claude-3-opus".to_string(),
            max_tokens: 4096,
            system: Some("You are ZeroClaw".to_string()),
            messages: vec![Message {
                role: "user".to_string(),
                content: "hello".to_string(),
            }],
            temperature: 0.7,
        };
        let json = serde_json::to_string(&req).unwrap();
        assert!(json.contains("\"system\":\"You are ZeroClaw\""));
    }

    #[test]
    fn chat_response_deserializes() {
        let json = r#"{"content":[{"type":"text","text":"Hello there!"}]}"#;
        let resp: ChatResponse = serde_json::from_str(json).unwrap();
        assert_eq!(resp.content.len(), 1);
        assert_eq!(resp.content[0].kind, "text");
        assert_eq!(resp.content[0].text.as_deref(), Some("Hello there!"));
    }

    #[test]
    fn chat_response_empty_content() {
        let json = r#"{"content":[]}"#;
        let resp: ChatResponse = serde_json::from_str(json).unwrap();
        assert!(resp.content.is_empty());
    }

    #[test]
    fn chat_response_multiple_blocks() {
        let json =
            r#"{"content":[{"type":"text","text":"First"},{"type":"text","text":"Second"}]}"#;
        let resp: ChatResponse = serde_json::from_str(json).unwrap();
        assert_eq!(resp.content.len(), 2);
        assert_eq!(resp.content[0].text.as_deref(), Some("First"));
        assert_eq!(resp.content[1].text.as_deref(), Some("Second"));
    }

    #[test]
    fn temperature_range_serializes() {
        for temp in [0.0, 0.5, 1.0, 2.0] {
            let req = ChatRequest {
                model: "claude-3-opus".to_string(),
                max_tokens: 4096,
                system: None,
                messages: vec![],
                temperature: temp,
            };
            let json = serde_json::to_string(&req).unwrap();
            assert!(json.contains(&format!("{temp}")));
        }
    }

    #[test]
    fn detects_auth_from_jwt_shape() {
        let kind = detect_auth_kind("a.b.c", None);
        assert_eq!(kind, AnthropicAuthKind::Authorization);
    }

    #[test]
    fn cache_control_serializes_correctly() {
        let cache = CacheControl::ephemeral();
        let json = serde_json::to_string(&cache).unwrap();
        assert_eq!(json, r#"{"type":"ephemeral"}"#);
    }

    #[test]
    fn system_prompt_string_variant_serializes() {
        let prompt = SystemPrompt::String("You are a helpful assistant".to_string());
        let json = serde_json::to_string(&prompt).unwrap();
        assert_eq!(json, r#""You are a helpful assistant""#);
    }

    #[test]
    fn system_prompt_blocks_variant_serializes() {
        let prompt = SystemPrompt::Blocks(vec![SystemBlock {
            block_type: "text".to_string(),
            text: "You are a helpful assistant".to_string(),
            cache_control: Some(CacheControl::ephemeral()),
        }]);
        let json = serde_json::to_string(&prompt).unwrap();
        assert!(json.contains(r#""type":"text""#));
        assert!(json.contains("You are a helpful assistant"));
        assert!(json.contains(r#""type":"ephemeral""#));
    }

    #[test]
    fn system_prompt_blocks_without_cache_control() {
        let prompt = SystemPrompt::Blocks(vec![SystemBlock {
            block_type: "text".to_string(),
            text: "Short prompt".to_string(),
            cache_control: None,
        }]);
        let json = serde_json::to_string(&prompt).unwrap();
        assert!(json.contains("Short prompt"));
        assert!(!json.contains("cache_control"));
    }

    #[test]
    fn native_content_text_without_cache_control() {
        let content = NativeContentOut::Text {
            text: "Hello".to_string(),
            cache_control: None,
        };
        let json = serde_json::to_string(&content).unwrap();
        assert!(json.contains(r#""type":"text""#));
        assert!(json.contains("Hello"));
        assert!(!json.contains("cache_control"));
    }

    #[test]
    fn native_content_text_with_cache_control() {
        let content = NativeContentOut::Text {
            text: "Hello".to_string(),
            cache_control: Some(CacheControl::ephemeral()),
        };
        let json = serde_json::to_string(&content).unwrap();
        assert!(json.contains(r#""type":"text""#));
        assert!(json.contains("Hello"));
        assert!(json.contains(r#""cache_control":{"type":"ephemeral"}"#));
    }

    #[test]
    fn native_content_tool_use_without_cache_control() {
        let content = NativeContentOut::ToolUse {
            id: "tool_123".to_string(),
            name: "get_weather".to_string(),
            input: serde_json::json!({"location": "San Francisco"}),
            cache_control: None,
        };
        let json = serde_json::to_string(&content).unwrap();
        assert!(json.contains(r#""type":"tool_use""#));
        assert!(json.contains("tool_123"));
        assert!(json.contains("get_weather"));
        assert!(!json.contains("cache_control"));
    }

    #[test]
    fn native_content_tool_result_with_cache_control() {
        let content = NativeContentOut::ToolResult {
            tool_use_id: "tool_123".to_string(),
            content: "Result data".to_string(),
            cache_control: Some(CacheControl::ephemeral()),
        };
        let json = serde_json::to_string(&content).unwrap();
        assert!(json.contains(r#""type":"tool_result""#));
        assert!(json.contains("tool_123"));
        assert!(json.contains("Result data"));
        assert!(json.contains(r#""cache_control":{"type":"ephemeral"}"#));
    }

    #[test]
    fn native_tool_spec_without_cache_control() {
        let schema = serde_json::json!({"type": "object"});
        let tool = NativeToolSpec {
            name: "get_weather",
            description: "Get weather info",
            input_schema: &schema,
            cache_control: None,
        };
        let json = serde_json::to_string(&tool).unwrap();
        assert!(json.contains("get_weather"));
        assert!(!json.contains("cache_control"));
    }

    #[test]
    fn native_tool_spec_with_cache_control() {
        let schema = serde_json::json!({"type": "object"});
        let tool = NativeToolSpec {
            name: "get_weather",
            description: "Get weather info",
            input_schema: &schema,
            cache_control: Some(CacheControl::ephemeral()),
        };
        let json = serde_json::to_string(&tool).unwrap();
        assert!(json.contains("get_weather"));
        assert!(json.contains(r#""cache_control":{"type":"ephemeral"}"#));
    }

    #[test]
    fn should_cache_system_small_prompt() {
        let small_prompt = "You are a helpful assistant.";
        assert!(!AnthropicProvider::should_cache_system(small_prompt));
    }

    #[test]
    fn should_cache_system_large_prompt() {
        let large_prompt = "a".repeat(3073); // Just over 3072 bytes
        assert!(AnthropicProvider::should_cache_system(&large_prompt));
    }

    #[test]
    fn should_cache_system_boundary() {
        let boundary_prompt = "a".repeat(3072); // Exactly 3072 bytes
        assert!(!AnthropicProvider::should_cache_system(&boundary_prompt));

        let over_boundary = "a".repeat(3073);
        assert!(AnthropicProvider::should_cache_system(&over_boundary));
    }

    #[test]
    fn should_cache_conversation_short() {
        let messages = vec![
            ChatMessage {
                role: "system".to_string(),
                content: "System prompt".to_string(),
            },
            ChatMessage {
                role: "user".to_string(),
                content: "Hello".to_string(),
            },
        ];
        // Only 1 non-system message — should not cache
        assert!(!AnthropicProvider::should_cache_conversation(&messages));
    }

    #[test]
    fn should_cache_conversation_long() {
        let mut messages = vec![ChatMessage {
            role: "system".to_string(),
            content: "System prompt".to_string(),
        }];
        // Add 3 non-system messages
        for i in 0..3 {
            messages.push(ChatMessage {
                role: if i % 2 == 0 { "user" } else { "assistant" }.to_string(),
                content: format!("Message {i}"),
            });
        }
        assert!(AnthropicProvider::should_cache_conversation(&messages));
    }

    #[test]
    fn should_cache_conversation_boundary() {
        let messages = vec![ChatMessage {
            role: "user".to_string(),
            content: "Hello".to_string(),
        }];
        // Exactly 1 non-system message — should not cache
        assert!(!AnthropicProvider::should_cache_conversation(&messages));

        // Add one more to cross boundary (>1)
        let messages = vec![
            ChatMessage {
                role: "user".to_string(),
                content: "Hello".to_string(),
            },
            ChatMessage {
                role: "assistant".to_string(),
                content: "Hi".to_string(),
            },
        ];
        assert!(AnthropicProvider::should_cache_conversation(&messages));
    }

    #[test]
    fn apply_cache_to_last_message_text() {
        let mut messages = vec![NativeMessage {
            role: "user".to_string(),
            content: vec![NativeContentOut::Text {
                text: "Hello".to_string(),
                cache_control: None,
            }],
        }];

        AnthropicProvider::apply_cache_to_last_message(&mut messages);

        match &messages[0].content[0] {
            NativeContentOut::Text { cache_control, .. } => {
                assert!(cache_control.is_some());
            }
            _ => panic!("Expected Text variant"),
        }
    }

    #[test]
    fn apply_cache_to_last_message_tool_result() {
        let mut messages = vec![NativeMessage {
            role: "user".to_string(),
            content: vec![NativeContentOut::ToolResult {
                tool_use_id: "tool_123".to_string(),
                content: "Result".to_string(),
                cache_control: None,
            }],
        }];

        AnthropicProvider::apply_cache_to_last_message(&mut messages);

        match &messages[0].content[0] {
            NativeContentOut::ToolResult { cache_control, .. } => {
                assert!(cache_control.is_some());
            }
            _ => panic!("Expected ToolResult variant"),
        }
    }

    #[test]
    fn apply_cache_to_last_message_does_not_affect_tool_use() {
        let mut messages = vec![NativeMessage {
            role: "assistant".to_string(),
            content: vec![NativeContentOut::ToolUse {
                id: "tool_123".to_string(),
                name: "get_weather".to_string(),
                input: serde_json::json!({}),
                cache_control: None,
            }],
        }];

        AnthropicProvider::apply_cache_to_last_message(&mut messages);

        // ToolUse should not be affected
        match &messages[0].content[0] {
            NativeContentOut::ToolUse { cache_control, .. } => {
                assert!(cache_control.is_none());
            }
            _ => panic!("Expected ToolUse variant"),
        }
    }

    #[test]
    fn apply_cache_empty_messages() {
        let mut messages = vec![];
        AnthropicProvider::apply_cache_to_last_message(&mut messages);
        // Should not panic
        assert!(messages.is_empty());
    }

    #[test]
    fn convert_tools_adds_cache_to_last_tool() {
        let tools = vec![
            ToolSpec {
                name: "tool1".to_string(),
                description: "First tool".to_string(),
                parameters: serde_json::json!({"type": "object"}),
            },
            ToolSpec {
                name: "tool2".to_string(),
                description: "Second tool".to_string(),
                parameters: serde_json::json!({"type": "object"}),
            },
        ];

        let native_tools = AnthropicProvider::convert_tools(Some(&tools)).unwrap();

        assert_eq!(native_tools.len(), 2);
        assert!(native_tools[0].cache_control.is_none());
        assert!(native_tools[1].cache_control.is_some());
    }

    #[test]
    fn convert_tools_single_tool_gets_cache() {
        let tools = vec![ToolSpec {
            name: "tool1".to_string(),
            description: "Only tool".to_string(),
            parameters: serde_json::json!({"type": "object"}),
        }];

        let native_tools = AnthropicProvider::convert_tools(Some(&tools)).unwrap();

        assert_eq!(native_tools.len(), 1);
        assert!(native_tools[0].cache_control.is_some());
    }

    #[test]
    fn convert_messages_small_system_prompt_uses_blocks_with_cache() {
        let messages = vec![ChatMessage {
            role: "system".to_string(),
            content: "Short system prompt".to_string(),
        }];

        let (system_prompt, _) = AnthropicProvider::convert_messages(&messages);

        match system_prompt.unwrap() {
            SystemPrompt::Blocks(blocks) => {
                assert_eq!(blocks.len(), 1);
                assert_eq!(blocks[0].text, "Short system prompt");
                assert!(
                    blocks[0].cache_control.is_some(),
                    "Small system prompts should have cache_control"
                );
            }
            SystemPrompt::String(_) => {
                panic!("Expected Blocks variant with cache_control for small prompt")
            }
        }
    }

    #[test]
    fn convert_messages_large_system_prompt() {
        let large_content = "a".repeat(3073);
        let messages = vec![ChatMessage {
            role: "system".to_string(),
            content: large_content.clone(),
        }];

        let (system_prompt, _) = AnthropicProvider::convert_messages(&messages);

        match system_prompt.unwrap() {
            SystemPrompt::Blocks(blocks) => {
                assert_eq!(blocks.len(), 1);
                assert_eq!(blocks[0].text, large_content);
                assert!(blocks[0].cache_control.is_some());
            }
            SystemPrompt::String(_) => panic!("Expected Blocks variant for large prompt"),
        }
    }

    #[test]
    fn native_chat_request_with_blocks_system() {
        // System prompts now always use Blocks format with cache_control
        let req = NativeChatRequest {
            model: "claude-3-opus".to_string(),
            max_tokens: 4096,
            system: Some(SystemPrompt::Blocks(vec![SystemBlock {
                block_type: "text".to_string(),
                text: "System".to_string(),
                cache_control: Some(CacheControl::ephemeral()),
            }])),
            messages: vec![NativeMessage {
                role: "user".to_string(),
                content: vec![NativeContentOut::Text {
                    text: "Hello".to_string(),
                    cache_control: None,
                }],
            }],
            temperature: 0.7,
            tools: None,
            tool_choice: None,
            stream: None,
        };

        let json = serde_json::to_string(&req).unwrap();
        assert!(json.contains("System"));
        assert!(
            json.contains(r#""cache_control":{"type":"ephemeral"}"#),
            "System prompt should include cache_control"
        );
    }

    #[tokio::test]
    async fn warmup_without_key_is_noop() {
        let provider = AnthropicProvider::new(None);
        let result = provider.warmup().await;
        assert!(result.is_ok());
    }

    #[test]
    fn convert_messages_preserves_multi_turn_history() {
        let messages = vec![
            ChatMessage {
                role: "system".to_string(),
                content: "You are helpful.".to_string(),
            },
            ChatMessage {
                role: "user".to_string(),
                content: "gen a 2 sum in golang".to_string(),
            },
            ChatMessage {
                role: "assistant".to_string(),
                content: "```go\nfunc twoSum(nums []int) {}\n```".to_string(),
            },
            ChatMessage {
                role: "user".to_string(),
                content: "what's meaning of make here?".to_string(),
            },
        ];

        let (system, native_msgs) = AnthropicProvider::convert_messages(&messages);

        // System prompt extracted
        assert!(system.is_some());
        // All 3 non-system messages preserved in order
        assert_eq!(native_msgs.len(), 3);
        assert_eq!(native_msgs[0].role, "user");
        assert_eq!(native_msgs[1].role, "assistant");
        assert_eq!(native_msgs[2].role, "user");
    }

    /// Integration test: spin up a mock Anthropic API server, call chat_with_tools
    /// with a multi-turn conversation + tools, and verify the request body contains
    /// ALL conversation turns and native tool definitions.
    #[tokio::test]
    async fn chat_with_tools_sends_full_history_and_native_tools() {
        use axum::{Json, Router, routing::post};
        use std::sync::{Arc, Mutex};
        use tokio::net::TcpListener;

        // Captured request body for assertion
        let captured: Arc<Mutex<Option<serde_json::Value>>> = Arc::new(Mutex::new(None));
        let captured_clone = captured.clone();

        let app = Router::new().route(
            "/v1/messages",
            post(move |Json(body): Json<serde_json::Value>| {
                let cap = captured_clone.clone();
                async move {
                    *cap.lock().unwrap() = Some(body);
                    // Return a minimal valid Anthropic response
                    Json(serde_json::json!({
                        "id": "msg_test",
                        "type": "message",
                        "role": "assistant",
                        "content": [{"type": "text", "text": "The make function creates a map."}],
                        "model": "claude-opus-4-6",
                        "stop_reason": "end_turn",
                        "usage": {"input_tokens": 100, "output_tokens": 20}
                    }))
                }
            }),
        );

        let listener = TcpListener::bind("127.0.0.1:0").await.unwrap();
        let addr = listener.local_addr().unwrap();
        let server_handle = tokio::spawn(async move {
            axum::serve(listener, app).await.unwrap();
        });

        // Create provider pointing at mock server
        let provider = AnthropicProvider {
            credential: Some("test-key".to_string()),
            base_url: format!("http://{addr}"),
            max_tokens: DEFAULT_ANTHROPIC_MAX_TOKENS,
        };

        // Multi-turn conversation: system → user (Go code) → assistant (code response) → user (follow-up)
        let messages = vec![
            ChatMessage::system("You are a helpful assistant."),
            ChatMessage::user("gen a 2 sum in golang"),
            ChatMessage::assistant(
                "```go\nfunc twoSum(nums []int, target int) []int {\n    m := make(map[int]int)\n    for i, n := range nums {\n        if j, ok := m[target-n]; ok {\n            return []int{j, i}\n        }\n        m[n] = i\n    }\n    return nil\n}\n```",
            ),
            ChatMessage::user("what's meaning of make here?"),
        ];

        let tools = vec![serde_json::json!({
            "type": "function",
            "function": {
                "name": "shell",
                "description": "Run a shell command",
                "parameters": {
                    "type": "object",
                    "properties": {
                        "command": {"type": "string"}
                    },
                    "required": ["command"]
                }
            }
        })];

        let result = provider
            .chat_with_tools(&messages, &tools, "claude-opus-4-6", 0.7)
            .await;
        assert!(result.is_ok(), "chat_with_tools failed: {:?}", result.err());

        let body = captured
            .lock()
            .unwrap()
            .take()
            .expect("No request captured");

        // Verify system prompt extracted to top-level field
        let system = &body["system"];
        assert!(
            system.to_string().contains("helpful assistant"),
            "System prompt missing: {system}"
        );

        // Verify ALL conversation turns present in messages array
        let msgs = body["messages"].as_array().expect("messages not an array");
        assert_eq!(
            msgs.len(),
            3,
            "Expected 3 messages (2 user + 1 assistant), got {}",
            msgs.len()
        );

        // Turn 1: user with Go request
        assert_eq!(msgs[0]["role"], "user");
        let turn1_text = msgs[0]["content"].to_string();
        assert!(
            turn1_text.contains("2 sum"),
            "Turn 1 missing Go request: {turn1_text}"
        );

        // Turn 2: assistant with Go code
        assert_eq!(msgs[1]["role"], "assistant");
        let turn2_text = msgs[1]["content"].to_string();
        assert!(
            turn2_text.contains("make(map[int]int)"),
            "Turn 2 missing Go code: {turn2_text}"
        );

        // Turn 3: user follow-up
        assert_eq!(msgs[2]["role"], "user");
        let turn3_text = msgs[2]["content"].to_string();
        assert!(
            turn3_text.contains("meaning of make"),
            "Turn 3 missing follow-up: {turn3_text}"
        );

        // Verify native tools are present
        let api_tools = body["tools"].as_array().expect("tools not an array");
        assert_eq!(api_tools.len(), 1);
        assert_eq!(api_tools[0]["name"], "shell");
        assert!(
            api_tools[0]["input_schema"].is_object(),
            "Missing input_schema"
        );

        server_handle.abort();
    }

    #[test]
    fn native_response_parses_usage() {
        let json = r#"{
            "content": [{"type": "text", "text": "Hello"}],
            "usage": {"input_tokens": 300, "output_tokens": 75}
        }"#;
        let resp: NativeChatResponse = serde_json::from_str(json).unwrap();
        let result = AnthropicProvider::parse_native_response(resp);
        let usage = result.usage.unwrap();
        assert_eq!(usage.input_tokens, Some(300));
        assert_eq!(usage.output_tokens, Some(75));
    }

    #[test]
    fn native_response_parses_without_usage() {
        let json = r#"{"content": [{"type": "text", "text": "Hello"}]}"#;
        let resp: NativeChatResponse = serde_json::from_str(json).unwrap();
        let result = AnthropicProvider::parse_native_response(resp);
        assert!(result.usage.is_none());
    }

    #[test]
    fn capabilities_returns_vision_and_native_tools() {
        let provider = AnthropicProvider::new(Some("test-key"));
        let caps = provider.capabilities();
        assert!(
            caps.native_tool_calling,
            "Anthropic should support native tool calling"
        );
        assert!(caps.vision, "Anthropic should support vision");
    }

    #[test]
    fn convert_messages_with_image_marker_data_uri() {
        let messages = vec![ChatMessage {
            role: "user".to_string(),
            content: "Check this image: [IMAGE:data:image/jpeg;base64,/9j/4AAQ] What do you see?"
                .to_string(),
        }];

        let (_, native_msgs) = AnthropicProvider::convert_messages(&messages);

        assert_eq!(native_msgs.len(), 1);
        assert_eq!(native_msgs[0].role, "user");
        // Should have 2 content blocks: image + text
        assert_eq!(native_msgs[0].content.len(), 2);

        // First block should be image
        match &native_msgs[0].content[0] {
            NativeContentOut::Image { source } => {
                assert_eq!(source.source_type, "base64");
                assert_eq!(source.media_type, "image/jpeg");
                assert_eq!(source.data, "/9j/4AAQ");
            }
            _ => panic!("Expected Image content block"),
        }

        // Second block should be text (parse_image_markers may leave extra spaces)
        match &native_msgs[0].content[1] {
            NativeContentOut::Text { text, .. } => {
                // The text may have extra spaces where the marker was removed
                assert!(
                    text.contains("Check this image:") && text.contains("What do you see?"),
                    "Expected text to contain 'Check this image:' and 'What do you see?', got: {}",
                    text
                );
            }
            _ => panic!("Expected Text content block"),
        }
    }

    #[test]
    fn convert_messages_with_only_image_marker() {
        let messages = vec![ChatMessage {
            role: "user".to_string(),
            content: "[IMAGE:data:image/png;base64,iVBORw0KGgo]".to_string(),
        }];

        let (_, native_msgs) = AnthropicProvider::convert_messages(&messages);

        assert_eq!(native_msgs.len(), 1);
        assert_eq!(native_msgs[0].content.len(), 2);

        // First block should be image
        match &native_msgs[0].content[0] {
            NativeContentOut::Image { source } => {
                assert_eq!(source.media_type, "image/png");
            }
            _ => panic!("Expected Image content block"),
        }

        // Second block should be placeholder text
        match &native_msgs[0].content[1] {
            NativeContentOut::Text { text, .. } => {
                assert_eq!(text, "[image]");
            }
            _ => panic!("Expected Text content block with [image] placeholder"),
        }
    }

    #[test]
    fn convert_messages_without_image_marker() {
        let messages = vec![ChatMessage {
            role: "user".to_string(),
            content: "Hello, how are you?".to_string(),
        }];

        let (_, native_msgs) = AnthropicProvider::convert_messages(&messages);

        assert_eq!(native_msgs.len(), 1);
        assert_eq!(native_msgs[0].content.len(), 1);

        match &native_msgs[0].content[0] {
            NativeContentOut::Text { text, .. } => {
                assert_eq!(text, "Hello, how are you?");
            }
            _ => panic!("Expected Text content block"),
        }
    }

    #[test]
    fn image_content_serializes_correctly() {
        let content = NativeContentOut::Image {
            source: ImageSource {
                source_type: "base64".to_string(),
                media_type: "image/jpeg".to_string(),
                data: "testdata".to_string(),
            },
        };
        let json = serde_json::to_string(&content).unwrap();
        // The outer "type" is the enum tag, inner "type" (source_type) is renamed
        assert!(json.contains(r#""type":"image""#), "JSON: {}", json);
        assert!(json.contains(r#""type":"base64""#), "JSON: {}", json); // source_type is serialized as "type"
        assert!(
            json.contains(r#""media_type":"image/jpeg""#),
            "JSON: {}",
            json
        );
        assert!(json.contains(r#""data":"testdata""#), "JSON: {}", json);
    }

    #[test]
    fn convert_messages_merges_consecutive_tool_results() {
        // Simulate a multi-tool-call turn: assistant with two tool_use blocks
        // followed by two separate tool result messages.
        let messages = vec![
            ChatMessage {
                role: "system".to_string(),
                content: "You are helpful.".to_string(),
            },
            ChatMessage {
                role: "user".to_string(),
                content: "Do two things.".to_string(),
            },
            ChatMessage {
                role: "assistant".to_string(),
                content: serde_json::json!({
                    "content": "",
                    "tool_calls": [
                        {"id": "call_1", "name": "shell", "arguments": "{\"command\":\"ls\"}"},
                        {"id": "call_2", "name": "shell", "arguments": "{\"command\":\"pwd\"}"}
                    ]
                })
                .to_string(),
            },
            ChatMessage {
                role: "tool".to_string(),
                content: serde_json::json!({
                    "tool_call_id": "call_1",
                    "content": "file1.txt\nfile2.txt"
                })
                .to_string(),
            },
            ChatMessage {
                role: "tool".to_string(),
                content: serde_json::json!({
                    "tool_call_id": "call_2",
                    "content": "/home/user"
                })
                .to_string(),
            },
        ];

        let (system, native_msgs) = AnthropicProvider::convert_messages(&messages);

        assert!(system.is_some());
        // Should be: user, assistant, user (merged tool results)
        // NOT: user, assistant, user, user (which Anthropic rejects)
        assert_eq!(
            native_msgs.len(),
            3,
            "Expected 3 messages (user, assistant, merged tool results), got {}.\nRoles: {:?}",
            native_msgs.len(),
            native_msgs.iter().map(|m| &m.role).collect::<Vec<_>>()
        );
        assert_eq!(native_msgs[0].role, "user");
        assert_eq!(native_msgs[1].role, "assistant");
        assert_eq!(native_msgs[2].role, "user");
        // The merged user message should contain both tool results
        assert_eq!(
            native_msgs[2].content.len(),
            2,
            "Expected 2 tool_result blocks in merged message"
        );
    }

    #[test]
    fn convert_messages_no_adjacent_same_role() {
        // Verify that convert_messages never produces adjacent messages with the
        // same role, regardless of input ordering.
        let messages = vec![
            ChatMessage {
                role: "user".to_string(),
                content: "Hello".to_string(),
            },
            ChatMessage {
                role: "assistant".to_string(),
                content: serde_json::json!({
                    "content": "I'll run a command",
                    "tool_calls": [
                        {"id": "tc1", "name": "shell", "arguments": "{\"command\":\"echo hi\"}"}
                    ]
                })
                .to_string(),
            },
            ChatMessage {
                role: "tool".to_string(),
                content: serde_json::json!({
                    "tool_call_id": "tc1",
                    "content": "hi"
                })
                .to_string(),
            },
            ChatMessage {
                role: "user".to_string(),
                content: "Thanks!".to_string(),
            },
        ];

        let (_system, native_msgs) = AnthropicProvider::convert_messages(&messages);

        for window in native_msgs.windows(2) {
            assert_ne!(
                window[0].role, window[1].role,
                "Adjacent messages must not share the same role: found two '{}' messages in a row",
                window[0].role
            );
        }
    }
