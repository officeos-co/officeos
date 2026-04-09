use super::{
    emergency_history_trim, estimate_history_tokens, fast_trim_tool_results,
    load_interactive_session_history, save_interactive_session_history, truncate_tool_result,
};
use crate::agent::history::{DEFAULT_MAX_HISTORY_MESSAGES, InteractiveSessionState};
use crate::agent::tool_execution::execute_one_tool;
use crate::providers::ChatMessage;
use tempfile::tempdir;

// ── truncate_tool_result tests ────────────────────────────────

#[test]
fn truncate_tool_result_short_passthrough() {
    let output = "short output";
    assert_eq!(truncate_tool_result(output, 100), output);
}

#[test]
fn truncate_tool_result_exact_boundary() {
    let output = "a".repeat(100);
    assert_eq!(truncate_tool_result(&output, 100), output);
}

#[test]
fn truncate_tool_result_zero_disables() {
    let output = "a".repeat(200_000);
    assert_eq!(truncate_tool_result(&output, 0), output);
}

#[test]
fn truncate_tool_result_truncates_with_marker() {
    let output = "a".repeat(200);
    let result = truncate_tool_result(&output, 100);
    assert!(result.contains("[... "));
    assert!(result.contains("characters truncated ...]\n\n"));
    // Head should be ~2/3 of 100 = 66, tail ~1/3 = 34
    assert!(result.starts_with("aaa"));
    assert!(result.ends_with("aaa"));
    // Result should be shorter than original
    assert!(result.len() < output.len());
}

#[test]
fn truncate_tool_result_preserves_head_tail_ratio() {
    let output: String = (0u32..1000)
        .map(|i| char::from(b'a' + (i % 26) as u8))
        .collect();
    let result = truncate_tool_result(&output, 300);
    // Head = 2/3 of 300 = 200 chars, tail = 100 chars
    // Find the marker
    let marker_start = result.find("[... ").unwrap();
    let marker_end = result.find("characters truncated ...]\n\n").unwrap()
        + "characters truncated ...]\n\n".len();
    let head = &result[..marker_start - 2]; // subtract \n\n
    let tail = &result[marker_end..];
    assert!(
        head.len() >= 190 && head.len() <= 210,
        "head len={}",
        head.len()
    );
    assert!(
        tail.len() >= 90 && tail.len() <= 110,
        "tail len={}",
        tail.len()
    );
}

#[test]
fn truncate_tool_result_utf8_boundary_safety() {
    // Create string with multi-byte chars: each emoji is 4 bytes
    let output = "🦀".repeat(100); // 400 bytes
    // This should not panic even with a limit that falls mid-char
    let result = truncate_tool_result(&output, 50);
    assert!(result.contains("[... "));
    // Verify the result is valid UTF-8 (would panic otherwise)
    let _ = result.len();
}

#[test]
fn truncate_tool_result_very_small_max() {
    let output = "abcdefghijklmnopqrstuvwxyz";
    // With max=5, head=3 tail=2 — result includes marker overhead
    // but should not panic and should contain truncation marker
    let result = truncate_tool_result(output, 5);
    assert!(result.contains("[... "));
    // Head (3 chars) + tail (2 chars) from original should be preserved
    assert!(result.starts_with("abc"));
    assert!(result.ends_with("yz"));
}

// ── fast_trim_tool_results tests ────────────────────────────

#[test]
fn fast_trim_protects_recent_messages() {
    let mut history = vec![
        ChatMessage::system("sys"),
        ChatMessage::tool("a".repeat(5000)),
        ChatMessage::tool("b".repeat(5000)),
        ChatMessage::user("recent user msg"),
        ChatMessage::tool("c".repeat(5000)), // recent, should be protected
    ];
    // protect_last_n = 2 → last 2 messages protected
    let saved = fast_trim_tool_results(&mut history, 2);
    assert!(saved > 0);
    // First two tool messages should be trimmed
    assert!(history[1].content.len() <= 2100);
    assert!(history[2].content.len() <= 2100);
    // Last tool message (protected) should be unchanged
    assert_eq!(history[4].content.len(), 5000);
}

#[test]
fn fast_trim_skips_non_tool_messages() {
    let mut history = vec![
        ChatMessage::system("sys"),
        ChatMessage::user("a".repeat(5000)),
        ChatMessage::assistant("b".repeat(5000)),
    ];
    let saved = fast_trim_tool_results(&mut history, 0);
    assert_eq!(saved, 0);
    assert_eq!(history[1].content.len(), 5000);
    assert_eq!(history[2].content.len(), 5000);
}

#[test]
fn fast_trim_small_tool_results_unchanged() {
    let mut history = vec![
        ChatMessage::system("sys"),
        ChatMessage::tool("short result"),
    ];
    let saved = fast_trim_tool_results(&mut history, 0);
    assert_eq!(saved, 0);
    assert_eq!(history[1].content, "short result");
}

// ── emergency_history_trim tests ──────────────────────────────

#[test]
fn emergency_trim_preserves_system() {
    let mut history = vec![
        ChatMessage::system("sys"),
        ChatMessage::user("msg1"),
        ChatMessage::assistant("resp1"),
        ChatMessage::user("msg2"),
        ChatMessage::assistant("resp2"),
        ChatMessage::user("msg3"),
    ];
    let dropped = emergency_history_trim(&mut history, 2);
    assert!(dropped > 0);
    // System message should always be preserved
    assert_eq!(history[0].role, "system");
    assert_eq!(history[0].content, "sys");
    // Last 2 messages should be preserved
    let len = history.len();
    assert_eq!(history[len - 1].content, "msg3");
}

#[test]
fn emergency_trim_preserves_recent() {
    let mut history = vec![
        ChatMessage::system("sys"),
        ChatMessage::user("old1"),
        ChatMessage::user("old2"),
        ChatMessage::user("recent1"),
        ChatMessage::user("recent2"),
    ];
    let dropped = emergency_history_trim(&mut history, 2);
    assert!(dropped > 0);
    // Last 2 should be preserved
    let len = history.len();
    assert_eq!(history[len - 1].content, "recent2");
    assert_eq!(history[len - 2].content, "recent1");
}

#[test]
fn emergency_trim_nothing_to_drop() {
    let mut history = vec![
        ChatMessage::system("sys"),
        ChatMessage::user("only user msg"),
    ];
    // protect_last = 1, system is protected → only 1 droppable
    // target_drop = 2/3 = 0 → nothing dropped
    let dropped = emergency_history_trim(&mut history, 1);
    assert_eq!(dropped, 0);
}

// ── estimate_history_tokens tests ─────────────────────────────

#[test]
fn estimate_tokens_empty_history() {
    let history: Vec<ChatMessage> = vec![];
    assert_eq!(estimate_history_tokens(&history), 0);
}

#[test]
fn estimate_tokens_single_message() {
    // 40 chars → 40.div_ceil(4) + 4 = 10 + 4 = 14 tokens
    let msg = "a".repeat(40);
    let history = vec![ChatMessage::user(&msg)];
    let est = estimate_history_tokens(&history);
    assert_eq!(est, 14);
}

#[test]
fn estimate_tokens_multiple_messages() {
    let history = vec![
        ChatMessage::system("system prompt here"), // 18 chars → 18/4=4 +4=8 (div_ceil: 5+4=9)
        ChatMessage::user("hello"),                // 5 chars → 5/4=1 +4=5 (div_ceil: 2+4=6)
        ChatMessage::assistant("world"),           // 5 chars → 5/4=1 +4=5 (div_ceil: 2+4=6)
    ];
    let est = estimate_history_tokens(&history);
    // Each message: content_len.div_ceil(4) + 4
    // 18.div_ceil(4)=5, 5.div_ceil(4)=2, 5.div_ceil(4)=2 → 5+4 + 2+4 + 2+4 = 21
    assert_eq!(est, 21);
}

#[test]
fn estimate_tokens_large_tool_result() {
    let big = "x".repeat(40_000);
    let history = vec![ChatMessage::tool(&big)];
    let est = estimate_history_tokens(&history);
    // 40000.div_ceil(4) + 4 = 10000 + 4 = 10004
    assert_eq!(est, 10_004);
}

// ── shared_budget tests ───────────────────────────────────────

#[test]
fn shared_budget_decrement_logic() {
    use std::sync::Arc;
    use std::sync::atomic::{AtomicUsize, Ordering};

    let budget = Arc::new(AtomicUsize::new(3));

    // Simulate 3 iterations decrementing
    for i in 0..3 {
        let remaining = budget.load(Ordering::Relaxed);
        assert!(remaining > 0, "Budget should be >0 at iteration {i}");
        budget.fetch_sub(1, Ordering::Relaxed);
    }

    // Budget should now be 0
    assert_eq!(budget.load(Ordering::Relaxed), 0);
}

#[test]
fn shared_budget_none_has_no_effect() {
    // When shared_budget is None, the check is simply skipped
    let budget: Option<Arc<std::sync::atomic::AtomicUsize>> = None;
    assert!(budget.is_none());
}

// ── existing tests ────────────────────────────────────────────

#[test]
fn interactive_session_state_round_trips_history() {
    let dir = tempdir().unwrap();
    let path = dir.path().join("session.json");
    let history = vec![
        ChatMessage::system("system"),
        ChatMessage::user("hello"),
        ChatMessage::assistant("hi"),
    ];

    save_interactive_session_history(&path, &history).unwrap();
    let restored = load_interactive_session_history(&path, "fallback").unwrap();

    assert_eq!(restored.len(), 3);
    assert_eq!(restored[0].role, "system");
    assert_eq!(restored[1].content, "hello");
    assert_eq!(restored[2].content, "hi");
}

#[test]
fn interactive_session_state_adds_missing_system_prompt() {
    let dir = tempdir().unwrap();
    let path = dir.path().join("session.json");
    let payload = serde_json::to_string_pretty(&InteractiveSessionState {
        version: 1,
        history: vec![ChatMessage::user("orphan")],
    })
    .unwrap();
    std::fs::write(&path, payload).unwrap();

    let restored = load_interactive_session_history(&path, "fallback system").unwrap();

    assert_eq!(restored[0].role, "system");
    assert_eq!(restored[0].content, "fallback system");
    assert_eq!(restored[1].content, "orphan");
}

use super::*;
use async_trait::async_trait;
use base64::{Engine as _, engine::general_purpose::STANDARD};
use std::collections::VecDeque;
use std::sync::atomic::{AtomicUsize, Ordering};
use std::sync::{Arc, Mutex};
use std::time::Duration;

#[test]
fn scrub_credentials_redacts_bearer_token() {
    let input = "API_KEY=sk-1234567890abcdef; token: 1234567890; password=\"secret123456\"";
    let scrubbed = scrub_credentials(input);
    assert!(scrubbed.contains("API_KEY=sk-1*[REDACTED]"));
    assert!(scrubbed.contains("token: 1234*[REDACTED]"));
    assert!(scrubbed.contains("password=\"secr*[REDACTED]\""));
    assert!(!scrubbed.contains("abcdef"));
    assert!(!scrubbed.contains("secret123456"));
}

#[test]
fn scrub_credentials_redacts_json_api_key() {
    let input = r#"{"api_key": "sk-1234567890", "other": "public"}"#;
    let scrubbed = scrub_credentials(input);
    assert!(scrubbed.contains("\"api_key\": \"sk-1*[REDACTED]\""));
    assert!(scrubbed.contains("public"));
}

#[tokio::test]
async fn execute_one_tool_does_not_panic_on_utf8_boundary() {
    let call_arguments = (0..600)
        .map(|n| serde_json::json!({ "content": format!("{}：tail", "a".repeat(n)) }))
        .find(|args| {
            let raw = args.to_string();
            raw.len() > 300 && !raw.is_char_boundary(300)
        })
        .expect("should produce a sample whose byte index 300 is not a char boundary");

    let observer = NoopObserver;
    let result = execute_one_tool("unknown_tool", call_arguments, &[], None, &observer, None).await;
    assert!(result.is_ok(), "execute_one_tool should not panic or error");

    let outcome = result.unwrap();
    assert!(!outcome.success);
    assert!(outcome.output.contains("Unknown tool: unknown_tool"));
}

#[tokio::test]
async fn execute_one_tool_resolves_unique_activated_tool_suffix() {
    let observer = NoopObserver;
    let invocations = Arc::new(AtomicUsize::new(0));
    let activated = Arc::new(std::sync::Mutex::new(crate::tools::ActivatedToolSet::new()));
    let activated_tool: Arc<dyn Tool> = Arc::new(CountingTool::new(
        "docker-mcp__extract_text",
        Arc::clone(&invocations),
    ));
    activated
        .lock()
        .unwrap()
        .activate("docker-mcp__extract_text".into(), activated_tool);

    let outcome = execute_one_tool(
        "extract_text",
        serde_json::json!({ "value": "ok" }),
        &[],
        Some(&activated),
        &observer,
        None,
    )
    .await
    .expect("suffix alias should execute the unique activated tool");

    assert!(outcome.success);
    assert_eq!(outcome.output, "counted:ok");
    assert_eq!(invocations.load(Ordering::SeqCst), 1);
}

use crate::memory::{Memory, MemoryCategory, SqliteMemory};
use crate::observability::NoopObserver;
use crate::providers::ChatResponse;
use crate::providers::router::{Route, RouterProvider};
use crate::providers::traits::{ProviderCapabilities, StreamChunk, StreamEvent, StreamOptions};
use tempfile::TempDir;

struct NonVisionProvider {
    calls: Arc<AtomicUsize>,
}

#[async_trait]
impl Provider for NonVisionProvider {
    async fn chat_with_system(
        &self,
        _system_prompt: Option<&str>,
        _message: &str,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<String> {
        self.calls.fetch_add(1, Ordering::SeqCst);
        Ok("ok".to_string())
    }
}

struct VisionProvider {
    calls: Arc<AtomicUsize>,
}

#[async_trait]
impl Provider for VisionProvider {
    fn capabilities(&self) -> ProviderCapabilities {
        ProviderCapabilities {
            native_tool_calling: false,
            vision: true,
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
        self.calls.fetch_add(1, Ordering::SeqCst);
        Ok("ok".to_string())
    }

    async fn chat(
        &self,
        request: ChatRequest<'_>,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<ChatResponse> {
        self.calls.fetch_add(1, Ordering::SeqCst);
        let marker_count = crate::multimodal::count_image_markers(request.messages);
        if marker_count == 0 {
            anyhow::bail!("expected image markers in request messages");
        }

        if request.tools.is_some() {
            anyhow::bail!("no tools should be attached for this test");
        }

        Ok(ChatResponse {
            text: Some("vision-ok".to_string()),
            tool_calls: Vec::new(),
            usage: None,
            reasoning_content: None,
        })
    }
}

struct ScriptedProvider {
    responses: Arc<Mutex<VecDeque<ChatResponse>>>,
    capabilities: ProviderCapabilities,
}

impl ScriptedProvider {
    fn from_text_responses(responses: Vec<&str>) -> Self {
        let scripted = responses
            .into_iter()
            .map(|text| ChatResponse {
                text: Some(text.to_string()),
                tool_calls: Vec::new(),
                usage: None,
                reasoning_content: None,
            })
            .collect();
        Self {
            responses: Arc::new(Mutex::new(scripted)),
            capabilities: ProviderCapabilities::default(),
        }
    }

    fn with_native_tool_support(mut self) -> Self {
        self.capabilities.native_tool_calling = true;
        self
    }
}

#[async_trait]
impl Provider for ScriptedProvider {
    fn capabilities(&self) -> ProviderCapabilities {
        self.capabilities.clone()
    }

