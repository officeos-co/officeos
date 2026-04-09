use super::*;
use tempfile::TempDir;

#[test]
fn provider_validation_checks_custom_url_shape() {
    assert!(provider_validation_error("openrouter").is_none());
    assert!(provider_validation_error("custom:https://example.com").is_none());
    assert!(provider_validation_error("anthropic-custom:https://example.com").is_none());

    let invalid_custom = provider_validation_error("custom:").unwrap_or_default();
    assert!(invalid_custom.contains("requires a URL"));

    let invalid_unknown = provider_validation_error("totally-fake").unwrap_or_default();
    assert!(invalid_unknown.contains("Unknown provider"));
}

#[test]
fn diag_item_icons() {
    assert_eq!(DiagItem::ok("t", "m").icon(), "✅");
    assert_eq!(DiagItem::warn("t", "m").icon(), "⚠️ ");
    assert_eq!(DiagItem::error("t", "m").icon(), "❌");
}

#[test]
fn config_validation_catches_bad_temperature() {
    let mut config = Config::default();
    config.default_temperature = 5.0;
    let mut items = Vec::new();
    check_config_semantics(&config, &mut items);
    let temp_item = items.iter().find(|i| i.message.contains("temperature"));
    assert!(temp_item.is_some());
    assert_eq!(temp_item.unwrap().severity, Severity::Error);
}

#[test]
fn config_validation_accepts_valid_temperature() {
    let mut config = Config::default();
    config.default_temperature = 0.7;
    let mut items = Vec::new();
    check_config_semantics(&config, &mut items);
    let temp_item = items.iter().find(|i| i.message.contains("temperature"));
    assert!(temp_item.is_some());
    assert_eq!(temp_item.unwrap().severity, Severity::Ok);
}

#[test]
fn config_validation_warns_no_channels() {
    let config = Config::default();
    let mut items = Vec::new();
    check_config_semantics(&config, &mut items);
    let ch_item = items.iter().find(|i| i.message.contains("channel"));
    assert!(ch_item.is_some());
    assert_eq!(ch_item.unwrap().severity, Severity::Warn);
}

#[test]
fn config_validation_catches_unknown_provider() {
    let mut config = Config::default();
    config.default_provider = Some("totally-fake".into());
    let mut items = Vec::new();
    check_config_semantics(&config, &mut items);
    let prov_item = items
        .iter()
        .find(|i| i.message.contains("default provider"));
    assert!(prov_item.is_some());
    assert_eq!(prov_item.unwrap().severity, Severity::Error);
}

#[test]
fn config_validation_catches_malformed_custom_provider() {
    let mut config = Config::default();
    config.default_provider = Some("custom:".into());
    let mut items = Vec::new();
    check_config_semantics(&config, &mut items);

    let prov_item = items.iter().find(|item| {
        item.message
            .contains("default provider \"custom:\" is invalid")
    });
    assert!(prov_item.is_some());
    assert_eq!(prov_item.unwrap().severity, Severity::Error);
}

#[test]
fn config_validation_accepts_custom_provider() {
    let mut config = Config::default();
    config.default_provider = Some("custom:https://my-api.com".into());
    let mut items = Vec::new();
    check_config_semantics(&config, &mut items);
    let prov_item = items.iter().find(|i| i.message.contains("is valid"));
    assert!(prov_item.is_some());
    assert_eq!(prov_item.unwrap().severity, Severity::Ok);
}

#[test]
fn config_validation_warns_bad_fallback() {
    let mut config = Config::default();
    config.reliability.fallback_providers = vec!["fake-provider".into()];
    let mut items = Vec::new();
    check_config_semantics(&config, &mut items);
    let fb_item = items
        .iter()
        .find(|i| i.message.contains("fallback provider"));
    assert!(fb_item.is_some());
    assert_eq!(fb_item.unwrap().severity, Severity::Warn);
}

#[test]
fn config_validation_warns_bad_custom_fallback() {
    let mut config = Config::default();
    config.reliability.fallback_providers = vec!["custom:".into()];
    let mut items = Vec::new();
    check_config_semantics(&config, &mut items);

    let fb_item = items.iter().find(|item| {
        item.message
            .contains("fallback provider \"custom:\" is invalid")
    });
    assert!(fb_item.is_some());
    assert_eq!(fb_item.unwrap().severity, Severity::Warn);
}

#[test]
fn config_validation_warns_empty_model_route() {
    let mut config = Config::default();
    config.model_routes = vec![crate::config::ModelRouteConfig {
        hint: "fast".into(),
        provider: "groq".into(),
        model: String::new(),
        api_key: None,
    }];
    let mut items = Vec::new();
    check_config_semantics(&config, &mut items);
    let route_item = items.iter().find(|i| i.message.contains("empty model"));
    assert!(route_item.is_some());
    assert_eq!(route_item.unwrap().severity, Severity::Warn);
}

