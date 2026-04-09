use super::*;
use crate::tools::ToolSpec;
use futures_util::StreamExt;
use std::sync::Arc;

struct MockProvider {
    calls: Arc<AtomicUsize>,
    fail_until_attempt: usize,
    response: &'static str,
    error: &'static str,
}

#[async_trait]
impl Provider for MockProvider {
    async fn chat_with_system(
        &self,
        _system_prompt: Option<&str>,
        _message: &str,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<String> {
        let attempt = self.calls.fetch_add(1, Ordering::SeqCst) + 1;
        if attempt <= self.fail_until_attempt {
            anyhow::bail!(self.error);
        }
        Ok(self.response.to_string())
    }

    async fn chat_with_history(
        &self,
        _messages: &[ChatMessage],
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<String> {
        let attempt = self.calls.fetch_add(1, Ordering::SeqCst) + 1;
        if attempt <= self.fail_until_attempt {
            anyhow::bail!(self.error);
        }
        Ok(self.response.to_string())
    }
}

/// Mock that records which model was used for each call.
struct ModelAwareMock {
    calls: Arc<AtomicUsize>,
    models_seen: parking_lot::Mutex<Vec<String>>,
    fail_models: Vec<&'static str>,
    response: &'static str,
}

#[async_trait]
impl Provider for ModelAwareMock {
    async fn chat_with_system(
        &self,
        _system_prompt: Option<&str>,
        _message: &str,
        model: &str,
        _temperature: f64,
    ) -> anyhow::Result<String> {
        self.calls.fetch_add(1, Ordering::SeqCst);
        self.models_seen.lock().push(model.to_string());
        if self.fail_models.contains(&model) {
            anyhow::bail!("500 model {} unavailable", model);
        }
        Ok(self.response.to_string())
    }
}

// ── Existing tests (preserved) ──

#[tokio::test]
async fn succeeds_without_retry() {
    let calls = Arc::new(AtomicUsize::new(0));
    let provider = ReliableProvider::new(
        vec![(
            "primary".into(),
            Box::new(MockProvider {
                calls: Arc::clone(&calls),
                fail_until_attempt: 0,
                response: "ok",
                error: "boom",
            }),
        )],
        2,
        1,
    );

    let result = provider.simple_chat("hello", "test", 0.0).await.unwrap();
    assert_eq!(result, "ok");
    assert_eq!(calls.load(Ordering::SeqCst), 1);
}

#[tokio::test]
async fn retries_then_recovers() {
    let calls = Arc::new(AtomicUsize::new(0));
    let provider = ReliableProvider::new(
        vec![(
            "primary".into(),
            Box::new(MockProvider {
                calls: Arc::clone(&calls),
                fail_until_attempt: 1,
                response: "recovered",
                error: "temporary",
            }),
        )],
        2,
        1,
    );

    let result = provider.simple_chat("hello", "test", 0.0).await.unwrap();
    assert_eq!(result, "recovered");
    assert_eq!(calls.load(Ordering::SeqCst), 2);
}

#[tokio::test]
async fn falls_back_after_retries_exhausted() {
    let primary_calls = Arc::new(AtomicUsize::new(0));
    let fallback_calls = Arc::new(AtomicUsize::new(0));

    let provider = ReliableProvider::new(
        vec![
            (
                "primary".into(),
                Box::new(MockProvider {
                    calls: Arc::clone(&primary_calls),
                    fail_until_attempt: usize::MAX,
                    response: "never",
                    error: "primary down",
                }),
            ),
            (
                "fallback".into(),
                Box::new(MockProvider {
                    calls: Arc::clone(&fallback_calls),
                    fail_until_attempt: 0,
                    response: "from fallback",
                    error: "fallback down",
                }),
            ),
        ],
        1,
        1,
    );

    let result = provider.simple_chat("hello", "test", 0.0).await.unwrap();
    assert_eq!(result, "from fallback");
    assert_eq!(primary_calls.load(Ordering::SeqCst), 2);
    assert_eq!(fallback_calls.load(Ordering::SeqCst), 1);
}

#[tokio::test]
async fn returns_aggregated_error_when_all_providers_fail() {
    let provider = ReliableProvider::new(
        vec![
            (
                "p1".into(),
                Box::new(MockProvider {
                    calls: Arc::new(AtomicUsize::new(0)),
                    fail_until_attempt: usize::MAX,
                    response: "never",
                    error: "p1 error",
                }),
            ),
            (
                "p2".into(),
                Box::new(MockProvider {
                    calls: Arc::new(AtomicUsize::new(0)),
                    fail_until_attempt: usize::MAX,
                    response: "never",
                    error: "p2 error",
                }),
            ),
        ],
        0,
        1,
    );

    let err = provider
        .simple_chat("hello", "test", 0.0)
        .await
        .expect_err("all providers should fail");
    let msg = err.to_string();
    assert!(msg.contains("All providers/models failed"));
    assert!(msg.contains("provider=p1 model=test"));
    assert!(msg.contains("provider=p2 model=test"));
    assert!(msg.contains("error=p1 error"));
    assert!(msg.contains("error=p2 error"));
    assert!(msg.contains("retryable"));
}

#[test]
fn non_retryable_detects_common_patterns() {
    assert!(is_non_retryable(&anyhow::anyhow!("400 Bad Request")));
    assert!(is_non_retryable(&anyhow::anyhow!("401 Unauthorized")));
    assert!(is_non_retryable(&anyhow::anyhow!("403 Forbidden")));
    assert!(is_non_retryable(&anyhow::anyhow!("404 Not Found")));
    assert!(is_non_retryable(&anyhow::anyhow!(
        "invalid api key provided"
    )));
    assert!(is_non_retryable(&anyhow::anyhow!("authentication failed")));
    assert!(is_non_retryable(&anyhow::anyhow!(
        "model glm-4.7 not found"
    )));
    assert!(is_non_retryable(&anyhow::anyhow!(
        "unsupported model: glm-4.7"
    )));
    assert!(!is_non_retryable(&anyhow::anyhow!("429 Too Many Requests")));
    assert!(!is_non_retryable(&anyhow::anyhow!("408 Request Timeout")));
    assert!(!is_non_retryable(&anyhow::anyhow!(
        "500 Internal Server Error"
    )));
    assert!(!is_non_retryable(&anyhow::anyhow!("502 Bad Gateway")));
    assert!(!is_non_retryable(&anyhow::anyhow!("timeout")));
    assert!(!is_non_retryable(&anyhow::anyhow!("connection reset")));
    assert!(!is_non_retryable(&anyhow::anyhow!(
        "model overloaded, try again later"
    )));
    // Context window errors are now recoverable (not non-retryable)
    assert!(!is_non_retryable(&anyhow::anyhow!(
        "OpenAI Codex stream error: Your input exceeds the context window of this model."
    )));
}

#[tokio::test]
async fn context_window_error_aborts_retries_and_model_fallbacks() {
    let calls = Arc::new(AtomicUsize::new(0));
    let mut model_fallbacks = std::collections::HashMap::new();
    model_fallbacks.insert(
        "gpt-5.3-codex".to_string(),
        vec!["gpt-5.2-codex".to_string()],
    );

    let provider = ReliableProvider::new(
            vec![(
                "openai-codex".into(),
                Box::new(MockProvider {
                    calls: Arc::clone(&calls),
                    fail_until_attempt: usize::MAX,
                    response: "never",
                    error: "OpenAI Codex stream error: Your input exceeds the context window of this model. Please adjust your input and try again.",
                }),
            )],
            4,
            1,
        )
        .with_model_fallbacks(model_fallbacks);

    let err = provider
        .simple_chat("hello", "gpt-5.3-codex", 0.0)
        .await
        .expect_err("context window overflow should fail fast");
    let msg = err.to_string();

    assert!(msg.contains("context window"));
    // chat_with_system has no history to truncate, so it bails immediately
    assert_eq!(calls.load(Ordering::SeqCst), 1);
}

#[tokio::test]
async fn aggregated_error_marks_non_retryable_model_mismatch_with_details() {
    let calls = Arc::new(AtomicUsize::new(0));
    let provider = ReliableProvider::new(
        vec![(
            "custom".into(),
            Box::new(MockProvider {
                calls: Arc::clone(&calls),
                fail_until_attempt: usize::MAX,
                response: "never",
                error: "unsupported model: glm-4.7",
            }),
        )],
        3,
        1,
    );

    let err = provider
        .simple_chat("hello", "glm-4.7", 0.0)
        .await
        .expect_err("provider should fail");
    let msg = err.to_string();

    assert!(msg.contains("non_retryable"));
    assert!(msg.contains("error=unsupported model: glm-4.7"));
    // Non-retryable errors should not consume retry budget.
    assert_eq!(calls.load(Ordering::SeqCst), 1);
}

#[tokio::test]
async fn skips_retries_on_non_retryable_error() {
    let primary_calls = Arc::new(AtomicUsize::new(0));
    let fallback_calls = Arc::new(AtomicUsize::new(0));

    let provider = ReliableProvider::new(
        vec![
            (
                "primary".into(),
                Box::new(MockProvider {
                    calls: Arc::clone(&primary_calls),
                    fail_until_attempt: usize::MAX,
                    response: "never",
                    error: "401 Unauthorized",
                }),
            ),
            (
                "fallback".into(),
                Box::new(MockProvider {
                    calls: Arc::clone(&fallback_calls),
                    fail_until_attempt: 0,
                    response: "from fallback",
                    error: "fallback err",
                }),
            ),
        ],
        3,
        1,
    );

    let result = provider.simple_chat("hello", "test", 0.0).await.unwrap();
    assert_eq!(result, "from fallback");
    // Primary should have been called only once (no retries)
    assert_eq!(primary_calls.load(Ordering::SeqCst), 1);
    assert_eq!(fallback_calls.load(Ordering::SeqCst), 1);
}

#[tokio::test]
async fn chat_with_history_retries_then_recovers() {
    let calls = Arc::new(AtomicUsize::new(0));
    let provider = ReliableProvider::new(
        vec![(
            "primary".into(),
            Box::new(MockProvider {
                calls: Arc::clone(&calls),
                fail_until_attempt: 1,
                response: "history ok",
                error: "temporary",
            }),
        )],
        2,
        1,
    );

    let messages = vec![ChatMessage::system("system"), ChatMessage::user("hello")];
    let result = provider
        .chat_with_history(&messages, "test", 0.0)
        .await
        .unwrap();
    assert_eq!(result, "history ok");
    assert_eq!(calls.load(Ordering::SeqCst), 2);
}

#[tokio::test]
async fn chat_with_history_falls_back() {
    let primary_calls = Arc::new(AtomicUsize::new(0));
    let fallback_calls = Arc::new(AtomicUsize::new(0));

    let provider = ReliableProvider::new(
        vec![
            (
                "primary".into(),
                Box::new(MockProvider {
                    calls: Arc::clone(&primary_calls),
                    fail_until_attempt: usize::MAX,
                    response: "never",
                    error: "primary down",
                }),
            ),
            (
                "fallback".into(),
                Box::new(MockProvider {
                    calls: Arc::clone(&fallback_calls),
                    fail_until_attempt: 0,
                    response: "fallback ok",
                    error: "fallback err",
                }),
            ),
        ],
        1,
        1,
    );

    let messages = vec![ChatMessage::user("hello")];
    let result = provider
        .chat_with_history(&messages, "test", 0.0)
        .await
        .unwrap();
    assert_eq!(result, "fallback ok");
    assert_eq!(primary_calls.load(Ordering::SeqCst), 2);
    assert_eq!(fallback_calls.load(Ordering::SeqCst), 1);
}

// ── New tests: model failover ──

#[tokio::test]
async fn model_failover_tries_fallback_model() {
    let calls = Arc::new(AtomicUsize::new(0));
    let mock = Arc::new(ModelAwareMock {
        calls: Arc::clone(&calls),
        models_seen: parking_lot::Mutex::new(Vec::new()),
        fail_models: vec!["claude-opus"],
        response: "ok from sonnet",
    });

    let mut fallbacks = HashMap::new();
    fallbacks.insert("claude-opus".to_string(), vec!["claude-sonnet".to_string()]);

    let provider = ReliableProvider::new(
        vec![(
            "anthropic".into(),
            Box::new(mock.clone()) as Box<dyn Provider>,
        )],
        0, // no retries — force immediate model failover
        1,
    )
    .with_model_fallbacks(fallbacks);

    let result = provider
        .simple_chat("hello", "claude-opus", 0.0)
        .await
        .unwrap();
    assert_eq!(result, "ok from sonnet");

    let seen = mock.models_seen.lock();
    assert_eq!(seen.len(), 2);
    assert_eq!(seen[0], "claude-opus");
    assert_eq!(seen[1], "claude-sonnet");
}

#[tokio::test]
async fn model_failover_all_models_fail() {
    let calls = Arc::new(AtomicUsize::new(0));
    let mock = Arc::new(ModelAwareMock {
        calls: Arc::clone(&calls),
        models_seen: parking_lot::Mutex::new(Vec::new()),
        fail_models: vec!["model-a", "model-b", "model-c"],
        response: "never",
    });

    let mut fallbacks = HashMap::new();
    fallbacks.insert(
        "model-a".to_string(),
        vec!["model-b".to_string(), "model-c".to_string()],
    );

    let provider = ReliableProvider::new(
        vec![("p1".into(), Box::new(mock.clone()) as Box<dyn Provider>)],
        0,
        1,
    )
    .with_model_fallbacks(fallbacks);

    let err = provider
        .simple_chat("hello", "model-a", 0.0)
        .await
        .expect_err("all models should fail");
    assert!(err.to_string().contains("All providers/models failed"));

    let seen = mock.models_seen.lock();
    assert_eq!(seen.len(), 3);
}

#[tokio::test]
async fn no_model_fallbacks_behaves_like_before() {
    let calls = Arc::new(AtomicUsize::new(0));
    let provider = ReliableProvider::new(
        vec![(
            "primary".into(),
            Box::new(MockProvider {
                calls: Arc::clone(&calls),
                fail_until_attempt: 0,
                response: "ok",
                error: "boom",
            }),
        )],
        2,
        1,
    );
    // No model_fallbacks set — should work exactly as before
    let result = provider.simple_chat("hello", "test", 0.0).await.unwrap();
    assert_eq!(result, "ok");
    assert_eq!(calls.load(Ordering::SeqCst), 1);
}

// ── New tests: auth rotation ──

#[tokio::test]
async fn auth_rotation_cycles_keys() {
    let provider = ReliableProvider::new(
        vec![(
            "p".into(),
            Box::new(MockProvider {
                calls: Arc::new(AtomicUsize::new(0)),
                fail_until_attempt: 0,
                response: "ok",
                error: "",
            }),
        )],
        0,
        1,
    )
    .with_api_keys(vec!["key-a".into(), "key-b".into(), "key-c".into()]);

    // Rotate 5 times, verify round-robin
    let keys: Vec<&str> = (0..5).map(|_| provider.rotate_key().unwrap()).collect();
    assert_eq!(keys, vec!["key-a", "key-b", "key-c", "key-a", "key-b"]);
}

#[tokio::test]
async fn auth_rotation_returns_none_when_empty() {
    let provider = ReliableProvider::new(vec![], 0, 1);
    assert!(provider.rotate_key().is_none());
}

// ── New tests: Retry-After parsing ──

#[test]
fn parse_retry_after_integer() {
    let err = anyhow::anyhow!("429 Too Many Requests, Retry-After: 5");
    assert_eq!(parse_retry_after_ms(&err), Some(5000));
}

#[test]
fn parse_retry_after_float() {
    let err = anyhow::anyhow!("Rate limited. retry_after: 2.5 seconds");
    assert_eq!(parse_retry_after_ms(&err), Some(2500));
}

#[test]
fn parse_retry_after_missing() {
    let err = anyhow::anyhow!("500 Internal Server Error");
    assert_eq!(parse_retry_after_ms(&err), None);
}

#[test]
fn rate_limited_detection() {
    assert!(is_rate_limited(&anyhow::anyhow!("429 Too Many Requests")));
    assert!(is_rate_limited(&anyhow::anyhow!(
        "HTTP 429 rate limit exceeded"
    )));
    assert!(!is_rate_limited(&anyhow::anyhow!("401 Unauthorized")));
    assert!(!is_rate_limited(&anyhow::anyhow!(
        "500 Internal Server Error"
    )));
}

#[test]
fn non_retryable_rate_limit_detects_plan_restricted_model() {
    let err = anyhow::anyhow!(
        "{}",
        "API error (429 Too Many Requests): {\"code\":1311,\"message\":\"the current account plan does not include glm-5\"}"
    );
    assert!(
        is_non_retryable_rate_limit(&err),
        "plan-restricted 429 should skip retries"
    );
}

#[test]
fn non_retryable_rate_limit_detects_insufficient_balance() {
    let err = anyhow::anyhow!(
        "{}",
        "API error (429 Too Many Requests): {\"code\":1113,\"message\":\"insufficient balance\"}"
    );
    assert!(
        is_non_retryable_rate_limit(&err),
        "insufficient-balance 429 should skip retries"
    );
}

#[test]
fn non_retryable_rate_limit_does_not_flag_generic_429() {
    let err = anyhow::anyhow!("429 Too Many Requests: rate limit exceeded");
    assert!(
        !is_non_retryable_rate_limit(&err),
        "generic rate-limit 429 should remain retryable"
    );
}

#[test]
fn compute_backoff_uses_retry_after() {
    let provider = ReliableProvider::new(vec![], 0, 500);
    let err = anyhow::anyhow!("429 Retry-After: 3");
    assert_eq!(provider.compute_backoff(500, &err), 3_000);
}

#[test]
fn compute_backoff_caps_at_30s() {
    let provider = ReliableProvider::new(vec![], 0, 500);
    let err = anyhow::anyhow!("429 Retry-After: 120");
    assert_eq!(provider.compute_backoff(500, &err), 30_000);
}

#[test]
fn compute_backoff_falls_back_to_base() {
    let provider = ReliableProvider::new(vec![], 0, 500);
    let err = anyhow::anyhow!("500 Server Error");
    assert_eq!(provider.compute_backoff(500, &err), 500);
}

// ── §2.1 API auth error (401/403) tests ──────────────────

#[test]
fn non_retryable_detects_401() {
    let err = anyhow::anyhow!("API error (401 Unauthorized): invalid api key");
    assert!(
        is_non_retryable(&err),
        "401 errors must be detected as non-retryable"
    );
}

#[test]
fn non_retryable_detects_403() {
    let err = anyhow::anyhow!("API error (403 Forbidden): access denied");
    assert!(
        is_non_retryable(&err),
        "403 errors must be detected as non-retryable"
    );
}

#[test]
fn non_retryable_detects_404() {
    let err = anyhow::anyhow!("API error (404 Not Found): model not found");
    assert!(
        is_non_retryable(&err),
        "404 errors must be detected as non-retryable"
    );
}

#[test]
fn non_retryable_does_not_flag_429() {
    let err = anyhow::anyhow!("429 Too Many Requests");
    assert!(
        !is_non_retryable(&err),
        "429 must NOT be treated as non-retryable (it is retryable with backoff)"
    );
}

#[test]
fn non_retryable_does_not_flag_408() {
    let err = anyhow::anyhow!("408 Request Timeout");
    assert!(
        !is_non_retryable(&err),
        "408 must NOT be treated as non-retryable (it is retryable)"
    );
}

#[test]
fn non_retryable_does_not_flag_500() {
    let err = anyhow::anyhow!("500 Internal Server Error");
    assert!(
        !is_non_retryable(&err),
        "500 must NOT be treated as non-retryable (server errors are retryable)"
    );
}

#[test]
fn non_retryable_does_not_flag_502() {
    let err = anyhow::anyhow!("502 Bad Gateway");
    assert!(
        !is_non_retryable(&err),
        "502 must NOT be treated as non-retryable"
    );
}

// ── §2.2 Rate limit Retry-After edge cases ───────────────

#[test]
fn parse_retry_after_zero() {
    let err = anyhow::anyhow!("429 Too Many Requests, Retry-After: 0");
    assert_eq!(
        parse_retry_after_ms(&err),
        Some(0),
        "Retry-After: 0 should parse as 0ms"
    );
}

#[test]
fn parse_retry_after_with_underscore_separator() {
    let err = anyhow::anyhow!("rate limited, retry_after: 10");
    assert_eq!(
        parse_retry_after_ms(&err),
        Some(10_000),
        "retry_after with underscore must be parsed"
    );
}

#[test]
fn parse_retry_after_space_separator() {
    let err = anyhow::anyhow!("Retry-After 7");
    assert_eq!(
        parse_retry_after_ms(&err),
        Some(7000),
        "Retry-After with space separator must be parsed"
    );
}

#[test]
fn rate_limited_false_for_generic_error() {
    let err = anyhow::anyhow!("Connection refused");
    assert!(
        !is_rate_limited(&err),
        "generic errors must not be flagged as rate-limited"
    );
}

// ── §2.3 Malformed API response error classification ─────

#[tokio::test]
async fn non_retryable_skips_retries_for_401() {
    let calls = Arc::new(AtomicUsize::new(0));
    let provider = ReliableProvider::new(
        vec![(
            "primary".into(),
            Box::new(MockProvider {
                calls: Arc::clone(&calls),
                fail_until_attempt: usize::MAX,
                response: "never",
                error: "API error (401 Unauthorized): invalid key",
            }),
        )],
        5,
        1,
    );

    let result = provider.simple_chat("hello", "test", 0.0).await;
    assert!(result.is_err(), "401 should fail without retries");
    assert_eq!(
        calls.load(Ordering::SeqCst),
        1,
        "must not retry on 401 — should be exactly 1 call"
    );
}

#[tokio::test]
async fn non_retryable_rate_limit_skips_retries_for_plan_errors() {
    let calls = Arc::new(AtomicUsize::new(0));
    let provider = ReliableProvider::new(
        vec![(
            "primary".into(),
            Box::new(MockProvider {
                calls: Arc::clone(&calls),
                fail_until_attempt: usize::MAX,
                response: "never",
                error: "API error (429 Too Many Requests): {\"code\":1311,\"message\":\"plan does not include glm-5\"}",
            }),
        )],
        5,
        1,
    );

    let result = provider.simple_chat("hello", "test", 0.0).await;
    assert!(
        result.is_err(),
        "plan-restricted 429 should fail quickly without retrying"
    );
    assert_eq!(
        calls.load(Ordering::SeqCst),
        1,
        "must not retry non-retryable 429 business errors"
    );
}

// ── Arc<ModelAwareMock> Provider impl for test ──

#[async_trait]
impl Provider for Arc<ModelAwareMock> {
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

/// Mock provider that implements `chat()` with native tool support.
struct NativeToolMock {
    calls: Arc<AtomicUsize>,
    fail_until_attempt: usize,
    response_text: &'static str,
    tool_calls: Vec<super::super::traits::ToolCall>,
    error: &'static str,
}

#[async_trait]
impl Provider for NativeToolMock {
    async fn chat_with_system(
        &self,
        _system_prompt: Option<&str>,
        _message: &str,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<String> {
        Ok(self.response_text.to_string())
    }

    fn supports_native_tools(&self) -> bool {
        true
    }

    async fn chat(
        &self,
        _request: ChatRequest<'_>,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<ChatResponse> {
        let attempt = self.calls.fetch_add(1, Ordering::SeqCst) + 1;
        if attempt <= self.fail_until_attempt {
            anyhow::bail!(self.error);
        }
        Ok(ChatResponse {
            text: Some(self.response_text.to_string()),
            tool_calls: self.tool_calls.clone(),
            usage: None,
            reasoning_content: None,
        })
    }
}

#[tokio::test]
async fn chat_delegates_to_inner_provider() {
    let calls = Arc::new(AtomicUsize::new(0));
    let tool_call = super::super::traits::ToolCall {
        id: "call_1".to_string(),
        name: "shell".to_string(),
        arguments: r#"{"command":"date"}"#.to_string(),
    };
    let provider = ReliableProvider::new(
        vec![(
            "primary".into(),
            Box::new(NativeToolMock {
                calls: Arc::clone(&calls),
                fail_until_attempt: 0,
                response_text: "ok",
                tool_calls: vec![tool_call.clone()],
                error: "boom",
            }) as Box<dyn Provider>,
        )],
        2,
        1,
    );

    let messages = vec![ChatMessage::user("what time is it?")];
    let request = ChatRequest {
        messages: &messages,
        tools: None,
    };
    let result = provider.chat(request, "test-model", 0.0).await.unwrap();

    assert_eq!(result.text.as_deref(), Some("ok"));
    assert_eq!(result.tool_calls.len(), 1);
    assert_eq!(result.tool_calls[0].name, "shell");
    assert_eq!(calls.load(Ordering::SeqCst), 1);
}

#[tokio::test]
async fn chat_retries_and_recovers() {
    let calls = Arc::new(AtomicUsize::new(0));
    let tool_call = super::super::traits::ToolCall {
        id: "call_1".to_string(),
        name: "shell".to_string(),
        arguments: r#"{"command":"date"}"#.to_string(),
    };
    let provider = ReliableProvider::new(
        vec![(
            "primary".into(),
            Box::new(NativeToolMock {
                calls: Arc::clone(&calls),
                fail_until_attempt: 2,
                response_text: "recovered",
                tool_calls: vec![tool_call],
                error: "temporary failure",
            }) as Box<dyn Provider>,
        )],
        3,
        1,
    );

    let messages = vec![ChatMessage::user("test")];
    let request = ChatRequest {
        messages: &messages,
        tools: None,
    };
    let result = provider.chat(request, "test-model", 0.0).await.unwrap();

    assert_eq!(result.text.as_deref(), Some("recovered"));
    assert!(
        calls.load(Ordering::SeqCst) > 1,
        "should have retried at least once"
    );
}

#[tokio::test]
async fn chat_preserves_native_tools_support() {
    let calls = Arc::new(AtomicUsize::new(0));
    let provider = ReliableProvider::new(
        vec![(
            "primary".into(),
            Box::new(NativeToolMock {
                calls: Arc::clone(&calls),
                fail_until_attempt: 0,
                response_text: "ok",
                tool_calls: vec![],
                error: "boom",
            }) as Box<dyn Provider>,
        )],
        2,
        1,
    );

    assert!(
        provider.supports_native_tools(),
        "ReliableProvider must propagate supports_native_tools from inner provider"
    );
}

// ── Gap 2-4: Parity tests for chat() ────────────────────────

/// Gap 2: `chat()` returns an aggregated error when all providers fail,
/// matching behavior of `returns_aggregated_error_when_all_providers_fail`.
#[tokio::test]
async fn chat_returns_aggregated_error_when_all_providers_fail() {
    let provider = ReliableProvider::new(
        vec![
            (
                "p1".into(),
                Box::new(NativeToolMock {
                    calls: Arc::new(AtomicUsize::new(0)),
                    fail_until_attempt: usize::MAX,
                    response_text: "never",
                    tool_calls: vec![],
                    error: "p1 chat error",
                }) as Box<dyn Provider>,
            ),
            (
                "p2".into(),
                Box::new(NativeToolMock {
                    calls: Arc::new(AtomicUsize::new(0)),
                    fail_until_attempt: usize::MAX,
                    response_text: "never",
                    tool_calls: vec![],
                    error: "p2 chat error",
                }) as Box<dyn Provider>,
            ),
        ],
        0,
        1,
    );

    let messages = vec![ChatMessage::user("hello")];
    let request = ChatRequest {
        messages: &messages,
        tools: None,
    };
    let err = provider
        .chat(request, "test", 0.0)
        .await
        .expect_err("all providers should fail");
    let msg = err.to_string();
    assert!(msg.contains("All providers/models failed"));
    assert!(msg.contains("provider=p1 model=test"));
    assert!(msg.contains("provider=p2 model=test"));
    assert!(msg.contains("error=p1 chat error"));
    assert!(msg.contains("error=p2 chat error"));
    assert!(msg.contains("retryable"));
}

/// Mock that records model names and can fail specific models,
/// implementing `chat()` for native tool calling parity tests.
struct NativeModelAwareMock {
    calls: Arc<AtomicUsize>,
    models_seen: parking_lot::Mutex<Vec<String>>,
    fail_models: Vec<&'static str>,
    response_text: &'static str,
}

#[async_trait]
impl Provider for NativeModelAwareMock {
    async fn chat_with_system(
        &self,
        _system_prompt: Option<&str>,
        _message: &str,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<String> {
        Ok(self.response_text.to_string())
    }

    fn supports_native_tools(&self) -> bool {
        true
    }

    async fn chat(
        &self,
        _request: ChatRequest<'_>,
        model: &str,
        _temperature: f64,
    ) -> anyhow::Result<ChatResponse> {
        self.calls.fetch_add(1, Ordering::SeqCst);
        self.models_seen.lock().push(model.to_string());
        if self.fail_models.contains(&model) {
            anyhow::bail!("500 model {} unavailable", model);
        }
        Ok(ChatResponse {
            text: Some(self.response_text.to_string()),
            tool_calls: vec![],
            usage: None,
            reasoning_content: None,
        })
    }
}

#[async_trait]
impl Provider for Arc<NativeModelAwareMock> {
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

    fn supports_native_tools(&self) -> bool {
        true
    }

    async fn chat(
        &self,
        request: ChatRequest<'_>,
        model: &str,
        temperature: f64,
    ) -> anyhow::Result<ChatResponse> {
        self.as_ref().chat(request, model, temperature).await
    }
}

/// Gap 3: `chat()` tries fallback models on failure,
/// matching behavior of `model_failover_tries_fallback_model`.
#[tokio::test]
async fn chat_tries_model_failover_on_failure() {
    let calls = Arc::new(AtomicUsize::new(0));
    let mock = Arc::new(NativeModelAwareMock {
        calls: Arc::clone(&calls),
        models_seen: parking_lot::Mutex::new(Vec::new()),
        fail_models: vec!["claude-opus"],
        response_text: "ok from sonnet",
    });

    let mut fallbacks = HashMap::new();
    fallbacks.insert("claude-opus".to_string(), vec!["claude-sonnet".to_string()]);

    let provider = ReliableProvider::new(
        vec![(
            "anthropic".into(),
            Box::new(mock.clone()) as Box<dyn Provider>,
        )],
        0, // no retries — force immediate model failover
        1,
    )
    .with_model_fallbacks(fallbacks);

    let messages = vec![ChatMessage::user("hello")];
    let request = ChatRequest {
        messages: &messages,
        tools: None,
    };
    let result = provider.chat(request, "claude-opus", 0.0).await.unwrap();
    assert_eq!(result.text.as_deref(), Some("ok from sonnet"));

    let seen = mock.models_seen.lock();
    assert_eq!(seen.len(), 2);
    assert_eq!(seen[0], "claude-opus");
    assert_eq!(seen[1], "claude-sonnet");
}

/// Gap 4: `chat()` skips retries on non-retryable errors (401, 403, etc.),
/// matching behavior of `skips_retries_on_non_retryable_error`.
#[tokio::test]
async fn chat_skips_non_retryable_errors() {
    let primary_calls = Arc::new(AtomicUsize::new(0));
    let fallback_calls = Arc::new(AtomicUsize::new(0));

    let provider = ReliableProvider::new(
        vec![
            (
                "primary".into(),
                Box::new(NativeToolMock {
                    calls: Arc::clone(&primary_calls),
                    fail_until_attempt: usize::MAX,
                    response_text: "never",
                    tool_calls: vec![],
                    error: "401 Unauthorized",
                }) as Box<dyn Provider>,
            ),
            (
                "fallback".into(),
                Box::new(NativeToolMock {
                    calls: Arc::clone(&fallback_calls),
                    fail_until_attempt: 0,
                    response_text: "from fallback",
                    tool_calls: vec![],
                    error: "fallback err",
                }) as Box<dyn Provider>,
            ),
        ],
        3,
        1,
    );

    let messages = vec![ChatMessage::user("hello")];
    let request = ChatRequest {
        messages: &messages,
        tools: None,
    };
    let result = provider.chat(request, "test", 0.0).await.unwrap();
    assert_eq!(result.text.as_deref(), Some("from fallback"));
    // Primary should have been called only once (no retries)
    assert_eq!(primary_calls.load(Ordering::SeqCst), 1);
    assert_eq!(fallback_calls.load(Ordering::SeqCst), 1);
}

// ── Context window truncation tests ─────────────────────────

#[test]
fn context_window_error_is_not_non_retryable() {
    // Context window errors should be recoverable via truncation
    assert!(!is_non_retryable(&anyhow::anyhow!(
        "exceeds the context window"
    )));
    assert!(!is_non_retryable(&anyhow::anyhow!(
        "maximum context length exceeded"
    )));
    assert!(!is_non_retryable(&anyhow::anyhow!(
        "too many tokens in the request"
    )));
    assert!(!is_non_retryable(&anyhow::anyhow!("token limit exceeded")));
}

#[test]
fn is_context_window_exceeded_detects_llamacpp() {
    assert!(is_context_window_exceeded(&anyhow::anyhow!(
        "request (8968 tokens) exceeds the available context size (8448 tokens), try increasing it"
    )));
}

#[test]
fn truncate_for_context_drops_oldest_non_system() {
    let mut messages = vec![
        ChatMessage::system("sys"),
        ChatMessage::user("msg1"),
        ChatMessage::assistant("resp1"),
        ChatMessage::user("msg2"),
        ChatMessage::assistant("resp2"),
        ChatMessage::user("msg3"),
    ];

    let dropped = truncate_for_context(&mut messages);

    // 5 non-system messages, drop oldest half = 2
    assert_eq!(dropped, 2);
    // System message preserved
    assert_eq!(messages[0].role, "system");
    // Remaining messages should be the newer ones
    assert_eq!(messages.len(), 4); // system + 3 remaining non-system
    // The last message should still be the most recent user message
    assert_eq!(messages.last().unwrap().content, "msg3");
}

#[test]
fn truncate_for_context_preserves_system_and_last_message() {
    // Only one non-system message: nothing to drop
    let mut messages = vec![ChatMessage::system("sys"), ChatMessage::user("only")];
    let dropped = truncate_for_context(&mut messages);
    assert_eq!(dropped, 0);
    assert_eq!(messages.len(), 2);

    // No system message, only one user message
    let mut messages = vec![ChatMessage::user("only")];
    let dropped = truncate_for_context(&mut messages);
    assert_eq!(dropped, 0);
    assert_eq!(messages.len(), 1);
}

/// Mock that fails with context error on first N calls, then succeeds.
/// Tracks the number of messages received on each call.
struct ContextOverflowMock {
    calls: Arc<AtomicUsize>,
    fail_until_attempt: usize,
    message_counts: parking_lot::Mutex<Vec<usize>>,
}

#[async_trait]
impl Provider for ContextOverflowMock {
    async fn chat_with_system(
        &self,
        _system_prompt: Option<&str>,
        _message: &str,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<String> {
        Ok("ok".to_string())
    }

    async fn chat_with_history(
        &self,
        messages: &[ChatMessage],
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<String> {
        let attempt = self.calls.fetch_add(1, Ordering::SeqCst) + 1;
        self.message_counts.lock().push(messages.len());
        if attempt <= self.fail_until_attempt {
            anyhow::bail!(
                "request (8968 tokens) exceeds the available context size (8448 tokens), try increasing it"
            );
        }
        Ok("recovered after truncation".to_string())
    }
}

#[tokio::test]
async fn chat_with_history_truncates_on_context_overflow() {
    let calls = Arc::new(AtomicUsize::new(0));
    let mock = ContextOverflowMock {
        calls: Arc::clone(&calls),
        fail_until_attempt: 1, // fail first call, succeed after truncation
        message_counts: parking_lot::Mutex::new(Vec::new()),
    };

    let provider = ReliableProvider::new(
        vec![("local".into(), Box::new(mock) as Box<dyn Provider>)],
        3,
        1,
    );

    let messages = vec![
        ChatMessage::system("system prompt"),
        ChatMessage::user("old message 1"),
        ChatMessage::assistant("old response 1"),
        ChatMessage::user("old message 2"),
        ChatMessage::assistant("old response 2"),
        ChatMessage::user("current question"),
    ];

    let result = provider
        .chat_with_history(&messages, "local-model", 0.0)
        .await
        .unwrap();
    assert_eq!(result, "recovered after truncation");
    // Should have been called twice: once with full messages, once with truncated
    assert_eq!(calls.load(Ordering::SeqCst), 2);
}

#[tokio::test]
async fn context_overflow_with_no_history_to_truncate_bails_immediately() {
    let calls = Arc::new(AtomicUsize::new(0));
    let mock = ContextOverflowMock {
        calls: Arc::clone(&calls),
        fail_until_attempt: 999, // always fail
        message_counts: parking_lot::Mutex::new(Vec::new()),
    };

    let provider = ReliableProvider::new(
        vec![("local".into(), Box::new(mock) as Box<dyn Provider>)],
        3,
        1,
    );

    // Only system + one user message — nothing to truncate
    let messages = vec![
        ChatMessage::system("huge system prompt that exceeds context window"),
        ChatMessage::user("hello"),
    ];

    let result = provider
        .chat_with_history(&messages, "local-model", 0.0)
        .await;
    assert!(result.is_err());
    let err_msg = result.unwrap_err().to_string();
    assert!(
        err_msg.contains("cannot be reduced further"),
        "Should bail with actionable message, got: {err_msg}"
    );
    // Should only be called once — no useless retries
    assert_eq!(
        calls.load(Ordering::SeqCst),
        1,
        "Should not retry when truncation is impossible"
    );
}

// ── Tool schema error detection tests ───────────────────────────────

#[test]
fn tool_schema_error_detects_groq_validation_failure() {
    let msg = r#"Groq API error (400 Bad Request): {"error":{"message":"tool call validation failed: attempted to call tool 'memory_recall' which was not in request"}}"#;
    let err = anyhow::anyhow!("{}", msg);
    assert!(is_tool_schema_error(&err));
}

#[test]
fn tool_schema_error_detects_not_in_request() {
    let err = anyhow::anyhow!("tool 'search' was not in request");
    assert!(is_tool_schema_error(&err));
}

#[test]
fn tool_schema_error_detects_not_found_in_tool_list() {
    let err = anyhow::anyhow!("function 'foo' not found in tool list");
    assert!(is_tool_schema_error(&err));
}

#[test]
fn tool_schema_error_detects_invalid_tool_call() {
    let err = anyhow::anyhow!("invalid_tool_call: no matching function");
    assert!(is_tool_schema_error(&err));
}

#[test]
fn tool_schema_error_ignores_unrelated_errors() {
    let err = anyhow::anyhow!("invalid api key");
    assert!(!is_tool_schema_error(&err));

    let err = anyhow::anyhow!("model not found");
    assert!(!is_tool_schema_error(&err));
}

#[test]
fn non_retryable_returns_false_for_tool_schema_400() {
    // A 400 error with tool schema validation text should NOT be non-retryable.
    let msg = "400 Bad Request: tool call validation failed: attempted to call tool 'x' which was not in request";
    let err = anyhow::anyhow!("{}", msg);
    assert!(!is_non_retryable(&err));
}

#[test]
fn non_retryable_returns_true_for_other_400_errors() {
    // A regular 400 error (e.g. invalid API key) should still be non-retryable.
    let err = anyhow::anyhow!("400 Bad Request: invalid api key provided");
    assert!(is_non_retryable(&err));
}

struct StreamingToolEventMock {
    stream_calls: Arc<AtomicUsize>,
    supports_tool_events: bool,
}

impl StreamingToolEventMock {
    fn new(supports_tool_events: bool) -> Self {
        Self {
            stream_calls: Arc::new(AtomicUsize::new(0)),
            supports_tool_events,
        }
    }
}

#[async_trait]
impl Provider for StreamingToolEventMock {
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
        self.supports_tool_events
    }

    fn stream_chat(
        &self,
        _request: ChatRequest<'_>,
        _model: &str,
        _temperature: f64,
        _options: StreamOptions,
    ) -> stream::BoxStream<'static, StreamResult<StreamEvent>> {
        self.stream_calls.fetch_add(1, Ordering::SeqCst);
        stream::iter(vec![
            Ok(StreamEvent::ToolCall(super::super::traits::ToolCall {
                id: "call_1".to_string(),
                name: "shell".to_string(),
                arguments: r#"{"command":"date"}"#.to_string(),
            })),
            Ok(StreamEvent::Final),
        ])
        .boxed()
    }
}

#[async_trait]
impl Provider for Arc<StreamingToolEventMock> {
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
    ) -> stream::BoxStream<'static, StreamResult<StreamEvent>> {
        self.as_ref()
            .stream_chat(request, model, temperature, options)
    }
}

#[tokio::test]
async fn stream_chat_prefers_provider_with_tool_event_support() {
    let primary = Arc::new(StreamingToolEventMock::new(false));
    let fallback = Arc::new(StreamingToolEventMock::new(true));
    let provider = ReliableProvider::new(
        vec![
            (
                "primary".into(),
                Box::new(Arc::clone(&primary)) as Box<dyn Provider>,
            ),
            (
                "fallback".into(),
                Box::new(Arc::clone(&fallback)) as Box<dyn Provider>,
            ),
        ],
        0,
        1,
    );

    let messages = vec![ChatMessage::user("hello")];
    let tools = vec![ToolSpec {
        name: "shell".to_string(),
        description: "run shell".to_string(),
        parameters: serde_json::json!({
            "type": "object",
            "properties": {
                "command": { "type": "string" }
            }
        }),
    }];
    let mut stream = provider.stream_chat(
        ChatRequest {
            messages: &messages,
            tools: Some(&tools),
        },
        "model",
        0.0,
        StreamOptions::new(true),
    );

    let first = stream.next().await.unwrap().unwrap();
    let second = stream.next().await.unwrap().unwrap();
    assert!(stream.next().await.is_none());

    match first {
        StreamEvent::ToolCall(call) => assert_eq!(call.name, "shell"),
        other => panic!("expected tool-call event, got {other:?}"),
    }
    assert!(matches!(second, StreamEvent::Final));
    assert_eq!(primary.stream_calls.load(Ordering::SeqCst), 0);
    assert_eq!(fallback.stream_calls.load(Ordering::SeqCst), 1);
}

#[tokio::test]
async fn stream_chat_errors_when_no_provider_supports_tool_events() {
    let primary = Arc::new(StreamingToolEventMock::new(false));
    let provider = ReliableProvider::new(
        vec![(
            "primary".into(),
            Box::new(Arc::clone(&primary)) as Box<dyn Provider>,
        )],
        0,
        1,
    );

    let messages = vec![ChatMessage::user("hello")];
    let tools = vec![ToolSpec {
        name: "shell".to_string(),
        description: "run shell".to_string(),
        parameters: serde_json::json!({"type": "object"}),
    }];
    let mut stream = provider.stream_chat(
        ChatRequest {
            messages: &messages,
            tools: Some(&tools),
        },
        "model",
        0.0,
        StreamOptions::new(true),
    );

    let first = stream.next().await.unwrap();
    let err = first.expect_err("stream should fail without tool-event support");
    assert!(
        err.to_string()
            .contains("No provider supports streaming tool events"),
        "unexpected stream error: {err}"
    );
    assert!(stream.next().await.is_none());
    assert_eq!(primary.stream_calls.load(Ordering::SeqCst), 0);
}

// ── stream_chat_with_history failover tests ──────────────────────

/// Mock provider that supports streaming via stream_chat_with_history.
struct StreamingHistoryMock {
    stream_calls: Arc<AtomicUsize>,
    supports: bool,
}

#[async_trait]
impl Provider for StreamingHistoryMock {
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
        self.supports
    }

    fn stream_chat_with_history(
        &self,
        messages: &[ChatMessage],
        _model: &str,
        _temperature: f64,
        _options: StreamOptions,
    ) -> stream::BoxStream<'static, StreamResult<StreamChunk>> {
        self.stream_calls.fetch_add(1, Ordering::SeqCst);
        // Echo the number of messages as the delta to verify history was passed through
        let msg_count = messages.len().to_string();
        stream::iter(vec![
            Ok(StreamChunk::delta(msg_count)),
            Ok(StreamChunk::final_chunk()),
        ])
        .boxed()
    }
}

#[tokio::test]
async fn stream_chat_with_history_delegates_to_streaming_provider() {
    let calls = Arc::new(AtomicUsize::new(0));
    let provider = ReliableProvider::new(
        vec![(
            "primary".into(),
            Box::new(StreamingHistoryMock {
                stream_calls: Arc::clone(&calls),
                supports: true,
            }) as Box<dyn Provider>,
        )],
        0,
        1,
    );

    let messages = vec![
        ChatMessage::system("system"),
        ChatMessage::user("msg1"),
        ChatMessage::assistant("resp1"),
        ChatMessage::user("msg2"),
    ];
    let mut stream =
        provider.stream_chat_with_history(&messages, "model", 0.0, StreamOptions::new(true));

    let first = stream.next().await.unwrap().unwrap();
    assert_eq!(first.delta, "4", "should pass all 4 messages to provider");
    let second = stream.next().await.unwrap().unwrap();
    assert!(second.is_final);
    assert!(stream.next().await.is_none());
    assert_eq!(calls.load(Ordering::SeqCst), 1);
}

#[tokio::test]
async fn stream_chat_with_history_skips_non_streaming_providers() {
    let non_streaming_calls = Arc::new(AtomicUsize::new(0));
    let streaming_calls = Arc::new(AtomicUsize::new(0));

    let provider = ReliableProvider::new(
        vec![
            (
                "non-streaming".into(),
                Box::new(StreamingHistoryMock {
                    stream_calls: Arc::clone(&non_streaming_calls),
                    supports: false,
                }) as Box<dyn Provider>,
            ),
            (
                "streaming".into(),
                Box::new(StreamingHistoryMock {
                    stream_calls: Arc::clone(&streaming_calls),
                    supports: true,
                }) as Box<dyn Provider>,
            ),
        ],
        0,
        1,
    );

    let messages = vec![ChatMessage::user("hello")];
    let mut stream =
        provider.stream_chat_with_history(&messages, "model", 0.0, StreamOptions::new(true));

    let first = stream.next().await.unwrap().unwrap();
    assert_eq!(first.delta, "1");
    assert_eq!(
        non_streaming_calls.load(Ordering::SeqCst),
        0,
        "non-streaming provider should be skipped"
    );
    assert_eq!(
        streaming_calls.load(Ordering::SeqCst),
        1,
        "streaming provider should be used"
    );
}

#[tokio::test]
async fn stream_chat_with_history_errors_when_no_provider_supports_streaming() {
    let provider = ReliableProvider::new(
        vec![(
            "non-streaming".into(),
            Box::new(StreamingHistoryMock {
                stream_calls: Arc::new(AtomicUsize::new(0)),
                supports: false,
            }) as Box<dyn Provider>,
        )],
        0,
        1,
    );

    let messages = vec![ChatMessage::user("hello")];
    let mut stream =
        provider.stream_chat_with_history(&messages, "model", 0.0, StreamOptions::new(true));

    let first = stream.next().await.unwrap();
    let err = first.expect_err("should fail when no provider supports streaming");
    assert!(
        err.to_string().contains("No provider supports streaming"),
        "unexpected error: {err}"
    );
    assert!(stream.next().await.is_none());
}

#[tokio::test]
async fn fallback_records_provider_fallback_info() {
    scope_provider_fallback(async {
        let provider = ReliableProvider::new(
            vec![
                (
                    "broken".into(),
                    Box::new(MockProvider {
                        calls: Arc::new(AtomicUsize::new(0)),
                        fail_until_attempt: 99, // always fail
                        response: "unused",
                        error: "401 Unauthorized",
                    }),
                ),
                (
                    "working".into(),
                    Box::new(MockProvider {
                        calls: Arc::new(AtomicUsize::new(0)),
                        fail_until_attempt: 0,
                        response: "hello from working",
                        error: "unused",
                    }),
                ),
            ],
            2,
            1,
        );

        let resp = provider.simple_chat("hi", "test-model", 0.0).await.unwrap();
        assert_eq!(resp, "hello from working");

        let fb = take_last_provider_fallback();
        assert!(fb.is_some(), "fallback info should be recorded");
        let fb = fb.unwrap();
        assert_eq!(fb.requested_provider, "broken");
        assert_eq!(fb.actual_provider, "working");
        assert_eq!(fb.actual_model, "test-model");

        // Second take should be None.
        assert!(take_last_provider_fallback().is_none());
    })
    .await;
}