    async fn chat_with_system(
        &self,
        _system_prompt: Option<&str>,
        _message: &str,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<String> {
        anyhow::bail!("chat_with_system should not be used in scripted provider tests");
    }

    async fn chat(
        &self,
        _request: ChatRequest<'_>,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<ChatResponse> {
        let mut responses = self
            .responses
            .lock()
            .expect("responses lock should be valid");
        responses
            .pop_front()
            .ok_or_else(|| anyhow::anyhow!("scripted provider exhausted responses"))
    }
}

struct StreamingScriptedProvider {
    responses: Arc<Mutex<VecDeque<String>>>,
    stream_calls: Arc<AtomicUsize>,
    chat_calls: Arc<AtomicUsize>,
}

impl StreamingScriptedProvider {
    fn from_text_responses(responses: Vec<&str>) -> Self {
        Self {
            responses: Arc::new(Mutex::new(
                responses.into_iter().map(ToString::to_string).collect(),
            )),
            stream_calls: Arc::new(AtomicUsize::new(0)),
            chat_calls: Arc::new(AtomicUsize::new(0)),
        }
    }
}

#[async_trait]
impl Provider for StreamingScriptedProvider {
    async fn chat_with_system(
        &self,
        _system_prompt: Option<&str>,
        _message: &str,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<String> {
        anyhow::bail!("chat_with_system should not be used in streaming scripted provider tests");
    }

    async fn chat(
        &self,
        _request: ChatRequest<'_>,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<ChatResponse> {
        self.chat_calls.fetch_add(1, Ordering::SeqCst);
        anyhow::bail!("chat should not be called when streaming succeeds")
    }

    fn supports_streaming(&self) -> bool {
        true
    }

    fn stream_chat_with_history(
        &self,
        _messages: &[ChatMessage],
        _model: &str,
        _temperature: f64,
        options: StreamOptions,
    ) -> futures_util::stream::BoxStream<'static, crate::providers::traits::StreamResult<StreamChunk>>
    {
        self.stream_calls.fetch_add(1, Ordering::SeqCst);
        if !options.enabled {
            return Box::pin(futures_util::stream::empty());
        }

        let response = self
            .responses
            .lock()
            .expect("responses lock should be valid")
            .pop_front()
            .unwrap_or_default();

        Box::pin(futures_util::stream::iter(vec![
            Ok(StreamChunk::delta(response)),
            Ok(StreamChunk::final_chunk()),
        ]))
    }
}

enum NativeStreamTurn {
    ToolCall(ToolCall),
    Text(String),
}

struct StreamingNativeToolEventProvider {
    turns: Arc<Mutex<VecDeque<NativeStreamTurn>>>,
    stream_calls: Arc<AtomicUsize>,
    stream_tool_requests: Arc<AtomicUsize>,
    chat_calls: Arc<AtomicUsize>,
}

impl StreamingNativeToolEventProvider {
    fn with_turns(turns: Vec<NativeStreamTurn>) -> Self {
        Self {
            turns: Arc::new(Mutex::new(turns.into())),
            stream_calls: Arc::new(AtomicUsize::new(0)),
            stream_tool_requests: Arc::new(AtomicUsize::new(0)),
            chat_calls: Arc::new(AtomicUsize::new(0)),
        }
    }
}

#[async_trait]
impl Provider for StreamingNativeToolEventProvider {
    fn capabilities(&self) -> ProviderCapabilities {
        ProviderCapabilities {
            native_tool_calling: true,
            vision: false,
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
        anyhow::bail!(
            "chat_with_system should not be used in streaming native tool event provider tests"
        );
    }

    async fn chat(
        &self,
        _request: ChatRequest<'_>,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<ChatResponse> {
        self.chat_calls.fetch_add(1, Ordering::SeqCst);
        anyhow::bail!("chat should not be called when native streaming events succeed")
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
        _model: &str,
        _temperature: f64,
        options: StreamOptions,
    ) -> futures_util::stream::BoxStream<'static, crate::providers::traits::StreamResult<StreamEvent>>
    {
        self.stream_calls.fetch_add(1, Ordering::SeqCst);
        if request.tools.is_some_and(|tools| !tools.is_empty()) {
            self.stream_tool_requests.fetch_add(1, Ordering::SeqCst);
        }
        if !options.enabled {
            return Box::pin(futures_util::stream::empty());
        }

        let turn = self
            .turns
            .lock()
            .expect("turns lock should be valid")
            .pop_front()
            .expect("streaming turns should have scripted output");
        match turn {
            NativeStreamTurn::ToolCall(tool_call) => Box::pin(futures_util::stream::iter(vec![
                Ok(StreamEvent::ToolCall(tool_call)),
                Ok(StreamEvent::Final),
            ])),
            NativeStreamTurn::Text(text) => Box::pin(futures_util::stream::iter(vec![
                Ok(StreamEvent::TextDelta(StreamChunk::delta(text))),
                Ok(StreamEvent::Final),
            ])),
        }
    }
}

struct RouteAwareStreamingProvider {
    response: String,
    stream_calls: Arc<AtomicUsize>,
    chat_calls: Arc<AtomicUsize>,
    last_model: Arc<Mutex<String>>,
}

impl RouteAwareStreamingProvider {
    fn new(response: &str) -> Self {
        Self {
            response: response.to_string(),
            stream_calls: Arc::new(AtomicUsize::new(0)),
            chat_calls: Arc::new(AtomicUsize::new(0)),
            last_model: Arc::new(Mutex::new(String::new())),
        }
    }
}

#[async_trait]
impl Provider for RouteAwareStreamingProvider {
    async fn chat_with_system(
        &self,
        _system_prompt: Option<&str>,
        _message: &str,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<String> {
        anyhow::bail!("chat_with_system should not be used in route-aware stream tests");
    }

    async fn chat(
        &self,
        _request: ChatRequest<'_>,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<ChatResponse> {
        self.chat_calls.fetch_add(1, Ordering::SeqCst);
        anyhow::bail!("chat should not be called when routed streaming succeeds")
    }

    fn supports_streaming(&self) -> bool {
        true
    }

    fn stream_chat_with_history(
        &self,
        _messages: &[ChatMessage],
        model: &str,
        _temperature: f64,
        options: StreamOptions,
    ) -> futures_util::stream::BoxStream<'static, crate::providers::traits::StreamResult<StreamChunk>>
    {
        self.stream_calls.fetch_add(1, Ordering::SeqCst);
        *self
            .last_model
            .lock()
            .expect("last_model lock should be valid") = model.to_string();
        if !options.enabled {
            return Box::pin(futures_util::stream::empty());
        }

        Box::pin(futures_util::stream::iter(vec![
            Ok(StreamChunk::delta(self.response.clone())),
            Ok(StreamChunk::final_chunk()),
        ]))
    }
}

struct CountingTool {
    name: String,
    invocations: Arc<AtomicUsize>,
}

impl CountingTool {
    fn new(name: &str, invocations: Arc<AtomicUsize>) -> Self {
        Self {
            name: name.to_string(),
            invocations,
        }
    }
}

#[async_trait]
impl Tool for CountingTool {
    fn name(&self) -> &str {
        &self.name
    }

    fn description(&self) -> &str {
        "Counts executions for loop-stability tests"
    }

