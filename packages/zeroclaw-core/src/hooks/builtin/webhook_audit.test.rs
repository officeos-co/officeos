use super::*;

// ── Glob matching tests ──────────────────────────────────────

#[test]
fn glob_exact_match() {
    assert!(glob_matches("file_write", "file_write"));
    assert!(!glob_matches("file_write", "file_read"));
}

#[test]
fn glob_wildcard_suffix() {
    assert!(glob_matches("mcp__*", "mcp__github"));
    assert!(glob_matches("mcp__*", "mcp__"));
    assert!(!glob_matches("mcp__*", "mcp_github"));
}

#[test]
fn glob_wildcard_prefix() {
    assert!(glob_matches("*_write", "file_write"));
    assert!(glob_matches("*_write", "_write"));
    assert!(!glob_matches("*_write", "file_read"));
}

#[test]
fn glob_wildcard_middle() {
    assert!(glob_matches("mcp__*__create", "mcp__github__create"));
    assert!(glob_matches("mcp__*__create", "mcp____create"));
    assert!(!glob_matches("mcp__*__create", "mcp__github__delete"));
}

#[test]
fn glob_star_matches_everything() {
    assert!(glob_matches("*", "anything_at_all"));
    assert!(glob_matches("*", ""));
}

#[test]
fn glob_empty_pattern() {
    assert!(glob_matches("", ""));
    assert!(!glob_matches("", "something"));
}

// ── matches_any_pattern ──────────────────────────────────────

#[test]
fn matches_any_pattern_works() {
    let patterns = vec!["Bash".to_string(), "mcp__*".to_string()];
    assert!(matches_any_pattern(&patterns, "Bash"));
    assert!(matches_any_pattern(&patterns, "mcp__github"));
    assert!(!matches_any_pattern(&patterns, "Write"));
}

#[test]
fn empty_patterns_matches_nothing() {
    let patterns: Vec<String> = vec![];
    assert!(!matches_any_pattern(&patterns, "anything"));
}

// ── before_tool_call tests ────────────────────────────────────

fn make_hook(patterns: Vec<&str>, include_args: bool) -> WebhookAuditHook {
    // Use https URL for tests to pass URL validation; localhost with http
    // is only allowed in debug builds, but use https to be safe.
    WebhookAuditHook::new(WebhookAuditConfig {
        enabled: true,
        url: "https://audit.example.com/webhook".to_string(),
        tool_patterns: patterns.into_iter().map(String::from).collect(),
        include_args,
        max_args_bytes: 4096,
    })
}

#[tokio::test]
async fn before_tool_call_captures_args_when_enabled() {
    let hook = make_hook(vec!["Bash", "mcp__*"], true);
    let args = serde_json::json!({"command": "ls"});
    let result = hook.before_tool_call("Bash".into(), args.clone()).await;
    assert!(!result.is_cancel());

    let pending = hook.pending_args.lock().unwrap();
    assert_eq!(pending.get("Bash"), Some(&vec![args]));
}

#[tokio::test]
async fn before_tool_call_concurrent_same_tool_no_data_loss() {
    let hook = make_hook(vec!["Bash"], true);
    let args1 = serde_json::json!({"command": "ls"});
    let args2 = serde_json::json!({"command": "pwd"});
    hook.before_tool_call("Bash".into(), args1.clone()).await;
    hook.before_tool_call("Bash".into(), args2.clone()).await;

    let pending = hook.pending_args.lock().unwrap();
    let bash_args = pending.get("Bash").unwrap();
    assert_eq!(bash_args.len(), 2);
    assert_eq!(bash_args[0], args1);
    assert_eq!(bash_args[1], args2);
}

#[tokio::test]
async fn before_tool_call_skips_non_matching_tools() {
    let hook = make_hook(vec!["Bash"], true);
    let args = serde_json::json!({"path": "/tmp"});
    let result = hook.before_tool_call("Write".into(), args).await;
    assert!(!result.is_cancel());

    let pending = hook.pending_args.lock().unwrap();
    assert!(pending.is_empty());
}

