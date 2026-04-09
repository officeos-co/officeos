use super::*;
use async_trait::async_trait;
use parking_lot::Mutex;
use std::collections::HashMap;

struct MockProvider {
    responses: Mutex<Vec<crate::providers::ChatResponse>>,
}

#[async_trait]
impl Provider for MockProvider {
    async fn chat_with_system(
        &self,
        _system_prompt: Option<&str>,
        _message: &str,
        _model: &str,
        _temperature: f64,
    ) -> Result<String> {
        Ok("ok".into())
    }

    async fn chat(
        &self,
        _request: ChatRequest<'_>,
        _model: &str,
        _temperature: f64,
    ) -> Result<crate::providers::ChatResponse> {
        let mut guard = self.responses.lock();
        if guard.is_empty() {
            return Ok(crate::providers::ChatResponse {
                text: Some("done".into()),
                tool_calls: vec![],
                usage: None,
                reasoning_content: None,
            });
        }
        Ok(guard.remove(0))
    }
}

struct ModelCaptureProvider {
    responses: Mutex<Vec<crate::providers::ChatResponse>>,
    seen_models: Arc<Mutex<Vec<String>>>,
}

#[async_trait]
impl Provider for ModelCaptureProvider {
    async fn chat_with_system(
        &self,
        _system_prompt: Option<&str>,
        _message: &str,
        _model: &str,
        _temperature: f64,
    ) -> Result<String> {
        Ok("ok".into())
    }

    async fn chat(
        &self,
        _request: ChatRequest<'_>,
        model: &str,
        _temperature: f64,
    ) -> Result<crate::providers::ChatResponse> {
        self.seen_models.lock().push(model.to_string());
        let mut guard = self.responses.lock();
        if guard.is_empty() {
            return Ok(crate::providers::ChatResponse {
                text: Some("done".into()),
                tool_calls: vec![],
                usage: None,
                reasoning_content: None,
            });
        }
        Ok(guard.remove(0))
    }
}

struct MockTool;

#[async_trait]
impl Tool for MockTool {
    fn name(&self) -> &str {
        "echo"
    }

    fn description(&self) -> &str {
        "echo"
    }

    fn parameters_schema(&self) -> serde_json::Value {
        serde_json::json!({"type": "object"})
    }

    async fn execute(&self, _args: serde_json::Value) -> Result<crate::tools::ToolResult> {
        Ok(crate::tools::ToolResult {
            success: true,
            output: "tool-out".into(),
            error: None,
        })
    }
}

#[tokio::test]
async fn turn_without_tools_returns_text() {
    let provider = Box::new(MockProvider {
        responses: Mutex::new(vec![crate::providers::ChatResponse {
            text: Some("hello".into()),
            tool_calls: vec![],
            usage: None,
            reasoning_content: None,
        }]),
    });

    let memory_cfg = crate::config::MemoryConfig {
        backend: "none".into(),
        ..crate::config::MemoryConfig::default()
    };
    let mem: Arc<dyn Memory> = Arc::from(
        crate::memory::create_memory(&memory_cfg, std::path::Path::new("/tmp"), None)
            .expect("memory creation should succeed with valid config"),
    );

    let observer: Arc<dyn Observer> = Arc::from(crate::observability::NoopObserver {});
    let mut agent = Agent::builder()
        .provider(provider)
        .tools(vec![Box::new(MockTool)])
        .memory(mem)
        .observer(observer)
        .tool_dispatcher(Box::new(XmlToolDispatcher))
        .workspace_dir(std::path::PathBuf::from("/tmp"))
        .build()
        .expect("agent builder should succeed with valid config");

    let response = agent.turn("hi").await.unwrap();
    assert_eq!(response, "hello");
}