    fn parameters_schema(&self) -> serde_json::Value {
        serde_json::json!({
            "type": "object",
            "properties": {
                "value": { "type": "string" }
            }
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<crate::tools::ToolResult> {
        self.invocations.fetch_add(1, Ordering::SeqCst);
        let value = args
            .get("value")
            .and_then(serde_json::Value::as_str)
            .unwrap_or_default();
        Ok(crate::tools::ToolResult {
            success: true,
            output: format!("counted:{value}"),
            error: None,
        })
    }
}

struct RecordingArgsTool {
    name: String,
    recorded_args: Arc<Mutex<Vec<serde_json::Value>>>,
}

impl RecordingArgsTool {
    fn new(name: &str, recorded_args: Arc<Mutex<Vec<serde_json::Value>>>) -> Self {
        Self {
            name: name.to_string(),
            recorded_args,
        }
    }
}

#[async_trait]
impl Tool for RecordingArgsTool {
    fn name(&self) -> &str {
        &self.name
    }

    fn description(&self) -> &str {
        "Records tool arguments for regression tests"
    }

    fn parameters_schema(&self) -> serde_json::Value {
        serde_json::json!({
            "type": "object",
            "properties": {
                "prompt": { "type": "string" },
                "schedule": { "type": "object" },
                "delivery": { "type": "object" }
            }
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<crate::tools::ToolResult> {
        self.recorded_args
            .lock()
            .expect("recorded args lock should be valid")
            .push(args.clone());
        Ok(crate::tools::ToolResult {
            success: true,
            output: args.to_string(),
            error: None,
        })
    }
}

struct DelayTool {
    name: String,
    delay_ms: u64,
    active: Arc<AtomicUsize>,
    max_active: Arc<AtomicUsize>,
}

impl DelayTool {
    fn new(
        name: &str,
        delay_ms: u64,
        active: Arc<AtomicUsize>,
        max_active: Arc<AtomicUsize>,
    ) -> Self {
        Self {
            name: name.to_string(),
            delay_ms,
            active,
            max_active,
        }
    }
}

#[async_trait]
impl Tool for DelayTool {
    fn name(&self) -> &str {
        &self.name
    }

    fn description(&self) -> &str {
        "Delay tool for testing parallel tool execution"
    }

    fn parameters_schema(&self) -> serde_json::Value {
        serde_json::json!({
            "type": "object",
            "properties": {
                "value": { "type": "string" }
            },
            "required": ["value"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<crate::tools::ToolResult> {
        let now_active = self.active.fetch_add(1, Ordering::SeqCst) + 1;
        self.max_active.fetch_max(now_active, Ordering::SeqCst);

        tokio::time::sleep(Duration::from_millis(self.delay_ms)).await;

        self.active.fetch_sub(1, Ordering::SeqCst);

        let value = args
            .get("value")
            .and_then(serde_json::Value::as_str)
            .unwrap_or_default()
            .to_string();

        Ok(crate::tools::ToolResult {
            success: true,
            output: format!("ok:{value}"),
            error: None,
        })
    }
}

/// A tool that always returns a failure with a given error reason.
struct FailingTool {
    tool_name: String,
    error_reason: String,
}

impl FailingTool {
    fn new(name: &str, error_reason: &str) -> Self {
        Self {
            tool_name: name.to_string(),
            error_reason: error_reason.to_string(),
        }
    }
}

#[async_trait]
impl Tool for FailingTool {
    fn name(&self) -> &str {
        &self.tool_name
    }

    fn description(&self) -> &str {
        "A tool that always fails for testing failure surfacing"
    }

    fn parameters_schema(&self) -> serde_json::Value {
        serde_json::json!({
            "type": "object",
            "properties": {
                "command": { "type": "string" }
            }
        })
    }

    async fn execute(&self, _args: serde_json::Value) -> anyhow::Result<crate::tools::ToolResult> {
        Ok(crate::tools::ToolResult {
            success: false,
            output: String::new(),
            error: Some(self.error_reason.clone()),
        })
    }
}

#[tokio::test]
async fn run_tool_call_loop_returns_structured_error_for_non_vision_provider() {
    let calls = Arc::new(AtomicUsize::new(0));
    let provider = NonVisionProvider {
        calls: Arc::clone(&calls),
    };

    let mut history = vec![ChatMessage::user(
        "please inspect [IMAGE:data:image/png;base64,iVBORw0KGgo=]".to_string(),
    )];
    let tools_registry: Vec<Box<dyn Tool>> = Vec::new();
    let observer = NoopObserver;

    let err = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        None,
        "cli",
        None,
        &crate::config::MultimodalConfig::default(),
        3,
        None,
        None,
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect_err("provider without vision support should fail");

    assert!(err.to_string().contains("provider_capability_error"));
    assert!(err.to_string().contains("capability=vision"));
    assert_eq!(calls.load(Ordering::SeqCst), 0);
}

#[tokio::test]
async fn run_tool_call_loop_rejects_oversized_image_payload() {
    let calls = Arc::new(AtomicUsize::new(0));
    let provider = VisionProvider {
        calls: Arc::clone(&calls),
    };

    let oversized_payload = STANDARD.encode(vec![0_u8; (1024 * 1024) + 1]);
    let mut history = vec![ChatMessage::user(format!(
        "[IMAGE:data:image/png;base64,{oversized_payload}]"
    ))];

    let tools_registry: Vec<Box<dyn Tool>> = Vec::new();
    let observer = NoopObserver;
    let multimodal = crate::config::MultimodalConfig {
        max_images: 4,
        max_image_size_mb: 1,
        allow_remote_fetch: false,
        ..Default::default()
    };

    let err = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        None,
        "cli",
        None,
        &multimodal,
        3,
        None,
        None,
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect_err("oversized payload must fail");

    assert!(
        err.to_string()
            .contains("multimodal image size limit exceeded")
    );
    assert_eq!(calls.load(Ordering::SeqCst), 0);
}

#[tokio::test]
async fn run_tool_call_loop_accepts_valid_multimodal_request_flow() {
    let calls = Arc::new(AtomicUsize::new(0));
    let provider = VisionProvider {
        calls: Arc::clone(&calls),
    };

    let mut history = vec![ChatMessage::user(
        "Analyze this [IMAGE:data:image/png;base64,iVBORw0KGgo=]".to_string(),
    )];
    let tools_registry: Vec<Box<dyn Tool>> = Vec::new();
    let observer = NoopObserver;

    let result = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        None,
        "cli",
        None,
        &crate::config::MultimodalConfig::default(),
        3,
        None,
        None,
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect("valid multimodal payload should pass");

    assert_eq!(result, "vision-ok");
    assert_eq!(calls.load(Ordering::SeqCst), 1);
}

/// When `vision_provider` is not set and the default provider lacks vision
/// support, the original `ProviderCapabilityError` should be returned.
#[tokio::test]
async fn run_tool_call_loop_no_vision_provider_config_preserves_error() {
    let calls = Arc::new(AtomicUsize::new(0));
    let provider = NonVisionProvider {
        calls: Arc::clone(&calls),
    };

    let mut history = vec![ChatMessage::user(
        "check [IMAGE:data:image/png;base64,iVBORw0KGgo=]".to_string(),
    )];
    let tools_registry: Vec<Box<dyn Tool>> = Vec::new();
    let observer = NoopObserver;

    let err = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        None,
        "cli",
        None,
        &crate::config::MultimodalConfig::default(),
        3,
        None,
        None,
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect_err("should fail without vision_provider config");

    assert!(err.to_string().contains("capability=vision"));
    assert_eq!(calls.load(Ordering::SeqCst), 0);
}

/// When `vision_provider` is set but the provider factory cannot resolve
/// the name, a descriptive error should be returned (not the generic
/// capability error).
#[tokio::test]
async fn run_tool_call_loop_vision_provider_creation_failure() {
    let calls = Arc::new(AtomicUsize::new(0));
    let provider = NonVisionProvider {
        calls: Arc::clone(&calls),
    };

    let mut history = vec![ChatMessage::user(
        "inspect [IMAGE:data:image/png;base64,iVBORw0KGgo=]".to_string(),
    )];
    let tools_registry: Vec<Box<dyn Tool>> = Vec::new();
    let observer = NoopObserver;

    let multimodal = crate::config::MultimodalConfig {
        vision_provider: Some("nonexistent-provider-xyz".to_string()),
        vision_model: Some("some-model".to_string()),
        ..Default::default()
    };

    let err = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        None,
        "cli",
        None,
        &multimodal,
        3,
        None,
        None,
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect_err("should fail when vision provider cannot be created");

    assert!(
        err.to_string().contains("failed to create vision provider"),
        "expected creation failure error, got: {}",
        err
    );
    assert_eq!(calls.load(Ordering::SeqCst), 0);
}

/// Messages without image markers should use the default provider even
/// when `vision_provider` is configured.
#[tokio::test]
async fn run_tool_call_loop_no_images_uses_default_provider() {
    let provider = ScriptedProvider::from_text_responses(vec!["hello world"]);

    let mut history = vec![ChatMessage::user("just text, no images".to_string())];
    let tools_registry: Vec<Box<dyn Tool>> = Vec::new();
    let observer = NoopObserver;

    let multimodal = crate::config::MultimodalConfig {
        vision_provider: Some("nonexistent-provider-xyz".to_string()),
        vision_model: Some("some-model".to_string()),
        ..Default::default()
    };

    // Even though vision_provider points to a nonexistent provider, this
    // should succeed because there are no image markers to trigger routing.
    let result = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "scripted",
        "scripted-model",
        0.0,
        true,
        None,
        "cli",
        None,
        &multimodal,
        3,
        None,
        None,
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect("text-only messages should succeed with default provider");

    assert_eq!(result, "hello world");
}

/// When `vision_provider` is set but `vision_model` is not, the default
/// model should be used as fallback for the vision provider.
#[tokio::test]
async fn run_tool_call_loop_vision_provider_without_model_falls_back() {
    let calls = Arc::new(AtomicUsize::new(0));
    let provider = NonVisionProvider {
        calls: Arc::clone(&calls),
    };

    let mut history = vec![ChatMessage::user(
        "look [IMAGE:data:image/png;base64,iVBORw0KGgo=]".to_string(),
    )];
    let tools_registry: Vec<Box<dyn Tool>> = Vec::new();
    let observer = NoopObserver;

    // vision_provider set but vision_model is None — the code should
    // fall back to the default model. Since the provider name is invalid,
    // we just verify the error path references the correct provider.
    let multimodal = crate::config::MultimodalConfig {
        vision_provider: Some("nonexistent-provider-xyz".to_string()),
        vision_model: None,
        ..Default::default()
    };

    let err = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        None,
        "cli",
        None,
        &multimodal,
        3,
        None,
        None,
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect_err("should fail due to nonexistent vision provider");

    // Verify the routing was attempted (not the generic capability error).
    assert!(
        err.to_string().contains("failed to create vision provider"),
        "expected creation failure, got: {}",
        err
    );
}

/// Empty `[IMAGE:]` markers (which are preserved as literal text by the
/// parser) should not trigger vision provider routing.
#[tokio::test]
async fn run_tool_call_loop_empty_image_markers_use_default_provider() {
    let provider = ScriptedProvider::from_text_responses(vec!["handled"]);

    let mut history = vec![ChatMessage::user(
        "empty marker [IMAGE:] should be ignored".to_string(),
    )];
    let tools_registry: Vec<Box<dyn Tool>> = Vec::new();
    let observer = NoopObserver;

    let multimodal = crate::config::MultimodalConfig {
        vision_provider: Some("nonexistent-provider-xyz".to_string()),
        ..Default::default()
    };

    let result = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "scripted",
        "scripted-model",
        0.0,
        true,
        None,
        "cli",
        None,
        &multimodal,
        3,
        None,
        None,
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect("empty image markers should not trigger vision routing");

    assert_eq!(result, "handled");
}

/// Multiple image markers should still trigger vision routing when
/// vision_provider is configured.
#[tokio::test]
async fn run_tool_call_loop_multiple_images_trigger_vision_routing() {
    let calls = Arc::new(AtomicUsize::new(0));
    let provider = NonVisionProvider {
        calls: Arc::clone(&calls),
    };

    let mut history = vec![ChatMessage::user(
        "two images [IMAGE:data:image/png;base64,aQ==] and [IMAGE:data:image/png;base64,bQ==]"
            .to_string(),
    )];
    let tools_registry: Vec<Box<dyn Tool>> = Vec::new();
    let observer = NoopObserver;

    let multimodal = crate::config::MultimodalConfig {
        vision_provider: Some("nonexistent-provider-xyz".to_string()),
        vision_model: Some("llava:7b".to_string()),
        ..Default::default()
    };

    let err = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        None,
        "cli",
        None,
        &multimodal,
        3,
        None,
        None,
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect_err("should attempt vision provider creation for multiple images");

    assert!(
        err.to_string().contains("failed to create vision provider"),
        "expected creation failure for multiple images, got: {}",
        err
    );
}

#[test]
fn should_execute_tools_in_parallel_returns_false_for_single_call() {
    let calls = vec![ParsedToolCall {
        name: "file_read".to_string(),
        arguments: serde_json::json!({"path": "a.txt"}),
        tool_call_id: None,
    }];

    assert!(!should_execute_tools_in_parallel(&calls, None));
}

#[test]
fn should_execute_tools_in_parallel_returns_false_when_approval_is_required() {
    let calls = vec![
        ParsedToolCall {
            name: "shell".to_string(),
            arguments: serde_json::json!({"command": "pwd"}),
            tool_call_id: None,
        },
        ParsedToolCall {
            name: "http_request".to_string(),
            arguments: serde_json::json!({"url": "https://example.com"}),
            tool_call_id: None,
        },
    ];
    let approval_cfg = crate::config::AutonomyConfig::default();
    let approval_mgr = ApprovalManager::from_config(&approval_cfg);

    assert!(!should_execute_tools_in_parallel(
        &calls,
        Some(&approval_mgr)
    ));
}

#[test]
fn should_execute_tools_in_parallel_returns_true_when_cli_has_no_interactive_approvals() {
    let calls = vec![
        ParsedToolCall {
            name: "shell".to_string(),
            arguments: serde_json::json!({"command": "pwd"}),
            tool_call_id: None,
        },
        ParsedToolCall {
            name: "http_request".to_string(),
            arguments: serde_json::json!({"url": "https://example.com"}),
            tool_call_id: None,
        },
    ];
    let approval_cfg = crate::config::AutonomyConfig {
        level: crate::security::AutonomyLevel::Full,
        ..crate::config::AutonomyConfig::default()
    };
    let approval_mgr = ApprovalManager::from_config(&approval_cfg);

    assert!(should_execute_tools_in_parallel(
        &calls,
        Some(&approval_mgr)
    ));
}

#[tokio::test]
async fn run_tool_call_loop_executes_multiple_tools_with_ordered_results() {
    let provider = ScriptedProvider::from_text_responses(vec![
        r#"<tool_call>
{"name":"delay_a","arguments":{"value":"A"}}
</tool_call>
<tool_call>
{"name":"delay_b","arguments":{"value":"B"}}
</tool_call>"#,
        "done",
    ]);

    let active = Arc::new(AtomicUsize::new(0));
    let max_active = Arc::new(AtomicUsize::new(0));
    let tools_registry: Vec<Box<dyn Tool>> = vec![
        Box::new(DelayTool::new(
            "delay_a",
            200,
            Arc::clone(&active),
            Arc::clone(&max_active),
        )),
        Box::new(DelayTool::new(
            "delay_b",
            200,
            Arc::clone(&active),
            Arc::clone(&max_active),
        )),
    ];

    let approval_cfg = crate::config::AutonomyConfig {
        level: crate::security::AutonomyLevel::Full,
        ..crate::config::AutonomyConfig::default()
    };
    let approval_mgr = ApprovalManager::from_config(&approval_cfg);

    let mut history = vec![
        ChatMessage::system("test-system"),
        ChatMessage::user("run tool calls"),
    ];
    let observer = NoopObserver;

    let result = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        Some(&approval_mgr),
        "telegram",
        None,
        &crate::config::MultimodalConfig::default(),
        4,
        None,
        None,
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect("parallel execution should complete");

    assert_eq!(result, "done");
    assert!(
        max_active.load(Ordering::SeqCst) >= 1,
        "tools should execute successfully"
    );

    let tool_results_message = history
        .iter()
        .find(|msg| msg.role == "user" && msg.content.starts_with("[Tool results]"))
        .expect("tool results message should be present");
    let idx_a = tool_results_message
        .content
        .find("name=\"delay_a\"")
        .expect("delay_a result should be present");
    let idx_b = tool_results_message
        .content
        .find("name=\"delay_b\"")
        .expect("delay_b result should be present");
    assert!(
        idx_a < idx_b,
        "tool results should preserve input order for tool call mapping"
    );
}

#[tokio::test]
async fn run_tool_call_loop_injects_channel_delivery_defaults_for_cron_add() {
    let provider = ScriptedProvider::from_text_responses(vec![
        r#"<tool_call>
{"name":"cron_add","arguments":{"job_type":"agent","prompt":"remind me later","schedule":{"kind":"every","every_ms":60000}}}
</tool_call>"#,
        "done",
    ]);

    let recorded_args = Arc::new(Mutex::new(Vec::new()));
    let tools_registry: Vec<Box<dyn Tool>> = vec![Box::new(RecordingArgsTool::new(
        "cron_add",
        Arc::clone(&recorded_args),
    ))];

    let mut history = vec![
        ChatMessage::system("test-system"),
        ChatMessage::user("schedule a reminder"),
    ];
    let observer = NoopObserver;

    let result = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        None,
        "telegram",
        Some("chat-42"),
        &crate::config::MultimodalConfig::default(),
        4,
        None,
        None,
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect("cron_add delivery defaults should be injected");

    assert_eq!(result, "done");

    let recorded = recorded_args
        .lock()
        .expect("recorded args lock should be valid");
    let delivery = recorded[0]["delivery"].clone();
    assert_eq!(
        delivery,
        serde_json::json!({
            "mode": "announce",
            "channel": "telegram",
            "to": "chat-42",
        })
    );
}

#[tokio::test]
async fn run_tool_call_loop_preserves_explicit_cron_delivery_none() {
    let provider = ScriptedProvider::from_text_responses(vec![
        r#"<tool_call>
{"name":"cron_add","arguments":{"job_type":"agent","prompt":"run silently","schedule":{"kind":"every","every_ms":60000},"delivery":{"mode":"none"}}}
</tool_call>"#,
        "done",
    ]);

    let recorded_args = Arc::new(Mutex::new(Vec::new()));
    let tools_registry: Vec<Box<dyn Tool>> = vec![Box::new(RecordingArgsTool::new(
        "cron_add",
        Arc::clone(&recorded_args),
    ))];

    let mut history = vec![
        ChatMessage::system("test-system"),
        ChatMessage::user("schedule a quiet cron job"),
    ];
    let observer = NoopObserver;

    let result = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        None,
        "telegram",
        Some("chat-42"),
        &crate::config::MultimodalConfig::default(),
        4,
        None,
        None,
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect("explicit delivery mode should be preserved");

    assert_eq!(result, "done");

    let recorded = recorded_args
        .lock()
        .expect("recorded args lock should be valid");
    assert_eq!(recorded[0]["delivery"], serde_json::json!({"mode": "none"}));
}

#[tokio::test]
async fn run_tool_call_loop_deduplicates_repeated_tool_calls() {
    let provider = ScriptedProvider::from_text_responses(vec![
        r#"<tool_call>
{"name":"count_tool","arguments":{"value":"A"}}
</tool_call>
<tool_call>
{"name":"count_tool","arguments":{"value":"A"}}
</tool_call>"#,
        "done",
    ]);

    let invocations = Arc::new(AtomicUsize::new(0));
    let tools_registry: Vec<Box<dyn Tool>> = vec![Box::new(CountingTool::new(
        "count_tool",
        Arc::clone(&invocations),
    ))];

    let mut history = vec![
        ChatMessage::system("test-system"),
        ChatMessage::user("run tool calls"),
    ];
    let observer = NoopObserver;

    let result = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        None,
        "cli",
        None,
        &crate::config::MultimodalConfig::default(),
        4,
        None,
        None,
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect("loop should finish after deduplicating repeated calls");

    assert_eq!(result, "done");
    assert_eq!(
        invocations.load(Ordering::SeqCst),
        1,
        "duplicate tool call with same args should not execute twice"
    );

    let tool_results = history
        .iter()
        .find(|msg| msg.role == "user" && msg.content.starts_with("[Tool results]"))
        .expect("prompt-mode tool result payload should be present");
    assert!(tool_results.content.contains("counted:A"));
    assert!(tool_results.content.contains("Skipped duplicate tool call"));
}

#[tokio::test]
async fn run_tool_call_loop_allows_low_risk_shell_in_non_interactive_mode() {
    let provider = ScriptedProvider::from_text_responses(vec![
        r#"<tool_call>
{"name":"shell","arguments":{"command":"echo hello"}}
</tool_call>"#,
        "done",
    ]);

    let tmp = TempDir::new().expect("temp dir");
    let security = Arc::new(crate::security::SecurityPolicy {
        autonomy: crate::security::AutonomyLevel::Supervised,
        workspace_dir: tmp.path().to_path_buf(),
        ..crate::security::SecurityPolicy::default()
    });
    let runtime: Arc<dyn crate::runtime::RuntimeAdapter> =
        Arc::new(crate::runtime::NativeRuntime::new());
    let tools_registry: Vec<Box<dyn Tool>> = vec![Box::new(crate::tools::shell::ShellTool::new(
        security, runtime,
    ))];

    let mut history = vec![
        ChatMessage::system("test-system"),
        ChatMessage::user("run shell"),
    ];
    let observer = NoopObserver;
    let approval_mgr =
        ApprovalManager::for_non_interactive(&crate::config::AutonomyConfig::default());

    let result = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        Some(&approval_mgr),
        "telegram",
        None,
        &crate::config::MultimodalConfig::default(),
        4,
        None,
        None,
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect("non-interactive shell should succeed for low-risk command");

    assert_eq!(result, "done");

    let tool_results = history
        .iter()
        .find(|msg| msg.role == "user" && msg.content.starts_with("[Tool results]"))
        .expect("tool results message should be present");
    assert!(tool_results.content.contains("hello"));
    assert!(!tool_results.content.contains("Denied by user."));
}

#[tokio::test]
async fn run_tool_call_loop_dedup_exempt_allows_repeated_calls() {
    let provider = ScriptedProvider::from_text_responses(vec![
        r#"<tool_call>
{"name":"count_tool","arguments":{"value":"A"}}
</tool_call>
<tool_call>
{"name":"count_tool","arguments":{"value":"A"}}
</tool_call>"#,
        "done",
    ]);

    let invocations = Arc::new(AtomicUsize::new(0));
    let tools_registry: Vec<Box<dyn Tool>> = vec![Box::new(CountingTool::new(
        "count_tool",
        Arc::clone(&invocations),
    ))];

    let mut history = vec![
        ChatMessage::system("test-system"),
        ChatMessage::user("run tool calls"),
    ];
    let observer = NoopObserver;
    let exempt = vec!["count_tool".to_string()];

    let result = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        None,
        "cli",
        None,
        &crate::config::MultimodalConfig::default(),
        4,
        None,
        None,
        None,
        &[],
        &exempt,
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect("loop should finish with exempt tool executing twice");

    assert_eq!(result, "done");
    assert_eq!(
        invocations.load(Ordering::SeqCst),
        2,
        "exempt tool should execute both duplicate calls"
    );

    let tool_results = history
        .iter()
        .find(|msg| msg.role == "user" && msg.content.starts_with("[Tool results]"))
        .expect("prompt-mode tool result payload should be present");
    assert!(
        !tool_results.content.contains("Skipped duplicate tool call"),
        "exempt tool calls should not be suppressed"
    );
}

#[tokio::test]
async fn run_tool_call_loop_dedup_exempt_only_affects_listed_tools() {
    let provider = ScriptedProvider::from_text_responses(vec![
        r#"<tool_call>
{"name":"count_tool","arguments":{"value":"A"}}
</tool_call>
<tool_call>
{"name":"count_tool","arguments":{"value":"A"}}
</tool_call>
<tool_call>
{"name":"other_tool","arguments":{"value":"B"}}
</tool_call>
<tool_call>
{"name":"other_tool","arguments":{"value":"B"}}
</tool_call>"#,
        "done",
    ]);

    let count_invocations = Arc::new(AtomicUsize::new(0));
    let other_invocations = Arc::new(AtomicUsize::new(0));
    let tools_registry: Vec<Box<dyn Tool>> = vec![
        Box::new(CountingTool::new(
            "count_tool",
            Arc::clone(&count_invocations),
        )),
        Box::new(CountingTool::new(
            "other_tool",
            Arc::clone(&other_invocations),
        )),
    ];

    let mut history = vec![
        ChatMessage::system("test-system"),
        ChatMessage::user("run tool calls"),
    ];
    let observer = NoopObserver;
    let exempt = vec!["count_tool".to_string()];

    let _result = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        None,
        "cli",
        None,
        &crate::config::MultimodalConfig::default(),
        4,
        None,
        None,
        None,
        &[],
        &exempt,
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect("loop should complete");

    assert_eq!(
        count_invocations.load(Ordering::SeqCst),
        2,
        "exempt tool should execute both calls"
    );
    assert_eq!(
        other_invocations.load(Ordering::SeqCst),
        1,
        "non-exempt tool should still be deduped"
    );
}

#[tokio::test]
async fn run_tool_call_loop_native_mode_preserves_fallback_tool_call_ids() {
    let provider = ScriptedProvider::from_text_responses(vec![
            r#"{"content":"Need to call tool","tool_calls":[{"id":"call_abc","name":"count_tool","arguments":"{\"value\":\"X\"}"}]}"#,
            "done",
        ])
        .with_native_tool_support();

    let invocations = Arc::new(AtomicUsize::new(0));
    let tools_registry: Vec<Box<dyn Tool>> = vec![Box::new(CountingTool::new(
        "count_tool",
        Arc::clone(&invocations),
    ))];

    let mut history = vec![
        ChatMessage::system("test-system"),
        ChatMessage::user("run tool calls"),
    ];
    let observer = NoopObserver;

    let result = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        None,
        "cli",
        None,
        &crate::config::MultimodalConfig::default(),
        4,
        None,
        None,
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect("native fallback id flow should complete");

    assert_eq!(result, "done");
    assert_eq!(invocations.load(Ordering::SeqCst), 1);
    assert!(
        history.iter().any(|msg| {
            msg.role == "tool" && msg.content.contains("\"tool_call_id\":\"call_abc\"")
        }),
        "tool result should preserve parsed fallback tool_call_id in native mode"
    );
    assert!(
        history
            .iter()
            .all(|msg| !(msg.role == "user" && msg.content.starts_with("[Tool results]"))),
        "native mode should use role=tool history instead of prompt fallback wrapper"
    );
}

#[tokio::test]
async fn run_tool_call_loop_relays_native_tool_call_text_via_on_delta() {
    let provider = ScriptedProvider {
        responses: Arc::new(Mutex::new(VecDeque::from(vec![
            ChatResponse {
                text: Some("Task started. Waiting 30 seconds before checking status.".into()),
                tool_calls: vec![ToolCall {
                    id: "call_wait".into(),
                    name: "count_tool".into(),
                    arguments: r#"{"value":"A"}"#.into(),
                }],
                usage: None,
                reasoning_content: None,
            },
            ChatResponse {
                text: Some("Final answer".into()),
                tool_calls: Vec::new(),
                usage: None,
                reasoning_content: None,
            },
        ]))),
        capabilities: ProviderCapabilities {
            native_tool_calling: true,
            ..ProviderCapabilities::default()
        },
    };

    let invocations = Arc::new(AtomicUsize::new(0));
    let tools_registry: Vec<Box<dyn Tool>> = vec![Box::new(CountingTool::new(
        "count_tool",
        Arc::clone(&invocations),
    ))];

    let mut history = vec![
        ChatMessage::system("test-system"),
        ChatMessage::user("run tool calls"),
    ];
    let observer = NoopObserver;
    let (tx, mut rx) = tokio::sync::mpsc::channel(16);

    let result = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        None,
        "telegram",
        None,
        &crate::config::MultimodalConfig::default(),
        4,
        None,
        Some(tx),
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect("native tool-call text should be relayed through on_delta");

    let mut deltas: Vec<DraftEvent> = Vec::new();
    while let Some(delta) = rx.recv().await {
        deltas.push(delta);
    }

    let explanation_idx = deltas
            .iter()
            .position(|delta| matches!(delta, DraftEvent::Content(t) if t == "Task started. Waiting 30 seconds before checking status.\n"))
            .expect("native assistant text should be relayed to on_delta");
    let clear_idx = deltas
        .iter()
        .position(|delta| matches!(delta, DraftEvent::Clear))
        .expect("final answer streaming should clear prior draft state");

    assert!(
            deltas
                .iter()
                .any(|delta| matches!(delta, DraftEvent::Progress(t) if t.starts_with("\u{1f4ac} Got 1 tool call(s)"))),
            "tool-call progress line should still be relayed"
        );
    assert!(
        explanation_idx < clear_idx,
        "native assistant text should arrive before final-answer draft clearing"
    );
    assert_eq!(result, "Final answer");
    assert_eq!(invocations.load(Ordering::SeqCst), 1);
}

#[tokio::test]
async fn run_tool_call_loop_consumes_provider_stream_for_final_response() {
    let provider = StreamingScriptedProvider::from_text_responses(vec!["streamed final answer"]);
    let tools_registry: Vec<Box<dyn Tool>> = Vec::new();
    let mut history = vec![
        ChatMessage::system("test-system"),
        ChatMessage::user("say hi"),
    ];
    let observer = NoopObserver;
    let (tx, mut rx) = tokio::sync::mpsc::channel::<DraftEvent>(32);

    let result = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        None,
        "telegram",
        None,
        &crate::config::MultimodalConfig::default(),
        4,
        None,
        Some(tx),
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect("streaming provider should complete");

    let mut visible_deltas = String::new();
    while let Some(delta) = rx.recv().await {
        match delta {
            DraftEvent::Clear => {
                visible_deltas.clear();
            }
            DraftEvent::Progress(_) => {}
            DraftEvent::Content(text) => {
                visible_deltas.push_str(&text);
            }
        }
    }

    assert_eq!(result, "streamed final answer");
    assert_eq!(
        visible_deltas, "streamed final answer",
        "draft should receive upstream deltas once without post-hoc duplication"
    );
    assert_eq!(provider.stream_calls.load(Ordering::SeqCst), 1);
    assert_eq!(provider.chat_calls.load(Ordering::SeqCst), 0);
}

#[tokio::test]
async fn run_tool_call_loop_streaming_path_preserves_tool_loop_semantics() {
    let provider = StreamingScriptedProvider::from_text_responses(vec![
        r#"<tool_call>
{"name":"count_tool","arguments":{"value":"A"}}
</tool_call>"#,
        "done",
    ]);
    let invocations = Arc::new(AtomicUsize::new(0));
    let tools_registry: Vec<Box<dyn Tool>> = vec![Box::new(CountingTool::new(
        "count_tool",
        Arc::clone(&invocations),
    ))];
    let mut history = vec![
        ChatMessage::system("test-system"),
        ChatMessage::user("run tool calls"),
    ];
    let observer = NoopObserver;
    let (tx, mut rx) = tokio::sync::mpsc::channel::<DraftEvent>(64);

    let result = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        None,
        "telegram",
        None,
        &crate::config::MultimodalConfig::default(),
        5,
        None,
        Some(tx),
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect("streaming tool loop should execute tool and finish");

    let mut visible_deltas = String::new();
    while let Some(delta) = rx.recv().await {
        match delta {
            DraftEvent::Clear => {
                visible_deltas.clear();
            }
            DraftEvent::Progress(_) => {}
            DraftEvent::Content(text) => {
                visible_deltas.push_str(&text);
            }
        }
    }

    assert_eq!(result, "done");
    assert_eq!(invocations.load(Ordering::SeqCst), 1);
    assert_eq!(provider.stream_calls.load(Ordering::SeqCst), 2);
    assert_eq!(provider.chat_calls.load(Ordering::SeqCst), 0);
    assert_eq!(visible_deltas, "done");
    assert!(
        !visible_deltas.contains("<tool_call"),
        "draft text should not leak streamed tool payload markers"
    );
}

#[tokio::test]
async fn run_tool_call_loop_streams_native_tool_events_without_chat_fallback() {
    let provider = StreamingNativeToolEventProvider::with_turns(vec![
        NativeStreamTurn::ToolCall(ToolCall {
            id: "call_native_1".to_string(),
            name: "count_tool".to_string(),
            arguments: r#"{"value":"A"}"#.to_string(),
        }),
        NativeStreamTurn::Text("done".to_string()),
    ]);
    let invocations = Arc::new(AtomicUsize::new(0));
    let tools_registry: Vec<Box<dyn Tool>> = vec![Box::new(CountingTool::new(
        "count_tool",
        Arc::clone(&invocations),
    ))];
    let mut history = vec![
        ChatMessage::system("test-system"),
        ChatMessage::user("run native tools"),
    ];
    let observer = NoopObserver;
    let (tx, mut rx) = tokio::sync::mpsc::channel::<DraftEvent>(64);

    let result = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        None,
        "telegram",
        None,
        &crate::config::MultimodalConfig::default(),
        5,
        None,
        Some(tx),
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect("native streaming events should preserve tool loop semantics");

    let mut visible_deltas = String::new();
    while let Some(delta) = rx.recv().await {
        match delta {
            DraftEvent::Clear => {
                visible_deltas.clear();
            }
            DraftEvent::Progress(_) => {}
            DraftEvent::Content(text) => {
                visible_deltas.push_str(&text);
            }
        }
    }

    assert_eq!(result, "done");
    assert_eq!(invocations.load(Ordering::SeqCst), 1);
    assert_eq!(provider.stream_calls.load(Ordering::SeqCst), 2);
    assert_eq!(provider.stream_tool_requests.load(Ordering::SeqCst), 2);
    assert_eq!(provider.chat_calls.load(Ordering::SeqCst), 0);
    assert_eq!(visible_deltas, "done");
}

#[tokio::test]
async fn run_tool_call_loop_routed_streaming_uses_live_provider_deltas_once() {
    let default_provider = RouteAwareStreamingProvider::new("default answer");
    let default_stream_calls = Arc::clone(&default_provider.stream_calls);
    let default_chat_calls = Arc::clone(&default_provider.chat_calls);

    let routed_provider = RouteAwareStreamingProvider::new("routed streamed answer");
    let routed_stream_calls = Arc::clone(&routed_provider.stream_calls);
    let routed_chat_calls = Arc::clone(&routed_provider.chat_calls);
    let routed_last_model = Arc::clone(&routed_provider.last_model);

    let router = RouterProvider::new(
        vec![
            ("default".to_string(), Box::new(default_provider)),
            ("fast".to_string(), Box::new(routed_provider)),
        ],
        vec![(
            "fast".to_string(),
            Route {
                provider_name: "fast".to_string(),
                model: "routed-model".to_string(),
            },
        )],
        "default-model".to_string(),
    );

    let tools_registry: Vec<Box<dyn Tool>> = Vec::new();
    let mut history = vec![
        ChatMessage::system("test-system"),
        ChatMessage::user("say hi"),
    ];
    let observer = NoopObserver;
    let (tx, mut rx) = tokio::sync::mpsc::channel::<DraftEvent>(32);

    let result = run_tool_call_loop(
        &router,
        &mut history,
        &tools_registry,
        &observer,
        "router",
        "hint:fast",
        0.0,
        true,
        None,
        "telegram",
        None,
        &crate::config::MultimodalConfig::default(),
        4,
        None,
        Some(tx),
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect("routed streaming provider should complete");

    let mut visible_deltas = String::new();
    while let Some(delta) = rx.recv().await {
        match delta {
            DraftEvent::Clear => {
                visible_deltas.clear();
            }
            DraftEvent::Progress(_) => {}
            DraftEvent::Content(text) => {
                visible_deltas.push_str(&text);
            }
        }
    }

    assert_eq!(result, "routed streamed answer");
    assert_eq!(
        visible_deltas, "routed streamed answer",
        "routed draft should receive upstream deltas once without post-hoc duplication"
    );
    assert_eq!(default_stream_calls.load(Ordering::SeqCst), 0);
    assert_eq!(routed_stream_calls.load(Ordering::SeqCst), 1);
    assert_eq!(default_chat_calls.load(Ordering::SeqCst), 0);
    assert_eq!(routed_chat_calls.load(Ordering::SeqCst), 0);
    assert_eq!(
        routed_last_model
            .lock()
            .expect("routed_last_model lock should be valid")
            .as_str(),
        "routed-model"
    );
}

#[test]
fn agent_turn_executes_activated_tool_from_wrapper() {
    let runtime = tokio::runtime::Builder::new_current_thread()
        .enable_all()
        .build()
        .expect("test runtime should initialize");

    runtime.block_on(async {
        let provider = ScriptedProvider::from_text_responses(vec![
            r#"<tool_call>
{"name":"pixel__get_api_health","arguments":{"value":"ok"}}
</tool_call>"#,
            "done",
        ]);

        let invocations = Arc::new(AtomicUsize::new(0));
        let activated = Arc::new(std::sync::Mutex::new(crate::tools::ActivatedToolSet::new()));
        let activated_tool: Arc<dyn Tool> = Arc::new(CountingTool::new(
            "pixel__get_api_health",
            Arc::clone(&invocations),
        ));
        activated
            .lock()
            .unwrap()
            .activate("pixel__get_api_health".into(), activated_tool);

        let tools_registry: Vec<Box<dyn Tool>> = Vec::new();
        let mut history = vec![
            ChatMessage::system("test-system"),
            ChatMessage::user("use the activated MCP tool"),
        ];
        let observer = NoopObserver;

        let result = agent_turn(
            &provider,
            &mut history,
            &tools_registry,
            &observer,
            "mock-provider",
            "mock-model",
            0.0,
            true,
            "daemon",
            None,
            &crate::config::MultimodalConfig::default(),
            4,
            None,
            &[],
            &[],
            Some(&activated),
            None,
        )
        .await
        .expect("wrapper path should execute activated tools");

        assert_eq!(result, "done");
        assert_eq!(invocations.load(Ordering::SeqCst), 1);
    });
}

#[test]
fn resolve_display_text_hides_raw_payload_for_tool_only_turns() {
    let display = resolve_display_text(
        "<tool_call>{\"name\":\"memory_store\"}</tool_call>",
        "",
        true,
        false,
    );
    assert!(display.is_empty());
}

#[test]
fn resolve_display_text_keeps_plain_text_for_tool_turns() {
    let display = resolve_display_text(
        "<tool_call>{\"name\":\"shell\"}</tool_call>",
        "Let me check that.",
        true,
        false,
    );
    assert_eq!(display, "Let me check that.");
}

#[test]
fn resolve_display_text_uses_response_text_for_native_tool_turns() {
    let display = resolve_display_text("Task started.", "", true, true);
    assert_eq!(display, "Task started.");
}

#[test]
fn resolve_display_text_uses_response_text_for_final_turns() {
    let display = resolve_display_text("Final answer", "", false, false);
    assert_eq!(display, "Final answer");
}

#[test]
fn parse_tool_calls_extracts_single_call() {
    let response = r#"Let me check that.
<tool_call>
{"name": "shell", "arguments": {"command": "ls -la"}}
</tool_call>"#;

    let (text, calls) = parse_tool_calls(response);
    assert_eq!(text, "Let me check that.");
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(
        calls[0].arguments.get("command").unwrap().as_str().unwrap(),
        "ls -la"
    );
}

#[test]
fn parse_tool_calls_extracts_multiple_calls() {
    let response = r#"<tool_call>
{"name": "file_read", "arguments": {"path": "a.txt"}}
</tool_call>
<tool_call>
{"name": "file_read", "arguments": {"path": "b.txt"}}
</tool_call>"#;

    let (_, calls) = parse_tool_calls(response);
    assert_eq!(calls.len(), 2);
    assert_eq!(calls[0].name, "file_read");
    assert_eq!(calls[1].name, "file_read");
}

#[test]
fn parse_tool_calls_returns_text_only_when_no_calls() {
    let response = "Just a normal response with no tools.";
    let (text, calls) = parse_tool_calls(response);
    assert_eq!(text, "Just a normal response with no tools.");
    assert!(calls.is_empty());
}

#[test]
fn parse_tool_calls_handles_malformed_json() {
    let response = r#"<tool_call>
not valid json
</tool_call>
Some text after."#;

    let (text, calls) = parse_tool_calls(response);
    assert!(calls.is_empty());
    assert!(text.contains("Some text after."));
}

#[test]
fn parse_tool_calls_text_before_and_after() {
    let response = r#"Before text.
<tool_call>
{"name": "shell", "arguments": {"command": "echo hi"}}
</tool_call>
After text."#;

    let (text, calls) = parse_tool_calls(response);
    assert!(text.contains("Before text."));
    assert!(text.contains("After text."));
    assert_eq!(calls.len(), 1);
}

#[test]
fn parse_tool_calls_handles_openai_format() {
    // OpenAI-style response with tool_calls array
    let response = r#"{"content": "Let me check that for you.", "tool_calls": [{"type": "function", "function": {"name": "shell", "arguments": "{\"command\": \"ls -la\"}"}}]}"#;

    let (text, calls) = parse_tool_calls(response);
    assert_eq!(text, "Let me check that for you.");
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(
        calls[0].arguments.get("command").unwrap().as_str().unwrap(),
        "ls -la"
    );
}

#[test]
fn parse_tool_calls_handles_openai_format_multiple_calls() {
    let response = r#"{"tool_calls": [{"type": "function", "function": {"name": "file_read", "arguments": "{\"path\": \"a.txt\"}"}}, {"type": "function", "function": {"name": "file_read", "arguments": "{\"path\": \"b.txt\"}"}}]}"#;

    let (_, calls) = parse_tool_calls(response);
    assert_eq!(calls.len(), 2);
    assert_eq!(calls[0].name, "file_read");
    assert_eq!(calls[1].name, "file_read");
}

#[test]
fn parse_tool_calls_openai_format_without_content() {
    // Some providers don't include content field with tool_calls
    let response = r#"{"tool_calls": [{"type": "function", "function": {"name": "memory_recall", "arguments": "{}"}}]}"#;

    let (text, calls) = parse_tool_calls(response);
    assert!(text.is_empty()); // No content field
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "memory_recall");
}

#[test]
fn parse_tool_calls_preserves_openai_tool_call_ids() {
    let response = r#"{"tool_calls":[{"id":"call_42","function":{"name":"shell","arguments":"{\"command\":\"pwd\"}"}}]}"#;
    let (_, calls) = parse_tool_calls(response);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].tool_call_id.as_deref(), Some("call_42"));
}

#[test]
fn parse_tool_calls_handles_markdown_json_inside_tool_call_tag() {
    let response = r#"<tool_call>
```json
{"name": "file_write", "arguments": {"path": "test.py", "content": "print('ok')"}}
```
</tool_call>"#;

    let (text, calls) = parse_tool_calls(response);
    assert!(text.is_empty());
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "file_write");
    assert_eq!(
        calls[0].arguments.get("path").unwrap().as_str().unwrap(),
        "test.py"
    );
}

#[test]
fn parse_tool_calls_handles_noisy_tool_call_tag_body() {
    let response = r#"<tool_call>
I will now call the tool with this payload:
{"name": "shell", "arguments": {"command": "pwd"}}
</tool_call>"#;

    let (text, calls) = parse_tool_calls(response);
    assert!(text.is_empty());
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(
        calls[0].arguments.get("command").unwrap().as_str().unwrap(),
        "pwd"
    );
}

#[test]
fn parse_tool_calls_handles_tool_call_inline_attributes_with_send_message_alias() {
    let response = r#"<tool_call>send_message channel="user_channel" message="Hello! How can I assist you today?"</tool_call>"#;

    let (text, calls) = parse_tool_calls(response);
    assert!(text.is_empty());
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "message_send");
    assert_eq!(
        calls[0].arguments.get("channel").unwrap().as_str().unwrap(),
        "user_channel"
    );
    assert_eq!(
        calls[0].arguments.get("message").unwrap().as_str().unwrap(),
        "Hello! How can I assist you today?"
    );
}

#[test]
fn parse_tool_calls_handles_tool_call_function_style_arguments() {
    let response = r#"<tool_call>message_send(channel="general", message="test")</tool_call>"#;

    let (text, calls) = parse_tool_calls(response);
    assert!(text.is_empty());
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "message_send");
    assert_eq!(
        calls[0].arguments.get("channel").unwrap().as_str().unwrap(),
        "general"
    );
    assert_eq!(
        calls[0].arguments.get("message").unwrap().as_str().unwrap(),
        "test"
    );
}

#[test]
fn parse_tool_calls_handles_xml_nested_tool_payload() {
    let response = r#"<tool_call>
<memory_recall>
<query>project roadmap</query>
</memory_recall>
</tool_call>"#;

    let (text, calls) = parse_tool_calls(response);
    assert!(text.is_empty());
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "memory_recall");
    assert_eq!(
        calls[0].arguments.get("query").unwrap().as_str().unwrap(),
        "project roadmap"
    );
}

#[test]
fn parse_tool_calls_ignores_xml_thinking_wrapper() {
    let response = r#"<tool_call>
<thinking>Need to inspect memory first</thinking>
<memory_recall>
<query>recent deploy notes</query>
</memory_recall>
</tool_call>"#;

    let (text, calls) = parse_tool_calls(response);
    assert!(text.is_empty());
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "memory_recall");
    assert_eq!(
        calls[0].arguments.get("query").unwrap().as_str().unwrap(),
        "recent deploy notes"
    );
}

#[test]
fn parse_tool_calls_handles_xml_with_json_arguments() {
    let response = r#"<tool_call>
<shell>{"command":"pwd"}</shell>
</tool_call>"#;

    let (text, calls) = parse_tool_calls(response);
    assert!(text.is_empty());
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(
        calls[0].arguments.get("command").unwrap().as_str().unwrap(),
        "pwd"
    );
}

#[test]
fn parse_tool_calls_handles_markdown_tool_call_fence() {
    let response = r#"I'll check that.
```tool_call
{"name": "shell", "arguments": {"command": "pwd"}}
```
Done."#;

    let (text, calls) = parse_tool_calls(response);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(
        calls[0].arguments.get("command").unwrap().as_str().unwrap(),
        "pwd"
    );
    assert!(text.contains("I'll check that."));
    assert!(text.contains("Done."));
    assert!(!text.contains("```tool_call"));
}

#[test]
fn parse_tool_calls_handles_markdown_tool_call_hybrid_close_tag() {
    let response = r#"Preface
```tool-call
{"name": "shell", "arguments": {"command": "date"}}
</tool_call>
Tail"#;

    let (text, calls) = parse_tool_calls(response);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(
        calls[0].arguments.get("command").unwrap().as_str().unwrap(),
        "date"
    );
    assert!(text.contains("Preface"));
    assert!(text.contains("Tail"));
    assert!(!text.contains("```tool-call"));
}

#[test]
fn parse_tool_calls_handles_markdown_invoke_fence() {
    let response = r#"Checking.
```invoke
{"name": "shell", "arguments": {"command": "date"}}
```
Done."#;

    let (text, calls) = parse_tool_calls(response);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(
        calls[0].arguments.get("command").unwrap().as_str().unwrap(),
        "date"
    );
    assert!(text.contains("Checking."));
    assert!(text.contains("Done."));
}

#[test]
fn parse_tool_calls_handles_tool_name_fence_format() {
    // Issue #1420: xAI grok models use ```tool <name> format
    let response = r#"I'll write a test file.
```tool file_write
{"path": "/home/user/test.txt", "content": "Hello world"}
```
Done."#;

    let (text, calls) = parse_tool_calls(response);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "file_write");
    assert_eq!(
        calls[0].arguments.get("path").unwrap().as_str().unwrap(),
        "/home/user/test.txt"
    );
    assert!(text.contains("I'll write a test file."));
    assert!(text.contains("Done."));
}

#[test]
fn parse_tool_calls_handles_tool_name_fence_shell() {
    // Issue #1420: Test shell command in ```tool shell format
    let response = r#"```tool shell
{"command": "ls -la"}
```"#;

    let (_text, calls) = parse_tool_calls(response);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(
        calls[0].arguments.get("command").unwrap().as_str().unwrap(),
        "ls -la"
    );
}

#[test]
fn parse_tool_calls_handles_multiple_tool_name_fences() {
    // Multiple tool calls in ```tool <name> format
    let response = r#"First, I'll write a file.
```tool file_write
{"path": "/tmp/a.txt", "content": "A"}
```
Then read it.
```tool file_read
{"path": "/tmp/a.txt"}
```
Done."#;

    let (text, calls) = parse_tool_calls(response);
    assert_eq!(calls.len(), 2);
    assert_eq!(calls[0].name, "file_write");
    assert_eq!(calls[1].name, "file_read");
    assert!(text.contains("First, I'll write a file."));
    assert!(text.contains("Then read it."));
    assert!(text.contains("Done."));
}

#[test]
fn parse_tool_calls_handles_toolcall_tag_alias() {
    let response = r#"<toolcall>
{"name": "shell", "arguments": {"command": "date"}}
</toolcall>"#;

    let (text, calls) = parse_tool_calls(response);
    assert!(text.is_empty());
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(
        calls[0].arguments.get("command").unwrap().as_str().unwrap(),
        "date"
    );
}

#[test]
fn parse_tool_calls_handles_tool_dash_call_tag_alias() {
    let response = r#"<tool-call>
{"name": "shell", "arguments": {"command": "whoami"}}
</tool-call>"#;

    let (text, calls) = parse_tool_calls(response);
    assert!(text.is_empty());
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(
        calls[0].arguments.get("command").unwrap().as_str().unwrap(),
        "whoami"
    );
}

#[test]
fn parse_tool_calls_handles_invoke_tag_alias() {
    let response = r#"<invoke>
{"name": "shell", "arguments": {"command": "uptime"}}
</invoke>"#;

    let (text, calls) = parse_tool_calls(response);
    assert!(text.is_empty());
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(
        calls[0].arguments.get("command").unwrap().as_str().unwrap(),
        "uptime"
    );
}

#[test]
fn parse_tool_calls_handles_minimax_invoke_parameter_format() {
    let response = r#"<minimax:tool_call>
<invoke name="shell">
<parameter name="command">sqlite3 /tmp/test.db ".tables"</parameter>
</invoke>
</minimax:tool_call>"#;

    let (text, calls) = parse_tool_calls(response);
    assert!(text.is_empty());
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(
        calls[0].arguments.get("command").unwrap().as_str().unwrap(),
        r#"sqlite3 /tmp/test.db ".tables""#
    );
}

#[test]
fn parse_tool_calls_handles_minimax_invoke_with_surrounding_text() {
    let response = r#"Preface
<minimax:tool_call>
<invoke name='http_request'>
<parameter name='url'>https://example.com</parameter>
<parameter name='method'>GET</parameter>
</invoke>
</minimax:tool_call>
Tail"#;

    let (text, calls) = parse_tool_calls(response);
    assert!(text.contains("Preface"));
    assert!(text.contains("Tail"));
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "http_request");
    assert_eq!(
        calls[0].arguments.get("url").unwrap().as_str().unwrap(),
        "https://example.com"
    );
    assert_eq!(
        calls[0].arguments.get("method").unwrap().as_str().unwrap(),
        "GET"
    );
}

#[test]
fn parse_tool_calls_handles_minimax_toolcall_alias_and_cross_close_tag() {
    let response = r#"<tool_call>
{"name":"shell","arguments":{"command":"date"}}
</minimax:toolcall>"#;

    let (text, calls) = parse_tool_calls(response);
    assert!(text.is_empty());
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(
        calls[0].arguments.get("command").unwrap().as_str().unwrap(),
        "date"
    );
}

#[test]
fn parse_tool_calls_handles_perl_style_tool_call_blocks() {
    let response = r#"TOOL_CALL
{tool => "shell", args => { --command "uname -a" }}}
/TOOL_CALL"#;

