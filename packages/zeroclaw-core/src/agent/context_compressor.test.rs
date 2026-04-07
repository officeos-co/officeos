    use super::*;

    fn msg(role: &str, content: &str) -> ChatMessage {
        ChatMessage {
            role: role.to_string(),
            content: content.to_string(),
        }
    }

    #[test]
    fn test_estimate_tokens() {
        let messages = vec![msg("user", "hello world")]; // 11 chars
        let tokens = estimate_tokens(&messages);
        // 11/4 ceil = 3, +4 framing = 7, *1.2 = 8.4 -> 8
        assert!(tokens > 0);
    }

    #[test]
    fn test_estimate_tokens_empty() {
        assert_eq!(estimate_tokens(&[]), 0);
    }

    #[test]
    fn test_parse_context_limit_anthropic() {
        let msg = "prompt is too long: 150000 tokens > 128000 maximum context length";
        assert_eq!(parse_context_limit_from_error(msg), Some(128_000));
    }

    #[test]
    fn test_parse_context_limit_openai() {
        let msg = "This model's maximum context length is 128000 tokens. However, your messages resulted in 150000 tokens.";
        assert_eq!(parse_context_limit_from_error(msg), Some(128_000));
    }

    #[test]
    fn test_parse_context_limit_llamacpp() {
        let msg = "request (8968 tokens) exceeds the available context size (8448 tokens)";
        assert_eq!(parse_context_limit_from_error(msg), Some(8448));
    }

    #[test]
    fn test_parse_context_limit_none() {
        assert_eq!(parse_context_limit_from_error("some random error"), None);
    }

    #[test]
    fn test_parse_context_limit_rejects_small() {
        let msg = "limit is 100 tokens";
        assert_eq!(parse_context_limit_from_error(msg), None); // < 1024
    }

    #[test]
    fn test_next_probe_tier() {
        assert_eq!(next_probe_tier(2_000_001), 2_000_000);
        assert_eq!(next_probe_tier(2_000_000), 1_000_000);
        assert_eq!(next_probe_tier(200_000), 128_000);
        assert_eq!(next_probe_tier(64_000), 32_000);
        assert_eq!(next_probe_tier(32_000), 32_000); // floor
        assert_eq!(next_probe_tier(10_000), 32_000); // below all tiers
    }

    #[test]
    fn test_align_boundary_forward_skips_tool() {
        let messages = vec![
            msg("system", "sys"),
            msg("user", "q"),
            msg("tool", "result1"),
            msg("tool", "result2"),
            msg("user", "next"),
        ];
        // Starting at index 2 (tool), should skip to index 4
        assert_eq!(align_boundary_forward(&messages, 2), 4);
    }

    #[test]
    fn test_align_boundary_forward_noop() {
        let messages = vec![
            msg("system", "sys"),
            msg("user", "q"),
            msg("assistant", "a"),
        ];
        assert_eq!(align_boundary_forward(&messages, 1), 1);
    }

    #[test]
    fn test_repair_tool_pairs_removes_orphaned() {
        let mut messages = vec![
            msg("system", "sys"),
            msg(
                "assistant",
                "[CONTEXT SUMMARY — 5 earlier messages compressed]\nstuff",
            ),
            msg("tool", "orphaned result"),
            msg("user", "next question"),
        ];
        repair_tool_pairs(&mut messages);
        assert_eq!(messages.len(), 3);
        assert_eq!(messages[2].role, "user");
    }

    #[test]
    fn test_repair_tool_pairs_no_false_positives() {
        let mut messages = vec![
            msg("system", "sys"),
            msg("user", "q"),
            msg("assistant", "calling tool"),
            msg("tool", "result"),
            msg("user", "thanks"),
        ];
        repair_tool_pairs(&mut messages);
        assert_eq!(messages.len(), 5); // no change
    }

    #[test]
    fn test_build_transcript() {
        let messages = vec![msg("user", "hello"), msg("assistant", "hi there")];
        let t = build_transcript(&messages, 10_000);
        assert!(t.contains("USER: hello"));
        assert!(t.contains("ASSISTANT: hi there"));
    }

    #[test]
    fn test_build_transcript_truncates() {
        let messages = vec![msg("user", &"x".repeat(1000))];
        let t = build_transcript(&messages, 100);
        assert!(t.len() <= 103); // 100 + "..."
    }

    #[test]
    fn test_truncate_chars() {
        assert_eq!(truncate_chars("hello world", 5), "hello...");
        assert_eq!(truncate_chars("hi", 10), "hi");
    }

    #[test]
    fn test_config_defaults() {
        let config = ContextCompressionConfig::default();
        assert!(config.enabled);
        assert!((config.threshold_ratio - 0.50).abs() < f64::EPSILON);
        assert_eq!(config.protect_first_n, 3);
        assert_eq!(config.protect_last_n, 4);
        assert_eq!(config.max_passes, 3);
        assert_eq!(config.summary_max_chars, 4_000);
        assert_eq!(config.source_max_chars, 50_000);
        assert_eq!(config.timeout_secs, 60);
        assert!(config.summary_model.is_none());
        assert_eq!(config.identifier_policy, "strict");
    }

    #[test]
    fn test_config_serde_defaults() {
        let json = "{}";
        let config: ContextCompressionConfig = serde_json::from_str(json).unwrap();
        assert!(config.enabled);
        assert_eq!(config.protect_first_n, 3);
        assert_eq!(config.max_passes, 3);
    }

    #[test]
    fn test_config_serde_override() {
        let json = r#"{"enabled": false, "protect_first_n": 5, "max_passes": 1}"#;
        let config: ContextCompressionConfig = serde_json::from_str(json).unwrap();
        assert!(!config.enabled);
        assert_eq!(config.protect_first_n, 5);
        assert_eq!(config.max_passes, 1);
    }

    // ── fast_trim_tool_results tests ────────────────────────────────

    #[test]
    fn test_fast_trim_protects_first_and_last_n() {
        let config = ContextCompressionConfig {
            protect_first_n: 2,
            protect_last_n: 2,
            tool_result_retrim_chars: 100,
            ..Default::default()
        };
        let compressor = ContextCompressor::new(config, 128_000);
        let big = "x".repeat(5_000);
        let mut history = vec![
            msg("system", "sys"),
            msg("tool", &big), // index 1 — protected (first 2)
            msg("user", "q"),
            msg("tool", &big),   // index 3 — trimmable
            msg("user", "next"), // index 4 — protected (last 2)
            msg("tool", &big),   // index 5 — protected (last 2)
        ];
        let saved = compressor.fast_trim_tool_results(&mut history);
        assert!(saved > 0);
        // Protected messages unchanged
        assert_eq!(history[1].content.len(), 5_000);
        assert_eq!(history[5].content.len(), 5_000);
        // Trimmable message was trimmed
        assert!(history[3].content.len() <= 200); // 100 + marker overhead
    }

    #[test]
    fn test_fast_trim_skips_images() {
        let config = ContextCompressionConfig {
            protect_first_n: 0,
            protect_last_n: 0,
            tool_result_retrim_chars: 100,
            ..Default::default()
        };
        let compressor = ContextCompressor::new(config, 128_000);
        let img = format!("data:image/{}", "x".repeat(5_000));
        let mut history = vec![msg("tool", &img)];
        let saved = compressor.fast_trim_tool_results(&mut history);
        assert_eq!(saved, 0);
        assert!(history[0].content.len() > 5_000);
    }

    #[test]
    fn test_fast_trim_skips_exempt_tools() {
        let config = ContextCompressionConfig {
            protect_first_n: 0,
            protect_last_n: 0,
            tool_result_retrim_chars: 100,
            tool_result_trim_exempt: vec!["KEEPME".to_string()],
            ..Default::default()
        };
        let compressor = ContextCompressor::new(config, 128_000);
        let content = format!("KEEPME {}", "x".repeat(5_000));
        let mut history = vec![msg("tool", &content)];
        let saved = compressor.fast_trim_tool_results(&mut history);
        assert_eq!(saved, 0);
    }

    #[test]
    fn test_fast_trim_skips_small_results() {
        let config = ContextCompressionConfig {
            protect_first_n: 0,
            protect_last_n: 0,
            tool_result_retrim_chars: 2_000,
            ..Default::default()
        };
        let compressor = ContextCompressor::new(config, 128_000);
        let mut history = vec![msg("tool", "small result")];
        let saved = compressor.fast_trim_tool_results(&mut history);
        assert_eq!(saved, 0);
    }

    #[test]
    fn test_fast_trim_skips_non_tool_messages() {
        let config = ContextCompressionConfig {
            protect_first_n: 0,
            protect_last_n: 0,
            tool_result_retrim_chars: 100,
            ..Default::default()
        };
        let compressor = ContextCompressor::new(config, 128_000);
        let big = "x".repeat(5_000);
        let mut history = vec![msg("user", &big), msg("assistant", &big)];
        let saved = compressor.fast_trim_tool_results(&mut history);
        assert_eq!(saved, 0);
    }

    #[test]
    fn test_fast_trim_config_defaults() {
        let config = ContextCompressionConfig::default();
        assert_eq!(config.tool_result_retrim_chars, 2_000);
        assert!(config.tool_result_trim_exempt.is_empty());
    }

    #[test]
    fn test_fast_trim_disabled_when_zero() {
        let config = ContextCompressionConfig {
            protect_first_n: 0,
            protect_last_n: 0,
            tool_result_retrim_chars: 0,
            ..Default::default()
        };
        let compressor = ContextCompressor::new(config, 128_000);
        let big = "x".repeat(5_000);
        let mut history = vec![msg("tool", &big)];
        let saved = compressor.fast_trim_tool_results(&mut history);
        assert_eq!(saved, 0);
    }