#[tokio::test]
async fn turn_with_native_dispatcher_handles_tool_results_variant() {
    let provider = Box::new(MockProvider {
        responses: Mutex::new(vec![
            crate::providers::ChatResponse {
                text: Some(String::new()),
                tool_calls: vec![crate::providers::ToolCall {
                    id: "tc1".into(),
                    name: "echo".into(),
                    arguments: "{}".into(),
                }],
                usage: None,
                reasoning_content: None,
            },
            crate::providers::ChatResponse {
                text: Some("done".into()),
                tool_calls: vec![],
                usage: None,
                reasoning_content: None,
            },
        ]),
    });

    let memory_cfg = crate::config::MemoryConfig {
        backend: "none".into(),
        ..crate::config::MemoryConfig::default()
    };
    let mem: Arc<dyn Memory> = Arc::from(
        crate::memory::create_memory(&memory_cfg, std::path::Path::new("/tmp"), None)
            .expect("memory creation should succeed with valid config"),
    );

    let observer: Arc<dyn Observer> = Arc::from(crate::observability::NoopObserver {});
    let mut agent = Agent::builder()
        .provider(provider)
        .tools(vec![Box::new(MockTool)])
        .memory(mem)
        .observer(observer)
        .tool_dispatcher(Box::new(NativeToolDispatcher))
        .workspace_dir(std::path::PathBuf::from("/tmp"))
        .build()
        .expect("agent builder should succeed with valid config");

    let response = agent.turn("hi").await.unwrap();
    assert_eq!(response, "done");
    assert!(
        agent
            .history()
            .iter()
            .any(|msg| matches!(msg, ConversationMessage::ToolResults(_)))
    );
}

#[tokio::test]
async fn turn_routes_with_hint_when_query_classification_matches() {
    let seen_models = Arc::new(Mutex::new(Vec::new()));
    let provider = Box::new(ModelCaptureProvider {
        responses: Mutex::new(vec![crate::providers::ChatResponse {
            text: Some("classified".into()),
            tool_calls: vec![],
            usage: None,
            reasoning_content: None,
        }]),
        seen_models: seen_models.clone(),
    });

    let memory_cfg = crate::config::MemoryConfig {
        backend: "none".into(),
        ..crate::config::MemoryConfig::default()
    };
    let mem: Arc<dyn Memory> = Arc::from(
        crate::memory::create_memory(&memory_cfg, std::path::Path::new("/tmp"), None)
            .expect("memory creation should succeed with valid config"),
    );

    let observer: Arc<dyn Observer> = Arc::from(crate::observability::NoopObserver {});
    let mut route_model_by_hint = HashMap::new();
    route_model_by_hint.insert("fast".to_string(), "anthropic/claude-haiku-4-5".to_string());
    let mut agent = Agent::builder()
        .provider(provider)
        .tools(vec![Box::new(MockTool)])
        .memory(mem)
        .observer(observer)
        .tool_dispatcher(Box::new(NativeToolDispatcher))
        .workspace_dir(std::path::PathBuf::from("/tmp"))
        .classification_config(crate::config::QueryClassificationConfig {
            enabled: true,
            rules: vec![crate::config::ClassificationRule {
                hint: "fast".to_string(),
                keywords: vec!["quick".to_string()],
                patterns: vec![],
                min_length: None,
                max_length: None,
                priority: 10,
            }],
        })
        .available_hints(vec!["fast".to_string()])
        .route_model_by_hint(route_model_by_hint)
        .build()
        .expect("agent builder should succeed with valid config");

    let response = agent.turn("quick summary please").await.unwrap();
    assert_eq!(response, "classified");
    let seen = seen_models.lock();
    assert_eq!(seen.as_slice(), &["hint:fast".to_string()]);
}