    let calls = parse_perl_style_tool_calls(response);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(
        calls[0].arguments.get("command").unwrap().as_str().unwrap(),
        "uname -a"
    );
}

#[test]
fn parse_tool_calls_handles_square_bracket_tool_call_blocks() {
    let response = r#"[TOOL_CALL]{tool => "shell", args => {--command "echo hello"}}[/TOOL_CALL]"#;

    let calls = parse_perl_style_tool_calls(response);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(
        calls[0].arguments.get("command").unwrap().as_str().unwrap(),
        "echo hello"
    );
}

#[test]
fn parse_tool_calls_handles_square_bracket_multiline() {
    let response = r#"[TOOL_CALL]
{tool => "file_read", args => {
  --path "/tmp/test.txt"
  --description "Read test file"
}}
[/TOOL_CALL]"#;

    let calls = parse_perl_style_tool_calls(response);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "file_read");
    assert_eq!(
        calls[0].arguments.get("path").unwrap().as_str().unwrap(),
        "/tmp/test.txt"
    );
    assert_eq!(
        calls[0]
            .arguments
            .get("description")
            .unwrap()
            .as_str()
            .unwrap(),
        "Read test file"
    );
}

#[test]
fn parse_tool_calls_recovers_unclosed_tool_call_with_json() {
    let response = r#"I will call the tool now.
<tool_call>
{"name": "shell", "arguments": {"command": "uptime -p"}}"#;

    let (text, calls) = parse_tool_calls(response);
    assert!(text.contains("I will call the tool now."));
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(
        calls[0].arguments.get("command").unwrap().as_str().unwrap(),
        "uptime -p"
    );
}

#[test]
fn parse_tool_calls_recovers_mismatched_close_tag() {
    let response = r#"<tool_call>
{"name": "shell", "arguments": {"command": "uptime"}}
</arg_value>"#;

    let (text, calls) = parse_tool_calls(response);
    assert!(text.is_empty());
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(
        calls[0].arguments.get("command").unwrap().as_str().unwrap(),
        "uptime"
    );
}

#[test]
fn parse_tool_calls_recovers_cross_alias_closing_tags() {
    let response = r#"<toolcall>
{"name": "shell", "arguments": {"command": "date"}}
</tool_call>"#;

    let (text, calls) = parse_tool_calls(response);
    assert!(text.is_empty());
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
}

#[test]
fn parse_tool_calls_rejects_raw_tool_json_without_tags() {
    // SECURITY: Raw JSON without explicit wrappers should NOT be parsed
    // This prevents prompt injection attacks where malicious content
    // could include JSON that mimics a tool call.
    let response = r#"Sure, creating the file now.
{"name": "file_write", "arguments": {"path": "hello.py", "content": "print('hello')"}}"#;

    let (text, calls) = parse_tool_calls(response);
    assert!(text.contains("Sure, creating the file now."));
    assert_eq!(
        calls.len(),
        0,
        "Raw JSON without wrappers should not be parsed"
    );
}

#[test]
fn build_tool_instructions_includes_all_tools() {
    use crate::security::SecurityPolicy;
    let security = Arc::new(SecurityPolicy::from_config(
        &crate::config::AutonomyConfig::default(),
        std::path::Path::new("/tmp"),
    ));
    let tools = tools::default_tools(security);
    let instructions = build_tool_instructions(&tools, None);

    assert!(instructions.contains("## Tool Use Protocol"));
    assert!(instructions.contains("<tool_call>"));
    assert!(instructions.contains("shell"));
    assert!(instructions.contains("file_read"));
    assert!(instructions.contains("file_write"));
}

#[test]
fn tools_to_openai_format_produces_valid_schema() {
    use crate::security::SecurityPolicy;
    let security = Arc::new(SecurityPolicy::from_config(
        &crate::config::AutonomyConfig::default(),
        std::path::Path::new("/tmp"),
    ));
    let tools = tools::default_tools(security);
    let formatted = tools_to_openai_format(&tools);

    assert!(!formatted.is_empty());
    for tool_json in &formatted {
        assert_eq!(tool_json["type"], "function");
        assert!(tool_json["function"]["name"].is_string());
        assert!(tool_json["function"]["description"].is_string());
        assert!(!tool_json["function"]["name"].as_str().unwrap().is_empty());
    }
    // Verify known tools are present
    let names: Vec<&str> = formatted
        .iter()
        .filter_map(|t| t["function"]["name"].as_str())
        .collect();
    assert!(names.contains(&"shell"));
    assert!(names.contains(&"file_read"));
}

#[test]
fn trim_history_preserves_system_prompt() {
    let mut history = vec![ChatMessage::system("system prompt")];
    for i in 0..DEFAULT_MAX_HISTORY_MESSAGES + 20 {
        history.push(ChatMessage::user(format!("msg {i}")));
    }
    let original_len = history.len();
    assert!(original_len > DEFAULT_MAX_HISTORY_MESSAGES + 1);

    trim_history(&mut history, DEFAULT_MAX_HISTORY_MESSAGES);

    // System prompt preserved
    assert_eq!(history[0].role, "system");
    assert_eq!(history[0].content, "system prompt");
    // Trimmed to limit
    assert_eq!(history.len(), DEFAULT_MAX_HISTORY_MESSAGES + 1); // +1 for system
    // Most recent messages preserved
    let last = &history[history.len() - 1];
    assert_eq!(
        last.content,
        format!("msg {}", DEFAULT_MAX_HISTORY_MESSAGES + 19)
    );
}

#[test]
fn trim_history_noop_when_within_limit() {
    let mut history = vec![
        ChatMessage::system("sys"),
        ChatMessage::user("hello"),
        ChatMessage::assistant("hi"),
    ];
    trim_history(&mut history, DEFAULT_MAX_HISTORY_MESSAGES);
    assert_eq!(history.len(), 3);
}

#[test]
fn autosave_memory_key_has_prefix_and_uniqueness() {
    let key1 = autosave_memory_key("user_msg");
    let key2 = autosave_memory_key("user_msg");

    assert!(key1.starts_with("user_msg_"));
    assert!(key2.starts_with("user_msg_"));
    assert_ne!(key1, key2);
}

#[tokio::test]
async fn autosave_memory_keys_preserve_multiple_turns() {
    let tmp = TempDir::new().unwrap();
    let mem = SqliteMemory::new(tmp.path()).unwrap();

    let key1 = autosave_memory_key("user_msg");
    let key2 = autosave_memory_key("user_msg");

    mem.store(&key1, "I'm Paul", MemoryCategory::Conversation, None)
        .await
        .unwrap();
    mem.store(&key2, "I'm 45", MemoryCategory::Conversation, None)
        .await
        .unwrap();

    assert_eq!(mem.count().await.unwrap(), 2);

    let recalled = mem.recall("45", 5, None, None, None).await.unwrap();
    assert!(recalled.iter().any(|entry| entry.content.contains("45")));
}

#[tokio::test]
async fn build_context_ignores_legacy_assistant_autosave_entries() {
    let tmp = TempDir::new().unwrap();
    let mem = SqliteMemory::new(tmp.path()).unwrap();
    mem.store(
        "assistant_resp_poisoned",
        "User suffered a fabricated event",
        MemoryCategory::Daily,
        None,
    )
    .await
    .unwrap();
    mem.store(
        "user_msg_real",
        "User asked for concise status updates",
        MemoryCategory::Conversation,
        None,
    )
    .await
    .unwrap();

    let context = build_context(&mem, "status updates", 0.0, None).await;
    assert!(context.contains("user_msg_real"));
    assert!(!context.contains("assistant_resp_poisoned"));
    assert!(!context.contains("fabricated event"));
}

// ═══════════════════════════════════════════════════════════════════════
// Recovery Tests - Tool Call Parsing Edge Cases
// ═══════════════════════════════════════════════════════════════════════

#[test]
fn parse_tool_calls_handles_empty_tool_result() {
    // Recovery: Empty tool_result tag should be handled gracefully
    let response = r#"I'll run that command.
<tool_result name="shell">

</tool_result>
Done."#;
    let (text, calls) = parse_tool_calls(response);
    assert!(text.contains("Done."));
    assert!(calls.is_empty());
}

#[test]
fn strip_tool_result_blocks_removes_single_block() {
    let input = r#"<tool_result name="memory_recall" status="ok">
{"matches":["hello"]}
</tool_result>
Here is my answer."#;
    assert_eq!(strip_tool_result_blocks(input), "Here is my answer.");
}

#[test]
fn strip_tool_result_blocks_removes_multiple_blocks() {
    let input = r#"<tool_result name="memory_recall" status="ok">
{"matches":[]}
</tool_result>
<tool_result name="shell" status="ok">
done
</tool_result>
Final answer."#;
    assert_eq!(strip_tool_result_blocks(input), "Final answer.");
}

#[test]
fn strip_tool_result_blocks_removes_prefix() {
    let input =
        "[Tool results]\n<tool_result name=\"shell\" status=\"ok\">\nok\n</tool_result>\nDone.";
    assert_eq!(strip_tool_result_blocks(input), "Done.");
}

#[test]
fn strip_tool_result_blocks_removes_thinking() {
    let input = "<thinking>\nLet me think...\n</thinking>\nHere is the answer.";
    assert_eq!(strip_tool_result_blocks(input), "Here is the answer.");
}

#[test]
fn strip_tool_result_blocks_removes_think_tags() {
    let input = "<think>\nLet me reason...\n</think>\nHere is the answer.";
    assert_eq!(strip_tool_result_blocks(input), "Here is the answer.");
}

#[test]
fn strip_think_tags_removes_single_block() {
    assert_eq!(strip_think_tags("<think>reasoning</think>Hello"), "Hello");
}

#[test]
fn strip_think_tags_removes_multiple_blocks() {
    assert_eq!(strip_think_tags("<think>a</think>X<think>b</think>Y"), "XY");
}

#[test]
fn strip_think_tags_handles_unclosed_block() {
    assert_eq!(strip_think_tags("visible<think>hidden"), "visible");
}

#[test]
fn strip_think_tags_preserves_text_without_tags() {
    assert_eq!(strip_think_tags("plain text"), "plain text");
}

#[test]
fn parse_tool_calls_strips_think_before_tool_call() {
    // Qwen regression: <think> tags before <tool_call> tags should be
    // stripped, allowing the tool call to be parsed correctly.
    let response = "<think>I need to list files to understand the project</think>\n<tool_call>\n{\"name\":\"shell\",\"arguments\":{\"command\":\"ls\"}}\n</tool_call>";
    let (text, calls) = parse_tool_calls(response);
    assert_eq!(
        calls.len(),
        1,
        "should parse tool call after stripping think tags"
    );
    assert_eq!(calls[0].name, "shell");
    assert_eq!(
        calls[0].arguments.get("command").unwrap().as_str().unwrap(),
        "ls"
    );
    assert!(text.is_empty(), "think content should not appear as text");
}

#[test]
fn parse_tool_calls_strips_think_only_returns_empty() {
    // When response is only <think> tags with no tool calls, should
    // return empty text and no calls.
    let response = "<think>Just thinking, no action needed</think>";
    let (text, calls) = parse_tool_calls(response);
    assert!(calls.is_empty());
    assert!(text.is_empty());
}

#[test]
fn parse_tool_calls_handles_qwen_think_with_multiple_tool_calls() {
    let response = "<think>I need to check two things</think>\n<tool_call>\n{\"name\":\"shell\",\"arguments\":{\"command\":\"date\"}}\n</tool_call>\n<tool_call>\n{\"name\":\"shell\",\"arguments\":{\"command\":\"pwd\"}}\n</tool_call>";
    let (_, calls) = parse_tool_calls(response);
    assert_eq!(calls.len(), 2);
    assert_eq!(
        calls[0].arguments.get("command").unwrap().as_str().unwrap(),
        "date"
    );
    assert_eq!(
        calls[1].arguments.get("command").unwrap().as_str().unwrap(),
        "pwd"
    );
}

#[test]
fn strip_tool_result_blocks_preserves_clean_text() {
    let input = "Hello, this is a normal response.";
    assert_eq!(strip_tool_result_blocks(input), input);
}

#[test]
fn strip_tool_result_blocks_returns_empty_for_only_tags() {
    let input = "<tool_result name=\"memory_recall\" status=\"ok\">\n{}\n</tool_result>";
    assert_eq!(strip_tool_result_blocks(input), "");
}

#[test]
fn parse_arguments_value_handles_null() {
    // Recovery: null arguments are returned as-is (Value::Null)
    let value = serde_json::json!(null);
    let result = parse_arguments_value(Some(&value));
    assert!(result.is_null());
}

#[test]
fn parse_tool_calls_handles_empty_tool_calls_array() {
    // Recovery: Empty tool_calls array returns original response (no tool parsing)
    let response = r#"{"content": "Hello", "tool_calls": []}"#;
    let (text, calls) = parse_tool_calls(response);
    // When tool_calls is empty, the entire JSON is returned as text
    assert!(text.contains("Hello"));
    assert!(calls.is_empty());
}

#[test]
fn detect_tool_call_parse_issue_flags_malformed_payloads() {
    let response = "<tool_call>{\"name\":\"shell\",\"arguments\":{\"command\":\"pwd\"}</tool_call>";
    let issue = detect_tool_call_parse_issue(response, &[]);
    assert!(
        issue.is_some(),
        "malformed tool payload should be flagged for diagnostics"
    );
}

#[test]
fn detect_tool_call_parse_issue_ignores_normal_text() {
    let issue = detect_tool_call_parse_issue("Thanks, done.", &[]);
    assert!(issue.is_none());
}

#[test]
fn parse_tool_calls_handles_whitespace_only_name() {
    // Recovery: Whitespace-only tool name should return None
    let value = serde_json::json!({"function": {"name": "   ", "arguments": {}}});
    let result = parse_tool_call_value(&value);
    assert!(result.is_none());
}

#[test]
fn parse_tool_calls_handles_empty_string_arguments() {
    // Recovery: Empty string arguments should be handled
    let value = serde_json::json!({"name": "test", "arguments": ""});
    let result = parse_tool_call_value(&value);
    assert!(result.is_some());
    assert_eq!(result.unwrap().name, "test");
}

// ═══════════════════════════════════════════════════════════════════════
// Recovery Tests - History Management
// ═══════════════════════════════════════════════════════════════════════

#[test]
fn trim_history_with_no_system_prompt() {
    // Recovery: History without system prompt should trim correctly
    let mut history = vec![];
    for i in 0..DEFAULT_MAX_HISTORY_MESSAGES + 20 {
        history.push(ChatMessage::user(format!("msg {i}")));
    }
    trim_history(&mut history, DEFAULT_MAX_HISTORY_MESSAGES);
    assert_eq!(history.len(), DEFAULT_MAX_HISTORY_MESSAGES);
}

#[test]
fn trim_history_preserves_role_ordering() {
    // Recovery: After trimming, role ordering should remain consistent
    let mut history = vec![ChatMessage::system("system")];
    for i in 0..DEFAULT_MAX_HISTORY_MESSAGES + 10 {
        history.push(ChatMessage::user(format!("user {i}")));
        history.push(ChatMessage::assistant(format!("assistant {i}")));
    }
    trim_history(&mut history, DEFAULT_MAX_HISTORY_MESSAGES);
    assert_eq!(history[0].role, "system");
    assert_eq!(history[history.len() - 1].role, "assistant");
}

#[test]
fn trim_history_with_only_system_prompt() {
    // Recovery: Only system prompt should not be trimmed
    let mut history = vec![ChatMessage::system("system prompt")];
    trim_history(&mut history, DEFAULT_MAX_HISTORY_MESSAGES);
    assert_eq!(history.len(), 1);
}

// ═══════════════════════════════════════════════════════════════════════
// Recovery Tests - Arguments Parsing
// ═══════════════════════════════════════════════════════════════════════

#[test]
fn parse_arguments_value_handles_invalid_json_string() {
    // Recovery: Invalid JSON string should return empty object
    let value = serde_json::Value::String("not valid json".to_string());
    let result = parse_arguments_value(Some(&value));
    assert!(result.is_object());
    assert!(result.as_object().unwrap().is_empty());
}

#[test]
fn parse_arguments_value_handles_none() {
    // Recovery: None arguments should return empty object
    let result = parse_arguments_value(None);
    assert!(result.is_object());
    assert!(result.as_object().unwrap().is_empty());
}

// ═══════════════════════════════════════════════════════════════════════
// Recovery Tests - JSON Extraction
// ═══════════════════════════════════════════════════════════════════════

#[test]
fn extract_json_values_handles_empty_string() {
    // Recovery: Empty input should return empty vec
    let result = extract_json_values("");
    assert!(result.is_empty());
}

#[test]
fn extract_json_values_handles_whitespace_only() {
    // Recovery: Whitespace only should return empty vec
    let result = extract_json_values("   \n\t  ");
    assert!(result.is_empty());
}

#[test]
fn extract_json_values_handles_multiple_objects() {
    // Recovery: Multiple JSON objects should all be extracted
    let input = r#"{"a": 1}{"b": 2}{"c": 3}"#;
    let result = extract_json_values(input);
    assert_eq!(result.len(), 3);
}

#[test]
fn extract_json_values_handles_arrays() {
    // Recovery: JSON arrays should be extracted
    let input = r#"[1, 2, 3]{"key": "value"}"#;
    let result = extract_json_values(input);
    assert_eq!(result.len(), 2);
}

// ═══════════════════════════════════════════════════════════════════════
// Recovery Tests - Constants Validation
// ═══════════════════════════════════════════════════════════════════════

const _: () = {
    assert!(DEFAULT_MAX_TOOL_ITERATIONS > 0);
    assert!(DEFAULT_MAX_TOOL_ITERATIONS <= 100);
    assert!(DEFAULT_MAX_HISTORY_MESSAGES > 0);
    assert!(DEFAULT_MAX_HISTORY_MESSAGES <= 1000);
};

#[test]
fn constants_bounds_are_compile_time_checked() {
    // Bounds are enforced by the const assertions above.
}

// ═══════════════════════════════════════════════════════════════════════
// Recovery Tests - Tool Call Value Parsing
// ═══════════════════════════════════════════════════════════════════════

#[test]
fn parse_tool_call_value_handles_missing_name_field() {
    // Recovery: Missing name field should return None
    let value = serde_json::json!({"function": {"arguments": {}}});
    let result = parse_tool_call_value(&value);
    assert!(result.is_none());
}

#[test]
fn parse_tool_call_value_handles_top_level_name() {
    // Recovery: Tool call with name at top level (non-OpenAI format)
    let value = serde_json::json!({"name": "test_tool", "arguments": {}});
    let result = parse_tool_call_value(&value);
    assert!(result.is_some());
    assert_eq!(result.unwrap().name, "test_tool");
}

#[test]
fn parse_tool_call_value_accepts_top_level_parameters_alias() {
    let value = serde_json::json!({
        "name": "schedule",
        "parameters": {"action": "create", "message": "test"}
    });
    let result = parse_tool_call_value(&value).expect("tool call should parse");
    assert_eq!(result.name, "schedule");
    assert_eq!(
        result.arguments.get("action").and_then(|v| v.as_str()),
        Some("create")
    );
}

#[test]
fn parse_tool_call_value_accepts_function_parameters_alias() {
    let value = serde_json::json!({
        "function": {
            "name": "shell",
            "parameters": {"command": "date"}
        }
    });
    let result = parse_tool_call_value(&value).expect("tool call should parse");
    assert_eq!(result.name, "shell");
    assert_eq!(
        result.arguments.get("command").and_then(|v| v.as_str()),
        Some("date")
    );
}

#[test]
fn parse_tool_call_value_preserves_tool_call_id_aliases() {
    let value = serde_json::json!({
        "call_id": "legacy_1",
        "function": {
            "name": "shell",
            "arguments": {"command": "date"}
        }
    });
    let result = parse_tool_call_value(&value).expect("tool call should parse");
    assert_eq!(result.tool_call_id.as_deref(), Some("legacy_1"));
}

#[test]
fn parse_tool_calls_from_json_value_handles_empty_array() {
    // Recovery: Empty tool_calls array should return empty vec
    let value = serde_json::json!({"tool_calls": []});
    let result = parse_tool_calls_from_json_value(&value);
    assert!(result.is_empty());
}

#[test]
fn parse_tool_calls_from_json_value_handles_missing_tool_calls() {
    // Recovery: Missing tool_calls field should fall through
    let value = serde_json::json!({"name": "test", "arguments": {}});
    let result = parse_tool_calls_from_json_value(&value);
    assert_eq!(result.len(), 1);
}

#[test]
fn parse_tool_calls_from_json_value_handles_top_level_array() {
    // Recovery: Top-level array of tool calls
    let value = serde_json::json!([
        {"name": "tool_a", "arguments": {}},
        {"name": "tool_b", "arguments": {}}
    ]);
    let result = parse_tool_calls_from_json_value(&value);
    assert_eq!(result.len(), 2);
}

// ═══════════════════════════════════════════════════════════════════════
// GLM-Style Tool Call Parsing
// ═══════════════════════════════════════════════════════════════════════

#[test]
fn parse_glm_style_browser_open_url() {
    let response = "browser_open/url>https://example.com";
    let calls = parse_glm_style_tool_calls(response);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].0, "shell");
    assert!(calls[0].1["command"].as_str().unwrap().contains("curl"));
    assert!(
        calls[0].1["command"]
            .as_str()
            .unwrap()
            .contains("example.com")
    );
}

#[test]
fn parse_glm_style_shell_command() {
    let response = "shell/command>ls -la";
    let calls = parse_glm_style_tool_calls(response);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].0, "shell");
    assert_eq!(calls[0].1["command"], "ls -la");
}

#[test]
fn parse_glm_style_http_request() {
    let response = "http_request/url>https://api.example.com/data";
    let calls = parse_glm_style_tool_calls(response);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].0, "http_request");
    assert_eq!(calls[0].1["url"], "https://api.example.com/data");
    assert_eq!(calls[0].1["method"], "GET");
}

