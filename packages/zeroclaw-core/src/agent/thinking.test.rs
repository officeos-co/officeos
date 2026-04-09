use super::*;

// ── ThinkingLevel parsing ────────────────────────────────────

#[test]
fn thinking_level_from_str_canonical_names() {
    assert_eq!(
        ThinkingLevel::from_str_insensitive("off"),
        Some(ThinkingLevel::Off)
    );
    assert_eq!(
        ThinkingLevel::from_str_insensitive("minimal"),
        Some(ThinkingLevel::Minimal)
    );
    assert_eq!(
        ThinkingLevel::from_str_insensitive("low"),
        Some(ThinkingLevel::Low)
    );
    assert_eq!(
        ThinkingLevel::from_str_insensitive("medium"),
        Some(ThinkingLevel::Medium)
    );
    assert_eq!(
        ThinkingLevel::from_str_insensitive("high"),
        Some(ThinkingLevel::High)
    );
    assert_eq!(
        ThinkingLevel::from_str_insensitive("max"),
        Some(ThinkingLevel::Max)
    );
}

#[test]
fn thinking_level_from_str_aliases() {
    assert_eq!(
        ThinkingLevel::from_str_insensitive("none"),
        Some(ThinkingLevel::Off)
    );
    assert_eq!(
        ThinkingLevel::from_str_insensitive("min"),
        Some(ThinkingLevel::Minimal)
    );
    assert_eq!(
        ThinkingLevel::from_str_insensitive("med"),
        Some(ThinkingLevel::Medium)
    );
    assert_eq!(
        ThinkingLevel::from_str_insensitive("default"),
        Some(ThinkingLevel::Medium)
    );
    assert_eq!(
        ThinkingLevel::from_str_insensitive("maximum"),
        Some(ThinkingLevel::Max)
    );
}

#[test]
fn thinking_level_from_str_case_insensitive() {
    assert_eq!(
        ThinkingLevel::from_str_insensitive("HIGH"),
        Some(ThinkingLevel::High)
    );
    assert_eq!(
        ThinkingLevel::from_str_insensitive("Max"),
        Some(ThinkingLevel::Max)
    );
    assert_eq!(
        ThinkingLevel::from_str_insensitive("OFF"),
        Some(ThinkingLevel::Off)
    );
}

#[test]
fn thinking_level_from_str_invalid_returns_none() {
    assert_eq!(ThinkingLevel::from_str_insensitive("turbo"), None);
    assert_eq!(ThinkingLevel::from_str_insensitive(""), None);
    assert_eq!(ThinkingLevel::from_str_insensitive("super-high"), None);
}

// ── Directive parsing ────────────────────────────────────────

#[test]
fn parse_directive_extracts_level_and_remaining_message() {
    let result = parse_thinking_directive("/think:high What is Rust?");
    assert!(result.is_some());
    let (level, remaining) = result.unwrap();
    assert_eq!(level, ThinkingLevel::High);
    assert_eq!(remaining, "What is Rust?");
}

#[test]
fn parse_directive_handles_directive_only() {
    let result = parse_thinking_directive("/think:off");
    assert!(result.is_some());
    let (level, remaining) = result.unwrap();
    assert_eq!(level, ThinkingLevel::Off);
    assert_eq!(remaining, "");
}

#[test]
fn parse_directive_strips_leading_whitespace() {
    let result = parse_thinking_directive("  /think:low  Tell me about Rust");
    assert!(result.is_some());
    let (level, remaining) = result.unwrap();
    assert_eq!(level, ThinkingLevel::Low);
    assert_eq!(remaining, "Tell me about Rust");
}

#[test]
fn parse_directive_returns_none_for_no_directive() {
    assert!(parse_thinking_directive("Hello world").is_none());
    assert!(parse_thinking_directive("").is_none());
    assert!(parse_thinking_directive("/think").is_none());
}

#[test]
fn parse_directive_returns_none_for_invalid_level() {
    assert!(parse_thinking_directive("/think:turbo What?").is_none());
}

#[test]
fn parse_directive_not_triggered_mid_message() {
    assert!(parse_thinking_directive("Hello /think:high world").is_none());
}

// ── Level application ────────────────────────────────────────