#[tokio::test]
async fn before_tool_call_skips_when_include_args_false() {
    let hook = make_hook(vec!["Bash"], false);
    let args = serde_json::json!({"command": "ls"});
    let result = hook.before_tool_call("Bash".into(), args).await;
    assert!(!result.is_cancel());

    let pending = hook.pending_args.lock().unwrap();
    assert!(pending.is_empty());
}

// ── Truncation tests ─────────────────────────────────────────

#[test]
fn truncate_args_within_limit() {
    let args = serde_json::json!({"key": "val"});
    let result = truncate_args(args.clone(), 1000);
    assert_eq!(result, args);
}

#[test]
fn truncate_args_over_limit() {
    let args = serde_json::json!({"key": "a]long value that exceeds limit"});
    let result = truncate_args(args, 10);
    assert!(result.is_string());
    let s = result.as_str().unwrap();
    assert!(s.ends_with("...[truncated]"));
}

#[test]
fn truncate_args_zero_means_no_limit() {
    let args = serde_json::json!({"key": "value"});
    let result = truncate_args(args.clone(), 0);
    assert_eq!(result, args);
}

// ── on_after_tool_call tests ─────────────────────────────────

#[tokio::test]
async fn on_after_tool_call_skips_non_matching() {
    let hook = make_hook(vec!["Bash"], true);
    let result = ToolResult {
        success: true,
        output: "ok".into(),
        error: None,
    };
    // Call with a non-matching tool — should not panic or do anything.
    hook.on_after_tool_call("Write", &result, Duration::from_millis(10))
        .await;
    // No assertion needed beyond "doesn't panic"; args map stays empty.
    let pending = hook.pending_args.lock().unwrap();
    assert!(pending.is_empty());
}

#[tokio::test]
async fn on_after_tool_call_skips_empty_url() {
    // Empty URL + enabled triggers a warning, but should not panic.
    let hook = WebhookAuditHook::new(WebhookAuditConfig {
        enabled: true,
        url: String::new(),
        tool_patterns: vec!["Bash".to_string()],
        include_args: false,
        max_args_bytes: 4096,
    });
    let result = ToolResult {
        success: true,
        output: "ok".into(),
        error: None,
    };
    // Should return immediately without spawning any HTTP request.
    hook.on_after_tool_call("Bash", &result, Duration::from_millis(5))
        .await;
}

// ── URL validation tests ─────────────────────────────────────

#[test]
fn validate_url_rejects_loopback_ipv4() {
    assert!(validate_webhook_url("https://127.0.0.1/hook").is_err());
    assert!(validate_webhook_url("https://127.0.0.100/hook").is_err());
}

#[test]
fn validate_url_rejects_loopback_ipv6() {
    assert!(validate_webhook_url("https://[::1]/hook").is_err());
}

#[test]
fn validate_url_rejects_private_rfc1918() {
    assert!(validate_webhook_url("https://10.0.0.1/hook").is_err());
    assert!(validate_webhook_url("https://172.16.5.1/hook").is_err());
    assert!(validate_webhook_url("https://192.168.1.1/hook").is_err());
}

#[test]
fn validate_url_rejects_link_local() {
    assert!(validate_webhook_url("https://169.254.1.1/hook").is_err());
    assert!(validate_webhook_url("https://[fe80::1]/hook").is_err());
}

#[test]
fn validate_url_rejects_http_non_localhost() {
    assert!(validate_webhook_url("http://example.com/hook").is_err());
}

#[test]
fn validate_url_accepts_https_public() {
    assert!(validate_webhook_url("https://audit.example.com/webhook").is_ok());
    assert!(validate_webhook_url("https://8.8.8.8/hook").is_ok());
}

#[test]
fn validate_url_rejects_non_http_scheme() {
    assert!(validate_webhook_url("ftp://example.com/hook").is_err());
}