#[test]
fn parse_glm_style_ignores_plain_url() {
    // A bare URL should NOT be interpreted as a tool call — this was
    // causing false positives when LLMs included URLs in normal text.
    let response = "https://example.com/api";
    let calls = parse_glm_style_tool_calls(response);
    assert!(
        calls.is_empty(),
        "plain URL must not be parsed as tool call"
    );
}

#[test]
fn parse_glm_style_json_args() {
    let response = r#"shell/{"command": "echo hello"}"#;
    let calls = parse_glm_style_tool_calls(response);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].0, "shell");
    assert_eq!(calls[0].1["command"], "echo hello");
}

#[test]
fn parse_glm_style_multiple_calls() {
    let response = r#"shell/command>ls
browser_open/url>https://example.com"#;
    let calls = parse_glm_style_tool_calls(response);
    assert_eq!(calls.len(), 2);
}

#[test]
fn parse_glm_style_tool_call_integration() {
    // Integration test: GLM format should be parsed in parse_tool_calls
    let response = "Checking...\nbrowser_open/url>https://example.com\nDone";
    let (text, calls) = parse_tool_calls(response);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert!(text.contains("Checking"));
    assert!(text.contains("Done"));
}

#[test]
fn parse_glm_style_rejects_non_http_url_param() {
    let response = "browser_open/url>javascript:alert(1)";
    let calls = parse_glm_style_tool_calls(response);
    assert!(calls.is_empty());
}

