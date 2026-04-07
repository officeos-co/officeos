    use super::*;
    use crate::gateway::{AppState, GatewayRateLimiter, IdempotencyStore, nodes};
    use crate::memory::{Memory, MemoryCategory, MemoryEntry};
    use crate::providers::Provider;
    use crate::security::pairing::PairingGuard;
    use async_trait::async_trait;
    use axum::response::IntoResponse;
    use http_body_util::BodyExt;
    use parking_lot::Mutex;
    use std::sync::Arc;
    use std::time::Duration;

    struct MockMemory;

    #[async_trait]
    impl Memory for MockMemory {
        fn name(&self) -> &str {
            "mock"
        }

        async fn store(
            &self,
            _key: &str,
            _content: &str,
            _category: MemoryCategory,
            _session_id: Option<&str>,
        ) -> anyhow::Result<()> {
            Ok(())
        }

        async fn recall(
            &self,
            _query: &str,
            _limit: usize,
            _session_id: Option<&str>,
            _since: Option<&str>,
            _until: Option<&str>,
        ) -> anyhow::Result<Vec<MemoryEntry>> {
            Ok(Vec::new())
        }

        async fn get(&self, _key: &str) -> anyhow::Result<Option<MemoryEntry>> {
            Ok(None)
        }

        async fn list(
            &self,
            _category: Option<&MemoryCategory>,
            _session_id: Option<&str>,
        ) -> anyhow::Result<Vec<MemoryEntry>> {
            Ok(Vec::new())
        }

        async fn forget(&self, _key: &str) -> anyhow::Result<bool> {
            Ok(false)
        }

        async fn count(&self) -> anyhow::Result<usize> {
            Ok(0)
        }

        async fn health_check(&self) -> bool {
            true
        }
    }

    struct MockProvider;

    #[async_trait]
    impl Provider for MockProvider {
        async fn chat_with_system(
            &self,
            _system_prompt: Option<&str>,
            _message: &str,
            _model: &str,
            _temperature: f64,
        ) -> anyhow::Result<String> {
            Ok("ok".to_string())
        }
    }

    fn test_state(config: crate::config::Config) -> AppState {
        AppState {
            config: Arc::new(Mutex::new(config)),
            provider: Arc::new(MockProvider),
            model: "test-model".into(),
            temperature: 0.0,
            mem: Arc::new(MockMemory),
            auto_save: false,
            webhook_secret_hash: None,
            pairing: Arc::new(PairingGuard::new(false, &[])),
            trust_forwarded_headers: false,
            rate_limiter: Arc::new(GatewayRateLimiter::new(100, 100, 100)),
            auth_limiter: Arc::new(crate::gateway::auth_rate_limit::AuthRateLimiter::new()),
            idempotency_store: Arc::new(IdempotencyStore::new(Duration::from_secs(300), 1000)),
            observer: Arc::new(crate::observability::NoopObserver),
            tools_registry: Arc::new(Vec::new()),
            cost_tracker: None,
            event_tx: tokio::sync::broadcast::channel(16).0,
            event_buffer: Arc::new(crate::gateway::sse::EventBuffer::new(16)),
            shutdown_tx: tokio::sync::watch::channel(false).0,
            node_registry: Arc::new(nodes::NodeRegistry::new(16)),
            session_backend: None,
            session_queue: Arc::new(crate::gateway::session_queue::SessionActorQueue::new(
                8, 30, 600,
            )),
            device_registry: None,
            pending_pairings: None,
            path_prefix: String::new(),
            canvas_store: crate::tools::canvas::CanvasStore::new(),
        }
    }

    async fn response_json(response: axum::response::Response) -> serde_json::Value {
        let body = response
            .into_body()
            .collect()
            .await
            .expect("response body")
            .to_bytes();
        serde_json::from_slice(&body).expect("valid json response")
    }

    #[test]
    fn masking_keeps_toml_valid_and_preserves_api_keys_type() {
        let mut cfg = crate::config::Config::default();
        cfg.api_key = Some("sk-live-123".to_string());
        cfg.reliability.api_keys = vec!["rk-1".to_string(), "rk-2".to_string()];
        cfg.gateway.paired_tokens = vec!["pair-token-1".to_string()];
        cfg.channels_config.wati = Some(crate::config::schema::WatiConfig {
            api_token: "wati-token".to_string(),
            api_url: "https://live-mt-server.wati.io".to_string(),
            tenant_id: None,
            allowed_numbers: vec![],
            proxy_url: None,
        });
        cfg.channels_config.feishu = Some(crate::config::schema::FeishuConfig {
            app_id: "cli_aabbcc".to_string(),
            app_secret: "feishu-secret".to_string(),
            encrypt_key: Some("feishu-encrypt".to_string()),
            verification_token: Some("feishu-verify".to_string()),
            allowed_users: vec!["*".to_string()],
            receive_mode: crate::config::schema::LarkReceiveMode::Websocket,
            port: None,
            proxy_url: None,
        });
        cfg.model_routes = vec![crate::config::schema::ModelRouteConfig {
            hint: "reasoning".to_string(),
            provider: "openrouter".to_string(),
            model: "anthropic/claude-sonnet-4.6".to_string(),
            api_key: Some("route-model-key".to_string()),
        }];
        cfg.embedding_routes = vec![crate::config::schema::EmbeddingRouteConfig {
            hint: "semantic".to_string(),
            provider: "openai".to_string(),
            model: "text-embedding-3-small".to_string(),
            dimensions: Some(1536),
            api_key: Some("route-embed-key".to_string()),
        }];

        let masked = mask_sensitive_fields(&cfg);
        let toml = toml::to_string_pretty(&masked).expect("masked config should serialize");
        let parsed: crate::config::Config =
            toml::from_str(&toml).expect("masked config should remain valid TOML for Config");

        assert_eq!(parsed.api_key.as_deref(), Some(MASKED_SECRET));
        assert_eq!(
            parsed.reliability.api_keys,
            vec![MASKED_SECRET.to_string(), MASKED_SECRET.to_string()]
        );
        assert_eq!(
            parsed.gateway.paired_tokens,
            vec![MASKED_SECRET.to_string()]
        );
        assert_eq!(
            parsed
                .channels_config
                .wati
                .as_ref()
                .map(|v| v.api_token.as_str()),
            Some(MASKED_SECRET)
        );
        assert_eq!(
            parsed
                .channels_config
                .feishu
                .as_ref()
                .map(|v| v.app_secret.as_str()),
            Some(MASKED_SECRET)
        );
        assert_eq!(
            parsed
                .channels_config
                .feishu
                .as_ref()
                .and_then(|v| v.encrypt_key.as_deref()),
            Some(MASKED_SECRET)
        );
        assert_eq!(
            parsed
                .channels_config
                .feishu
                .as_ref()
                .and_then(|v| v.verification_token.as_deref()),
            Some(MASKED_SECRET)
        );
        assert_eq!(
            parsed
                .model_routes
                .first()
                .and_then(|v| v.api_key.as_deref()),
            Some(MASKED_SECRET)
        );
        assert_eq!(
            parsed
                .embedding_routes
                .first()
                .and_then(|v| v.api_key.as_deref()),
            Some(MASKED_SECRET)
        );
    }

    #[test]
    fn hydrate_config_for_save_restores_masked_secrets_and_paths() {
        let mut current = crate::config::Config::default();
        current.config_path = std::path::PathBuf::from("/tmp/current/config.toml");
        current.workspace_dir = std::path::PathBuf::from("/tmp/current/workspace");
        current.api_key = Some("real-key".to_string());
        current.reliability.api_keys = vec!["r1".to_string(), "r2".to_string()];
        current.gateway.paired_tokens = vec!["pair-1".to_string(), "pair-2".to_string()];
        current.channels_config.wati = Some(crate::config::schema::WatiConfig {
            api_token: "wati-real".to_string(),
            api_url: "https://live-mt-server.wati.io".to_string(),
            tenant_id: None,
            allowed_numbers: vec![],
            proxy_url: None,
        });
        current.channels_config.feishu = Some(crate::config::schema::FeishuConfig {
            app_id: "cli_current".to_string(),
            app_secret: "feishu-secret-real".to_string(),
            encrypt_key: Some("feishu-encrypt-real".to_string()),
            verification_token: Some("feishu-verify-real".to_string()),
            allowed_users: vec!["*".to_string()],
            receive_mode: crate::config::schema::LarkReceiveMode::Websocket,
            port: None,
            proxy_url: None,
        });
        current.model_routes = vec![
            crate::config::schema::ModelRouteConfig {
                hint: "reasoning".to_string(),
                provider: "openrouter".to_string(),
                model: "anthropic/claude-sonnet-4.6".to_string(),
                api_key: Some("route-model-key-1".to_string()),
            },
            crate::config::schema::ModelRouteConfig {
                hint: "fast".to_string(),
                provider: "openrouter".to_string(),
                model: "openai/gpt-4.1-mini".to_string(),
                api_key: Some("route-model-key-2".to_string()),
            },
        ];
        current.embedding_routes = vec![
            crate::config::schema::EmbeddingRouteConfig {
                hint: "semantic".to_string(),
                provider: "openai".to_string(),
                model: "text-embedding-3-small".to_string(),
                dimensions: Some(1536),
                api_key: Some("route-embed-key-1".to_string()),
            },
            crate::config::schema::EmbeddingRouteConfig {
                hint: "archive".to_string(),
                provider: "custom:https://emb.example.com/v1".to_string(),
                model: "bge-m3".to_string(),
                dimensions: Some(1024),
                api_key: Some("route-embed-key-2".to_string()),
            },
        ];

        let mut incoming = mask_sensitive_fields(&current);
        incoming.default_model = Some("gpt-4.1-mini".to_string());
        // Simulate UI changing only one key and keeping the first masked.
        incoming.reliability.api_keys = vec![MASKED_SECRET.to_string(), "r2-new".to_string()];
        incoming.gateway.paired_tokens = vec![MASKED_SECRET.to_string(), "pair-2-new".to_string()];
        if let Some(wati) = incoming.channels_config.wati.as_mut() {
            wati.api_token = MASKED_SECRET.to_string();
        }
        if let Some(feishu) = incoming.channels_config.feishu.as_mut() {
            feishu.app_secret = MASKED_SECRET.to_string();
            feishu.encrypt_key = Some(MASKED_SECRET.to_string());
            feishu.verification_token = Some("feishu-verify-new".to_string());
        }
        incoming.model_routes[1].api_key = Some("route-model-key-2-new".to_string());
        incoming.embedding_routes[1].api_key = Some("route-embed-key-2-new".to_string());

        let hydrated = hydrate_config_for_save(incoming, &current);

        assert_eq!(hydrated.config_path, current.config_path);
        assert_eq!(hydrated.workspace_dir, current.workspace_dir);
        assert_eq!(hydrated.api_key, current.api_key);
        assert_eq!(hydrated.default_model.as_deref(), Some("gpt-4.1-mini"));
        assert_eq!(
            hydrated.reliability.api_keys,
            vec!["r1".to_string(), "r2-new".to_string()]
        );
        assert_eq!(
            hydrated.gateway.paired_tokens,
            vec!["pair-1".to_string(), "pair-2-new".to_string()]
        );
        assert_eq!(
            hydrated
                .channels_config
                .wati
                .as_ref()
                .map(|v| v.api_token.as_str()),
            Some("wati-real")
        );
        assert_eq!(
            hydrated
                .channels_config
                .feishu
                .as_ref()
                .map(|v| v.app_secret.as_str()),
            Some("feishu-secret-real")
        );
        assert_eq!(
            hydrated
                .channels_config
                .feishu
                .as_ref()
                .and_then(|v| v.encrypt_key.as_deref()),
            Some("feishu-encrypt-real")
        );
        assert_eq!(
            hydrated
                .channels_config
                .feishu
                .as_ref()
                .and_then(|v| v.verification_token.as_deref()),
            Some("feishu-verify-new")
        );
        assert_eq!(
            hydrated.model_routes[0].api_key.as_deref(),
            Some("route-model-key-1")
        );
        assert_eq!(
            hydrated.model_routes[1].api_key.as_deref(),
            Some("route-model-key-2-new")
        );
        assert_eq!(
            hydrated.embedding_routes[0].api_key.as_deref(),
            Some("route-embed-key-1")
        );
        assert_eq!(
            hydrated.embedding_routes[1].api_key.as_deref(),
            Some("route-embed-key-2-new")
        );
    }

    #[test]
    fn hydrate_config_for_save_restores_route_keys_by_identity_and_clears_unmatched_masks() {
        let mut current = crate::config::Config::default();
        current.model_routes = vec![
            crate::config::schema::ModelRouteConfig {
                hint: "reasoning".to_string(),
                provider: "openrouter".to_string(),
                model: "anthropic/claude-sonnet-4.6".to_string(),
                api_key: Some("route-model-key-1".to_string()),
            },
            crate::config::schema::ModelRouteConfig {
                hint: "fast".to_string(),
                provider: "openrouter".to_string(),
                model: "openai/gpt-4.1-mini".to_string(),
                api_key: Some("route-model-key-2".to_string()),
            },
        ];
        current.embedding_routes = vec![
            crate::config::schema::EmbeddingRouteConfig {
                hint: "semantic".to_string(),
                provider: "openai".to_string(),
                model: "text-embedding-3-small".to_string(),
                dimensions: Some(1536),
                api_key: Some("route-embed-key-1".to_string()),
            },
            crate::config::schema::EmbeddingRouteConfig {
                hint: "archive".to_string(),
                provider: "custom:https://emb.example.com/v1".to_string(),
                model: "bge-m3".to_string(),
                dimensions: Some(1024),
                api_key: Some("route-embed-key-2".to_string()),
            },
        ];

        let mut incoming = mask_sensitive_fields(&current);
        incoming.model_routes.swap(0, 1);
        incoming.embedding_routes.swap(0, 1);
        incoming
            .model_routes
            .push(crate::config::schema::ModelRouteConfig {
                hint: "new".to_string(),
                provider: "openai".to_string(),
                model: "gpt-4.1".to_string(),
                api_key: Some(MASKED_SECRET.to_string()),
            });
        incoming
            .embedding_routes
            .push(crate::config::schema::EmbeddingRouteConfig {
                hint: "new-embed".to_string(),
                provider: "custom:https://emb2.example.com/v1".to_string(),
                model: "bge-small".to_string(),
                dimensions: Some(768),
                api_key: Some(MASKED_SECRET.to_string()),
            });

        let hydrated = hydrate_config_for_save(incoming, &current);

        assert_eq!(
            hydrated.model_routes[0].api_key.as_deref(),
            Some("route-model-key-2")
        );
        assert_eq!(
            hydrated.model_routes[1].api_key.as_deref(),
            Some("route-model-key-1")
        );
        assert_eq!(hydrated.model_routes[2].api_key, None);
        assert_eq!(
            hydrated.embedding_routes[0].api_key.as_deref(),
            Some("route-embed-key-2")
        );
        assert_eq!(
            hydrated.embedding_routes[1].api_key.as_deref(),
            Some("route-embed-key-1")
        );
        assert_eq!(hydrated.embedding_routes[2].api_key, None);
        assert!(
            hydrated
                .model_routes
                .iter()
                .all(|route| route.api_key.as_deref() != Some(MASKED_SECRET))
        );
        assert!(
            hydrated
                .embedding_routes
                .iter()
                .all(|route| route.api_key.as_deref() != Some(MASKED_SECRET))
        );
    }

    #[tokio::test]
    async fn cron_api_shell_roundtrip_includes_delivery() {
        let tmp = tempfile::TempDir::new().unwrap();
        let config = crate::config::Config {
            workspace_dir: tmp.path().join("workspace"),
            config_path: tmp.path().join("config.toml"),
            ..crate::config::Config::default()
        };
        std::fs::create_dir_all(&config.workspace_dir).unwrap();
        let state = test_state(config);

        let add_response = handle_api_cron_add(
            State(state.clone()),
            HeaderMap::new(),
            Json(
                serde_json::from_value::<CronAddBody>(serde_json::json!({
                    "name": "test-job",
                    "schedule": "*/5 * * * *",
                    "command": "echo hello",
                    "delivery": {
                        "mode": "announce",
                        "channel": "discord",
                        "to": "1234567890",
                        "best_effort": true
                    }
                }))
                .expect("body should deserialize"),
            ),
        )
        .await
        .into_response();

        let add_json = response_json(add_response).await;
        assert_eq!(add_json["status"], "ok");
        assert_eq!(add_json["job"]["delivery"]["mode"], "announce");
        assert_eq!(add_json["job"]["delivery"]["channel"], "discord");
        assert_eq!(add_json["job"]["delivery"]["to"], "1234567890");

        let list_response = handle_api_cron_list(State(state), HeaderMap::new())
            .await
            .into_response();
        let list_json = response_json(list_response).await;
        let jobs = list_json["jobs"].as_array().expect("jobs array");
        assert_eq!(jobs.len(), 1);
        assert_eq!(jobs[0]["delivery"]["mode"], "announce");
        assert_eq!(jobs[0]["delivery"]["channel"], "discord");
        assert_eq!(jobs[0]["delivery"]["to"], "1234567890");
    }

    #[tokio::test]
    async fn cron_api_accepts_agent_jobs() {
        let tmp = tempfile::TempDir::new().unwrap();
        let config = crate::config::Config {
            workspace_dir: tmp.path().join("workspace"),
            config_path: tmp.path().join("config.toml"),
            ..crate::config::Config::default()
        };
        std::fs::create_dir_all(&config.workspace_dir).unwrap();
        let state = test_state(config);

        let response = handle_api_cron_add(
            State(state.clone()),
            HeaderMap::new(),
            Json(
                serde_json::from_value::<CronAddBody>(serde_json::json!({
                    "name": "agent-job",
                    "schedule": "*/5 * * * *",
                    "job_type": "agent",
                    "command": "ignored shell command",
                    "prompt": "summarize the latest logs"
                }))
                .expect("body should deserialize"),
            ),
        )
        .await
        .into_response();

        let json = response_json(response).await;
        assert_eq!(json["status"], "ok");

        let config = state.config.lock().clone();
        let jobs = crate::cron::list_jobs(&config).unwrap();
        assert_eq!(jobs.len(), 1);
        assert_eq!(jobs[0].job_type, crate::cron::JobType::Agent);
        assert_eq!(jobs[0].prompt.as_deref(), Some("summarize the latest logs"));
    }

    #[tokio::test]
    async fn cron_api_rejects_announce_delivery_without_target() {
        let tmp = tempfile::TempDir::new().unwrap();
        let config = crate::config::Config {
            workspace_dir: tmp.path().join("workspace"),
            config_path: tmp.path().join("config.toml"),
            ..crate::config::Config::default()
        };
        std::fs::create_dir_all(&config.workspace_dir).unwrap();
        let state = test_state(config);

        let response = handle_api_cron_add(
            State(state.clone()),
            HeaderMap::new(),
            Json(
                serde_json::from_value::<CronAddBody>(serde_json::json!({
                    "name": "invalid-delivery-job",
                    "schedule": "*/5 * * * *",
                    "command": "echo hello",
                    "delivery": {
                        "mode": "announce",
                        "channel": "discord"
                    }
                }))
                .expect("body should deserialize"),
            ),
        )
        .await
        .into_response();

        assert_eq!(response.status(), StatusCode::BAD_REQUEST);
        let json = response_json(response).await;
        assert!(
            json["error"]
                .as_str()
                .unwrap_or_default()
                .contains("delivery.to is required")
        );

        let config = state.config.lock().clone();
        assert!(crate::cron::list_jobs(&config).unwrap().is_empty());
    }

    #[tokio::test]
    async fn cron_api_rejects_announce_delivery_with_unsupported_channel() {
        let tmp = tempfile::TempDir::new().unwrap();
        let config = crate::config::Config {
            workspace_dir: tmp.path().join("workspace"),
            config_path: tmp.path().join("config.toml"),
            ..crate::config::Config::default()
        };
        std::fs::create_dir_all(&config.workspace_dir).unwrap();
        let state = test_state(config);

        let response = handle_api_cron_add(
            State(state.clone()),
            HeaderMap::new(),
            Json(
                serde_json::from_value::<CronAddBody>(serde_json::json!({
                    "name": "invalid-delivery-job",
                    "schedule": "*/5 * * * *",
                    "command": "echo hello",
                    "delivery": {
                        "mode": "announce",
                        "channel": "email",
                        "to": "alerts@example.com"
                    }
                }))
                .expect("body should deserialize"),
            ),
        )
        .await
        .into_response();

        assert_eq!(response.status(), StatusCode::BAD_REQUEST);
        let json = response_json(response).await;
        assert!(
            json["error"]
                .as_str()
                .unwrap_or_default()
                .contains("unsupported delivery channel")
        );

        let config = state.config.lock().clone();
        assert!(crate::cron::list_jobs(&config).unwrap().is_empty());
    }