#[test]
fn config_validation_warns_empty_embedding_route_model() {
    let mut config = Config::default();
    config.embedding_routes = vec![crate::config::EmbeddingRouteConfig {
        hint: "semantic".into(),
        provider: "openai".into(),
        model: String::new(),
        dimensions: Some(1536),
        api_key: None,
    }];

    let mut items = Vec::new();
    check_config_semantics(&config, &mut items);
    let route_item = items.iter().find(|item| {
        item.message
            .contains("embedding route \"semantic\" has empty model")
    });
    assert!(route_item.is_some());
    assert_eq!(route_item.unwrap().severity, Severity::Warn);
}

#[test]
fn config_validation_warns_invalid_embedding_route_provider() {
    let mut config = Config::default();
    config.embedding_routes = vec![crate::config::EmbeddingRouteConfig {
        hint: "semantic".into(),
        provider: "groq".into(),
        model: "text-embedding-3-small".into(),
        dimensions: None,
        api_key: None,
    }];

    let mut items = Vec::new();
    check_config_semantics(&config, &mut items);
    let route_item = items
        .iter()
        .find(|item| item.message.contains("uses invalid provider \"groq\""));
    assert!(route_item.is_some());
    assert_eq!(route_item.unwrap().severity, Severity::Warn);
}

#[test]
fn config_validation_warns_missing_embedding_hint_target() {
    let mut config = Config::default();
    config.memory.embedding_model = "hint:semantic".into();

    let mut items = Vec::new();
    check_config_semantics(&config, &mut items);
    let route_item = items.iter().find(|item| {
        item.message
            .contains("no matching [[embedding_routes]] entry exists")
    });
    assert!(route_item.is_some());
    assert_eq!(route_item.unwrap().severity, Severity::Warn);
}

#[test]
fn environment_check_finds_git() {
    let mut items = Vec::new();
    check_environment(&mut items);
    let git_item = items.iter().find(|i| i.message.starts_with("git:"));
    // git should be available in any CI/dev environment
    assert!(git_item.is_some());
    assert_eq!(git_item.unwrap().severity, Severity::Ok);
}

#[test]
fn parse_df_available_mb_uses_last_data_line() {
    let stdout =
        "Filesystem 1M-blocks Used Available Use% Mounted on\n/dev/sda1 1000 500 500 50% /\n";
    assert_eq!(parse_df_available_mb(stdout), Some(500));
}

#[test]
fn truncate_for_display_preserves_utf8_boundaries() {
    let preview = truncate_for_display("🙂example-alpha-build", 3);
    assert_eq!(preview, "🙂ex…");
}

#[test]
fn workspace_probe_path_is_hidden_and_unique() {
    let tmp = TempDir::new().unwrap();
    let first = workspace_probe_path(tmp.path());
    let second = workspace_probe_path(tmp.path());

    assert_ne!(first, second);
    assert!(
        first
            .file_name()
            .and_then(|name| name.to_str())
            .is_some_and(|name| name.starts_with(".zeroclaw_doctor_probe_"))
    );
}

#[test]
fn config_validation_reports_delegate_agents_in_sorted_order() {
    let mut config = Config::default();
    config.agents.insert(
        "zeta".into(),
        crate::config::DelegateAgentConfig {
            provider: "totally-fake".into(),
            model: "model-z".into(),
            system_prompt: None,
            api_key: None,
            temperature: None,
            max_depth: 3,
            agentic: false,
            allowed_tools: Vec::new(),
            max_iterations: 10,
            timeout_secs: None,
            agentic_timeout_secs: None,
            skills_directory: None,
            memory_namespace: None,
        },
    );
    config.agents.insert(
        "alpha".into(),
        crate::config::DelegateAgentConfig {
            provider: "totally-fake".into(),
            model: "model-a".into(),
            system_prompt: None,
            api_key: None,
            temperature: None,
            max_depth: 3,
            agentic: false,
            allowed_tools: Vec::new(),
            max_iterations: 10,
            timeout_secs: None,
            agentic_timeout_secs: None,
            skills_directory: None,
            memory_namespace: None,
        },
    );

    let mut items = Vec::new();
    check_config_semantics(&config, &mut items);

    let agent_messages: Vec<_> = items
        .iter()
        .filter(|item| item.message.starts_with("agent \""))
        .map(|item| item.message.as_str())
        .collect();

    assert_eq!(agent_messages.len(), 2);
    assert!(agent_messages[0].contains("agent \"alpha\""));
    assert!(agent_messages[1].contains("agent \"zeta\""));
}