#[test]
fn apply_thinking_level_off_is_concise() {
    let params = apply_thinking_level(ThinkingLevel::Off);
    assert!(params.temperature_adjustment < 0.0);
    assert!(params.max_tokens_adjustment < 0);
    assert!(params.system_prompt_prefix.is_some());
    assert!(
        params
            .system_prompt_prefix
            .unwrap()
            .to_lowercase()
            .contains("concise")
    );
}

#[test]
fn apply_thinking_level_medium_is_neutral() {
    let params = apply_thinking_level(ThinkingLevel::Medium);
    assert!((params.temperature_adjustment - 0.0).abs() < f64::EPSILON);
    assert_eq!(params.max_tokens_adjustment, 0);
    assert!(params.system_prompt_prefix.is_none());
}

#[test]
fn apply_thinking_level_high_adds_step_by_step() {
    let params = apply_thinking_level(ThinkingLevel::High);
    assert!(params.temperature_adjustment > 0.0);
    assert!(params.max_tokens_adjustment > 0);
    let prefix = params.system_prompt_prefix.unwrap();
    assert!(prefix.to_lowercase().contains("step by step"));
}

#[test]
fn apply_thinking_level_max_is_most_thorough() {
    let params = apply_thinking_level(ThinkingLevel::Max);
    assert!(params.temperature_adjustment > 0.0);
    assert!(params.max_tokens_adjustment > 0);
    let prefix = params.system_prompt_prefix.unwrap();
    assert!(prefix.to_lowercase().contains("exhaustively"));
}

// ── Resolution hierarchy ─────────────────────────────────────

#[test]
fn resolve_inline_directive_takes_priority() {
    let config = ThinkingConfig {
        default_level: ThinkingLevel::Low,
    };
    let result =
        resolve_thinking_level(Some(ThinkingLevel::Max), Some(ThinkingLevel::High), &config);
    assert_eq!(result, ThinkingLevel::Max);
}

#[test]
fn resolve_session_override_takes_priority_over_config() {
    let config = ThinkingConfig {
        default_level: ThinkingLevel::Low,
    };
    let result = resolve_thinking_level(None, Some(ThinkingLevel::High), &config);
    assert_eq!(result, ThinkingLevel::High);
}

#[test]
fn resolve_falls_back_to_config_default() {
    let config = ThinkingConfig {
        default_level: ThinkingLevel::Minimal,
    };
    let result = resolve_thinking_level(None, None, &config);
    assert_eq!(result, ThinkingLevel::Minimal);
}

#[test]
fn resolve_default_config_uses_medium() {
    let config = ThinkingConfig::default();
    let result = resolve_thinking_level(None, None, &config);
    assert_eq!(result, ThinkingLevel::Medium);
}

// ── Temperature clamping ─────────────────────────────────────

#[test]
fn clamp_temperature_within_range() {
    assert!((clamp_temperature(0.7) - 0.7).abs() < f64::EPSILON);
    assert!((clamp_temperature(0.0) - 0.0).abs() < f64::EPSILON);
    assert!((clamp_temperature(2.0) - 2.0).abs() < f64::EPSILON);
}

#[test]
fn clamp_temperature_below_minimum() {
    assert!((clamp_temperature(-0.5) - 0.0).abs() < f64::EPSILON);
}

#[test]
fn clamp_temperature_above_maximum() {
    assert!((clamp_temperature(3.0) - 2.0).abs() < f64::EPSILON);
}

// ── Serde round-trip ─────────────────────────────────────────

#[test]
fn thinking_config_deserializes_from_toml() {
    let toml_str = r#"default_level = "high""#;
    let config: ThinkingConfig = toml::from_str(toml_str).unwrap();
    assert_eq!(config.default_level, ThinkingLevel::High);
}

#[test]
fn thinking_config_default_level_deserializes() {
    let toml_str = "";
    let config: ThinkingConfig = toml::from_str(toml_str).unwrap();
    assert_eq!(config.default_level, ThinkingLevel::Medium);
}

#[test]
fn thinking_level_serializes_lowercase() {
    let level = ThinkingLevel::High;
    let json = serde_json::to_string(&level).unwrap();
    assert_eq!(json, "\"high\"");
}