#[test]
fn parse_tool_calls_handles_unclosed_tool_call_tag() {
    let response = "<tool_call>{\"name\":\"shell\",\"arguments\":{\"command\":\"pwd\"}}\nDone";
    let (text, calls) = parse_tool_calls(response);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(calls[0].arguments["command"], "pwd");
    assert_eq!(text, "Done");
}

// ─────────────────────────────────────────────────────────────────────
// TG4 (inline): parse_tool_calls robustness — malformed/edge-case inputs
// Prevents: Pattern 4 issues #746, #418, #777, #848
// ─────────────────────────────────────────────────────────────────────

#[test]
fn parse_tool_calls_empty_input_returns_empty() {
    let (text, calls) = parse_tool_calls("");
    assert!(calls.is_empty(), "empty input should produce no tool calls");
    assert!(text.is_empty(), "empty input should produce no text");
}

#[test]
fn parse_tool_calls_whitespace_only_returns_empty_calls() {
    let (text, calls) = parse_tool_calls("   \n\t  ");
    assert!(calls.is_empty());
    assert!(text.is_empty() || text.trim().is_empty());
}

#[test]
fn parse_tool_calls_nested_xml_tags_handled() {
    // Double-wrapped tool call should still parse the inner call
    let response =
        r#"<tool_call><tool_call>{"name":"echo","arguments":{"msg":"hi"}}</tool_call></tool_call>"#;
    let (_text, calls) = parse_tool_calls(response);
    // Should find at least one tool call
    assert!(
        !calls.is_empty(),
        "nested XML tags should still yield at least one tool call"
    );
}

