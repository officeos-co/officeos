    use super::test_util::{EnvGuard, env_lock};
    use super::*;

    #[test]
    fn resolve_provider_credential_prefers_explicit_argument() {
        let resolved = resolve_provider_credential("openrouter", Some("  explicit-key  "));
        assert_eq!(resolved, Some("explicit-key".to_string()));
    }

    #[test]
    fn resolve_provider_credential_uses_minimax_oauth_env_for_placeholder() {
        let _env_lock = env_lock();
        let _oauth_guard = EnvGuard::set(MINIMAX_OAUTH_TOKEN_ENV, Some("oauth-token"));
        let _api_guard = EnvGuard::set(MINIMAX_API_KEY_ENV, Some("api-key"));
        let _refresh_guard = EnvGuard::set(MINIMAX_OAUTH_REFRESH_TOKEN_ENV, None);

        let resolved = resolve_provider_credential("minimax", Some(MINIMAX_OAUTH_PLACEHOLDER));

        assert_eq!(resolved.as_deref(), Some("oauth-token"));
    }

    #[test]
    fn resolve_provider_credential_falls_back_to_minimax_api_key_for_placeholder() {
        let _env_lock = env_lock();
        let _oauth_guard = EnvGuard::set(MINIMAX_OAUTH_TOKEN_ENV, None);
        let _api_guard = EnvGuard::set(MINIMAX_API_KEY_ENV, Some("api-key"));
        let _refresh_guard = EnvGuard::set(MINIMAX_OAUTH_REFRESH_TOKEN_ENV, None);

        let resolved = resolve_provider_credential("minimax", Some(MINIMAX_OAUTH_PLACEHOLDER));

        assert_eq!(resolved.as_deref(), Some("api-key"));
    }

    #[test]
    fn resolve_provider_credential_placeholder_ignores_generic_api_key_fallback() {
        let _env_lock = env_lock();
        let _oauth_guard = EnvGuard::set(MINIMAX_OAUTH_TOKEN_ENV, None);
        let _api_guard = EnvGuard::set(MINIMAX_API_KEY_ENV, None);
        let _refresh_guard = EnvGuard::set(MINIMAX_OAUTH_REFRESH_TOKEN_ENV, None);
        let _generic_guard = EnvGuard::set("API_KEY", Some("generic-key"));

        let resolved = resolve_provider_credential("minimax", Some(MINIMAX_OAUTH_PLACEHOLDER));

        assert!(resolved.is_none());
    }

    #[test]
    fn resolve_provider_credential_bedrock_uses_internal_credential_path() {
        let _generic_guard = EnvGuard::set("API_KEY", Some("generic-key"));
        let _override_guard = EnvGuard::set("OPENROUTER_API_KEY", Some("openrouter-key"));
        let _bedrock_guard = EnvGuard::set("BEDROCK_API_KEY", None);

        assert_eq!(
            resolve_provider_credential("bedrock", Some("explicit")),
            Some("explicit".to_string())
        );
        assert!(resolve_provider_credential("bedrock", None).is_none());
        assert!(resolve_provider_credential("aws-bedrock", None).is_none());
    }

    #[test]
    fn resolve_provider_credential_bedrock_returns_bearer_token_from_env() {
        let _bedrock_guard = EnvGuard::set("BEDROCK_API_KEY", Some("bedrock-bearer-token"));

        assert_eq!(
            resolve_provider_credential("bedrock", None),
            Some("bedrock-bearer-token".to_string())
        );
        assert_eq!(
            resolve_provider_credential("aws-bedrock", None),
            Some("bedrock-bearer-token".to_string())
        );
    }

    #[test]
    fn resolve_qwen_oauth_context_prefers_explicit_override() {
        let _env_lock = env_lock();
        let fake_home = format!("/tmp/zeroclaw-qwen-oauth-home-{}", std::process::id());
        let _home_guard = EnvGuard::set("HOME", Some(fake_home.as_str()));
        let _token_guard = EnvGuard::set(QWEN_OAUTH_TOKEN_ENV, Some("oauth-token"));
        let _resource_guard = EnvGuard::set(
            QWEN_OAUTH_RESOURCE_URL_ENV,
            Some("coding-intl.dashscope.aliyuncs.com"),
        );

        let context = resolve_qwen_oauth_context(Some("  explicit-qwen-token  "));

        assert_eq!(context.credential.as_deref(), Some("explicit-qwen-token"));
        assert!(context.base_url.is_none());
    }

    #[test]
    fn resolve_qwen_oauth_context_uses_env_token_and_resource_url() {
        let _env_lock = env_lock();
        let fake_home = format!("/tmp/zeroclaw-qwen-oauth-home-{}-env", std::process::id());
        let _home_guard = EnvGuard::set("HOME", Some(fake_home.as_str()));
        let _token_guard = EnvGuard::set(QWEN_OAUTH_TOKEN_ENV, Some("oauth-token"));
        let _refresh_guard = EnvGuard::set(QWEN_OAUTH_REFRESH_TOKEN_ENV, None);
        let _resource_guard = EnvGuard::set(
            QWEN_OAUTH_RESOURCE_URL_ENV,
            Some("coding-intl.dashscope.aliyuncs.com"),
        );
        let _dashscope_guard = EnvGuard::set("DASHSCOPE_API_KEY", Some("dashscope-fallback"));

        let context = resolve_qwen_oauth_context(Some(QWEN_OAUTH_PLACEHOLDER));

        assert_eq!(context.credential.as_deref(), Some("oauth-token"));
        assert_eq!(
            context.base_url.as_deref(),
            Some("https://coding-intl.dashscope.aliyuncs.com/v1")
        );
    }

    #[test]
    fn resolve_qwen_oauth_context_reads_cached_credentials_file() {
        let _env_lock = env_lock();
        let fake_home = format!("/tmp/zeroclaw-qwen-oauth-home-{}-file", std::process::id());
        let creds_dir = PathBuf::from(&fake_home).join(".qwen");
        std::fs::create_dir_all(&creds_dir).unwrap();
        let creds_path = creds_dir.join("oauth_creds.json");
        std::fs::write(
            &creds_path,
            r#"{"access_token":"cached-token","refresh_token":"cached-refresh","resource_url":"https://resource.example.com","expiry_date":4102444800000}"#,
        )
        .unwrap();

        let _home_guard = EnvGuard::set("HOME", Some(fake_home.as_str()));
        let _token_guard = EnvGuard::set(QWEN_OAUTH_TOKEN_ENV, None);
        let _refresh_guard = EnvGuard::set(QWEN_OAUTH_REFRESH_TOKEN_ENV, None);
        let _resource_guard = EnvGuard::set(QWEN_OAUTH_RESOURCE_URL_ENV, None);
        let _dashscope_guard = EnvGuard::set("DASHSCOPE_API_KEY", None);

        let context = resolve_qwen_oauth_context(Some(QWEN_OAUTH_PLACEHOLDER));

        assert_eq!(context.credential.as_deref(), Some("cached-token"));
        assert_eq!(
            context.base_url.as_deref(),
            Some("https://resource.example.com/v1")
        );
    }

    #[test]
    fn resolve_qwen_oauth_context_placeholder_does_not_use_dashscope_fallback() {
        let _env_lock = env_lock();
        let fake_home = format!(
            "/tmp/zeroclaw-qwen-oauth-home-{}-placeholder",
            std::process::id()
        );
        let _home_guard = EnvGuard::set("HOME", Some(fake_home.as_str()));
        let _token_guard = EnvGuard::set(QWEN_OAUTH_TOKEN_ENV, None);
        let _refresh_guard = EnvGuard::set(QWEN_OAUTH_REFRESH_TOKEN_ENV, None);
        let _resource_guard = EnvGuard::set(QWEN_OAUTH_RESOURCE_URL_ENV, None);
        let _dashscope_guard = EnvGuard::set("DASHSCOPE_API_KEY", Some("dashscope-fallback"));

        let context = resolve_qwen_oauth_context(Some(QWEN_OAUTH_PLACEHOLDER));

        assert!(context.credential.is_none());
    }

    #[test]
    fn regional_alias_predicates_cover_expected_variants() {
        assert!(is_moonshot_alias("moonshot"));
        assert!(is_moonshot_alias("kimi-global"));
        assert!(is_glm_alias("glm"));
        assert!(is_glm_alias("bigmodel"));
        assert!(is_minimax_alias("minimax-io"));
        assert!(is_minimax_alias("minimaxi"));
        assert!(is_minimax_alias("minimax-oauth"));
        assert!(is_minimax_alias("minimax-portal-cn"));
        assert!(is_qwen_alias("dashscope"));
        assert!(is_qwen_alias("qwen-us"));
        assert!(is_qwen_alias("qwen-code"));
        assert!(is_qwen_oauth_alias("qwen-code"));
        assert!(is_qwen_oauth_alias("qwen_oauth"));
        assert!(is_zai_alias("z.ai"));
        assert!(is_zai_alias("zai-cn"));
        assert!(is_qianfan_alias("qianfan"));
        assert!(is_qianfan_alias("baidu"));
        assert!(is_doubao_alias("doubao"));
        assert!(is_doubao_alias("volcengine"));
        assert!(is_doubao_alias("ark"));
        assert!(is_doubao_alias("doubao-cn"));

        assert!(!is_moonshot_alias("openrouter"));
        assert!(!is_glm_alias("openai"));
        assert!(!is_qwen_alias("gemini"));
        assert!(!is_zai_alias("anthropic"));
        assert!(!is_qianfan_alias("cohere"));
        assert!(!is_doubao_alias("deepseek"));
    }

    #[test]
    fn canonical_china_provider_name_maps_regional_aliases() {
        assert_eq!(canonical_china_provider_name("moonshot"), Some("moonshot"));
        assert_eq!(canonical_china_provider_name("kimi-intl"), Some("moonshot"));
        assert_eq!(canonical_china_provider_name("glm"), Some("glm"));
        assert_eq!(canonical_china_provider_name("zhipu-cn"), Some("glm"));
        assert_eq!(canonical_china_provider_name("minimax"), Some("minimax"));
        assert_eq!(canonical_china_provider_name("minimax-cn"), Some("minimax"));
        assert_eq!(canonical_china_provider_name("qwen"), Some("qwen"));
        assert_eq!(canonical_china_provider_name("dashscope-us"), Some("qwen"));
        assert_eq!(canonical_china_provider_name("qwen-code"), Some("qwen"));
        assert_eq!(canonical_china_provider_name("zai"), Some("zai"));
        assert_eq!(canonical_china_provider_name("z.ai-cn"), Some("zai"));
        assert_eq!(canonical_china_provider_name("qianfan"), Some("qianfan"));
        assert_eq!(canonical_china_provider_name("baidu"), Some("qianfan"));
        assert_eq!(canonical_china_provider_name("doubao"), Some("doubao"));
        assert_eq!(canonical_china_provider_name("volcengine"), Some("doubao"));
        assert_eq!(canonical_china_provider_name("bailian"), Some("bailian"));
        assert_eq!(
            canonical_china_provider_name("aliyun-bailian"),
            Some("bailian")
        );
        assert_eq!(canonical_china_provider_name("aliyun"), Some("bailian"));
        assert_eq!(canonical_china_provider_name("openai"), None);
    }

    #[test]
    fn regional_endpoint_aliases_map_to_expected_urls() {
        assert_eq!(minimax_base_url("minimax"), Some(MINIMAX_INTL_BASE_URL));
        assert_eq!(
            minimax_base_url("minimax-intl"),
            Some(MINIMAX_INTL_BASE_URL)
        );
        assert_eq!(minimax_base_url("minimax-cn"), Some(MINIMAX_CN_BASE_URL));

        assert_eq!(glm_base_url("glm"), Some(GLM_GLOBAL_BASE_URL));
        assert_eq!(glm_base_url("glm-cn"), Some(GLM_CN_BASE_URL));
        assert_eq!(glm_base_url("bigmodel"), Some(GLM_CN_BASE_URL));

        assert_eq!(moonshot_base_url("moonshot"), Some(MOONSHOT_CN_BASE_URL));
        assert_eq!(
            moonshot_base_url("moonshot-intl"),
            Some(MOONSHOT_INTL_BASE_URL)
        );

        assert_eq!(qwen_base_url("qwen"), Some(QWEN_CN_BASE_URL));
        assert_eq!(qwen_base_url("qwen-cn"), Some(QWEN_CN_BASE_URL));
        assert_eq!(qwen_base_url("qwen-intl"), Some(QWEN_INTL_BASE_URL));
        assert_eq!(qwen_base_url("qwen-us"), Some(QWEN_US_BASE_URL));
        assert_eq!(qwen_base_url("qwen-code"), Some(QWEN_CN_BASE_URL));

        assert_eq!(zai_base_url("zai"), Some(ZAI_GLOBAL_BASE_URL));
        assert_eq!(zai_base_url("z.ai"), Some(ZAI_GLOBAL_BASE_URL));
        assert_eq!(zai_base_url("zai-global"), Some(ZAI_GLOBAL_BASE_URL));
        assert_eq!(zai_base_url("z.ai-global"), Some(ZAI_GLOBAL_BASE_URL));
        assert_eq!(zai_base_url("zai-cn"), Some(ZAI_CN_BASE_URL));
        assert_eq!(zai_base_url("z.ai-cn"), Some(ZAI_CN_BASE_URL));
    }

    // ── Primary providers ────────────────────────────────────

    #[test]
    fn factory_openrouter() {
        assert!(create_provider("openrouter", Some("provider-test-credential")).is_ok());
        assert!(create_provider("openrouter", None).is_ok());
    }

    #[test]
    fn factory_anthropic() {
        assert!(create_provider("anthropic", Some("provider-test-credential")).is_ok());
    }

    #[test]
    fn factory_openai() {
        assert!(create_provider("openai", Some("provider-test-credential")).is_ok());
    }

    #[test]
    fn factory_ollama() {
        assert!(create_provider("ollama", None).is_ok());
        // Ollama may use API key when a remote endpoint is configured.
        assert!(create_provider("ollama", Some("dummy")).is_ok());
        assert!(create_provider("ollama", Some("any-value-here")).is_ok());
    }

    // ── OpenAI-compatible providers ──────────────────────────

    #[test]
    fn factory_venice() {
        let provider = create_provider("venice", Some("vn-key")).unwrap();
        assert!(
            !provider.capabilities().native_tool_calling,
            "Venice should use prompt-guided tools, not native tool calling"
        );
    }

    #[test]
    fn factory_vercel() {
        assert!(create_provider("vercel", Some("key")).is_ok());
        assert!(create_provider("vercel-ai", Some("key")).is_ok());
    }

    #[test]
    fn vercel_gateway_base_url_matches_public_gateway_endpoint() {
        assert_eq!(
            VERCEL_AI_GATEWAY_BASE_URL,
            "https://ai-gateway.vercel.sh/v1"
        );
    }

    #[test]
    fn factory_cloudflare() {
        assert!(create_provider("cloudflare", Some("key")).is_ok());
        assert!(create_provider("cloudflare-ai", Some("key")).is_ok());
    }

    #[test]
    fn factory_moonshot() {
        assert!(create_provider("moonshot", Some("key")).is_ok());
        assert!(create_provider("kimi", Some("key")).is_ok());
        assert!(create_provider("moonshot-intl", Some("key")).is_ok());
        assert!(create_provider("moonshot-cn", Some("key")).is_ok());
        assert!(create_provider("kimi-intl", Some("key")).is_ok());
        assert!(create_provider("kimi-cn", Some("key")).is_ok());
    }

    #[test]
    fn factory_kimi_code() {
        assert!(create_provider("kimi-code", Some("key")).is_ok());
        assert!(create_provider("kimi_coding", Some("key")).is_ok());
        assert!(create_provider("kimi_for_coding", Some("key")).is_ok());
    }

    #[test]
    fn factory_synthetic() {
        assert!(create_provider("synthetic", Some("key")).is_ok());
    }

    #[test]
    fn factory_opencode() {
        assert!(create_provider("opencode", Some("key")).is_ok());
        assert!(create_provider("opencode-zen", Some("key")).is_ok());
    }

    #[test]
    fn factory_opencode_go() {
        assert!(create_provider("opencode-go", Some("key")).is_ok());
    }

    #[test]
    fn resolve_provider_credential_opencode_go_env() {
        let _env_lock = env_lock();
        let _provider_guard = EnvGuard::set("OPENCODE_GO_API_KEY", Some("go-test-key"));
        let _generic_guard = EnvGuard::set("API_KEY", None);
        let _zeroclaw_guard = EnvGuard::set("ZEROCLAW_API_KEY", None);

        let resolved = resolve_provider_credential("opencode-go", None);
        assert_eq!(resolved.as_deref(), Some("go-test-key"));
    }

    #[test]
    fn factory_zai() {
        assert!(create_provider("zai", Some("key")).is_ok());
        assert!(create_provider("z.ai", Some("key")).is_ok());
        assert!(create_provider("zai-global", Some("key")).is_ok());
        assert!(create_provider("z.ai-global", Some("key")).is_ok());
        assert!(create_provider("zai-cn", Some("key")).is_ok());
        assert!(create_provider("z.ai-cn", Some("key")).is_ok());
    }

    #[test]
    fn factory_glm() {
        assert!(create_provider("glm", Some("key")).is_ok());
        assert!(create_provider("zhipu", Some("key")).is_ok());
        assert!(create_provider("glm-cn", Some("key")).is_ok());
        assert!(create_provider("zhipu-cn", Some("key")).is_ok());
        assert!(create_provider("glm-global", Some("key")).is_ok());
        assert!(create_provider("bigmodel", Some("key")).is_ok());
    }

    #[test]
    fn factory_minimax() {
        assert!(create_provider("minimax", Some("key")).is_ok());
        assert!(create_provider("minimax-intl", Some("key")).is_ok());
        assert!(create_provider("minimax-io", Some("key")).is_ok());
        assert!(create_provider("minimax-global", Some("key")).is_ok());
        assert!(create_provider("minimax-cn", Some("key")).is_ok());
        assert!(create_provider("minimaxi", Some("key")).is_ok());
        assert!(create_provider("minimax-oauth", Some("key")).is_ok());
        assert!(create_provider("minimax-oauth-cn", Some("key")).is_ok());
        assert!(create_provider("minimax-portal", Some("key")).is_ok());
        assert!(create_provider("minimax-portal-cn", Some("key")).is_ok());
    }

    #[test]
    fn factory_minimax_disables_native_tool_calling() {
        let minimax = create_provider("minimax", Some("key")).expect("provider should resolve");
        assert!(!minimax.supports_native_tools());

        let minimax_cn =
            create_provider("minimax-cn", Some("key")).expect("provider should resolve");
        assert!(!minimax_cn.supports_native_tools());
    }

    #[test]
    fn factory_qianfan() {
        assert!(create_provider("qianfan", Some("key")).is_ok());
        assert!(create_provider("baidu", Some("key")).is_ok());
    }

    #[test]
    fn factory_doubao() {
        assert!(create_provider("doubao", Some("key")).is_ok());
        assert!(create_provider("volcengine", Some("key")).is_ok());
        assert!(create_provider("ark", Some("key")).is_ok());
        assert!(create_provider("doubao-cn", Some("key")).is_ok());
    }

    #[test]
    fn factory_qwen() {
        assert!(create_provider("qwen", Some("key")).is_ok());
        assert!(create_provider("dashscope", Some("key")).is_ok());
        assert!(create_provider("qwen-cn", Some("key")).is_ok());
        assert!(create_provider("dashscope-cn", Some("key")).is_ok());
        assert!(create_provider("qwen-intl", Some("key")).is_ok());
        assert!(create_provider("dashscope-intl", Some("key")).is_ok());
        assert!(create_provider("qwen-international", Some("key")).is_ok());
        assert!(create_provider("dashscope-international", Some("key")).is_ok());
        assert!(create_provider("qwen-us", Some("key")).is_ok());
        assert!(create_provider("dashscope-us", Some("key")).is_ok());
        assert!(create_provider("qwen-code", Some("key")).is_ok());
        assert!(create_provider("qwen-oauth", Some("key")).is_ok());
    }

    #[test]
    fn qwen_provider_supports_vision() {
        let provider = create_provider("qwen", Some("key")).expect("qwen provider should build");
        assert!(provider.supports_vision());

        let oauth_provider =
            create_provider("qwen-code", Some("key")).expect("qwen oauth provider should build");
        assert!(oauth_provider.supports_vision());
    }

    #[test]
    fn factory_lmstudio() {
        assert!(create_provider("lmstudio", Some("key")).is_ok());
        assert!(create_provider("lm-studio", Some("key")).is_ok());
        assert!(create_provider("lmstudio", None).is_ok());
    }

    #[test]
    fn factory_llamacpp() {
        assert!(create_provider("llamacpp", Some("key")).is_ok());
        assert!(create_provider("llama.cpp", Some("key")).is_ok());
        assert!(create_provider("llamacpp", None).is_ok());
    }

    #[test]
    fn factory_sglang() {
        assert!(create_provider("sglang", None).is_ok());
        assert!(create_provider("sglang", Some("key")).is_ok());
    }

    #[test]
    fn factory_vllm() {
        assert!(create_provider("vllm", None).is_ok());
        assert!(create_provider("vllm", Some("key")).is_ok());
    }

    #[test]
    fn factory_osaurus() {
        // Osaurus works without an explicit key (defaults to "osaurus").
        assert!(create_provider("osaurus", None).is_ok());
        // Osaurus also works with an explicit key.
        assert!(create_provider("osaurus", Some("custom-key")).is_ok());
    }

    #[test]
    fn factory_osaurus_uses_default_key_when_none() {
        // Verify that create_provider_with_url_and_options succeeds even
        // without an API key — the match arm provides a default placeholder.
        let options = ProviderRuntimeOptions::default();
        let p = create_provider_with_url_and_options("osaurus", None, None, &options);
        assert!(p.is_ok());
    }

    #[test]
    fn factory_osaurus_custom_url() {
        // Verify that a custom api_url overrides the default localhost endpoint.
        let options = ProviderRuntimeOptions::default();
        let p = create_provider_with_url_and_options(
            "osaurus",
            Some("key"),
            Some("http://192.168.1.100:1337/v1"),
            &options,
        );
        assert!(p.is_ok());
    }

    #[test]
    fn resolve_provider_credential_osaurus_env() {
        let _env_lock = env_lock();
        let _guard = EnvGuard::set("OSAURUS_API_KEY", Some("osaurus-test-key"));
        let resolved = resolve_provider_credential("osaurus", None);
        assert_eq!(resolved, Some("osaurus-test-key".to_string()));
    }

    #[test]
    fn resolve_provider_credential_volcengine_env() {
        let _env_lock = env_lock();
        let _guard = EnvGuard::set("VOLCENGINE_API_KEY", Some("volc-test-key"));
        let resolved = resolve_provider_credential("volcengine", None);
        assert_eq!(resolved, Some("volc-test-key".to_string()));
    }

    #[test]
    fn resolve_provider_credential_aihubmix_env() {
        let _env_lock = env_lock();
        let _guard = EnvGuard::set("AIHUBMIX_API_KEY", Some("aihubmix-test-key"));
        let resolved = resolve_provider_credential("aihubmix", None);
        assert_eq!(resolved, Some("aihubmix-test-key".to_string()));
    }

    #[test]
    fn resolve_provider_credential_siliconflow_env() {
        let _env_lock = env_lock();
        let _guard = EnvGuard::set("SILICONFLOW_API_KEY", Some("sf-test-key"));
        let resolved = resolve_provider_credential("siliconflow", None);
        assert_eq!(resolved, Some("sf-test-key".to_string()));
    }

    #[test]
    fn factory_aihubmix() {
        assert!(create_provider("aihubmix", Some("key")).is_ok());
    }

    #[test]
    fn factory_siliconflow() {
        assert!(create_provider("siliconflow", Some("key")).is_ok());
        assert!(create_provider("silicon-flow", Some("key")).is_ok());
    }

    // ── Extended ecosystem ───────────────────────────────────

    #[test]
    fn factory_groq() {
        assert!(create_provider("groq", Some("key")).is_ok());
    }

    #[test]
    fn factory_mistral() {
        assert!(create_provider("mistral", Some("key")).is_ok());
    }

    #[test]
    fn factory_xai() {
        assert!(create_provider("xai", Some("key")).is_ok());
        assert!(create_provider("grok", Some("key")).is_ok());
    }

    #[test]
    fn factory_deepseek() {
        assert!(create_provider("deepseek", Some("key")).is_ok());
    }

    #[test]
    fn deepseek_provider_keeps_vision_disabled() {
        let provider =
            create_provider("deepseek", Some("key")).expect("deepseek provider should build");
        assert!(!provider.supports_vision());
    }

    #[test]
    fn factory_together() {
        assert!(create_provider("together", Some("key")).is_ok());
        assert!(create_provider("together-ai", Some("key")).is_ok());
    }

    #[test]
    fn factory_fireworks() {
        assert!(create_provider("fireworks", Some("key")).is_ok());
        assert!(create_provider("fireworks-ai", Some("key")).is_ok());
    }

    #[test]
    fn factory_novita() {
        assert!(create_provider("novita", Some("key")).is_ok());
    }

    #[test]
    fn factory_perplexity() {
        assert!(create_provider("perplexity", Some("key")).is_ok());
    }

    #[test]
    fn factory_cohere() {
        assert!(create_provider("cohere", Some("key")).is_ok());
    }

    #[test]
    fn factory_nvidia() {
        assert!(create_provider("nvidia", Some("nvapi-test")).is_ok());
        assert!(create_provider("nvidia-nim", Some("nvapi-test")).is_ok());
        assert!(create_provider("build.nvidia.com", Some("nvapi-test")).is_ok());
    }

    // ── AI inference routers ─────────────────────────────────

    #[test]
    fn factory_astrai() {
        assert!(create_provider("astrai", Some("sk-astrai-test")).is_ok());
    }

    #[test]
    fn factory_avian() {
        assert!(create_provider("avian", Some("sk-avian-test")).is_ok());
    }

    #[test]
    fn factory_deepmyst() {
        assert!(create_provider("deepmyst", Some("key")).is_ok());
        assert!(create_provider("deep-myst", Some("key")).is_ok());
    }

    #[test]
    fn resolve_provider_credential_deepmyst_env() {
        let _env_lock = env_lock();
        let _guard = EnvGuard::set("DEEPMYST_API_KEY", Some("dm-test-key"));
        let resolved = resolve_provider_credential("deepmyst", None);
        assert_eq!(resolved, Some("dm-test-key".to_string()));
    }

    // ── Custom / BYOP provider ─────────────────────────────

    #[test]
    fn factory_custom_url() {
        let p = create_provider("custom:https://my-llm.example.com", Some("key"));
        assert!(p.is_ok());
    }

    #[test]
    fn factory_custom_localhost() {
        let p = create_provider("custom:http://localhost:1234", Some("key"));
        assert!(p.is_ok());
    }

    #[test]
    fn factory_custom_no_key() {
        let p = create_provider("custom:https://my-llm.example.com", None);
        assert!(p.is_ok());
    }

    #[test]
    fn factory_custom_empty_url_errors() {
        match create_provider("custom:", None) {
            Err(e) => assert!(
                e.to_string().contains("requires a URL"),
                "Expected 'requires a URL', got: {e}"
            ),
            Ok(_) => panic!("Expected error for empty custom URL"),
        }
    }

    #[test]
    fn factory_custom_invalid_url_errors() {
        match create_provider("custom:not-a-url", None) {
            Err(e) => assert!(
                e.to_string().contains("requires a valid URL"),
                "Expected 'requires a valid URL', got: {e}"
            ),
            Ok(_) => panic!("Expected error for invalid custom URL"),
        }
    }

    #[test]
    fn factory_custom_unsupported_scheme_errors() {
        match create_provider("custom:ftp://example.com", None) {
            Err(e) => assert!(
                e.to_string().contains("http:// or https://"),
                "Expected scheme validation error, got: {e}"
            ),
            Ok(_) => panic!("Expected error for unsupported custom URL scheme"),
        }
    }

    #[test]
    fn factory_custom_trims_whitespace() {
        let p = create_provider("custom:  https://my-llm.example.com  ", Some("key"));
        assert!(p.is_ok());
    }

    // ── Anthropic-compatible custom endpoints ─────────────────

    #[test]
    fn factory_anthropic_custom_url() {
        let p = create_provider("anthropic-custom:https://api.example.com", Some("key"));
        assert!(p.is_ok());
    }

    #[test]
    fn factory_anthropic_custom_trailing_slash() {
        let p = create_provider("anthropic-custom:https://api.example.com/", Some("key"));
        assert!(p.is_ok());
    }

    #[test]
    fn factory_anthropic_custom_no_key() {
        let p = create_provider("anthropic-custom:https://api.example.com", None);
        assert!(p.is_ok());
    }

    #[test]
    fn factory_anthropic_custom_empty_url_errors() {
        match create_provider("anthropic-custom:", None) {
            Err(e) => assert!(
                e.to_string().contains("requires a URL"),
                "Expected 'requires a URL', got: {e}"
            ),
            Ok(_) => panic!("Expected error for empty anthropic-custom URL"),
        }
    }

    #[test]
    fn factory_anthropic_custom_invalid_url_errors() {
        match create_provider("anthropic-custom:not-a-url", None) {
            Err(e) => assert!(
                e.to_string().contains("requires a valid URL"),
                "Expected 'requires a valid URL', got: {e}"
            ),
            Ok(_) => panic!("Expected error for invalid anthropic-custom URL"),
        }
    }

    #[test]
    fn factory_anthropic_custom_unsupported_scheme_errors() {
        match create_provider("anthropic-custom:ftp://example.com", None) {
            Err(e) => assert!(
                e.to_string().contains("http:// or https://"),
                "Expected scheme validation error, got: {e}"
            ),
            Ok(_) => panic!("Expected error for unsupported anthropic-custom URL scheme"),
        }
    }

    // ── Error cases ──────────────────────────────────────────

    #[test]
    fn factory_unknown_provider_errors() {
        let p = create_provider("nonexistent", None);
        assert!(p.is_err());
        let msg = p.err().unwrap().to_string();
        assert!(msg.contains("Unknown provider"));
        assert!(msg.contains("nonexistent"));
    }

    #[test]
    fn factory_empty_name_errors() {
        assert!(create_provider("", None).is_err());
    }

    #[test]
    fn resilient_provider_ignores_duplicate_and_invalid_fallbacks() {
        let reliability = crate::config::ReliabilityConfig {
            provider_retries: 1,
            provider_backoff_ms: 100,
            fallback_providers: vec![
                "openrouter".into(),
                "nonexistent-provider".into(),
                "openai".into(),
                "openai".into(),
            ],
            api_keys: Vec::new(),
            model_fallbacks: std::collections::HashMap::new(),
            channel_initial_backoff_secs: 2,
            channel_max_backoff_secs: 60,
            scheduler_poll_secs: 15,
            scheduler_retries: 2,
        };

        let provider = create_resilient_provider(
            "openrouter",
            Some("provider-test-credential"),
            None,
            &reliability,
        );
        assert!(provider.is_ok());
    }

    #[test]
    fn resilient_provider_errors_for_invalid_primary() {
        let reliability = crate::config::ReliabilityConfig::default();
        let provider = create_resilient_provider(
            "totally-invalid",
            Some("provider-test-credential"),
            None,
            &reliability,
        );
        assert!(provider.is_err());
    }

    /// Fallback providers resolve their own credentials via provider-specific
    /// env vars rather than inheriting the primary provider's key.  A provider
    /// that requires no key (e.g. lmstudio, ollama) must initialize
    /// successfully even when the primary uses a completely different key.
    #[test]
    fn resilient_fallback_resolves_own_credential() {
        let reliability = crate::config::ReliabilityConfig {
            provider_retries: 1,
            provider_backoff_ms: 100,
            fallback_providers: vec!["lmstudio".into(), "ollama".into()],
            api_keys: Vec::new(),
            model_fallbacks: std::collections::HashMap::new(),
            channel_initial_backoff_secs: 2,
            channel_max_backoff_secs: 60,
            scheduler_poll_secs: 15,
            scheduler_retries: 2,
        };

        // Primary uses a ZAI key; fallbacks (lmstudio, ollama) should NOT
        // receive this key; they resolve their own credentials independently.
        let provider = create_resilient_provider("zai", Some("zai-test-key"), None, &reliability);
        assert!(provider.is_ok());
    }

    /// `custom:` URL entries work as fallback providers, enabling arbitrary
    /// OpenAI-compatible endpoints (e.g. local LM Studio on a Docker host).
    #[test]
    fn resilient_fallback_supports_custom_url() {
        let reliability = crate::config::ReliabilityConfig {
            provider_retries: 1,
            provider_backoff_ms: 100,
            fallback_providers: vec!["custom:http://host.docker.internal:1234/v1".into()],
            api_keys: Vec::new(),
            model_fallbacks: std::collections::HashMap::new(),
            channel_initial_backoff_secs: 2,
            channel_max_backoff_secs: 60,
            scheduler_poll_secs: 15,
            scheduler_retries: 2,
        };

        let provider =
            create_resilient_provider("openai", Some("openai-test-key"), None, &reliability);
        assert!(provider.is_ok());
    }

    /// Mixed fallback chain: named providers, custom URLs, and invalid entries
    /// all coexist.  Invalid entries are silently ignored; valid ones initialize.
    #[test]
    fn resilient_fallback_mixed_chain() {
        let reliability = crate::config::ReliabilityConfig {
            provider_retries: 1,
            provider_backoff_ms: 100,
            fallback_providers: vec![
                "deepseek".into(),
                "custom:http://localhost:8080/v1".into(),
                "nonexistent-provider".into(),
                "lmstudio".into(),
            ],
            api_keys: Vec::new(),
            model_fallbacks: std::collections::HashMap::new(),
            channel_initial_backoff_secs: 2,
            channel_max_backoff_secs: 60,
            scheduler_poll_secs: 15,
            scheduler_retries: 2,
        };

        let provider = create_resilient_provider("zai", Some("zai-test-key"), None, &reliability);
        assert!(provider.is_ok());
    }

    #[test]
    fn ollama_with_custom_url() {
        let provider = create_provider_with_url("ollama", None, Some("http://10.100.2.32:11434"));
        assert!(provider.is_ok());
    }

    #[test]
    fn ollama_cloud_with_custom_url() {
        let provider =
            create_provider_with_url("ollama", Some("ollama-key"), Some("https://ollama.com"));
        assert!(provider.is_ok());
    }

    /// Osaurus works as a fallback provider alongside other named providers.
    #[test]
    fn resilient_fallback_includes_osaurus() {
        let reliability = crate::config::ReliabilityConfig {
            provider_retries: 1,
            provider_backoff_ms: 100,
            fallback_providers: vec!["osaurus".into(), "lmstudio".into()],
            api_keys: Vec::new(),
            model_fallbacks: std::collections::HashMap::new(),
            channel_initial_backoff_secs: 2,
            channel_max_backoff_secs: 60,
            scheduler_poll_secs: 15,
            scheduler_retries: 2,
        };

        let provider = create_resilient_provider("zai", Some("zai-test-key"), None, &reliability);
        assert!(provider.is_ok());
    }

    #[test]
    fn listed_providers_have_unique_ids_and_aliases() {
        let providers = list_providers();
        let mut canonical_ids = std::collections::HashSet::new();
        let mut aliases = std::collections::HashSet::new();

        for provider in providers {
            assert!(
                canonical_ids.insert(provider.name),
                "Duplicate canonical provider id: {}",
                provider.name
            );

            for alias in provider.aliases {
                assert_ne!(
                    *alias, provider.name,
                    "Alias must differ from canonical id: {}",
                    provider.name
                );
                assert!(
                    !canonical_ids.contains(alias),
                    "Alias conflicts with canonical provider id: {}",
                    alias
                );
                assert!(aliases.insert(alias), "Duplicate provider alias: {}", alias);
            }
        }
    }

    // ── API error sanitization ───────────────────────────────

    #[test]
    fn sanitize_scrubs_sk_prefix() {
        let input = "request failed: sk-1234567890abcdef";
        let out = sanitize_api_error(input);
        assert!(!out.contains("sk-1234567890abcdef"));
        assert!(out.contains("[REDACTED]"));
    }

    #[test]
    fn sanitize_scrubs_multiple_prefixes() {
        let input = "keys sk-abcdef xoxb-12345 xoxp-67890";
        let out = sanitize_api_error(input);
        assert!(!out.contains("sk-abcdef"));
        assert!(!out.contains("xoxb-12345"));
        assert!(!out.contains("xoxp-67890"));
    }

    #[test]
    fn sanitize_short_prefix_then_real_key() {
        let input = "error with sk- prefix and key sk-1234567890";
        let result = sanitize_api_error(input);
        assert!(!result.contains("sk-1234567890"));
        assert!(result.contains("[REDACTED]"));
    }

    #[test]
    fn sanitize_sk_proj_comment_then_real_key() {
        let input = "note: sk- then sk-proj-abc123def456";
        let result = sanitize_api_error(input);
        assert!(!result.contains("sk-proj-abc123def456"));
        assert!(result.contains("[REDACTED]"));
    }

    #[test]
    fn sanitize_keeps_bare_prefix() {
        let input = "only prefix sk- present";
        let result = sanitize_api_error(input);
        assert!(result.contains("sk-"));
    }

    #[test]
    fn sanitize_handles_json_wrapped_key() {
        let input = r#"{"error":"invalid key sk-abc123xyz"}"#;
        let result = sanitize_api_error(input);
        assert!(!result.contains("sk-abc123xyz"));
    }

    #[test]
    fn sanitize_handles_delimiter_boundaries() {
        let input = "bad token xoxb-abc123}; next";
        let result = sanitize_api_error(input);
        assert!(!result.contains("xoxb-abc123"));
        assert!(result.contains("};"));
    }

    #[test]
    fn sanitize_truncates_long_error() {
        let long = "a".repeat(600);
        let result = sanitize_api_error(&long);
        assert!(result.len() <= 503);
        assert!(result.ends_with("..."));
    }

    #[test]
    fn sanitize_truncates_after_scrub() {
        let input = format!("{} sk-abcdef123456 {}", "a".repeat(290), "b".repeat(290));
        let result = sanitize_api_error(&input);
        assert!(!result.contains("sk-abcdef123456"));
        assert!(result.len() <= 503);
    }

    #[test]
    fn sanitize_preserves_unicode_boundaries() {
        let input = format!("{} sk-abcdef123", "hello🙂".repeat(80));
        let result = sanitize_api_error(&input);
        assert!(std::str::from_utf8(result.as_bytes()).is_ok());
        assert!(!result.contains("sk-abcdef123"));
    }

    #[test]
    fn sanitize_no_secret_no_change() {
        let input = "simple upstream timeout";
        let result = sanitize_api_error(input);
        assert_eq!(result, input);
    }

    #[test]
    fn scrub_github_personal_access_token() {
        let input = "auth failed with token ghp_abc123def456";
        let result = scrub_secret_patterns(input);
        assert_eq!(result, "auth failed with token [REDACTED]");
    }

    #[test]
    fn scrub_github_oauth_token() {
        let input = "Bearer gho_1234567890abcdef";
        let result = scrub_secret_patterns(input);
        assert_eq!(result, "Bearer [REDACTED]");
    }

    #[test]
    fn scrub_github_user_token() {
        let input = "token ghu_sessiontoken123";
        let result = scrub_secret_patterns(input);
        assert_eq!(result, "token [REDACTED]");
    }

    #[test]
    fn scrub_github_fine_grained_pat() {
        let input = "failed: github_pat_11AABBC_xyzzy789";
        let result = scrub_secret_patterns(input);
        assert_eq!(result, "failed: [REDACTED]");
    }

    // --- parse_provider_profile ---

    #[test]
    fn parse_provider_profile_plain_name() {
        let (name, profile) = parse_provider_profile("gemini");
        assert_eq!(name, "gemini");
        assert_eq!(profile, None);
    }

    #[test]
    fn parse_provider_profile_with_profile() {
        let (name, profile) = parse_provider_profile("openai-codex:second");
        assert_eq!(name, "openai-codex");
        assert_eq!(profile, Some("second"));
    }

    #[test]
    fn parse_provider_profile_custom_url_not_split() {
        let input = "custom:https://my-api.example.com/v1";
        let (name, profile) = parse_provider_profile(input);
        assert_eq!(name, input);
        assert_eq!(profile, None);
    }

    #[test]
    fn parse_provider_profile_anthropic_custom_not_split() {
        let input = "anthropic-custom:https://bedrock.example.com";
        let (name, profile) = parse_provider_profile(input);
        assert_eq!(name, input);
        assert_eq!(profile, None);
    }

    #[test]
    fn parse_provider_profile_empty_profile_ignored() {
        let (name, profile) = parse_provider_profile("openai-codex:");
        assert_eq!(name, "openai-codex:");
        assert_eq!(profile, None);
    }

    #[test]
    fn parse_provider_profile_extra_colons_kept() {
        let (name, profile) = parse_provider_profile("provider:profile:extra");
        assert_eq!(name, "provider");
        assert_eq!(profile, Some("profile:extra"));
    }

    // --- resilient fallback with profile syntax ---

    #[test]
    fn resilient_fallback_with_profile_syntax() {
        let _guard = env_lock();

        let reliability = crate::config::ReliabilityConfig {
            provider_retries: 1,
            provider_backoff_ms: 100,
            fallback_providers: vec!["openai-codex:second".into()],
            api_keys: Vec::new(),
            model_fallbacks: std::collections::HashMap::new(),
            channel_initial_backoff_secs: 2,
            channel_max_backoff_secs: 60,
            scheduler_poll_secs: 15,
            scheduler_retries: 2,
        };

        // openai-codex resolves its own OAuth credential; it should not
        // fail even with a profile override that has no local token file.
        // The provider initializes successfully and will attempt auth at
        // request time.
        let provider = create_resilient_provider("lmstudio", None, None, &reliability);
        assert!(provider.is_ok());
    }

    #[test]
    fn resilient_fallback_mixed_profiles_and_custom() {
        let _guard = env_lock();

        let reliability = crate::config::ReliabilityConfig {
            provider_retries: 1,
            provider_backoff_ms: 100,
            fallback_providers: vec![
                "openai-codex:second".into(),
                "custom:http://localhost:8080/v1".into(),
                "lmstudio".into(),
                "nonexistent-provider".into(),
            ],
            api_keys: Vec::new(),
            model_fallbacks: std::collections::HashMap::new(),
            channel_initial_backoff_secs: 2,
            channel_max_backoff_secs: 60,
            scheduler_poll_secs: 15,
            scheduler_retries: 2,
        };

        let provider = create_resilient_provider("ollama", None, None, &reliability);
        assert!(provider.is_ok());
    }

    // ── API key prefix pre-flight ───────────────────────────

    #[test]
    fn api_key_prefix_cross_provider_mismatch() {
        // Anthropic key used with openrouter
        assert_eq!(
            check_api_key_prefix("openrouter", "sk-ant-api03-xyz"),
            Some("anthropic")
        );
        // OpenRouter key used with anthropic
        assert_eq!(
            check_api_key_prefix("anthropic", "sk-or-v1-xyz"),
            Some("openrouter")
        );
        // Anthropic key used with openai
        assert_eq!(
            check_api_key_prefix("openai", "sk-ant-xyz"),
            Some("anthropic")
        );
        // Groq key used with openai
        assert_eq!(check_api_key_prefix("openai", "gsk_xyz"), Some("groq"));
    }

    #[test]
    fn api_key_prefix_correct_match() {
        assert_eq!(check_api_key_prefix("anthropic", "sk-ant-api03-xyz"), None);
        assert_eq!(check_api_key_prefix("openrouter", "sk-or-v1-xyz"), None);
        assert_eq!(check_api_key_prefix("openai", "sk-proj-xyz"), None);
        assert_eq!(check_api_key_prefix("groq", "gsk_xyz"), None);
    }

    #[test]
    fn api_key_prefix_unknown_provider_skips() {
        // Providers without known key formats should never flag a mismatch.
        assert_eq!(check_api_key_prefix("deepseek", "sk-ant-xyz"), None);
        assert_eq!(check_api_key_prefix("ollama", "anything"), None);
    }

    #[test]
    fn api_key_prefix_unknown_key_format_skips() {
        // Keys without a recognisable prefix should never flag a mismatch.
        assert_eq!(check_api_key_prefix("openai", "my-custom-key-123"), None);
        assert_eq!(check_api_key_prefix("anthropic", "some-random-key"), None);
    }

    #[test]
    fn provider_runtime_options_default_has_empty_extra_headers() {
        let options = ProviderRuntimeOptions::default();
        assert!(options.extra_headers.is_empty());
    }

    #[test]
    fn provider_runtime_options_extra_headers_passed_through() {
        let mut extra_headers = std::collections::HashMap::new();
        extra_headers.insert("X-Title".to_string(), "zeroclaw".to_string());
        let options = ProviderRuntimeOptions {
            extra_headers,
            ..ProviderRuntimeOptions::default()
        };
        assert_eq!(options.extra_headers.len(), 1);
        assert_eq!(options.extra_headers.get("X-Title").unwrap(), "zeroclaw");
    }

    #[test]
    fn env_provider_url_overrides_api_url() {
        // SAFETY: test-only, single-threaded test runner.
        unsafe { std::env::set_var("ZEROCLAW_PROVIDER_URL", "http://env-ollama:11434") };

        let options = ProviderRuntimeOptions::default();

        let provider = create_provider_with_url_and_options(
            "ollama",
            Some("http://config-ollama:11434"),
            None,
            &options,
        );

        assert!(provider.is_ok());

        // SAFETY: test-only, single-threaded test runner.
        unsafe { std::env::remove_var("ZEROCLAW_PROVIDER_URL") };
    }