#[tokio::test]
async fn from_config_passes_extra_headers_to_custom_provider() {
    use axum::{Json, Router, http::HeaderMap, routing::post};
    use tempfile::TempDir;
    use tokio::net::TcpListener;

    let captured_headers: Arc<std::sync::Mutex<Option<HashMap<String, String>>>> =
        Arc::new(std::sync::Mutex::new(None));
    let captured_headers_clone = captured_headers.clone();

    let app = Router::new().route(
        "/chat/completions",
        post(
            move |headers: HeaderMap, Json(_body): Json<serde_json::Value>| {
                let captured_headers = captured_headers_clone.clone();
                async move {
                    let collected = headers
                        .iter()
                        .filter_map(|(name, value)| {
                            value
                                .to_str()
                                .ok()
                                .map(|value| (name.as_str().to_string(), value.to_string()))
                        })
                        .collect();
                    *captured_headers.lock().unwrap() = Some(collected);
                    Json(serde_json::json!({
                        "choices": [{
                            "message": {
                                "content": "hello from mock"
                            }
                        }]
                    }))
                }
            },
        ),
    );

    let listener = TcpListener::bind("127.0.0.1:0").await.unwrap();
    let addr = listener.local_addr().unwrap();
    let server_handle = tokio::spawn(async move {
        axum::serve(listener, app).await.unwrap();
    });

    let tmp = TempDir::new().expect("temp dir");
    let workspace_dir = tmp.path().join("workspace");
    std::fs::create_dir_all(&workspace_dir).unwrap();
    // Seed the required personality files that the Phase 3 strict
    // loader expects during agent boot. In production these are
    // mounted into the pod as a K8s ConfigMap by the dashboard
    // backend; in tests we write them inline.
    for (name, content) in [
        ("SOUL.md", "Test soul."),
        ("IDENTITY.md", "Test identity."),
        ("AGENTS.md", "Test agents."),
    ] {
        std::fs::write(workspace_dir.join(name), content).unwrap();
    }

    let mut config = crate::config::Config::default();
    config.workspace_dir = workspace_dir;
    config.config_path = tmp.path().join("config.toml");
    config.api_key = Some("test-key".to_string());
    config.default_provider = Some(format!("custom:http://{addr}"));
    config.default_model = Some("test-model".to_string());
    config.memory.backend = "none".to_string();
    config.memory.auto_save = false;
    config.extra_headers.insert(
        "User-Agent".to_string(),
        "zeroclaw-web-test/1.0".to_string(),
    );
    config
        .extra_headers
        .insert("X-Title".to_string(), "zeroclaw-web".to_string());

    let mut agent = Agent::from_config(&config)
        .await
        .expect("agent from config");
    let response = agent.turn("hello").await.expect("agent turn");

    assert_eq!(response, "hello from mock");

    let headers = captured_headers
        .lock()
        .unwrap()
        .clone()
        .expect("captured headers");
    assert_eq!(
        headers.get("user-agent").map(String::as_str),
        Some("zeroclaw-web-test/1.0")
    );
    assert_eq!(
        headers.get("x-title").map(String::as_str),
        Some("zeroclaw-web")
    );

    server_handle.abort();
}

#[test]
fn builder_allowed_tools_none_keeps_all_tools() {
    let provider = Box::new(MockProvider {
        responses: Mutex::new(vec![]),
    });

    let memory_cfg = crate::config::MemoryConfig {
        backend: "none".into(),
        ..crate::config::MemoryConfig::default()
    };
    let mem: Arc<dyn Memory> = Arc::from(
        crate::memory::create_memory(&memory_cfg, std::path::Path::new("/tmp"), None)
            .expect("memory creation should succeed with valid config"),
    );

    let observer: Arc<dyn Observer> = Arc::from(crate::observability::NoopObserver {});
    let agent = Agent::builder()
        .provider(provider)
        .tools(vec![Box::new(MockTool)])
        .memory(mem)
        .observer(observer)
        .tool_dispatcher(Box::new(NativeToolDispatcher))
        .workspace_dir(std::path::PathBuf::from("/tmp"))
        .allowed_tools(None)
        .build()
        .expect("agent builder should succeed with valid config");

    assert_eq!(agent.tool_specs.len(), 1);
    assert_eq!(agent.tool_specs[0].name, "echo");
}