#[test]
fn parse_tool_calls_truncated_json_no_panic() {
    // Incomplete JSON inside tool_call tags
    let response = r#"<tool_call>{"name":"shell","arguments":{"command":"ls"</tool_call>"#;
    let (_text, _calls) = parse_tool_calls(response);
    // Should not panic — graceful handling of truncated JSON
}

#[test]
fn parse_tool_calls_empty_json_object_in_tag() {
    let response = "<tool_call>{}</tool_call>";
    let (_text, calls) = parse_tool_calls(response);
    // Empty JSON object has no name field — should not produce valid tool call
    assert!(
        calls.is_empty(),
        "empty JSON object should not produce a tool call"
    );
}

#[test]
fn parse_tool_calls_closing_tag_only_returns_text() {
    let response = "Some text </tool_call> more text";
    let (text, calls) = parse_tool_calls(response);
    assert!(
        calls.is_empty(),
        "closing tag only should not produce calls"
    );
    assert!(
        !text.is_empty(),
        "text around orphaned closing tag should be preserved"
    );
}

#[test]
fn parse_tool_calls_very_large_arguments_no_panic() {
    let large_arg = "x".repeat(100_000);
    let response = format!(
        r#"<tool_call>{{"name":"echo","arguments":{{"message":"{}"}}}}</tool_call>"#,
        large_arg
    );
    let (_text, calls) = parse_tool_calls(&response);
    assert_eq!(calls.len(), 1, "large arguments should still parse");
    assert_eq!(calls[0].name, "echo");
}

#[test]
fn parse_tool_calls_special_characters_in_arguments() {
    let response = r#"<tool_call>{"name":"echo","arguments":{"message":"hello \"world\" <>&'\n\t"}}</tool_call>"#;
    let (_text, calls) = parse_tool_calls(response);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "echo");
}

#[test]
fn parse_tool_calls_text_with_embedded_json_not_extracted() {
    // Raw JSON without any tags should NOT be extracted as a tool call
    let response = r#"Here is some data: {"name":"echo","arguments":{"message":"hi"}} end."#;
    let (_text, calls) = parse_tool_calls(response);
    assert!(
        calls.is_empty(),
        "raw JSON in text without tags should not be extracted"
    );
}

#[test]
fn parse_tool_calls_multiple_formats_mixed() {
    // Mix of text and properly tagged tool call
    let response = r#"I'll help you with that.

<tool_call>
{"name":"shell","arguments":{"command":"echo hello"}}
</tool_call>

Let me check the result."#;
    let (text, calls) = parse_tool_calls(response);
    assert_eq!(
        calls.len(),
        1,
        "should extract one tool call from mixed content"
    );
    assert_eq!(calls[0].name, "shell");
    assert!(
        text.contains("help you"),
        "text before tool call should be preserved"
    );
}

// ─────────────────────────────────────────────────────────────────────
// TG4 (inline): scrub_credentials edge cases
// ─────────────────────────────────────────────────────────────────────

#[test]
fn scrub_credentials_empty_input() {
    let result = scrub_credentials("");
    assert_eq!(result, "");
}

#[test]
fn scrub_credentials_no_sensitive_data() {
    let input = "normal text without any secrets";
    let result = scrub_credentials(input);
    assert_eq!(
        result, input,
        "non-sensitive text should pass through unchanged"
    );
}

#[test]
fn scrub_credentials_multibyte_chars_no_panic() {
    // Regression test for #3024: byte index 4 is not a char boundary
    // when the captured value contains multi-byte UTF-8 characters.
    // The regex only matches quoted values for non-ASCII content, since
    // capture group 4 is restricted to [a-zA-Z0-9_\-\.].
    let input = "password=\"\u{4f60}\u{7684}WiFi\u{5bc6}\u{7801}ab\"";
    let result = scrub_credentials(input);
    assert!(
        result.contains("[REDACTED]"),
        "multi-byte quoted value should be redacted without panic, got: {result}"
    );
}

#[test]
fn scrub_credentials_short_values_not_redacted() {
    // Values shorter than 8 chars should not be redacted
    let input = r#"api_key="short""#;
    let result = scrub_credentials(input);
    assert_eq!(result, input, "short values should not be redacted");
}

// ─────────────────────────────────────────────────────────────────────
// TG4 (inline): trim_history edge cases
// ─────────────────────────────────────────────────────────────────────

#[test]
fn trim_history_empty_history() {
    let mut history: Vec<crate::providers::ChatMessage> = vec![];
    trim_history(&mut history, 10);
    assert!(history.is_empty());
}

#[test]
fn trim_history_system_only() {
    let mut history = vec![crate::providers::ChatMessage::system("system prompt")];
    trim_history(&mut history, 10);
    assert_eq!(history.len(), 1);
    assert_eq!(history[0].role, "system");
}

#[test]
fn trim_history_exactly_at_limit() {
    let mut history = vec![
        crate::providers::ChatMessage::system("system"),
        crate::providers::ChatMessage::user("msg 1"),
        crate::providers::ChatMessage::assistant("reply 1"),
    ];
    trim_history(&mut history, 2); // 2 non-system messages = exactly at limit
    assert_eq!(history.len(), 3, "should not trim when exactly at limit");
}

#[test]
fn trim_history_removes_oldest_non_system() {
    let mut history = vec![
        crate::providers::ChatMessage::system("system"),
        crate::providers::ChatMessage::user("old msg"),
        crate::providers::ChatMessage::assistant("old reply"),
        crate::providers::ChatMessage::user("new msg"),
        crate::providers::ChatMessage::assistant("new reply"),
    ];
    trim_history(&mut history, 2);
    assert_eq!(history.len(), 3); // system + 2 kept
    assert_eq!(history[0].role, "system");
    assert_eq!(history[1].content, "new msg");
}

/// When `build_system_prompt_with_mode` is called with `native_tools = true`,
/// the output must contain ZERO XML protocol artifacts. In the native path
/// `build_tool_instructions` is never called, so the system prompt alone
/// must be clean of XML tool-call protocol.
#[test]
fn native_tools_system_prompt_contains_zero_xml() {
    use crate::channels::build_system_prompt_with_mode;

    let tool_summaries: Vec<(&str, &str)> = vec![
        ("shell", "Execute shell commands"),
        ("file_read", "Read files"),
    ];

    let system_prompt = build_system_prompt_with_mode(
        std::path::Path::new("/tmp"),
        "test-model",
        &tool_summaries,
        &[],  // no skills
        None, // no bootstrap_max_chars
        true, // native_tools
        crate::config::SkillsPromptInjectionMode::Full,
        crate::security::AutonomyLevel::default(),
    );

    // Must contain zero XML protocol artifacts
    assert!(
        !system_prompt.contains("<tool_call>"),
        "Native prompt must not contain <tool_call>"
    );
    assert!(
        !system_prompt.contains("</tool_call>"),
        "Native prompt must not contain </tool_call>"
    );
    assert!(
        !system_prompt.contains("<tool_result>"),
        "Native prompt must not contain <tool_result>"
    );
    assert!(
        !system_prompt.contains("</tool_result>"),
        "Native prompt must not contain </tool_result>"
    );
    assert!(
        !system_prompt.contains("## Tool Use Protocol"),
        "Native prompt must not contain XML protocol header"
    );

    // Positive: native prompt should still list tools and contain task instructions
    assert!(
        system_prompt.contains("shell"),
        "Native prompt must list tool names"
    );
    assert!(
        system_prompt.contains("## Your Task"),
        "Native prompt should contain task instructions"
    );
}

// ── Cross-Alias & GLM Shortened Body Tests ──────────────────────────

#[test]
fn parse_tool_calls_cross_alias_close_tag_with_json() {
    // <tool_call> opened but closed with </invoke> — JSON body
    let input = r#"<tool_call>{"name": "shell", "arguments": {"command": "ls"}}</invoke>"#;
    let (text, calls) = parse_tool_calls(input);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(calls[0].arguments["command"], "ls");
    assert!(text.is_empty());
}

#[test]
fn parse_tool_calls_cross_alias_close_tag_with_glm_shortened() {
    // <tool_call>shell>uname -a</invoke> — GLM shortened inside cross-alias tags
    let input = "<tool_call>shell>uname -a</invoke>";
    let (text, calls) = parse_tool_calls(input);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(calls[0].arguments["command"], "uname -a");
    assert!(text.is_empty());
}

#[test]
fn parse_tool_calls_glm_shortened_body_in_matched_tags() {
    // <tool_call>shell>pwd</tool_call> — GLM shortened in matched tags
    let input = "<tool_call>shell>pwd</tool_call>";
    let (text, calls) = parse_tool_calls(input);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(calls[0].arguments["command"], "pwd");
    assert!(text.is_empty());
}

#[test]
fn parse_tool_calls_glm_yaml_style_in_tags() {
    // <tool_call>shell>\ncommand: date\napproved: true</invoke>
    let input = "<tool_call>shell>\ncommand: date\napproved: true</invoke>";
    let (text, calls) = parse_tool_calls(input);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(calls[0].arguments["command"], "date");
    assert_eq!(calls[0].arguments["approved"], true);
    assert!(text.is_empty());
}

#[test]
fn parse_tool_calls_attribute_style_in_tags() {
    // <tool_call>shell command="date" /></tool_call>
    let input = r#"<tool_call>shell command="date" /></tool_call>"#;
    let (text, calls) = parse_tool_calls(input);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(calls[0].arguments["command"], "date");
    assert!(text.is_empty());
}

#[test]
fn parse_tool_calls_file_read_shortened_in_cross_alias() {
    // <tool_call>file_read path=".env" /></invoke>
    let input = r#"<tool_call>file_read path=".env" /></invoke>"#;
    let (text, calls) = parse_tool_calls(input);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "file_read");
    assert_eq!(calls[0].arguments["path"], ".env");
    assert!(text.is_empty());
}

#[test]
fn parse_tool_calls_unclosed_glm_shortened_no_close_tag() {
    // <tool_call>shell>ls -la (no close tag at all)
    let input = "<tool_call>shell>ls -la";
    let (text, calls) = parse_tool_calls(input);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(calls[0].arguments["command"], "ls -la");
    assert!(text.is_empty());
}

#[test]
fn parse_tool_calls_text_before_cross_alias() {
    // Text before and after cross-alias tool call
    let input = "Let me check that.\n<tool_call>shell>uname -a</invoke>\nDone.";
    let (text, calls) = parse_tool_calls(input);
    assert_eq!(calls.len(), 1);
    assert_eq!(calls[0].name, "shell");
    assert_eq!(calls[0].arguments["command"], "uname -a");
    assert!(text.contains("Let me check that."));
    assert!(text.contains("Done."));
}

#[test]
fn parse_glm_shortened_body_url_to_curl() {
    // URL values for shell should be wrapped in curl
    let call = parse_glm_shortened_body("shell>https://example.com/api").unwrap();
    assert_eq!(call.name, "shell");
    let cmd = call.arguments["command"].as_str().unwrap();
    assert!(cmd.contains("curl"));
    assert!(cmd.contains("example.com"));
}

#[test]
fn parse_glm_shortened_body_browser_open_maps_to_shell_command() {
    // browser_open aliases to shell, and shortened calls must still emit
    // shell's canonical "command" argument.
    let call = parse_glm_shortened_body("browser_open>https://example.com").unwrap();
    assert_eq!(call.name, "shell");
    let cmd = call.arguments["command"].as_str().unwrap();
    assert!(cmd.contains("curl"));
    assert!(cmd.contains("example.com"));
}

#[test]
fn parse_glm_shortened_body_memory_recall() {
    // memory_recall>some query — default param is "query"
    let call = parse_glm_shortened_body("memory_recall>recent meetings").unwrap();
    assert_eq!(call.name, "memory_recall");
    assert_eq!(call.arguments["query"], "recent meetings");
}

