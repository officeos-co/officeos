    use super::*;
    use crate::config::{EmbeddingRouteConfig, StorageProviderConfig};
    use tempfile::TempDir;

    #[test]
    fn factory_sqlite() {
        let tmp = TempDir::new().unwrap();
        let cfg = MemoryConfig {
            backend: "sqlite".into(),
            ..MemoryConfig::default()
        };
        let mem = create_memory(&cfg, tmp.path(), None).unwrap();
        assert_eq!(mem.name(), "sqlite");
    }

    #[test]
    fn assistant_autosave_key_detection_matches_legacy_patterns() {
        assert!(is_assistant_autosave_key("assistant_resp"));
        assert!(is_assistant_autosave_key("assistant_resp_1234"));
        assert!(is_assistant_autosave_key("ASSISTANT_RESP_abcd"));
        assert!(!is_assistant_autosave_key("assistant_response"));
        assert!(!is_assistant_autosave_key("user_msg_1234"));
    }

    #[test]
    fn autosave_content_filter_drops_cron_and_distilled_noise() {
        assert!(should_skip_autosave_content("[cron:auto] patrol check"));
        assert!(should_skip_autosave_content(
            "[DISTILLED_MEMORY_CHUNK 1/2] DISTILLED_INDEX_SIG:abc123"
        ));
        assert!(should_skip_autosave_content(
            "[Heartbeat Task | decision] Should I run tasks?"
        ));
        assert!(should_skip_autosave_content(
            "[Heartbeat Task | high] Execute scheduled patrol"
        ));
        assert!(!should_skip_autosave_content(
            "User prefers concise answers."
        ));
    }

    #[test]
    fn factory_none_uses_noop_memory() {
        let tmp = TempDir::new().unwrap();
        let cfg = MemoryConfig {
            backend: "none".into(),
            ..MemoryConfig::default()
        };
        let mem = create_memory(&cfg, tmp.path(), None).unwrap();
        assert_eq!(mem.name(), "none");
    }

    #[test]
    fn factory_unknown_falls_back_to_none() {
        let tmp = TempDir::new().unwrap();
        let cfg = MemoryConfig {
            backend: "redis".into(),
            ..MemoryConfig::default()
        };
        let mem = create_memory(&cfg, tmp.path(), None).unwrap();
        assert_eq!(mem.name(), "none");
    }

    #[test]
    fn migration_factory_none_is_rejected() {
        let tmp = TempDir::new().unwrap();
        let error = create_memory_for_migration("none", tmp.path())
            .err()
            .expect("backend=none should be rejected for migration");
        assert!(error.to_string().contains("disables persistence"));
    }

    #[test]
    fn effective_backend_name_prefers_storage_override() {
        let storage = StorageProviderConfig {
            provider: "obsidian".into(),
            ..StorageProviderConfig::default()
        };

        assert_eq!(
            effective_memory_backend_name("sqlite", Some(&storage)),
            "obsidian"
        );
    }

    #[test]
    fn resolve_embedding_config_uses_base_config_when_model_is_not_hint() {
        let cfg = MemoryConfig {
            embedding_provider: "openai".into(),
            embedding_model: "text-embedding-3-small".into(),
            embedding_dimensions: 1536,
            ..MemoryConfig::default()
        };

        let resolved = resolve_embedding_config(&cfg, &[], Some("base-key"));
        assert_eq!(
            resolved,
            ResolvedEmbeddingConfig {
                provider: "openai".into(),
                model: "text-embedding-3-small".into(),
                dimensions: 1536,
                api_key: Some("base-key".into()),
            }
        );
    }

    #[test]
    fn resolve_embedding_config_uses_matching_route_with_api_key_override() {
        let cfg = MemoryConfig {
            embedding_provider: "none".into(),
            embedding_model: "hint:semantic".into(),
            embedding_dimensions: 1536,
            ..MemoryConfig::default()
        };
        let routes = vec![EmbeddingRouteConfig {
            hint: "semantic".into(),
            provider: "custom:https://api.example.com/v1".into(),
            model: "custom-embed-v2".into(),
            dimensions: Some(1024),
            api_key: Some("route-key".into()),
        }];

        let resolved = resolve_embedding_config(&cfg, &routes, Some("base-key"));
        assert_eq!(
            resolved,
            ResolvedEmbeddingConfig {
                provider: "custom:https://api.example.com/v1".into(),
                model: "custom-embed-v2".into(),
                dimensions: 1024,
                api_key: Some("route-key".into()),
            }
        );
    }

    #[test]
    fn resolve_embedding_config_falls_back_when_hint_is_missing() {
        let cfg = MemoryConfig {
            embedding_provider: "openai".into(),
            embedding_model: "hint:semantic".into(),
            embedding_dimensions: 1536,
            ..MemoryConfig::default()
        };

        let resolved = resolve_embedding_config(&cfg, &[], Some("base-key"));
        assert_eq!(
            resolved,
            ResolvedEmbeddingConfig {
                provider: "openai".into(),
                model: "hint:semantic".into(),
                dimensions: 1536,
                api_key: Some("base-key".into()),
            }
        );
    }

    #[test]
    fn resolve_embedding_config_falls_back_when_route_is_invalid() {
        let cfg = MemoryConfig {
            embedding_provider: "openai".into(),
            embedding_model: "hint:semantic".into(),
            embedding_dimensions: 1536,
            ..MemoryConfig::default()
        };
        let routes = vec![EmbeddingRouteConfig {
            hint: "semantic".into(),
            provider: String::new(),
            model: "text-embedding-3-small".into(),
            dimensions: Some(0),
            api_key: None,
        }];

        let resolved = resolve_embedding_config(&cfg, &routes, Some("base-key"));
        assert_eq!(
            resolved,
            ResolvedEmbeddingConfig {
                provider: "openai".into(),
                model: "hint:semantic".into(),
                dimensions: 1536,
                api_key: Some("base-key".into()),
            }
        );
    }

    // Regression guard for issue #3083: when default_provider is "gemini"
    // (api_key = gemini key) but embedding_provider is "cohere", the
    // embedding provider's own env var (COHERE_API_KEY) must take precedence
    // over the caller-supplied key (which belongs to the default provider).
    //
    // Uses COHERE_API_KEY to avoid accidental collision with OPENAI_API_KEY
    // that may be set in the developer environment.
    #[test]
    fn resolve_embedding_config_uses_embedding_provider_env_key_not_default_provider_key() {
        // COHERE_API_KEY is almost certainly unset in normal dev environments.
        let prev = std::env::var("COHERE_API_KEY").ok();
        // SAFETY: test-only, single-threaded test runner.
        unsafe { std::env::set_var("COHERE_API_KEY", "cohere-from-env") };

        let cfg = MemoryConfig {
            embedding_provider: "cohere".into(),
            embedding_model: "embed-english-v3.0".into(),
            embedding_dimensions: 1024,
            ..MemoryConfig::default()
        };

        // Simulate: caller passes the Gemini (default_provider) api key.
        let resolved = resolve_embedding_config(&cfg, &[], Some("gemini-key-must-not-be-used"));

        // Restore env.
        match prev {
            // SAFETY: test-only, single-threaded test runner.
            Some(v) => unsafe { std::env::set_var("COHERE_API_KEY", v) },
            // SAFETY: test-only, single-threaded test runner.
            None => unsafe { std::env::remove_var("COHERE_API_KEY") },
        }

        assert_eq!(
            resolved.api_key.as_deref(),
            Some("cohere-from-env"),
            "embedding api_key must come from COHERE_API_KEY env var, not from the default provider key"
        );
        assert_ne!(
            resolved.api_key.as_deref(),
            Some("gemini-key-must-not-be-used"),
            "default_provider key must not leak to the embedding provider"
        );
    }
