use super::*;

// ── estimate_complexity ─────────────────────────────────────

#[test]
fn simple_short_message() {
    assert_eq!(estimate_complexity("hi"), ComplexityTier::Simple);
    assert_eq!(estimate_complexity("hello"), ComplexityTier::Simple);
    assert_eq!(estimate_complexity("yes"), ComplexityTier::Simple);
}

#[test]
fn complex_long_message() {
    let long = "a".repeat(201);
    assert_eq!(estimate_complexity(&long), ComplexityTier::Complex);
}

#[test]
fn complex_code_fence() {
    let msg = "Here is some code:\n```rust\nfn main() {}\n```";
    assert_eq!(estimate_complexity(msg), ComplexityTier::Complex);
}

#[test]
fn complex_multiple_reasoning_keywords() {
    let msg = "Please explain why this design is better and analyze the trade-off";
    assert_eq!(estimate_complexity(msg), ComplexityTier::Complex);
}

#[test]
fn standard_medium_message() {
    // 50+ chars but no code fence, < 2 reasoning keywords
    let msg = "Can you help me find a good restaurant in this area please?";
    assert_eq!(estimate_complexity(msg), ComplexityTier::Standard);
}

#[test]
fn standard_short_with_one_keyword() {
    // < 50 chars but has 1 reasoning keyword → still not Simple
    let msg = "explain this";
    assert_eq!(estimate_complexity(msg), ComplexityTier::Standard);
}

// ── auto_classify ───────────────────────────────────────────

#[test]
fn auto_classify_maps_tiers_to_hints() {
    let ac = AutoClassifyConfig {
        simple_hint: Some("fast".into()),
        standard_hint: None,
        complex_hint: Some("reasoning".into()),
        ..Default::default()
    };
    assert_eq!(ac.hint_for(ComplexityTier::Simple), Some("fast"));
    assert_eq!(ac.hint_for(ComplexityTier::Standard), None);
    assert_eq!(ac.hint_for(ComplexityTier::Complex), Some("reasoning"));
}

// ── evaluate_response ───────────────────────────────────────

#[test]
fn empty_response_scores_low() {
    let result = evaluate_response("hello", "", ComplexityTier::Simple, None);
    assert!(result.score <= 0.5, "empty response should score low");
}

#[test]
fn good_response_scores_high() {
    let result = evaluate_response(
        "what is 2+2?",
        "The answer is 4.",
        ComplexityTier::Simple,
        None,
    );
    assert!(
        result.score >= 0.9,
        "good simple response should score high, got {}",
        result.score
    );
}

#[test]
fn cop_out_response_penalized() {
    let result = evaluate_response(
        "explain quantum computing",
        "I don't know much about that.",
        ComplexityTier::Standard,
        None,
    );
    assert!(
        result.score < 1.0,
        "cop-out should be penalized, got {}",
        result.score
    );
}

#[test]
fn code_query_without_code_response_penalized() {
    let result = evaluate_response(
        "write a function to sort an array",
        "You should use a sorting algorithm.",
        ComplexityTier::Standard,
        None,
    );
    // "code_presence" check should fail
    let code_check = result.checks.iter().find(|c| c.name == "code_presence");
    assert!(
        code_check.is_some() && !code_check.unwrap().passed,
        "code check should fail"
    );
}

#[test]
fn retry_hint_escalation() {
    let ac = AutoClassifyConfig {
        simple_hint: Some("fast".into()),
        standard_hint: Some("default".into()),
        complex_hint: Some("reasoning".into()),
        ..Default::default()
    };
    // Empty response for a Simple query → should suggest Standard hint
    let result = evaluate_response("hello", "", ComplexityTier::Simple, Some(&ac));
    assert_eq!(result.retry_hint, Some("default".into()));
}

#[test]
fn no_retry_when_already_complex() {
    let ac = AutoClassifyConfig {
        simple_hint: Some("fast".into()),
        standard_hint: Some("default".into()),
        complex_hint: Some("reasoning".into()),
        ..Default::default()
    };
    // Empty response for Complex → no escalation possible
    let result = evaluate_response("explain everything", "", ComplexityTier::Complex, Some(&ac));
    assert_eq!(result.retry_hint, None);
}

#[test]
fn max_retries_defaults() {
    let config = EvalConfig::default();
    assert!(!config.enabled);
    assert_eq!(config.max_retries, 1);
    assert!((config.min_quality_score - 0.5).abs() < f64::EPSILON);
}

#[test]
fn cost_optimized_hint_default() {
    let config = AutoClassifyConfig::default();
    assert_eq!(config.cost_optimized_hint, "cost-optimized");
}