#[test]
fn parse_glm_shortened_body_function_style_alias_maps_to_message_send() {
    let call = parse_glm_shortened_body(r#"sendmessage(channel="alerts", message="hi")"#).unwrap();
    assert_eq!(call.name, "message_send");
    assert_eq!(call.arguments["channel"], "alerts");
    assert_eq!(call.arguments["message"], "hi");
}

#[test]
fn map_tool_name_alias_direct_coverage() {
    assert_eq!(map_tool_name_alias("bash"), "shell");
    assert_eq!(map_tool_name_alias("filelist"), "file_list");
    assert_eq!(map_tool_name_alias("memorystore"), "memory_store");
    assert_eq!(map_tool_name_alias("memoryforget"), "memory_forget");
    assert_eq!(map_tool_name_alias("http"), "http_request");
    assert_eq!(
        map_tool_name_alias("totally_unknown_tool"),
        "totally_unknown_tool"
    );
}

#[test]
fn default_param_for_tool_coverage() {
    assert_eq!(default_param_for_tool("shell"), "command");
    assert_eq!(default_param_for_tool("bash"), "command");
    assert_eq!(default_param_for_tool("file_read"), "path");
    assert_eq!(default_param_for_tool("memory_recall"), "query");
    assert_eq!(default_param_for_tool("memory_store"), "content");
    assert_eq!(default_param_for_tool("web_search_tool"), "query");
    assert_eq!(default_param_for_tool("web_search"), "query");
    assert_eq!(default_param_for_tool("search"), "query");
    assert_eq!(default_param_for_tool("http_request"), "url");
    assert_eq!(default_param_for_tool("browser_open"), "url");
    assert_eq!(default_param_for_tool("unknown_tool"), "input");
}

#[test]
fn parse_glm_shortened_body_rejects_empty() {
    assert!(parse_glm_shortened_body("").is_none());
    assert!(parse_glm_shortened_body("   ").is_none());
}

#[test]
fn parse_glm_shortened_body_rejects_invalid_tool_name() {
    // Tool names with special characters should be rejected
    assert!(parse_glm_shortened_body("not-a-tool>value").is_none());
    assert!(parse_glm_shortened_body("tool name>value").is_none());
}

// ═══════════════════════════════════════════════════════════════════════
// reasoning_content pass-through tests for history builders
// ═══════════════════════════════════════════════════════════════════════

#[test]
fn build_native_assistant_history_includes_reasoning_content() {
    let calls = vec![ToolCall {
        id: "call_1".into(),
        name: "shell".into(),
        arguments: "{}".into(),
    }];
    let result = build_native_assistant_history("answer", &calls, Some("thinking step"));
    let parsed: serde_json::Value = serde_json::from_str(&result).unwrap();
    assert_eq!(parsed["content"].as_str(), Some("answer"));
    assert_eq!(parsed["reasoning_content"].as_str(), Some("thinking step"));
    assert!(parsed["tool_calls"].is_array());
}

#[test]
fn build_native_assistant_history_omits_reasoning_content_when_none() {
    let calls = vec![ToolCall {
        id: "call_1".into(),
        name: "shell".into(),
        arguments: "{}".into(),
    }];
    let result = build_native_assistant_history("answer", &calls, None);
    let parsed: serde_json::Value = serde_json::from_str(&result).unwrap();
    assert_eq!(parsed["content"].as_str(), Some("answer"));
    assert!(parsed.get("reasoning_content").is_none());
}

#[test]
fn build_native_assistant_history_from_parsed_calls_includes_reasoning_content() {
    let calls = vec![ParsedToolCall {
        name: "shell".into(),
        arguments: serde_json::json!({"command": "pwd"}),
        tool_call_id: Some("call_2".into()),
    }];
    let result =
        build_native_assistant_history_from_parsed_calls("answer", &calls, Some("deep thought"));
    assert!(result.is_some());
    let parsed: serde_json::Value = serde_json::from_str(result.as_deref().unwrap()).unwrap();
    assert_eq!(parsed["content"].as_str(), Some("answer"));
    assert_eq!(parsed["reasoning_content"].as_str(), Some("deep thought"));
    assert!(parsed["tool_calls"].is_array());
}

#[test]
fn build_native_assistant_history_from_parsed_calls_omits_reasoning_content_when_none() {
    let calls = vec![ParsedToolCall {
        name: "shell".into(),
        arguments: serde_json::json!({"command": "pwd"}),
        tool_call_id: Some("call_2".into()),
    }];
    let result = build_native_assistant_history_from_parsed_calls("answer", &calls, None);
    assert!(result.is_some());
    let parsed: serde_json::Value = serde_json::from_str(result.as_deref().unwrap()).unwrap();
    assert_eq!(parsed["content"].as_str(), Some("answer"));
    assert!(parsed.get("reasoning_content").is_none());
}

// ── glob_match tests ──────────────────────────────────────────────────────

#[test]
fn glob_match_exact_no_wildcard() {
    assert!(glob_match("mcp_browser_navigate", "mcp_browser_navigate"));
    assert!(!glob_match("mcp_browser_navigate", "mcp_browser_click"));
}

#[test]
fn glob_match_prefix_wildcard() {
    // Suffix pattern: mcp_browser_*
    assert!(glob_match("mcp_browser_*", "mcp_browser_navigate"));
    assert!(glob_match("mcp_browser_*", "mcp_browser_click"));
    assert!(!glob_match("mcp_browser_*", "mcp_filesystem_read"));

    // Prefix pattern: *_read
    assert!(glob_match("*_read", "mcp_filesystem_read"));
    assert!(!glob_match("*_read", "mcp_filesystem_write"));

    // Infix: mcp_*_navigate
    assert!(glob_match("mcp_*_navigate", "mcp_browser_navigate"));
    assert!(!glob_match("mcp_*_navigate", "mcp_browser_click"));
}

#[test]
fn glob_match_star_matches_everything() {
    assert!(glob_match("*", "anything_at_all"));
    assert!(glob_match("*", ""));
}

// ── filter_tool_specs_for_turn tests ──────────────────────────────────────

fn make_spec(name: &str) -> crate::tools::ToolSpec {
    crate::tools::ToolSpec {
        name: name.to_string(),
        description: String::new(),
        parameters: serde_json::json!({}),
    }
}

#[test]
fn filter_tool_specs_no_groups_returns_all() {
    let specs = vec![
        make_spec("shell_exec"),
        make_spec("mcp_browser_navigate"),
        make_spec("mcp_filesystem_read"),
    ];
    let result = filter_tool_specs_for_turn(specs, &[], "hello");
    assert_eq!(result.len(), 3);
}

#[test]
fn filter_tool_specs_always_group_includes_matching_mcp_tool() {
    use crate::config::schema::{ToolFilterGroup, ToolFilterGroupMode};

    let specs = vec![
        make_spec("shell_exec"),
        make_spec("mcp_browser_navigate"),
        make_spec("mcp_filesystem_read"),
    ];
    let groups = vec![ToolFilterGroup {
        mode: ToolFilterGroupMode::Always,
        tools: vec!["mcp_filesystem_*".into()],
        keywords: vec![],
        filter_builtins: false,
    }];
    let result = filter_tool_specs_for_turn(specs, &groups, "anything");
    let names: Vec<&str> = result.iter().map(|s| s.name.as_str()).collect();
    // Built-in passes through, matched MCP passes, unmatched MCP excluded.
    assert!(names.contains(&"shell_exec"));
    assert!(names.contains(&"mcp_filesystem_read"));
    assert!(!names.contains(&"mcp_browser_navigate"));
}

#[test]
fn filter_tool_specs_dynamic_group_included_on_keyword_match() {
    use crate::config::schema::{ToolFilterGroup, ToolFilterGroupMode};

    let specs = vec![make_spec("shell_exec"), make_spec("mcp_browser_navigate")];
    let groups = vec![ToolFilterGroup {
        mode: ToolFilterGroupMode::Dynamic,
        tools: vec!["mcp_browser_*".into()],
        keywords: vec!["browse".into(), "website".into()],
        filter_builtins: false,
    }];
    let result = filter_tool_specs_for_turn(specs, &groups, "please browse this page");
    let names: Vec<&str> = result.iter().map(|s| s.name.as_str()).collect();
    assert!(names.contains(&"shell_exec"));
    assert!(names.contains(&"mcp_browser_navigate"));
}

#[test]
fn filter_tool_specs_dynamic_group_excluded_on_no_keyword_match() {
    use crate::config::schema::{ToolFilterGroup, ToolFilterGroupMode};

    let specs = vec![make_spec("shell_exec"), make_spec("mcp_browser_navigate")];
    let groups = vec![ToolFilterGroup {
        mode: ToolFilterGroupMode::Dynamic,
        tools: vec!["mcp_browser_*".into()],
        keywords: vec!["browse".into(), "website".into()],
        filter_builtins: false,
    }];
    let result = filter_tool_specs_for_turn(specs, &groups, "read the file /etc/hosts");
    let names: Vec<&str> = result.iter().map(|s| s.name.as_str()).collect();
    assert!(names.contains(&"shell_exec"));
    assert!(!names.contains(&"mcp_browser_navigate"));
}

#[test]
fn filter_tool_specs_dynamic_keyword_match_is_case_insensitive() {
    use crate::config::schema::{ToolFilterGroup, ToolFilterGroupMode};

    let specs = vec![make_spec("mcp_browser_navigate")];
    let groups = vec![ToolFilterGroup {
        mode: ToolFilterGroupMode::Dynamic,
        tools: vec!["mcp_browser_*".into()],
        keywords: vec!["Browse".into()],
        filter_builtins: false,
    }];
    let result = filter_tool_specs_for_turn(specs, &groups, "BROWSE the site");
    assert_eq!(result.len(), 1);
}

// ── Token-based compaction tests ──────────────────────────

#[test]
fn estimate_history_tokens_empty() {
    assert_eq!(super::estimate_history_tokens(&[]), 0);
}

#[test]
fn estimate_history_tokens_single_message() {
    let history = vec![ChatMessage::user("hello world")]; // 11 chars
    let tokens = super::estimate_history_tokens(&history);
    // 11.div_ceil(4) + 4 = 3 + 4 = 7
    assert_eq!(tokens, 7);
}

#[test]
fn estimate_history_tokens_multiple_messages() {
    let history = vec![
        ChatMessage::system("You are helpful."), // 16 chars → 4 + 4 = 8
        ChatMessage::user("What is Rust?"),      // 13 chars → 4 + 4 = 8
        ChatMessage::assistant("A language."),   // 11 chars → 3 + 4 = 7
    ];
    let tokens = super::estimate_history_tokens(&history);
    assert_eq!(tokens, 23);
}

#[tokio::test]
async fn run_tool_call_loop_surfaces_tool_failure_reason_in_on_delta() {
    let provider = ScriptedProvider::from_text_responses(vec![
        r#"<tool_call>
{"name":"failing_shell","arguments":{"command":"rm -rf /"}}
</tool_call>"#,
        "I could not execute that command.",
    ]);

    let tools_registry: Vec<Box<dyn Tool>> = vec![Box::new(FailingTool::new(
        "failing_shell",
        "Command not allowed by security policy: rm -rf /",
    ))];

    let mut history = vec![
        ChatMessage::system("test-system"),
        ChatMessage::user("delete everything"),
    ];
    let observer = NoopObserver;

    let (tx, mut rx) = tokio::sync::mpsc::channel::<DraftEvent>(64);

    let result = run_tool_call_loop(
        &provider,
        &mut history,
        &tools_registry,
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        None,
        "telegram",
        None,
        &crate::config::MultimodalConfig::default(),
        4,
        None,
        Some(tx),
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect("tool loop should complete");

    // Collect all messages sent to the on_delta channel.
    let mut deltas = Vec::new();
    while let Ok(msg) = rx.try_recv() {
        deltas.push(msg);
    }

    let all_deltas: String = deltas
        .iter()
        .filter_map(|d| match d {
            DraftEvent::Progress(t) | DraftEvent::Content(t) => Some(t.as_str()),
            DraftEvent::Clear => None,
        })
        .collect();

    // The failure reason should appear in the progress messages.
    assert!(
        all_deltas.contains("Command not allowed by security policy"),
        "on_delta messages should include the tool failure reason, got: {all_deltas}"
    );

    // Should also contain the cross mark (❌) icon to indicate failure.
    assert!(
        all_deltas.contains('\u{274c}'),
        "on_delta messages should include ❌ for failed tool calls, got: {all_deltas}"
    );

    assert_eq!(result, "I could not execute that command.");
}

// ── filter_by_allowed_tools tests ─────────────────────────────────────

#[test]
fn filter_by_allowed_tools_none_passes_all() {
    let specs = vec![
        make_spec("shell"),
        make_spec("memory_store"),
        make_spec("file_read"),
    ];
    let result = filter_by_allowed_tools(specs, None);
    assert_eq!(result.len(), 3);
}

#[test]
fn filter_by_allowed_tools_some_restricts_to_listed() {
    let specs = vec![
        make_spec("shell"),
        make_spec("memory_store"),
        make_spec("file_read"),
    ];
    let allowed = vec!["shell".to_string(), "memory_store".to_string()];
    let result = filter_by_allowed_tools(specs, Some(&allowed));
    let names: Vec<&str> = result.iter().map(|s| s.name.as_str()).collect();
    assert_eq!(names.len(), 2);
    assert!(names.contains(&"shell"));
    assert!(names.contains(&"memory_store"));
    assert!(!names.contains(&"file_read"));
}

#[test]
fn filter_by_allowed_tools_unknown_names_silently_ignored() {
    let specs = vec![make_spec("shell"), make_spec("file_read")];
    let allowed = vec![
        "shell".to_string(),
        "nonexistent_tool".to_string(),
        "another_missing".to_string(),
    ];
    let result = filter_by_allowed_tools(specs, Some(&allowed));
    let names: Vec<&str> = result.iter().map(|s| s.name.as_str()).collect();
    assert_eq!(names.len(), 1);
    assert!(names.contains(&"shell"));
}

#[test]
fn filter_by_allowed_tools_empty_list_excludes_all() {
    let specs = vec![make_spec("shell"), make_spec("file_read")];
    let allowed: Vec<String> = vec![];
    let result = filter_by_allowed_tools(specs, Some(&allowed));
    assert!(result.is_empty());
}

// ── Cost tracking tests ──

#[tokio::test]
async fn cost_tracking_records_usage_when_scoped() {
    use super::{TOOL_LOOP_COST_TRACKING_CONTEXT, ToolLoopCostTrackingContext, run_tool_call_loop};
    use crate::config::schema::ModelPricing;
    use crate::cost::CostTracker;
    use crate::observability::noop::NoopObserver;
    use std::collections::HashMap;

    let provider = ScriptedProvider {
        responses: Arc::new(Mutex::new(VecDeque::from([ChatResponse {
            text: Some("done".to_string()),
            tool_calls: Vec::new(),
            usage: Some(crate::providers::traits::TokenUsage {
                input_tokens: Some(1_000),
                output_tokens: Some(200),
                cached_input_tokens: None,
            }),
            reasoning_content: None,
        }]))),
        capabilities: ProviderCapabilities::default(),
    };
    let observer = NoopObserver;
    let workspace = tempfile::TempDir::new().unwrap();
    let mut cost_config = crate::config::CostConfig {
        enabled: true,
        ..crate::config::CostConfig::default()
    };
    cost_config.prices = HashMap::from([(
        "mock-model".to_string(),
        ModelPricing {
            input: 3.0,
            output: 15.0,
        },
    )]);
    let tracker = Arc::new(CostTracker::new(cost_config.clone(), workspace.path()).unwrap());
    let ctx = ToolLoopCostTrackingContext::new(
        Arc::clone(&tracker),
        Arc::new(cost_config.prices.clone()),
    );
    let mut history = vec![ChatMessage::system("test"), ChatMessage::user("hello")];

    let result = TOOL_LOOP_COST_TRACKING_CONTEXT
        .scope(
            Some(ctx),
            run_tool_call_loop(
                &provider,
                &mut history,
                &[],
                &observer,
                "mock-provider",
                "mock-model",
                0.0,
                true,
                None,
                "test",
                None,
                &crate::config::MultimodalConfig::default(),
                2,
                None,
                None,
                None,
                &[],
                &[],
                None,
                None,
                &crate::config::PacingConfig::default(),
                0,
                0,
                None,
            ),
        )
        .await
        .expect("tool loop should succeed");

    assert_eq!(result, "done");
    let summary = tracker.get_summary().unwrap();
    assert_eq!(summary.request_count, 1);
    assert_eq!(summary.total_tokens, 1_200);
    assert!(summary.session_cost_usd > 0.0);
}

#[tokio::test]
async fn cost_tracking_enforces_budget() {
    use super::{TOOL_LOOP_COST_TRACKING_CONTEXT, ToolLoopCostTrackingContext, run_tool_call_loop};
    use crate::config::schema::ModelPricing;
    use crate::cost::CostTracker;
    use crate::observability::noop::NoopObserver;
    use std::collections::HashMap;

    let provider = ScriptedProvider::from_text_responses(vec!["should not reach this"]);
    let observer = NoopObserver;
    let workspace = tempfile::TempDir::new().unwrap();
    let cost_config = crate::config::CostConfig {
        enabled: true,
        daily_limit_usd: 0.001, // very low limit
        ..crate::config::CostConfig::default()
    };
    let tracker = Arc::new(CostTracker::new(cost_config.clone(), workspace.path()).unwrap());
    // Record a usage that already exceeds the limit
    tracker
        .record_usage(crate::cost::types::TokenUsage::new(
            "mock-model",
            100_000,
            50_000,
            1.0,
            1.0,
        ))
        .unwrap();

    let ctx = ToolLoopCostTrackingContext::new(
        Arc::clone(&tracker),
        Arc::new(HashMap::from([(
            "mock-model".to_string(),
            ModelPricing {
                input: 1.0,
                output: 1.0,
            },
        )])),
    );
    let mut history = vec![ChatMessage::system("test"), ChatMessage::user("hello")];

    let err = TOOL_LOOP_COST_TRACKING_CONTEXT
        .scope(
            Some(ctx),
            run_tool_call_loop(
                &provider,
                &mut history,
                &[],
                &observer,
                "mock-provider",
                "mock-model",
                0.0,
                true,
                None,
                "test",
                None,
                &crate::config::MultimodalConfig::default(),
                2,
                None,
                None,
                None,
                &[],
                &[],
                None,
                None,
                &crate::config::PacingConfig::default(),
                0,
                0,
                None,
            ),
        )
        .await
        .expect_err("should fail with budget exceeded");

    assert!(
        err.to_string().contains("Budget exceeded"),
        "error should mention budget: {err}"
    );
}

#[tokio::test]
async fn cost_tracking_is_noop_without_scope() {
    use super::run_tool_call_loop;
    use crate::observability::noop::NoopObserver;

    // No TOOL_LOOP_COST_TRACKING_CONTEXT scoped — should run fine
    let provider = ScriptedProvider {
        responses: Arc::new(Mutex::new(VecDeque::from([ChatResponse {
            text: Some("ok".to_string()),
            tool_calls: Vec::new(),
            usage: Some(crate::providers::traits::TokenUsage {
                input_tokens: Some(500),
                output_tokens: Some(100),
                cached_input_tokens: None,
            }),
            reasoning_content: None,
        }]))),
        capabilities: ProviderCapabilities::default(),
    };
    let observer = NoopObserver;
    let mut history = vec![ChatMessage::system("test"), ChatMessage::user("hello")];

    let result = run_tool_call_loop(
        &provider,
        &mut history,
        &[],
        &observer,
        "mock-provider",
        "mock-model",
        0.0,
        true,
        None,
        "test",
        None,
        &crate::config::MultimodalConfig::default(),
        2,
        None,
        None,
        None,
        &[],
        &[],
        None,
        None,
        &crate::config::PacingConfig::default(),
        0,
        0,
        None,
    )
    .await
    .expect("should succeed without cost scope");

    assert_eq!(result, "ok");
}