#[test]
fn builder_allowed_tools_some_filters_tools() {
    let provider = Box::new(MockProvider {
        responses: Mutex::new(vec![]),
    });

    let memory_cfg = crate::config::MemoryConfig {
        backend: "none".into(),
        ..crate::config::MemoryConfig::default()
    };
    let mem: Arc<dyn Memory> = Arc::from(
        crate::memory::create_memory(&memory_cfg, std::path::Path::new("/tmp"), None)
            .expect("memory creation should succeed with valid config"),
    );

    let observer: Arc<dyn Observer> = Arc::from(crate::observability::NoopObserver {});
    let agent = Agent::builder()
        .provider(provider)
        .tools(vec![Box::new(MockTool)])
        .memory(mem)
        .observer(observer)
        .tool_dispatcher(Box::new(NativeToolDispatcher))
        .workspace_dir(std::path::PathBuf::from("/tmp"))
        .allowed_tools(Some(vec!["nonexistent".to_string()]))
        .build()
        .expect("agent builder should succeed with valid config");

    assert!(
        agent.tool_specs.is_empty(),
        "No tools should match a non-existent allowlist entry"
    );
}

#[test]
fn seed_history_prepends_system_and_skips_system_from_seed() {
    let provider = Box::new(MockProvider {
        responses: Mutex::new(vec![]),
    });

    let memory_cfg = crate::config::MemoryConfig {
        backend: "none".into(),
        ..crate::config::MemoryConfig::default()
    };
    let mem: Arc<dyn Memory> = Arc::from(
        crate::memory::create_memory(&memory_cfg, std::path::Path::new("/tmp"), None)
            .expect("memory creation should succeed with valid config"),
    );

    let observer: Arc<dyn Observer> = Arc::from(crate::observability::NoopObserver {});
    let mut agent = Agent::builder()
        .provider(provider)
        .tools(vec![Box::new(MockTool)])
        .memory(mem)
        .observer(observer)
        .tool_dispatcher(Box::new(NativeToolDispatcher))
        .workspace_dir(std::path::PathBuf::from("/tmp"))
        .build()
        .expect("agent builder should succeed with valid config");

    let seed = vec![
        ChatMessage::system("old system prompt"),
        ChatMessage::user("hello"),
        ChatMessage::assistant("hi there"),
    ];
    agent.seed_history(&seed);

    let history = agent.history();
    // First message should be a freshly built system prompt (not the seed one)
    assert!(matches!(&history[0], ConversationMessage::Chat(m) if m.role == "system"));
    // System message from seed should be skipped, so next is user
    assert!(
        matches!(&history[1], ConversationMessage::Chat(m) if m.role == "user" && m.content == "hello")
    );
    assert!(
        matches!(&history[2], ConversationMessage::Chat(m) if m.role == "assistant" && m.content == "hi there")
    );
    assert_eq!(history.len(), 3);
}

/// Mock provider that captures whether tool specs were passed to `stream_chat`
/// and returns a tool call followed by a text response through the stream.
struct StreamToolCaptureProvider {
    tools_received: Arc<Mutex<Vec<bool>>>,
    call_count: Arc<Mutex<usize>>,
}

#[async_trait]
impl Provider for StreamToolCaptureProvider {
    async fn chat_with_system(
        &self,
        _system_prompt: Option<&str>,
        _message: &str,
        _model: &str,
        _temperature: f64,
    ) -> Result<String> {
        Ok("ok".into())
    }

    async fn chat(
        &self,
        request: ChatRequest<'_>,
        _model: &str,
        _temperature: f64,
    ) -> Result<crate::providers::ChatResponse> {
        self.tools_received.lock().push(request.tools.is_some());
        let mut count = self.call_count.lock();
        *count += 1;
        if *count == 1 {
            Ok(crate::providers::ChatResponse {
                text: Some(String::new()),
                tool_calls: vec![crate::providers::ToolCall {
                    id: "tc_stream_1".into(),
                    name: "echo".into(),
                    arguments: "{}".into(),
                }],
                usage: None,
                reasoning_content: None,
            })
        } else {
            Ok(crate::providers::ChatResponse {
                text: Some("stream-done".into()),
                tool_calls: vec![],
                usage: None,
                reasoning_content: None,
            })
        }
    }

