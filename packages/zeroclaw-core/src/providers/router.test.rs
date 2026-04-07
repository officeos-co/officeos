    use super::*;
    use crate::tools::ToolSpec;
    use futures_util::StreamExt;
    use std::sync::Arc;
    use std::sync::atomic::{AtomicUsize, Ordering};

    struct MockProvider {
        calls: Arc<AtomicUsize>,
        response: &'static str,
        last_model: parking_lot::Mutex<String>,
    }

    impl MockProvider {
        fn new(response: &'static str) -> Self {
            Self {
                calls: Arc::new(AtomicUsize::new(0)),
                response,
                last_model: parking_lot::Mutex::new(String::new()),
            }
        }

        fn call_count(&self) -> usize {
            self.calls.load(Ordering::SeqCst)
        }

        fn last_model(&self) -> String {
            self.last_model.lock().clone()
        }
    }

    #[async_trait]
    impl Provider for MockProvider {
        async fn chat_with_system(
            &self,
            _system_prompt: Option<&str>,
            _message: &str,
            model: &str,
            _temperature: f64,
        ) -> anyhow::Result<String> {
            self.calls.fetch_add(1, Ordering::SeqCst);
            *self.last_model.lock() = model.to_string();
            Ok(self.response.to_string())
        }
    }

    fn make_router(
        providers: Vec<(&'static str, &'static str)>,
        routes: Vec<(&str, &str, &str)>,
    ) -> (RouterProvider, Vec<Arc<MockProvider>>) {
        let mocks: Vec<Arc<MockProvider>> = providers
            .iter()
            .map(|(_, response)| Arc::new(MockProvider::new(response)))
            .collect();

        let provider_list: Vec<(String, Box<dyn Provider>)> = providers
            .iter()
            .zip(mocks.iter())
            .map(|((name, _), mock)| {
                (
                    (*name).to_string(),
                    Box::new(Arc::clone(mock)) as Box<dyn Provider>,
                )
            })
            .collect();

        let route_list: Vec<(String, Route)> = routes
            .iter()
            .map(|(hint, provider_name, model)| {
                (
                    (*hint).to_string(),
                    Route {
                        provider_name: (*provider_name).to_string(),
                        model: (*model).to_string(),
                    },
                )
            })
            .collect();

        let router = RouterProvider::new(provider_list, route_list, "default-model".to_string());

        (router, mocks)
    }

    // Arc<MockProvider> should also be a Provider
    #[async_trait]
    impl Provider for Arc<MockProvider> {
        async fn chat_with_system(
            &self,
            system_prompt: Option<&str>,
            message: &str,
            model: &str,
            temperature: f64,
        ) -> anyhow::Result<String> {
            self.as_ref()
                .chat_with_system(system_prompt, message, model, temperature)
                .await
        }
    }

    struct StreamingMockProvider {
        stream_calls: Arc<AtomicUsize>,
        last_stream_model: parking_lot::Mutex<String>,
        response: &'static str,
    }

    impl StreamingMockProvider {
        fn new(response: &'static str) -> Self {
            Self {
                stream_calls: Arc::new(AtomicUsize::new(0)),
                last_stream_model: parking_lot::Mutex::new(String::new()),
                response,
            }
        }
    }

    #[async_trait]
    impl Provider for StreamingMockProvider {
        async fn chat_with_system(
            &self,
            _system_prompt: Option<&str>,
            _message: &str,
            _model: &str,
            _temperature: f64,
        ) -> anyhow::Result<String> {
            Ok("ok".to_string())
        }

        fn supports_streaming(&self) -> bool {
            true
        }

        fn stream_chat_with_history(
            &self,
            _messages: &[ChatMessage],
            model: &str,
            _temperature: f64,
            _options: StreamOptions,
        ) -> BoxStream<'static, StreamResult<StreamChunk>> {
            self.stream_calls.fetch_add(1, Ordering::SeqCst);
            *self.last_stream_model.lock() = model.to_string();
            let chunks = vec![
                Ok(StreamChunk::delta(self.response)),
                Ok(StreamChunk::final_chunk()),
            ];
            futures_util::stream::iter(chunks).boxed()
        }
    }

    #[async_trait]
    impl Provider for Arc<StreamingMockProvider> {
        async fn chat_with_system(
            &self,
            system_prompt: Option<&str>,
            message: &str,
            model: &str,
            temperature: f64,
        ) -> anyhow::Result<String> {
            self.as_ref()
                .chat_with_system(system_prompt, message, model, temperature)
                .await
        }

        fn supports_streaming(&self) -> bool {
            self.as_ref().supports_streaming()
        }

        fn stream_chat_with_history(
            &self,
            messages: &[ChatMessage],
            model: &str,
            temperature: f64,
            options: StreamOptions,
        ) -> BoxStream<'static, StreamResult<StreamChunk>> {
            self.as_ref()
                .stream_chat_with_history(messages, model, temperature, options)
        }
    }

    struct ToolEventStreamingMockProvider {
        stream_calls: Arc<AtomicUsize>,
        tool_event_calls: Arc<AtomicUsize>,
        last_stream_model: parking_lot::Mutex<String>,
    }

    impl ToolEventStreamingMockProvider {
        fn new() -> Self {
            Self {
                stream_calls: Arc::new(AtomicUsize::new(0)),
                tool_event_calls: Arc::new(AtomicUsize::new(0)),
                last_stream_model: parking_lot::Mutex::new(String::new()),
            }
        }
    }

    #[async_trait]
    impl Provider for ToolEventStreamingMockProvider {
        async fn chat_with_system(
            &self,
            _system_prompt: Option<&str>,
            _message: &str,
            _model: &str,
            _temperature: f64,
        ) -> anyhow::Result<String> {
            Ok("ok".to_string())
        }

        fn supports_streaming(&self) -> bool {
            true
        }

        fn supports_streaming_tool_events(&self) -> bool {
            true
        }

        fn stream_chat(
            &self,
            request: ChatRequest<'_>,
            model: &str,
            _temperature: f64,
            _options: StreamOptions,
        ) -> BoxStream<'static, StreamResult<StreamEvent>> {
            self.stream_calls.fetch_add(1, Ordering::SeqCst);
            if request.tools.is_some_and(|tools| !tools.is_empty()) {
                self.tool_event_calls.fetch_add(1, Ordering::SeqCst);
            }
            *self.last_stream_model.lock() = model.to_string();
            futures_util::stream::iter(vec![
                Ok(StreamEvent::ToolCall(crate::providers::ToolCall {
                    id: "call_router_1".to_string(),
                    name: "shell".to_string(),
                    arguments: r#"{"command":"date"}"#.to_string(),
                })),
                Ok(StreamEvent::Final),
            ])
            .boxed()
        }
    }

    #[async_trait]
    impl Provider for Arc<ToolEventStreamingMockProvider> {
        async fn chat_with_system(
            &self,
            system_prompt: Option<&str>,
            message: &str,
            model: &str,
            temperature: f64,
        ) -> anyhow::Result<String> {
            self.as_ref()
                .chat_with_system(system_prompt, message, model, temperature)
                .await
        }

        fn supports_streaming(&self) -> bool {
            self.as_ref().supports_streaming()
        }

        fn supports_streaming_tool_events(&self) -> bool {
            self.as_ref().supports_streaming_tool_events()
        }

        fn stream_chat(
            &self,
            request: ChatRequest<'_>,
            model: &str,
            temperature: f64,
            options: StreamOptions,
        ) -> BoxStream<'static, StreamResult<StreamEvent>> {
            self.as_ref()
                .stream_chat(request, model, temperature, options)
        }
    }

    #[tokio::test]
    async fn routes_hint_to_correct_provider() {
        let (router, mocks) = make_router(
            vec![("fast", "fast-response"), ("smart", "smart-response")],
            vec![
                ("fast", "fast", "llama-3-70b"),
                ("reasoning", "smart", "claude-opus"),
            ],
        );

        let result = router
            .simple_chat("hello", "hint:reasoning", 0.5)
            .await
            .unwrap();
        assert_eq!(result, "smart-response");
        assert_eq!(mocks[1].call_count(), 1);
        assert_eq!(mocks[1].last_model(), "claude-opus");
        assert_eq!(mocks[0].call_count(), 0);
    }

    #[tokio::test]
    async fn routes_fast_hint() {
        let (router, mocks) = make_router(
            vec![("fast", "fast-response"), ("smart", "smart-response")],
            vec![("fast", "fast", "llama-3-70b")],
        );

        let result = router.simple_chat("hello", "hint:fast", 0.5).await.unwrap();
        assert_eq!(result, "fast-response");
        assert_eq!(mocks[0].call_count(), 1);
        assert_eq!(mocks[0].last_model(), "llama-3-70b");
    }

    #[tokio::test]
    async fn unknown_hint_falls_back_to_default() {
        let (router, mocks) = make_router(
            vec![("default", "default-response"), ("other", "other-response")],
            vec![],
        );

        let result = router
            .simple_chat("hello", "hint:nonexistent", 0.5)
            .await
            .unwrap();
        assert_eq!(result, "default-response");
        assert_eq!(mocks[0].call_count(), 1);
        // Falls back to default with the hint as model name
        assert_eq!(mocks[0].last_model(), "hint:nonexistent");
    }

    #[tokio::test]
    async fn non_hint_model_uses_default_provider() {
        let (router, mocks) = make_router(
            vec![
                ("primary", "primary-response"),
                ("secondary", "secondary-response"),
            ],
            vec![("code", "secondary", "codellama")],
        );

        let result = router
            .simple_chat("hello", "anthropic/claude-sonnet-4-20250514", 0.5)
            .await
            .unwrap();
        assert_eq!(result, "primary-response");
        assert_eq!(mocks[0].call_count(), 1);
        assert_eq!(mocks[0].last_model(), "anthropic/claude-sonnet-4-20250514");
    }

    #[test]
    fn resolve_preserves_model_for_non_hints() {
        let (router, _) = make_router(vec![("default", "ok")], vec![]);

        let (idx, model) = router.resolve("gpt-4o");
        assert_eq!(idx, 0);
        assert_eq!(model, "gpt-4o");
    }

    #[test]
    fn resolve_strips_hint_prefix() {
        let (router, _) = make_router(
            vec![("fast", "ok"), ("smart", "ok")],
            vec![("reasoning", "smart", "claude-opus")],
        );

        let (idx, model) = router.resolve("hint:reasoning");
        assert_eq!(idx, 1);
        assert_eq!(model, "claude-opus");
    }

    #[test]
    fn skips_routes_with_unknown_provider() {
        let (router, _) = make_router(
            vec![("default", "ok")],
            vec![("broken", "nonexistent", "model")],
        );

        // Route should not exist
        assert!(!router.routes.contains_key("broken"));
    }

    #[tokio::test]
    async fn warmup_calls_all_providers() {
        let (router, _) = make_router(vec![("a", "ok"), ("b", "ok")], vec![]);

        // Warmup should not error
        assert!(router.warmup().await.is_ok());
    }

    #[tokio::test]
    async fn chat_with_system_passes_system_prompt() {
        let mock = Arc::new(MockProvider::new("response"));
        let router = RouterProvider::new(
            vec![(
                "default".into(),
                Box::new(Arc::clone(&mock)) as Box<dyn Provider>,
            )],
            vec![],
            "model".into(),
        );

        let result = router
            .chat_with_system(Some("system"), "hello", "model", 0.5)
            .await
            .unwrap();
        assert_eq!(result, "response");
        assert_eq!(mock.call_count(), 1);
    }

    #[tokio::test]
    async fn chat_with_tools_delegates_to_resolved_provider() {
        let mock = Arc::new(MockProvider::new("tool-response"));
        let router = RouterProvider::new(
            vec![(
                "default".into(),
                Box::new(Arc::clone(&mock)) as Box<dyn Provider>,
            )],
            vec![],
            "model".into(),
        );

        let messages = vec![ChatMessage {
            role: "user".to_string(),
            content: "use tools".to_string(),
        }];
        let tools = vec![serde_json::json!({
            "type": "function",
            "function": {
                "name": "shell",
                "description": "Run shell command",
                "parameters": {}
            }
        })];

        // chat_with_tools should delegate through the router to the mock.
        // MockProvider's default chat_with_tools calls chat_with_history -> chat_with_system.
        let result = router
            .chat_with_tools(&messages, &tools, "model", 0.7)
            .await
            .unwrap();
        assert_eq!(result.text.as_deref(), Some("tool-response"));
        assert_eq!(mock.call_count(), 1);
        assert_eq!(mock.last_model(), "model");
    }

    #[tokio::test]
    async fn chat_with_tools_routes_hint_correctly() {
        let (router, mocks) = make_router(
            vec![("fast", "fast-tool"), ("smart", "smart-tool")],
            vec![("reasoning", "smart", "claude-opus")],
        );

        let messages = vec![ChatMessage {
            role: "user".to_string(),
            content: "reason about this".to_string(),
        }];
        let tools = vec![serde_json::json!({"type": "function", "function": {"name": "test"}})];

        let result = router
            .chat_with_tools(&messages, &tools, "hint:reasoning", 0.5)
            .await
            .unwrap();
        assert_eq!(result.text.as_deref(), Some("smart-tool"));
        assert_eq!(mocks[1].call_count(), 1);
        assert_eq!(mocks[1].last_model(), "claude-opus");
        assert_eq!(mocks[0].call_count(), 0);
    }

    // ── Cost-optimized routing tests ────────────────────────────────

    use crate::providers::traits::ProviderCapabilities;

    /// Mock provider with configurable capability flags.
    struct CapableMockProvider {
        response: &'static str,
        vision: bool,
        tools: bool,
    }

    impl CapableMockProvider {
        fn new(response: &'static str, vision: bool, tools: bool) -> Self {
            Self {
                response,
                vision,
                tools,
            }
        }
    }

    #[async_trait]
    impl Provider for CapableMockProvider {
        fn capabilities(&self) -> ProviderCapabilities {
            ProviderCapabilities {
                native_tool_calling: self.tools,
                vision: self.vision,
                prompt_caching: false,
            }
        }

        async fn chat_with_system(
            &self,
            _system_prompt: Option<&str>,
            _message: &str,
            _model: &str,
            _temperature: f64,
        ) -> anyhow::Result<String> {
            Ok(self.response.to_string())
        }
    }

    fn make_pricing(entries: Vec<(&str, f64, f64)>) -> HashMap<String, ModelPricing> {
        entries
            .into_iter()
            .map(|(model, input, output)| (model.to_string(), ModelPricing { input, output }))
            .collect()
    }

    #[test]
    fn cost_optimized_selects_cheapest_provider() {
        let providers: Vec<(String, Box<dyn Provider>)> = vec![
            (
                "expensive".into(),
                Box::new(CapableMockProvider::new("exp", false, false)),
            ),
            (
                "cheap".into(),
                Box::new(CapableMockProvider::new("chp", false, false)),
            ),
        ];
        let routes = vec![
            (
                "expensive".to_string(),
                Route {
                    provider_name: "expensive".into(),
                    model: "big-model".into(),
                },
            ),
            (
                "cheap".to_string(),
                Route {
                    provider_name: "cheap".into(),
                    model: "small-model".into(),
                },
            ),
        ];
        let router = RouterProvider::new(providers, routes, "default-model".into());

        let prices = make_pricing(vec![("big-model", 15.0, 75.0), ("small-model", 0.25, 1.25)]);

        let (idx, model) =
            router.resolve_cost_optimized("hint:cost-optimized", &prices, false, false);
        assert_eq!(model, "small-model");
        assert_eq!(idx, 1);
    }

    #[test]
    fn cost_optimized_respects_vision_requirement() {
        let providers: Vec<(String, Box<dyn Provider>)> = vec![
            (
                "no-vision".into(),
                Box::new(CapableMockProvider::new("nv", false, false)),
            ),
            (
                "has-vision".into(),
                Box::new(CapableMockProvider::new("hv", true, false)),
            ),
        ];
        let routes = vec![
            (
                "cheap".to_string(),
                Route {
                    provider_name: "no-vision".into(),
                    model: "cheap-model".into(),
                },
            ),
            (
                "vision".to_string(),
                Route {
                    provider_name: "has-vision".into(),
                    model: "vision-model".into(),
                },
            ),
        ];
        let router = RouterProvider::new(providers, routes, "default-model".into());

        let prices = make_pricing(vec![
            ("cheap-model", 0.10, 0.40),
            ("vision-model", 3.0, 15.0),
        ]);

        // With vision required, the cheap model (no vision) is filtered out
        let (_, model) = router.resolve_cost_optimized("hint:cheapest", &prices, true, false);
        assert_eq!(model, "vision-model");
    }

    #[test]
    fn cost_optimized_respects_tools_requirement() {
        let providers: Vec<(String, Box<dyn Provider>)> = vec![
            (
                "no-tools".into(),
                Box::new(CapableMockProvider::new("nt", false, false)),
            ),
            (
                "has-tools".into(),
                Box::new(CapableMockProvider::new("ht", false, true)),
            ),
        ];
        let routes = vec![
            (
                "basic".to_string(),
                Route {
                    provider_name: "no-tools".into(),
                    model: "basic-model".into(),
                },
            ),
            (
                "tools".to_string(),
                Route {
                    provider_name: "has-tools".into(),
                    model: "tools-model".into(),
                },
            ),
        ];
        let router = RouterProvider::new(providers, routes, "default-model".into());

        let prices = make_pricing(vec![
            ("basic-model", 0.10, 0.40),
            ("tools-model", 5.0, 15.0),
        ]);

        // With tools required, the basic model (no tools) is filtered out
        let (_, model) = router.resolve_cost_optimized("hint:cost-optimized", &prices, false, true);
        assert_eq!(model, "tools-model");
    }

    #[test]
    fn cost_optimized_falls_back_when_no_pricing() {
        let (router, _) = make_router(
            vec![("default", "ok"), ("other", "ok")],
            vec![("route-a", "other", "some-model")],
        );

        // Empty pricing map — no matches possible
        let prices: HashMap<String, ModelPricing> = HashMap::new();
        let (idx, model) =
            router.resolve_cost_optimized("hint:cost-optimized", &prices, false, false);
        assert_eq!(idx, 0);
        assert_eq!(model, "default-model");
    }

    #[test]
    fn cost_optimized_with_single_route() {
        let providers: Vec<(String, Box<dyn Provider>)> = vec![(
            "only".into(),
            Box::new(CapableMockProvider::new("ok", false, false)),
        )];
        let routes = vec![(
            "single".to_string(),
            Route {
                provider_name: "only".into(),
                model: "the-model".into(),
            },
        )];
        let router = RouterProvider::new(providers, routes, "default-model".into());

        let prices = make_pricing(vec![("the-model", 1.0, 2.0)]);

        let (idx, model) = router.resolve_cost_optimized("hint:cheapest", &prices, false, false);
        assert_eq!(idx, 0);
        assert_eq!(model, "the-model");
    }

    #[test]
    fn cost_optimized_prefers_lower_total_cost() {
        let providers: Vec<(String, Box<dyn Provider>)> = vec![
            (
                "p1".into(),
                Box::new(CapableMockProvider::new("r1", false, false)),
            ),
            (
                "p2".into(),
                Box::new(CapableMockProvider::new("r2", false, false)),
            ),
            (
                "p3".into(),
                Box::new(CapableMockProvider::new("r3", false, false)),
            ),
        ];
        let routes = vec![
            (
                "a".to_string(),
                Route {
                    provider_name: "p1".into(),
                    model: "model-a".into(),
                },
            ),
            (
                "b".to_string(),
                Route {
                    provider_name: "p2".into(),
                    model: "model-b".into(),
                },
            ),
            (
                "c".to_string(),
                Route {
                    provider_name: "p3".into(),
                    model: "model-c".into(),
                },
            ),
        ];
        let router = RouterProvider::new(providers, routes, "default-model".into());

        let prices = make_pricing(vec![
            ("model-a", 10.0, 50.0), // total: 60
            ("model-b", 0.15, 0.60), // total: 0.75 (cheapest)
            ("model-c", 3.0, 15.0),  // total: 18
        ]);

        let (idx, model) =
            router.resolve_cost_optimized("hint:cost-optimized", &prices, false, false);
        assert_eq!(model, "model-b");
        assert_eq!(idx, 1);
    }

    #[test]
    fn cost_optimized_strategy_score() {
        let prices = make_pricing(vec![("cheap", 0.10, 0.40), ("expensive", 15.0, 75.0)]);
        let strategy = CostOptimizedStrategy::new(prices);

        assert!((strategy.score("cheap").unwrap() - 0.50).abs() < f64::EPSILON);
        assert!((strategy.score("expensive").unwrap() - 90.0).abs() < f64::EPSILON);
        assert!(strategy.score("unknown").is_none());
    }

    #[tokio::test]
    async fn supports_streaming_returns_true_when_any_provider_supports_it() {
        let streaming = Arc::new(StreamingMockProvider::new("stream"));
        let router = RouterProvider::new(
            vec![
                (
                    "default".into(),
                    Box::new(MockProvider::new("default")) as Box<dyn Provider>,
                ),
                (
                    "streaming".into(),
                    Box::new(Arc::clone(&streaming)) as Box<dyn Provider>,
                ),
            ],
            vec![(
                "reasoning".into(),
                Route {
                    provider_name: "streaming".into(),
                    model: "claude-opus".into(),
                },
            )],
            "model".into(),
        );

        assert!(router.supports_streaming());
    }

    #[tokio::test]
    async fn stream_chat_with_history_routes_hint_to_correct_provider_and_model() {
        let streaming = Arc::new(StreamingMockProvider::new("streamed response"));
        let router = RouterProvider::new(
            vec![
                (
                    "default".into(),
                    Box::new(MockProvider::new("default")) as Box<dyn Provider>,
                ),
                (
                    "streaming".into(),
                    Box::new(Arc::clone(&streaming)) as Box<dyn Provider>,
                ),
            ],
            vec![(
                "reasoning".into(),
                Route {
                    provider_name: "streaming".into(),
                    model: "claude-opus".into(),
                },
            )],
            "model".into(),
        );

        let messages = vec![ChatMessage::user("hello")];
        let mut stream = router.stream_chat_with_history(
            &messages,
            "hint:reasoning",
            0.0,
            StreamOptions::new(true),
        );

        let mut collected = String::new();
        while let Some(chunk) = stream.next().await {
            let chunk = chunk.expect("stream chunk should be ok");
            collected.push_str(&chunk.delta);
        }

        assert_eq!(collected, "streamed response");
        assert_eq!(streaming.stream_calls.load(Ordering::SeqCst), 1);
        assert_eq!(*streaming.last_stream_model.lock(), "claude-opus");
    }

    #[tokio::test]
    async fn stream_chat_routes_hint_with_structured_tool_events() {
        let streaming = Arc::new(ToolEventStreamingMockProvider::new());
        let router = RouterProvider::new(
            vec![
                (
                    "default".into(),
                    Box::new(MockProvider::new("default")) as Box<dyn Provider>,
                ),
                (
                    "streaming".into(),
                    Box::new(Arc::clone(&streaming)) as Box<dyn Provider>,
                ),
            ],
            vec![(
                "reasoning".into(),
                Route {
                    provider_name: "streaming".into(),
                    model: "claude-opus".into(),
                },
            )],
            "model".into(),
        );

        let messages = vec![ChatMessage::user("hello")];
        let tools = vec![ToolSpec {
            name: "shell".to_string(),
            description: "run shell commands".to_string(),
            parameters: serde_json::json!({
                "type": "object",
                "properties": {
                    "command": { "type": "string" }
                }
            }),
        }];

        let mut stream = router.stream_chat(
            ChatRequest {
                messages: &messages,
                tools: Some(&tools),
            },
            "hint:reasoning",
            0.0,
            StreamOptions::new(true),
        );

        let first = stream.next().await.unwrap().unwrap();
        let second = stream.next().await.unwrap().unwrap();
        assert!(stream.next().await.is_none());

        match first {
            StreamEvent::ToolCall(call) => {
                assert_eq!(call.name, "shell");
                assert_eq!(call.arguments, r#"{"command":"date"}"#);
            }
            other => panic!("expected tool-call event, got {other:?}"),
        }
        assert!(matches!(second, StreamEvent::Final));
        assert_eq!(streaming.stream_calls.load(Ordering::SeqCst), 1);
        assert_eq!(streaming.tool_event_calls.load(Ordering::SeqCst), 1);
        assert_eq!(*streaming.last_stream_model.lock(), "claude-opus");
    }