    fn supports_native_tools(&self) -> bool {
        true
    }

    fn stream_chat(
        &self,
        request: ChatRequest<'_>,
        _model: &str,
        _temperature: f64,
        _options: crate::providers::traits::StreamOptions,
    ) -> futures_util::stream::BoxStream<
        'static,
        crate::providers::traits::StreamResult<crate::providers::traits::StreamEvent>,
    > {
        use futures_util::stream::{self, StreamExt};
        self.tools_received.lock().push(request.tools.is_some());
        let mut count = self.call_count.lock();
        *count += 1;
        if *count == 1 {
            let tc = crate::providers::traits::StreamEvent::ToolCall(crate::providers::ToolCall {
                id: "tc_stream_1".into(),
                name: "echo".into(),
                arguments: "{}".into(),
            });
            stream::iter(vec![
                Ok(tc),
                Ok(crate::providers::traits::StreamEvent::Final),
            ])
            .boxed()
        } else {
            let chunk = crate::providers::traits::StreamEvent::TextDelta(
                crate::providers::traits::StreamChunk {
                    delta: "stream-done".into(),
                    is_final: false,
                    reasoning: None,
                    token_count: 0,
                },
            );
            stream::iter(vec![
                Ok(chunk),
                Ok(crate::providers::traits::StreamEvent::Final),
            ])
            .boxed()
        }
    }
}

#[tokio::test]
async fn turn_streamed_passes_tool_specs_to_provider() {
    let tools_received = Arc::new(Mutex::new(Vec::new()));
    let provider = Box::new(StreamToolCaptureProvider {
        tools_received: tools_received.clone(),
        call_count: Arc::new(Mutex::new(0)),
    });

    let memory_cfg = crate::config::MemoryConfig {
        backend: "none".into(),
        ..crate::config::MemoryConfig::default()
    };
    let mem: Arc<dyn Memory> = Arc::from(
        crate::memory::create_memory(&memory_cfg, std::path::Path::new("/tmp"), None)
            .expect("memory creation should succeed with valid config"),
    );

    let observer: Arc<dyn Observer> = Arc::from(crate::observability::NoopObserver {});
    let mut agent = Agent::builder()
        .provider(provider)
        .tools(vec![Box::new(MockTool)])
        .memory(mem)
        .observer(observer)
        .tool_dispatcher(Box::new(NativeToolDispatcher))
        .workspace_dir(std::path::PathBuf::from("/tmp"))
        .build()
        .expect("agent builder should succeed with valid config");

    let (event_tx, mut event_rx) = tokio::sync::mpsc::channel::<TurnEvent>(64);
    let response = agent
        .turn_streamed("use the echo tool", event_tx)
        .await
        .unwrap();
    assert_eq!(response, "stream-done");

    // Verify tools were passed in both stream_chat calls
    let received = tools_received.lock();
    assert!(
        received.len() >= 2,
        "Expected at least 2 stream_chat calls, got {}",
        received.len()
    );
    assert!(
        received[0],
        "First stream_chat call should have received tool specs"
    );
    assert!(
        received[1],
        "Second stream_chat call should have received tool specs"
    );

    // Collect events and verify tool call + tool result were emitted
    let mut events = Vec::new();
    while let Ok(ev) = event_rx.try_recv() {
        events.push(ev);
    }
    let has_tool_call = events
        .iter()
        .any(|e| matches!(e, TurnEvent::ToolCall { name, .. } if name == "echo"));
    let has_tool_result = events
        .iter()
        .any(|e| matches!(e, TurnEvent::ToolResult { name, .. } if name == "echo"));
    assert!(
        has_tool_call,
        "Should have emitted a ToolCall event for 'echo'"
    );
    assert!(
        has_tool_result,
        "Should have emitted a ToolResult event for 'echo'"
    );
}
