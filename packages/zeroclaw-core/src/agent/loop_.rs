use crate::approval::{ApprovalManager, ApprovalRequest, ApprovalResponse};
use crate::config::Config;
use crate::cost::types::BudgetCheck;
use crate::i18n::ToolDescriptions;
use crate::memory::{self, Memory, MemoryCategory, decay};
use crate::multimodal;
use crate::observability::{self, Observer, ObserverEvent, runtime_trace};
use crate::providers::traits::StreamEvent;
use crate::providers::{
    self, ChatMessage, ChatRequest, Provider, ProviderCapabilityError, ToolCall,
};
use crate::runtime;
use crate::security::{AutonomyLevel, SecurityPolicy};
use crate::tools::{self, Tool};
use crate::util::truncate_with_ellipsis;
use anyhow::Result;
use futures_util::StreamExt;
use regex::{Regex, RegexSet};
use std::collections::HashSet;
use std::fmt::Write;
use std::io::Write as _;
use std::path::PathBuf;
use std::sync::{Arc, LazyLock, Mutex};
use std::time::{Duration, Instant};
use tokio_util::sync::CancellationToken;
use uuid::Uuid;

// Cost tracking moved to `super::cost`.
pub(crate) use super::cost::{
    TOOL_LOOP_COST_TRACKING_CONTEXT, ToolLoopCostTrackingContext, check_tool_loop_budget,
    record_tool_loop_cost_usage,
};

/// Minimum characters per chunk when relaying LLM text to a streaming draft.
const STREAM_CHUNK_MIN_CHARS: usize = 80;
/// Rolling window size for detecting streamed tool-call payload markers.
const STREAM_TOOL_MARKER_WINDOW_CHARS: usize = 512;

/// Default maximum agentic tool-use iterations per user message to prevent runaway loops.
/// Used as a safe fallback when `max_tool_iterations` is unset or configured as zero.
const DEFAULT_MAX_TOOL_ITERATIONS: usize = 10;

// History management moved to `super::history`.
pub(crate) use super::history::{
    emergency_history_trim, estimate_history_tokens, fast_trim_tool_results,
    load_interactive_session_history, save_interactive_session_history, trim_history,
    truncate_tool_result,
};

/// Minimum user-message length (in chars) for auto-save to memory.
/// Matches the channel-side constant in `channels/mod.rs`.
const AUTOSAVE_MIN_MESSAGE_CHARS: usize = 20;

/// Callback type for checking if model has been switched during tool execution.
/// Returns Some((provider, model)) if a switch was requested, None otherwise.
pub type ModelSwitchCallback = Arc<Mutex<Option<(String, String)>>>;

/// Global model switch request state - used for runtime model switching via model_switch tool.
/// This is set by the model_switch tool and checked by the agent loop.
#[allow(clippy::type_complexity)]
static MODEL_SWITCH_REQUEST: LazyLock<Arc<Mutex<Option<(String, String)>>>> =
    LazyLock::new(|| Arc::new(Mutex::new(None)));

/// Get the global model switch request state
pub fn get_model_switch_state() -> ModelSwitchCallback {
    Arc::clone(&MODEL_SWITCH_REQUEST)
}

/// Clear any pending model switch request
pub fn clear_model_switch_request() {
    if let Ok(guard) = MODEL_SWITCH_REQUEST.lock() {
        let mut guard = guard;
        *guard = None;
    }
}

fn glob_match(pattern: &str, name: &str) -> bool {
    match pattern.find('*') {
        None => pattern == name,
        Some(star) => {
            let prefix = &pattern[..star];
            let suffix = &pattern[star + 1..];
            name.starts_with(prefix)
                && name.ends_with(suffix)
                && name.len() >= prefix.len() + suffix.len()
        }
    }
}

/// Returns the subset of `tool_specs` that should be sent to the LLM for this turn.
///
/// Rules (mirrors NullClaw `filterToolSpecsForTurn`):
/// - Built-in tools (names that do not start with `"mcp_"`) always pass through.
/// - When `groups` is empty, all tools pass through (backward compatible default).
/// - An MCP tool is included if at least one group matches it:
///   - `always` group: included unconditionally if any pattern matches the tool name.
///   - `dynamic` group: included if any pattern matches AND the user message contains
///     at least one keyword (case-insensitive substring).
pub(crate) fn filter_tool_specs_for_turn(
    tool_specs: Vec<crate::tools::ToolSpec>,
    groups: &[crate::config::schema::ToolFilterGroup],
    user_message: &str,
) -> Vec<crate::tools::ToolSpec> {
    use crate::config::schema::ToolFilterGroupMode;

    if groups.is_empty() {
        return tool_specs;
    }

    let msg_lower = user_message.to_ascii_lowercase();

    tool_specs
        .into_iter()
        .filter(|spec| {
            // Built-in tools always pass through.
            if !spec.name.starts_with("mcp_") {
                return true;
            }
            // MCP tool: include if any active group matches.
            groups.iter().any(|group| {
                let pattern_matches = group.tools.iter().any(|pat| glob_match(pat, &spec.name));
                if !pattern_matches {
                    return false;
                }
                match group.mode {
                    ToolFilterGroupMode::Always => true,
                    ToolFilterGroupMode::Dynamic => group
                        .keywords
                        .iter()
                        .any(|kw| msg_lower.contains(&kw.to_ascii_lowercase())),
                }
            })
        })
        .collect()
}

/// Filters a tool spec list by an optional capability allowlist.
///
/// When `allowed` is `None`, all specs pass through unchanged.
/// When `allowed` is `Some(list)`, only specs whose name appears in the list
/// are retained. Unknown names in the allowlist are silently ignored.
pub(crate) fn filter_by_allowed_tools(
    specs: Vec<crate::tools::ToolSpec>,
    allowed: Option<&[String]>,
) -> Vec<crate::tools::ToolSpec> {
    match allowed {
        None => specs,
        Some(list) => specs
            .into_iter()
            .filter(|spec| list.iter().any(|name| name == &spec.name))
            .collect(),
    }
}

tokio::task_local! {
    /// Stable thread/conversation identifier from the incoming channel message.
    /// Used by [`PerSenderTracker`] to isolate rate-limit buckets per chat.
    /// Set from the channel's thread ID, topic ID, or message ID.
    pub static TOOL_LOOP_THREAD_ID: Option<String>;
}

/// Run a future with the thread ID set in task-local storage.
/// Rate-limiting reads this to assign per-sender buckets.
pub async fn scope_thread_id<F>(thread_id: Option<String>, future: F) -> F::Output
where
    F: std::future::Future,
{
    TOOL_LOOP_THREAD_ID.scope(thread_id, future).await
}

/// Computes the list of MCP tool names that should be excluded for a given turn
/// based on `tool_filter_groups` and the user message.
///
/// Returns an empty `Vec` when `groups` is empty (no filtering).
fn compute_excluded_mcp_tools(
    tools_registry: &[Box<dyn Tool>],
    groups: &[crate::config::schema::ToolFilterGroup],
    user_message: &str,
) -> Vec<String> {
    if groups.is_empty() {
        return Vec::new();
    }
    let filtered_specs = filter_tool_specs_for_turn(
        tools_registry.iter().map(|t| t.spec()).collect(),
        groups,
        user_message,
    );
    let included: HashSet<&str> = filtered_specs.iter().map(|s| s.name.as_str()).collect();
    tools_registry
        .iter()
        .filter(|t| t.name().starts_with("mcp_") && !included.contains(t.name()))
        .map(|t| t.name().to_string())
        .collect()
}

static SENSITIVE_KEY_PATTERNS: LazyLock<RegexSet> = LazyLock::new(|| {
    RegexSet::new([
        r"(?i)token",
        r"(?i)api[_-]?key",
        r"(?i)password",
        r"(?i)secret",
        r"(?i)user[_-]?key",
        r"(?i)bearer",
        r"(?i)credential",
    ])
    .unwrap()
});

static SENSITIVE_KV_REGEX: LazyLock<Regex> = LazyLock::new(|| {
    Regex::new(r#"(?i)(token|api[_-]?key|password|secret|user[_-]?key|bearer|credential)["']?\s*[:=]\s*(?:"([^"]{8,})"|'([^']{8,})'|([a-zA-Z0-9_\-\.]{8,}))"#).unwrap()
});

/// Scrub credentials from tool output to prevent accidental exfiltration.
/// Replaces known credential patterns with a redacted placeholder while preserving
/// a small prefix for context.
pub(crate) fn scrub_credentials(input: &str) -> String {
    SENSITIVE_KV_REGEX
        .replace_all(input, |caps: &regex::Captures| {
            let full_match = &caps[0];
            let key = &caps[1];
            let val = caps
                .get(2)
                .or(caps.get(3))
                .or(caps.get(4))
                .map(|m| m.as_str())
                .unwrap_or("");

            // Preserve first 4 chars for context, then redact.
            // Use char_indices to find the byte offset of the 4th character
            // so we never slice in the middle of a multi-byte UTF-8 sequence.
            let prefix = if val.len() > 4 {
                val.char_indices()
                    .nth(4)
                    .map(|(byte_idx, _)| &val[..byte_idx])
                    .unwrap_or(val)
            } else {
                ""
            };

            if full_match.contains(':') {
                if full_match.contains('"') {
                    format!("\"{}\": \"{}*[REDACTED]\"", key, prefix)
                } else {
                    format!("{}: {}*[REDACTED]", key, prefix)
                }
            } else if full_match.contains('=') {
                if full_match.contains('"') {
                    format!("{}=\"{}*[REDACTED]\"", key, prefix)
                } else {
                    format!("{}={}*[REDACTED]", key, prefix)
                }
            } else {
                format!("{}: {}*[REDACTED]", key, prefix)
            }
        })
        .to_string()
}

/// Default trigger for auto-compaction when non-system message count exceeds this threshold.
/// Prefer passing the config-driven value via `run_tool_call_loop`; this constant is only
/// used when callers omit the parameter.
/// Minimum interval between progress sends to avoid flooding the draft channel.
pub(crate) const PROGRESS_MIN_INTERVAL_MS: u64 = 500;

/// Structured event sent through the draft channel so channels can
/// differentiate between status/progress updates and actual response content.
#[derive(Debug, Clone)]
pub enum DraftEvent {
    /// Clear accumulated draft content (e.g. before streaming a new response).
    Clear,
    /// Progress / status text — channels can show this in a status bar
    /// rather than in the message body (e.g. "🤔 Thinking...", "⏳ shell_command").
    Progress(String),
    /// Actual response content delta to append to the draft message.
    Content(String),
}

tokio::task_local! {
    pub(crate) static TOOL_CHOICE_OVERRIDE: Option<String>;
}

/// Convert a tool registry to OpenAI function-calling format for native tool support.
fn tools_to_openai_format(tools_registry: &[Box<dyn Tool>]) -> Vec<serde_json::Value> {
    tools_registry
        .iter()
        .map(|tool| {
            serde_json::json!({
                "type": "function",
                "function": {
                    "name": tool.name(),
                    "description": tool.description(),
                    "parameters": tool.parameters_schema()
                }
            })
        })
        .collect()
}

fn autosave_memory_key(prefix: &str) -> String {
    format!("{prefix}_{}", Uuid::new_v4())
}

/// Build context preamble by searching memory for relevant entries.
/// Entries with a hybrid score below `min_relevance_score` are dropped to
/// prevent unrelated memories from bleeding into the conversation.
/// Core memories are exempt from time decay (evergreen).
async fn build_context(
    mem: &dyn Memory,
    user_msg: &str,
    min_relevance_score: f64,
    session_id: Option<&str>,
) -> String {
    let mut context = String::new();

    // Pull relevant memories for this message
    if let Ok(mut entries) = mem.recall(user_msg, 5, session_id, None, None).await {
        // Apply time decay: older non-Core memories score lower
        decay::apply_time_decay(&mut entries, decay::DEFAULT_HALF_LIFE_DAYS);

        let relevant: Vec<_> = entries
            .iter()
            .filter(|e| match e.score {
                Some(score) => score >= min_relevance_score,
                None => true,
            })
            .collect();

        if !relevant.is_empty() {
            context.push_str("[Memory context]\n");
            for entry in &relevant {
                if memory::is_assistant_autosave_key(&entry.key) {
                    continue;
                }
                if memory::should_skip_autosave_content(&entry.content) {
                    continue;
                }
                // Skip entries containing tool_result blocks — they can leak
                // stale tool output from previous heartbeat ticks into new
                // sessions, presenting the LLM with orphan tool_result data.
                if entry.content.contains("<tool_result") {
                    continue;
                }
                let _ = writeln!(context, "- {}: {}", entry.key, entry.content);
            }
            if context == "[Memory context]\n" {
                context.clear();
            } else {
                context.push_str("[/Memory context]\n\n");
            }
        }
    }

    context
}

// Tool execution moved to `super::tool_execution`.
pub(crate) use super::tool_execution::{
    ToolExecutionOutcome, execute_tools_parallel, execute_tools_sequential,
    should_execute_tools_in_parallel,
};

fn parse_arguments_value(raw: Option<&serde_json::Value>) -> serde_json::Value {
    match raw {
        Some(serde_json::Value::String(s)) => serde_json::from_str::<serde_json::Value>(s)
            .unwrap_or_else(|_| serde_json::Value::Object(serde_json::Map::new())),
        Some(value) => value.clone(),
        None => serde_json::Value::Object(serde_json::Map::new()),
    }
}

fn parse_tool_call_id(
    root: &serde_json::Value,
    function: Option<&serde_json::Value>,
) -> Option<String> {
    function
        .and_then(|func| func.get("id"))
        .or_else(|| root.get("id"))
        .or_else(|| root.get("tool_call_id"))
        .or_else(|| root.get("call_id"))
        .and_then(serde_json::Value::as_str)
        .map(str::trim)
        .filter(|id| !id.is_empty())
        .map(ToString::to_string)
}

fn canonicalize_json_for_tool_signature(value: &serde_json::Value) -> serde_json::Value {
    match value {
        serde_json::Value::Object(map) => {
            let mut keys: Vec<String> = map.keys().cloned().collect();
            keys.sort_unstable();
            let mut ordered = serde_json::Map::new();
            for key in keys {
                if let Some(child) = map.get(&key) {
                    ordered.insert(key, canonicalize_json_for_tool_signature(child));
                }
            }
            serde_json::Value::Object(ordered)
        }
        serde_json::Value::Array(items) => serde_json::Value::Array(
            items
                .iter()
                .map(canonicalize_json_for_tool_signature)
                .collect(),
        ),
        _ => value.clone(),
    }
}

fn parse_tool_call_value(value: &serde_json::Value) -> Option<ParsedToolCall> {
    if let Some(function) = value.get("function") {
        let tool_call_id = parse_tool_call_id(value, Some(function));
        let name = function
            .get("name")
            .and_then(|v| v.as_str())
            .unwrap_or("")
            .trim()
            .to_string();
        if !name.is_empty() {
            let arguments = parse_arguments_value(
                function
                    .get("arguments")
                    .or_else(|| function.get("parameters")),
            );
            return Some(ParsedToolCall {
                name,
                arguments,
                tool_call_id,
            });
        }
    }

    let tool_call_id = parse_tool_call_id(value, None);
    let name = value
        .get("name")
        .and_then(|v| v.as_str())
        .unwrap_or("")
        .trim()
        .to_string();

    if name.is_empty() {
        return None;
    }

    let arguments =
        parse_arguments_value(value.get("arguments").or_else(|| value.get("parameters")));
    Some(ParsedToolCall {
        name,
        arguments,
        tool_call_id,
    })
}

fn parse_tool_calls_from_json_value(value: &serde_json::Value) -> Vec<ParsedToolCall> {
    let mut calls = Vec::new();

    if let Some(tool_calls) = value.get("tool_calls").and_then(|v| v.as_array()) {
        for call in tool_calls {
            if let Some(parsed) = parse_tool_call_value(call) {
                calls.push(parsed);
            }
        }

        if !calls.is_empty() {
            return calls;
        }
    }

    if let Some(array) = value.as_array() {
        for item in array {
            if let Some(parsed) = parse_tool_call_value(item) {
                calls.push(parsed);
            }
        }
        return calls;
    }

    if let Some(parsed) = parse_tool_call_value(value) {
        calls.push(parsed);
    }

    calls
}

fn is_xml_meta_tag(tag: &str) -> bool {
    let normalized = tag.to_ascii_lowercase();
    matches!(
        normalized.as_str(),
        "tool_call"
            | "toolcall"
            | "tool-call"
            | "invoke"
            | "thinking"
            | "thought"
            | "analysis"
            | "reasoning"
            | "reflection"
    )
}

/// Match opening XML tags: `<tag_name>`.  Does NOT use backreferences.
static XML_OPEN_TAG_RE: LazyLock<Regex> =
    LazyLock::new(|| Regex::new(r"<([a-zA-Z_][a-zA-Z0-9_-]*)>").unwrap());

/// MiniMax XML invoke format:
/// `<invoke name="shell"><parameter name="command">pwd</parameter></invoke>`
static MINIMAX_INVOKE_RE: LazyLock<Regex> = LazyLock::new(|| {
    Regex::new(r#"(?is)<invoke\b[^>]*\bname\s*=\s*(?:"([^"]+)"|'([^']+)')[^>]*>(.*?)</invoke>"#)
        .unwrap()
});

static MINIMAX_PARAMETER_RE: LazyLock<Regex> = LazyLock::new(|| {
    Regex::new(
        r#"(?is)<parameter\b[^>]*\bname\s*=\s*(?:"([^"]+)"|'([^']+)')[^>]*>(.*?)</parameter>"#,
    )
    .unwrap()
});

/// Extracts all `<tag>…</tag>` pairs from `input`, returning `(tag_name, inner_content)`.
/// Handles matching closing tags without regex backreferences.
fn extract_xml_pairs(input: &str) -> Vec<(&str, &str)> {
    let mut results = Vec::new();
    let mut search_start = 0;
    while let Some(open_cap) = XML_OPEN_TAG_RE.captures(&input[search_start..]) {
        let full_open = open_cap.get(0).unwrap();
        let tag_name = open_cap.get(1).unwrap().as_str();
        let open_end = search_start + full_open.end();

        let closing_tag = format!("</{tag_name}>");
        if let Some(close_pos) = input[open_end..].find(&closing_tag) {
            let inner = &input[open_end..open_end + close_pos];
            results.push((tag_name, inner.trim()));
            search_start = open_end + close_pos + closing_tag.len();
        } else {
            search_start = open_end;
        }
    }
    results
}

/// Parse XML-style tool calls in `<tool_call>` bodies.
/// Supports both nested argument tags and JSON argument payloads:
/// - `<memory_recall><query>...</query></memory_recall>`
/// - `<shell>{"command":"pwd"}</shell>`
fn parse_xml_tool_calls(xml_content: &str) -> Option<Vec<ParsedToolCall>> {
    let mut calls = Vec::new();
    let trimmed = xml_content.trim();

    if !trimmed.starts_with('<') || !trimmed.contains('>') {
        return None;
    }

    for (tool_name_str, inner_content) in extract_xml_pairs(trimmed) {
        let tool_name = tool_name_str.to_string();
        if is_xml_meta_tag(&tool_name) {
            continue;
        }

        if inner_content.is_empty() {
            continue;
        }

        let mut args = serde_json::Map::new();

        if let Some(first_json) = extract_json_values(inner_content).into_iter().next() {
            match first_json {
                serde_json::Value::Object(object_args) => {
                    args = object_args;
                }
                other => {
                    args.insert("value".to_string(), other);
                }
            }
        } else {
            for (key_str, value) in extract_xml_pairs(inner_content) {
                let key = key_str.to_string();
                if is_xml_meta_tag(&key) {
                    continue;
                }
                if !value.is_empty() {
                    args.insert(key, serde_json::Value::String(value.to_string()));
                }
            }

            if args.is_empty() {
                args.insert(
                    "content".to_string(),
                    serde_json::Value::String(inner_content.to_string()),
                );
            }
        }

        calls.push(ParsedToolCall {
            name: tool_name,
            arguments: serde_json::Value::Object(args),
            tool_call_id: None,
        });
    }

    if calls.is_empty() { None } else { Some(calls) }
}

/// Parse MiniMax-style XML tool calls with attributed invoke/parameter tags.
fn parse_minimax_invoke_calls(response: &str) -> Option<(String, Vec<ParsedToolCall>)> {
    let mut calls = Vec::new();
    let mut text_parts = Vec::new();
    let mut last_end = 0usize;

    for cap in MINIMAX_INVOKE_RE.captures_iter(response) {
        let Some(full_match) = cap.get(0) else {
            continue;
        };

        let before = response[last_end..full_match.start()].trim();
        if !before.is_empty() {
            text_parts.push(before.to_string());
        }

        let name = cap
            .get(1)
            .or_else(|| cap.get(2))
            .map(|m| m.as_str().trim())
            .filter(|v| !v.is_empty());
        let body = cap.get(3).map(|m| m.as_str()).unwrap_or("").trim();
        last_end = full_match.end();

        let Some(name) = name else {
            continue;
        };

        let mut args = serde_json::Map::new();
        for param_cap in MINIMAX_PARAMETER_RE.captures_iter(body) {
            let key = param_cap
                .get(1)
                .or_else(|| param_cap.get(2))
                .map(|m| m.as_str().trim())
                .unwrap_or_default();
            if key.is_empty() {
                continue;
            }
            let value = param_cap
                .get(3)
                .map(|m| m.as_str().trim())
                .unwrap_or_default();
            if value.is_empty() {
                continue;
            }

            let parsed = extract_json_values(value).into_iter().next();
            args.insert(
                key.to_string(),
                parsed.unwrap_or_else(|| serde_json::Value::String(value.to_string())),
            );
        }

        if args.is_empty() {
            if let Some(first_json) = extract_json_values(body).into_iter().next() {
                match first_json {
                    serde_json::Value::Object(obj) => args = obj,
                    other => {
                        args.insert("value".to_string(), other);
                    }
                }
            } else if !body.is_empty() {
                args.insert(
                    "content".to_string(),
                    serde_json::Value::String(body.to_string()),
                );
            }
        }

        calls.push(ParsedToolCall {
            name: name.to_string(),
            arguments: serde_json::Value::Object(args),
            tool_call_id: None,
        });
    }

    if calls.is_empty() {
        return None;
    }

    let after = response[last_end..].trim();
    if !after.is_empty() {
        text_parts.push(after.to_string());
    }

    let text = text_parts
        .join("\n")
        .replace("<minimax:tool_call>", "")
        .replace("</minimax:tool_call>", "")
        .replace("<minimax:toolcall>", "")
        .replace("</minimax:toolcall>", "")
        .trim()
        .to_string();

    Some((text, calls))
}

const TOOL_CALL_OPEN_TAGS: [&str; 6] = [
    "<tool_call>",
    "<toolcall>",
    "<tool-call>",
    "<invoke>",
    "<minimax:tool_call>",
    "<minimax:toolcall>",
];

const TOOL_CALL_CLOSE_TAGS: [&str; 6] = [
    "</tool_call>",
    "</toolcall>",
    "</tool-call>",
    "</invoke>",
    "</minimax:tool_call>",
    "</minimax:toolcall>",
];

fn find_first_tag<'a>(haystack: &str, tags: &'a [&'a str]) -> Option<(usize, &'a str)> {
    tags.iter()
        .filter_map(|tag| haystack.find(tag).map(|idx| (idx, *tag)))
        .min_by_key(|(idx, _)| *idx)
}

fn extract_first_json_value_with_end(input: &str) -> Option<(serde_json::Value, usize)> {
    let trimmed = input.trim_start();
    let trim_offset = input.len().saturating_sub(trimmed.len());

    for (byte_idx, ch) in trimmed.char_indices() {
        if ch != '{' && ch != '[' {
            continue;
        }

        let slice = &trimmed[byte_idx..];
        let mut stream = serde_json::Deserializer::from_str(slice).into_iter::<serde_json::Value>();
        if let Some(Ok(value)) = stream.next() {
            let consumed = stream.byte_offset();
            if consumed > 0 {
                return Some((value, trim_offset + byte_idx + consumed));
            }
        }
    }

    None
}

fn strip_leading_close_tags(mut input: &str) -> &str {
    loop {
        let trimmed = input.trim_start();
        if !trimmed.starts_with("</") {
            return trimmed;
        }

        let Some(close_end) = trimmed.find('>') else {
            return "";
        };
        input = &trimmed[close_end + 1..];
    }
}

/// Extract JSON values from a string.
///
/// # Security Warning
///
/// This function extracts ANY JSON objects/arrays from the input. It MUST only
/// be used on content that is already trusted to be from the LLM, such as
/// content inside `<invoke>` tags where the LLM has explicitly indicated intent
/// to make a tool call. Do NOT use this on raw user input or content that
/// could contain prompt injection payloads.
fn extract_json_values(input: &str) -> Vec<serde_json::Value> {
    let mut values = Vec::new();
    let trimmed = input.trim();
    if trimmed.is_empty() {
        return values;
    }

    if let Ok(value) = serde_json::from_str::<serde_json::Value>(trimmed) {
        values.push(value);
        return values;
    }

    let char_positions: Vec<(usize, char)> = trimmed.char_indices().collect();
    let mut idx = 0;
    while idx < char_positions.len() {
        let (byte_idx, ch) = char_positions[idx];
        if ch == '{' || ch == '[' {
            let slice = &trimmed[byte_idx..];
            let mut stream =
                serde_json::Deserializer::from_str(slice).into_iter::<serde_json::Value>();
            if let Some(Ok(value)) = stream.next() {
                let consumed = stream.byte_offset();
                if consumed > 0 {
                    values.push(value);
                    let next_byte = byte_idx + consumed;
                    while idx < char_positions.len() && char_positions[idx].0 < next_byte {
                        idx += 1;
                    }
                    continue;
                }
            }
        }
        idx += 1;
    }

    values
}

/// Find the end position of a JSON object by tracking balanced braces.
fn find_json_end(input: &str) -> Option<usize> {
    let trimmed = input.trim_start();
    let offset = input.len() - trimmed.len();

    if !trimmed.starts_with('{') {
        return None;
    }

    let mut depth = 0;
    let mut in_string = false;
    let mut escape_next = false;

    for (i, ch) in trimmed.char_indices() {
        if escape_next {
            escape_next = false;
            continue;
        }

        match ch {
            '\\' if in_string => escape_next = true,
            '"' => in_string = !in_string,
            '{' if !in_string => depth += 1,
            '}' if !in_string => {
                depth -= 1;
                if depth == 0 {
                    return Some(offset + i + ch.len_utf8());
                }
            }
            _ => {}
        }
    }

    None
}

/// Parse XML attribute-style tool calls from response text.
/// This handles MiniMax and similar providers that output:
/// ```xml
/// <minimax:toolcall>
/// <invoke name="shell">
/// <parameter name="command">ls</parameter>
/// </invoke>
/// </minimax:toolcall>
/// ```
fn parse_xml_attribute_tool_calls(response: &str) -> Vec<ParsedToolCall> {
    let mut calls = Vec::new();

    // Regex to find <invoke name="toolname">...</invoke> blocks
    static INVOKE_RE: LazyLock<Regex> = LazyLock::new(|| {
        Regex::new(r#"(?s)<invoke\s+name="([^"]+)"[^>]*>(.*?)</invoke>"#).unwrap()
    });

    // Regex to find <parameter name="paramname">value</parameter>
    static PARAM_RE: LazyLock<Regex> = LazyLock::new(|| {
        Regex::new(r#"<parameter\s+name="([^"]+)"[^>]*>([^<]*)</parameter>"#).unwrap()
    });

    for cap in INVOKE_RE.captures_iter(response) {
        let tool_name = cap.get(1).map(|m| m.as_str()).unwrap_or("");
        let inner = cap.get(2).map(|m| m.as_str()).unwrap_or("");

        if tool_name.is_empty() {
            continue;
        }

        let mut arguments = serde_json::Map::new();

        for param_cap in PARAM_RE.captures_iter(inner) {
            let param_name = param_cap.get(1).map(|m| m.as_str()).unwrap_or("");
            let param_value = param_cap.get(2).map(|m| m.as_str()).unwrap_or("");

            if !param_name.is_empty() {
                arguments.insert(
                    param_name.to_string(),
                    serde_json::Value::String(param_value.to_string()),
                );
            }
        }

        if !arguments.is_empty() {
            calls.push(ParsedToolCall {
                name: map_tool_name_alias(tool_name).to_string(),
                arguments: serde_json::Value::Object(arguments),
                tool_call_id: None,
            });
        }
    }

    calls
}

/// Parse Perl/hash-ref style tool calls from response text.
/// This handles formats like:
/// ```text
/// TOOL_CALL
/// {tool => "shell", args => {
///   --command "ls -la"
///   --description "List current directory contents"
/// }}
/// /TOOL_CALL
/// ```
/// Also handles the square bracket variant emitted by models like MiniMax 2.7:
/// ```text
/// [TOOL_CALL]{tool => "shell", args => {--command "echo hello"}}[/TOOL_CALL]
/// ```
fn parse_perl_style_tool_calls(response: &str) -> Vec<ParsedToolCall> {
    let mut calls = Vec::new();

    // Regex to find TOOL_CALL blocks - handle double closing braces }}
    // Matches both `TOOL_CALL { ... }} /TOOL_CALL` and `[TOOL_CALL]{ ... }}[/TOOL_CALL]`
    static PERL_RE: LazyLock<Regex> = LazyLock::new(|| {
        Regex::new(r"(?s)(?:\[TOOL_CALL\]|TOOL_CALL)\s*\{(.+?)\}\}\s*(?:\[/TOOL_CALL\]|/TOOL_CALL)")
            .unwrap()
    });

    // Regex to find tool => "name" in the content
    static TOOL_NAME_RE: LazyLock<Regex> =
        LazyLock::new(|| Regex::new(r#"tool\s*=>\s*"([^"]+)""#).unwrap());

    // Regex to find args => { ... } block.
    // The closing brace is optional: in the square bracket variant [TOOL_CALL]{...}}[/TOOL_CALL]
    // the outer regex may consume the inner closing brace, so the args content may run to end of string.
    static ARGS_BLOCK_RE: LazyLock<Regex> =
        LazyLock::new(|| Regex::new(r"(?s)args\s*=>\s*\{(.+?)(?:\}|$)").unwrap());

    // Regex to find --key "value" pairs
    static ARGS_RE: LazyLock<Regex> =
        LazyLock::new(|| Regex::new(r#"--(\w+)\s+"([^"]+)""#).unwrap());

    for cap in PERL_RE.captures_iter(response) {
        let content = cap.get(1).map(|m| m.as_str()).unwrap_or("");

        // Extract tool name
        let tool_name = TOOL_NAME_RE
            .captures(content)
            .and_then(|c| c.get(1))
            .map(|m| m.as_str())
            .unwrap_or("");

        if tool_name.is_empty() {
            continue;
        }

        // Extract args block
        let args_block = ARGS_BLOCK_RE
            .captures(content)
            .and_then(|c| c.get(1))
            .map(|m| m.as_str())
            .unwrap_or("");

        let mut arguments = serde_json::Map::new();

        for arg_cap in ARGS_RE.captures_iter(args_block) {
            let key = arg_cap.get(1).map(|m| m.as_str()).unwrap_or("");
            let value = arg_cap.get(2).map(|m| m.as_str()).unwrap_or("");

            if !key.is_empty() {
                arguments.insert(
                    key.to_string(),
                    serde_json::Value::String(value.to_string()),
                );
            }
        }

        if !arguments.is_empty() {
            calls.push(ParsedToolCall {
                name: map_tool_name_alias(tool_name).to_string(),
                arguments: serde_json::Value::Object(arguments),
                tool_call_id: None,
            });
        }
    }

    calls
}

/// Parse FunctionCall-style tool calls from response text.
/// This handles formats like:
/// ```text
/// <FunctionCall>
/// file_read
/// <code>path>/Users/kylelampa/Documents/zeroclaw/README.md</code>
/// </FunctionCall>
/// ```
fn parse_function_call_tool_calls(response: &str) -> Vec<ParsedToolCall> {
    let mut calls = Vec::new();

    // Regex to find <FunctionCall> blocks
    static FUNC_RE: LazyLock<Regex> = LazyLock::new(|| {
        Regex::new(r"(?s)<FunctionCall>\s*(\w+)\s*<code>([^<]+)</code>\s*</FunctionCall>").unwrap()
    });

    for cap in FUNC_RE.captures_iter(response) {
        let tool_name = cap.get(1).map(|m| m.as_str()).unwrap_or("");
        let args_text = cap.get(2).map(|m| m.as_str()).unwrap_or("");

        if tool_name.is_empty() {
            continue;
        }

        // Parse key>value pairs (e.g., path>/Users/.../file.txt)
        let mut arguments = serde_json::Map::new();
        for line in args_text.lines() {
            let line = line.trim();
            if let Some(pos) = line.find('>') {
                let key = line[..pos].trim();
                let value = line[pos + 1..].trim();
                if !key.is_empty() && !value.is_empty() {
                    arguments.insert(
                        key.to_string(),
                        serde_json::Value::String(value.to_string()),
                    );
                }
            }
        }

        if !arguments.is_empty() {
            calls.push(ParsedToolCall {
                name: map_tool_name_alias(tool_name).to_string(),
                arguments: serde_json::Value::Object(arguments),
                tool_call_id: None,
            });
        }
    }

    calls
}

/// Parse GLM-style tool calls from response text.
/// Map tool name aliases from various LLM providers to ZeroClaw tool names.
/// This handles variations like "fileread" -> "file_read", "bash" -> "shell", etc.
fn map_tool_name_alias(tool_name: &str) -> &str {
    match tool_name {
        // Shell variations (including GLM aliases that map to shell)
        "shell" | "bash" | "sh" | "exec" | "command" | "cmd" | "browser_open" | "browser"
        | "web_search" => "shell",
        // Messaging variations
        "send_message" | "sendmessage" => "message_send",
        // File tool variations
        "fileread" | "file_read" | "readfile" | "read_file" | "file" => "file_read",
        "filewrite" | "file_write" | "writefile" | "write_file" => "file_write",
        "filelist" | "file_list" | "listfiles" | "list_files" => "file_list",
        // Memory variations
        "memoryrecall" | "memory_recall" | "recall" | "memrecall" => "memory_recall",
        "memorystore" | "memory_store" | "store" | "memstore" => "memory_store",
        "memoryforget" | "memory_forget" | "forget" | "memforget" => "memory_forget",
        // HTTP variations
        "http_request" | "http" | "fetch" | "curl" | "wget" => "http_request",
        _ => tool_name,
    }
}

fn build_curl_command(url: &str) -> Option<String> {
    if !(url.starts_with("http://") || url.starts_with("https://")) {
        return None;
    }

    if url.chars().any(char::is_whitespace) {
        return None;
    }

    let escaped = url.replace('\'', r#"'\\''"#);
    Some(format!("curl -s '{}'", escaped))
}

fn parse_glm_style_tool_calls(text: &str) -> Vec<(String, serde_json::Value, Option<String>)> {
    let mut calls = Vec::new();

    for line in text.lines() {
        let line = line.trim();
        if line.is_empty() {
            continue;
        }

        // Format: tool_name/param>value or tool_name/{json}
        if let Some(pos) = line.find('/') {
            let tool_part = &line[..pos];
            let rest = &line[pos + 1..];

            if tool_part.chars().all(|c| c.is_alphanumeric() || c == '_') {
                let tool_name = map_tool_name_alias(tool_part);

                if let Some(gt_pos) = rest.find('>') {
                    let param_name = rest[..gt_pos].trim();
                    let value = rest[gt_pos + 1..].trim();

                    let arguments = match tool_name {
                        "shell" => {
                            if param_name == "url" {
                                let Some(command) = build_curl_command(value) else {
                                    continue;
                                };
                                serde_json::json!({ "command": command })
                            } else if value.starts_with("http://") || value.starts_with("https://")
                            {
                                if let Some(command) = build_curl_command(value) {
                                    serde_json::json!({ "command": command })
                                } else {
                                    serde_json::json!({ "command": value })
                                }
                            } else {
                                serde_json::json!({ "command": value })
                            }
                        }
                        "http_request" => {
                            serde_json::json!({"url": value, "method": "GET"})
                        }
                        _ => serde_json::json!({ param_name: value }),
                    };

                    calls.push((tool_name.to_string(), arguments, Some(line.to_string())));
                    continue;
                }

                if rest.starts_with('{') {
                    if let Ok(json_args) = serde_json::from_str::<serde_json::Value>(rest) {
                        calls.push((tool_name.to_string(), json_args, Some(line.to_string())));
                    }
                }
            }
        }
    }

    calls
}

/// Return the canonical default parameter name for a tool.
///
/// When a model emits a shortened call like `shell>uname -a` (without an
/// explicit `/param_name`), we need to infer which parameter the value maps
/// to. This function encodes the mapping for known ZeroClaw tools.
fn default_param_for_tool(tool: &str) -> &'static str {
    match tool {
        "shell" | "bash" | "sh" | "exec" | "command" | "cmd" => "command",
        // All file tools default to "path"
        "file_read" | "fileread" | "readfile" | "read_file" | "file" | "file_write"
        | "filewrite" | "writefile" | "write_file" | "file_edit" | "fileedit" | "editfile"
        | "edit_file" | "file_list" | "filelist" | "listfiles" | "list_files" => "path",
        // Memory recall/forget and web search tools all default to "query"
        "memory_recall" | "memoryrecall" | "recall" | "memrecall" | "memory_forget"
        | "memoryforget" | "forget" | "memforget" | "web_search_tool" | "web_search"
        | "websearch" | "search" => "query",
        "memory_store" | "memorystore" | "store" | "memstore" => "content",
        // HTTP and browser tools default to "url"
        "http_request" | "http" | "fetch" | "curl" | "wget" | "browser_open" | "browser" => "url",
        _ => "input",
    }
}

/// Parse GLM-style shortened tool call bodies found inside `<tool_call>` tags.
///
/// Handles three sub-formats that GLM-4.7 emits:
///
/// 1. **Shortened**: `tool_name>value` — single value mapped via
///    [`default_param_for_tool`].
/// 2. **YAML-like multi-line**: `tool_name>\nkey: value\nkey: value` — each
///    subsequent `key: value` line becomes a parameter.
/// 3. **Attribute-style**: `tool_name key="value" [/]>` — XML-like attributes.
///
/// Returns `None` if the body does not match any of these formats.
fn parse_glm_shortened_body(body: &str) -> Option<ParsedToolCall> {
    let body = body.trim();
    if body.is_empty() {
        return None;
    }

    let function_style = body.find('(').and_then(|open| {
        if body.ends_with(')') && open > 0 {
            Some((body[..open].trim(), body[open + 1..body.len() - 1].trim()))
        } else {
            None
        }
    });

    // Check attribute-style FIRST: `tool_name key="value" />`
    // Must come before `>` check because `/>` contains `>` and would
    // misparse the tool name in the first branch.
    let (tool_raw, value_part) = if let Some((tool, args)) = function_style {
        (tool, args)
    } else if body.contains("=\"") {
        // Attribute-style: split at first whitespace to get tool name
        let split_pos = body.find(|c: char| c.is_whitespace()).unwrap_or(body.len());
        let tool = body[..split_pos].trim();
        let attrs = body[split_pos..]
            .trim()
            .trim_end_matches("/>")
            .trim_end_matches('>')
            .trim_end_matches('/')
            .trim();
        (tool, attrs)
    } else if let Some(gt_pos) = body.find('>') {
        // GLM shortened: `tool_name>value`
        let tool = body[..gt_pos].trim();
        let value = body[gt_pos + 1..].trim();
        // Strip trailing self-close markers that some models emit
        let value = value.trim_end_matches("/>").trim_end_matches('/').trim();
        (tool, value)
    } else {
        return None;
    };

    // Validate tool name: must be alphanumeric + underscore only
    let tool_raw = tool_raw.trim_end_matches(|c: char| c.is_whitespace());
    if tool_raw.is_empty() || !tool_raw.chars().all(|c| c.is_alphanumeric() || c == '_') {
        return None;
    }

    let tool_name = map_tool_name_alias(tool_raw);

    // Try attribute-style: `key="value" key2="value2"`
    if value_part.contains("=\"") {
        let mut args = serde_json::Map::new();
        // Simple attribute parser: key="value" pairs
        let mut rest = value_part;
        while let Some(eq_pos) = rest.find("=\"") {
            let key_start = rest[..eq_pos]
                .rfind(|c: char| c.is_whitespace())
                .map(|p| p + 1)
                .unwrap_or(0);
            let key = rest[key_start..eq_pos]
                .trim()
                .trim_matches(|c: char| c == ',' || c == ';');
            let after_quote = &rest[eq_pos + 2..];
            if let Some(end_quote) = after_quote.find('"') {
                let value = &after_quote[..end_quote];
                if !key.is_empty() {
                    args.insert(
                        key.to_string(),
                        serde_json::Value::String(value.to_string()),
                    );
                }
                rest = &after_quote[end_quote + 1..];
            } else {
                break;
            }
        }
        if !args.is_empty() {
            return Some(ParsedToolCall {
                name: tool_name.to_string(),
                arguments: serde_json::Value::Object(args),
                tool_call_id: None,
            });
        }
    }

    // Try YAML-style multi-line: each line is `key: value`
    if value_part.contains('\n') {
        let mut args = serde_json::Map::new();
        for line in value_part.lines() {
            let line = line.trim();
            if line.is_empty() {
                continue;
            }
            if let Some(colon_pos) = line.find(':') {
                let key = line[..colon_pos].trim();
                let value = line[colon_pos + 1..].trim();
                if !key.is_empty() && !value.is_empty() {
                    // Normalize boolean-like values
                    let json_value = match value {
                        "true" | "yes" => serde_json::Value::Bool(true),
                        "false" | "no" => serde_json::Value::Bool(false),
                        _ => serde_json::Value::String(value.to_string()),
                    };
                    args.insert(key.to_string(), json_value);
                }
            }
        }
        if !args.is_empty() {
            return Some(ParsedToolCall {
                name: tool_name.to_string(),
                arguments: serde_json::Value::Object(args),
                tool_call_id: None,
            });
        }
    }

    // Single-value shortened: `tool>value`
    if !value_part.is_empty() {
        let param = default_param_for_tool(tool_raw);
        let arguments = match tool_name {
            "shell" => {
                if value_part.starts_with("http://") || value_part.starts_with("https://") {
                    if let Some(cmd) = build_curl_command(value_part) {
                        serde_json::json!({ "command": cmd })
                    } else {
                        serde_json::json!({ "command": value_part })
                    }
                } else {
                    serde_json::json!({ "command": value_part })
                }
            }
            "http_request" => serde_json::json!({"url": value_part, "method": "GET"}),
            _ => serde_json::json!({ param: value_part }),
        };
        return Some(ParsedToolCall {
            name: tool_name.to_string(),
            arguments,
            tool_call_id: None,
        });
    }

    None
}

// ── Tool-Call Parsing ─────────────────────────────────────────────────────
// LLM responses may contain tool calls in multiple formats depending on
// the provider. Parsing follows a priority chain:
//   1. OpenAI-style JSON with `tool_calls` array (native API)
//   2. XML tags: <tool_call>, <toolcall>, <tool-call>, <invoke>
//   3. Markdown code blocks with `tool_call` language
//   4. GLM-style line-based format (e.g. `shell/command>ls`)
// SECURITY: We never fall back to extracting arbitrary JSON from the
// response body, because that would enable prompt-injection attacks where
// malicious content in emails/files/web pages mimics a tool call.

/// Parse tool calls from an LLM response that uses XML-style function calling.
///
/// Expected format (common with system-prompt-guided tool use):
/// ```text
/// <tool_call>
/// {"name": "shell", "arguments": {"command": "ls"}}
/// </tool_call>
/// ```
///
/// Also accepts common tag variants (`<toolcall>`, `<tool-call>`) for model
/// compatibility.
///
/// Also supports JSON with `tool_calls` array from OpenAI-format responses.
fn parse_tool_calls(response: &str) -> (String, Vec<ParsedToolCall>) {
    // Strip `<think>...</think>` blocks before parsing.  Qwen and other
    // reasoning models embed chain-of-thought inline in the response text;
    // these tags can interfere with `<tool_call>` extraction and must be
    // removed first.
    let cleaned = strip_think_tags(response);
    let response = cleaned.as_str();

    let mut text_parts = Vec::new();
    let mut calls = Vec::new();
    let mut remaining = response;

    // First, try to parse as OpenAI-style JSON response with tool_calls array
    // This handles providers like Minimax that return tool_calls in native JSON format
    if let Ok(json_value) = serde_json::from_str::<serde_json::Value>(response.trim()) {
        calls = parse_tool_calls_from_json_value(&json_value);
        if !calls.is_empty() {
            // If we found tool_calls, extract any content field as text
            if let Some(content) = json_value.get("content").and_then(|v| v.as_str()) {
                if !content.trim().is_empty() {
                    text_parts.push(content.trim().to_string());
                }
            }
            return (text_parts.join("\n"), calls);
        }
    }

    if let Some((minimax_text, minimax_calls)) = parse_minimax_invoke_calls(response) {
        if !minimax_calls.is_empty() {
            return (minimax_text, minimax_calls);
        }
    }

    // Fall back to XML-style tool-call tag parsing.
    while let Some((start, open_tag)) = find_first_tag(remaining, &TOOL_CALL_OPEN_TAGS) {
        // Everything before the tag is text
        let before = &remaining[..start];
        if !before.trim().is_empty() {
            text_parts.push(before.trim().to_string());
        }

        let Some(close_tag) = (match open_tag {
            "<tool_call>" => Some("</tool_call>"),
            "<toolcall>" => Some("</toolcall>"),
            "<tool-call>" => Some("</tool-call>"),
            "<invoke>" => Some("</invoke>"),
            "<minimax:tool_call>" => Some("</minimax:tool_call>"),
            "<minimax:toolcall>" => Some("</minimax:toolcall>"),
            _ => None,
        }) else {
            break;
        };

        let after_open = &remaining[start + open_tag.len()..];
        if let Some(close_idx) = after_open.find(close_tag) {
            let inner = &after_open[..close_idx];
            let mut parsed_any = false;

            // Try JSON format first
            let json_values = extract_json_values(inner);
            for value in json_values {
                let parsed_calls = parse_tool_calls_from_json_value(&value);
                if !parsed_calls.is_empty() {
                    parsed_any = true;
                    calls.extend(parsed_calls);
                }
            }

            // If JSON parsing failed, try XML format (DeepSeek/GLM style)
            if !parsed_any {
                if let Some(xml_calls) = parse_xml_tool_calls(inner) {
                    calls.extend(xml_calls);
                    parsed_any = true;
                }
            }

            if !parsed_any {
                // GLM-style shortened body: `shell>uname -a` or `shell\ncommand: date`
                if let Some(glm_call) = parse_glm_shortened_body(inner) {
                    calls.push(glm_call);
                    parsed_any = true;
                }
            }

            if !parsed_any {
                tracing::warn!(
                    "Malformed <tool_call>: expected tool-call object in tag body (JSON/XML/GLM)"
                );
            }

            remaining = &after_open[close_idx + close_tag.len()..];
        } else {
            // Matching close tag not found — try cross-alias close tags first.
            // Models sometimes mix open/close tag aliases (e.g. <tool_call>...</invoke>).
            let mut resolved = false;
            if let Some((cross_idx, cross_tag)) = find_first_tag(after_open, &TOOL_CALL_CLOSE_TAGS)
            {
                let inner = &after_open[..cross_idx];
                let mut parsed_any = false;

                // Try JSON
                let json_values = extract_json_values(inner);
                for value in json_values {
                    let parsed_calls = parse_tool_calls_from_json_value(&value);
                    if !parsed_calls.is_empty() {
                        parsed_any = true;
                        calls.extend(parsed_calls);
                    }
                }

                // Try XML
                if !parsed_any {
                    if let Some(xml_calls) = parse_xml_tool_calls(inner) {
                        calls.extend(xml_calls);
                        parsed_any = true;
                    }
                }

                // Try GLM shortened body
                if !parsed_any {
                    if let Some(glm_call) = parse_glm_shortened_body(inner) {
                        calls.push(glm_call);
                        parsed_any = true;
                    }
                }

                if parsed_any {
                    remaining = &after_open[cross_idx + cross_tag.len()..];
                    resolved = true;
                }
            }

            if resolved {
                continue;
            }

            // No cross-alias close tag resolved — fall back to JSON recovery
            // from unclosed tags (brace-balancing).
            if let Some(json_end) = find_json_end(after_open) {
                if let Ok(value) =
                    serde_json::from_str::<serde_json::Value>(&after_open[..json_end])
                {
                    let parsed_calls = parse_tool_calls_from_json_value(&value);
                    if !parsed_calls.is_empty() {
                        calls.extend(parsed_calls);
                        remaining = strip_leading_close_tags(&after_open[json_end..]);
                        continue;
                    }
                }
            }

            if let Some((value, consumed_end)) = extract_first_json_value_with_end(after_open) {
                let parsed_calls = parse_tool_calls_from_json_value(&value);
                if !parsed_calls.is_empty() {
                    calls.extend(parsed_calls);
                    remaining = strip_leading_close_tags(&after_open[consumed_end..]);
                    continue;
                }
            }

            // Last resort: try GLM shortened body on everything after the open tag.
            // The model may have emitted `<tool_call>shell>ls` with no close tag at all.
            let glm_input = after_open.trim();
            if let Some(glm_call) = parse_glm_shortened_body(glm_input) {
                calls.push(glm_call);
                remaining = "";
                continue;
            }

            remaining = &remaining[start..];
            break;
        }
    }

    // If XML tags found nothing, try markdown code blocks with tool_call language.
    // Models behind OpenRouter sometimes output ```tool_call ... ``` or hybrid
    // ```tool_call ... </tool_call> instead of structured API calls or XML tags.
    if calls.is_empty() {
        static MD_TOOL_CALL_RE: LazyLock<Regex> = LazyLock::new(|| {
            Regex::new(
                r"(?s)```(?:tool[_-]?call|invoke)\s*\n(.*?)(?:```|</tool[_-]?call>|</toolcall>|</invoke>|</minimax:toolcall>)",
            )
            .unwrap()
        });
        let mut md_text_parts: Vec<String> = Vec::new();
        let mut last_end = 0;

        for cap in MD_TOOL_CALL_RE.captures_iter(response) {
            let full_match = cap.get(0).unwrap();
            let before = &response[last_end..full_match.start()];
            if !before.trim().is_empty() {
                md_text_parts.push(before.trim().to_string());
            }
            let inner = &cap[1];
            let json_values = extract_json_values(inner);
            for value in json_values {
                let parsed_calls = parse_tool_calls_from_json_value(&value);
                calls.extend(parsed_calls);
            }
            last_end = full_match.end();
        }

        if !calls.is_empty() {
            let after = &response[last_end..];
            if !after.trim().is_empty() {
                md_text_parts.push(after.trim().to_string());
            }
            text_parts = md_text_parts;
            remaining = "";
        }
    }

    // Try ```tool <name> format used by some providers (e.g., xAI grok)
    // Example: ```tool file_write\n{"path": "...", "content": "..."}\n```
    if calls.is_empty() {
        static MD_TOOL_NAME_RE: LazyLock<Regex> =
            LazyLock::new(|| Regex::new(r"(?s)```tool\s+(\w+)\s*\n(.*?)(?:```|$)").unwrap());
        let mut md_text_parts: Vec<String> = Vec::new();
        let mut last_end = 0;

        for cap in MD_TOOL_NAME_RE.captures_iter(response) {
            let full_match = cap.get(0).unwrap();
            let before = &response[last_end..full_match.start()];
            if !before.trim().is_empty() {
                md_text_parts.push(before.trim().to_string());
            }
            let tool_name = &cap[1];
            let inner = &cap[2];

            // Try to parse the inner content as JSON arguments
            let json_values = extract_json_values(inner);
            if json_values.is_empty() {
                // Log a warning if we found a tool block but couldn't parse arguments
                tracing::warn!(
                    tool_name = %tool_name,
                    inner = %inner.chars().take(100).collect::<String>(),
                    "Found ```tool <name> block but could not parse JSON arguments"
                );
            } else {
                for value in json_values {
                    let arguments = if value.is_object() {
                        value
                    } else {
                        serde_json::Value::Object(serde_json::Map::new())
                    };
                    calls.push(ParsedToolCall {
                        name: tool_name.to_string(),
                        arguments,
                        tool_call_id: None,
                    });
                }
            }
            last_end = full_match.end();
        }

        if !calls.is_empty() {
            let after = &response[last_end..];
            if !after.trim().is_empty() {
                md_text_parts.push(after.trim().to_string());
            }
            text_parts = md_text_parts;
            remaining = "";
        }
    }

    // XML attribute-style tool calls:
    // <minimax:toolcall>
    // <invoke name="shell">
    // <parameter name="command">ls</parameter>
    // </invoke>
    // </minimax:toolcall>
    if calls.is_empty() {
        let xml_calls = parse_xml_attribute_tool_calls(remaining);
        if !xml_calls.is_empty() {
            let mut cleaned_text = remaining.to_string();
            for call in xml_calls {
                calls.push(call);
                // Try to remove the XML from text
                if let Some(start) = cleaned_text.find("<minimax:toolcall>") {
                    if let Some(end) = cleaned_text.find("</minimax:toolcall>") {
                        let end_pos = end + "</minimax:toolcall>".len();
                        if end_pos <= cleaned_text.len() {
                            cleaned_text =
                                format!("{}{}", &cleaned_text[..start], &cleaned_text[end_pos..]);
                        }
                    }
                }
            }
            if !cleaned_text.trim().is_empty() {
                text_parts.push(cleaned_text.trim().to_string());
            }
            remaining = "";
        }
    }

    // Perl/hash-ref style tool calls:
    // TOOL_CALL
    // {tool => "shell", args => {
    //   --command "ls -la"
    //   --description "List current directory contents"
    // }}
    // /TOOL_CALL
    if calls.is_empty() {
        let perl_calls = parse_perl_style_tool_calls(remaining);
        if !perl_calls.is_empty() {
            let mut cleaned_text = remaining.to_string();
            for call in perl_calls {
                calls.push(call);
                // Try to remove the TOOL_CALL block from text
                while let Some(start) = cleaned_text.find("TOOL_CALL") {
                    if let Some(end) = cleaned_text.find("/TOOL_CALL") {
                        let end_pos = end + "/TOOL_CALL".len();
                        if end_pos <= cleaned_text.len() {
                            cleaned_text =
                                format!("{}{}", &cleaned_text[..start], &cleaned_text[end_pos..]);
                        }
                    } else {
                        break;
                    }
                }
            }
            if !cleaned_text.trim().is_empty() {
                text_parts.push(cleaned_text.trim().to_string());
            }
            remaining = "";
        }
    }

    // <FunctionCall>
    // file_read
    // <code>path>/Users/...</code>
    // </FunctionCall>
    if calls.is_empty() {
        let func_calls = parse_function_call_tool_calls(remaining);
        if !func_calls.is_empty() {
            let mut cleaned_text = remaining.to_string();
            for call in func_calls {
                calls.push(call);
                // Try to remove the FunctionCall block from text
                while let Some(start) = cleaned_text.find("<FunctionCall>") {
                    if let Some(end) = cleaned_text.find("</FunctionCall>") {
                        let end_pos = end + "</FunctionCall>".len();
                        if end_pos <= cleaned_text.len() {
                            cleaned_text =
                                format!("{}{}", &cleaned_text[..start], &cleaned_text[end_pos..]);
                        }
                    } else {
                        break;
                    }
                }
            }
            if !cleaned_text.trim().is_empty() {
                text_parts.push(cleaned_text.trim().to_string());
            }
            remaining = "";
        }
    }

    // GLM-style tool calls (browser_open/url>https://..., shell/command>ls, etc.)
    if calls.is_empty() {
        let glm_calls = parse_glm_style_tool_calls(remaining);
        if !glm_calls.is_empty() {
            let mut cleaned_text = remaining.to_string();
            for (name, args, raw) in &glm_calls {
                calls.push(ParsedToolCall {
                    name: name.clone(),
                    arguments: args.clone(),
                    tool_call_id: None,
                });
                if let Some(r) = raw {
                    cleaned_text = cleaned_text.replace(r, "");
                }
            }
            if !cleaned_text.trim().is_empty() {
                text_parts.push(cleaned_text.trim().to_string());
            }
            remaining = "";
        }
    }

    // SECURITY: We do NOT fall back to extracting arbitrary JSON from the response
    // here. That would enable prompt injection attacks where malicious content
    // (e.g., in emails, files, or web pages) could include JSON that mimics a
    // tool call. Tool calls MUST be explicitly wrapped in either:
    // 1. OpenAI-style JSON with a "tool_calls" array
    // 2. ZeroClaw tool-call tags (<tool_call>, <toolcall>, <tool-call>)
    // 3. Markdown code blocks with tool_call/toolcall/tool-call language
    // 4. Explicit GLM line-based call formats (e.g. `shell/command>...`)
    // This ensures only the LLM's intentional tool calls are executed.

    // Remaining text after last tool call
    if !remaining.trim().is_empty() {
        text_parts.push(remaining.trim().to_string());
    }

    (text_parts.join("\n"), calls)
}

/// Remove `<think>...</think>` blocks from model output.
/// Qwen and other reasoning models embed chain-of-thought inline in the
/// response text using `<think>` tags.  These must be removed before parsing
/// tool-call tags or displaying output.
fn strip_think_tags(s: &str) -> String {
    let mut result = String::with_capacity(s.len());
    let mut rest = s;
    loop {
        if let Some(start) = rest.find("<think>") {
            result.push_str(&rest[..start]);
            if let Some(end) = rest[start..].find("</think>") {
                rest = &rest[start + end + "</think>".len()..];
            } else {
                // Unclosed tag: drop the rest to avoid leaking partial reasoning.
                break;
            }
        } else {
            result.push_str(rest);
            break;
        }
    }
    result.trim().to_string()
}

/// Strip prompt-guided tool artifacts from visible output while preserving
/// raw model text in history for future turns.
fn strip_tool_result_blocks(text: &str) -> String {
    static TOOL_RESULT_RE: LazyLock<Regex> =
        LazyLock::new(|| Regex::new(r"(?s)<tool_result[^>]*>.*?</tool_result>").unwrap());
    static THINKING_RE: LazyLock<Regex> =
        LazyLock::new(|| Regex::new(r"(?s)<thinking>.*?</thinking>").unwrap());
    static THINK_RE: LazyLock<Regex> =
        LazyLock::new(|| Regex::new(r"(?s)<think>.*?</think>").unwrap());
    static TOOL_RESULTS_PREFIX_RE: LazyLock<Regex> =
        LazyLock::new(|| Regex::new(r"(?m)^\[Tool results\]\s*\n?").unwrap());
    static EXCESS_BLANK_LINES_RE: LazyLock<Regex> =
        LazyLock::new(|| Regex::new(r"\n{3,}").unwrap());

    let result = TOOL_RESULT_RE.replace_all(text, "");
    let result = THINKING_RE.replace_all(&result, "");
    let result = THINK_RE.replace_all(&result, "");
    let result = TOOL_RESULTS_PREFIX_RE.replace_all(&result, "");
    let result = EXCESS_BLANK_LINES_RE.replace_all(result.trim(), "\n\n");

    result.trim().to_string()
}

fn detect_tool_call_parse_issue(response: &str, parsed_calls: &[ParsedToolCall]) -> Option<String> {
    if !parsed_calls.is_empty() {
        return None;
    }

    let trimmed = response.trim();
    if trimmed.is_empty() {
        return None;
    }

    let looks_like_tool_payload = trimmed.contains("<tool_call")
        || trimmed.contains("<toolcall")
        || trimmed.contains("<tool-call")
        || trimmed.contains("```tool_call")
        || trimmed.contains("```toolcall")
        || trimmed.contains("```tool-call")
        || trimmed.contains("```tool file_")
        || trimmed.contains("```tool shell")
        || trimmed.contains("```tool web_")
        || trimmed.contains("```tool memory_")
        || trimmed.contains("```tool ") // Generic ```tool <name> pattern
        || trimmed.contains("\"tool_calls\"")
        || trimmed.contains("TOOL_CALL")
        || trimmed.contains("[TOOL_CALL]")
        || trimmed.contains("<FunctionCall>");

    if looks_like_tool_payload {
        Some("response resembled a tool-call payload but no valid tool call could be parsed".into())
    } else {
        None
    }
}

/// Build assistant history entry in JSON format for native tool-call APIs.
/// `convert_messages` in the OpenRouter provider parses this JSON to reconstruct
/// the proper `NativeMessage` with structured `tool_calls`.
fn build_native_assistant_history(
    text: &str,
    tool_calls: &[ToolCall],
    reasoning_content: Option<&str>,
) -> String {
    let calls_json: Vec<serde_json::Value> = tool_calls
        .iter()
        .map(|tc| {
            serde_json::json!({
                "id": tc.id,
                "name": tc.name,
                "arguments": tc.arguments,
            })
        })
        .collect();

    let content = if text.trim().is_empty() {
        serde_json::Value::Null
    } else {
        serde_json::Value::String(text.trim().to_string())
    };

    let mut obj = serde_json::json!({
        "content": content,
        "tool_calls": calls_json,
    });

    if let Some(rc) = reasoning_content {
        obj.as_object_mut().unwrap().insert(
            "reasoning_content".to_string(),
            serde_json::Value::String(rc.to_string()),
        );
    }

    obj.to_string()
}

fn build_native_assistant_history_from_parsed_calls(
    text: &str,
    tool_calls: &[ParsedToolCall],
    reasoning_content: Option<&str>,
) -> Option<String> {
    let calls_json = tool_calls
        .iter()
        .map(|tc| {
            Some(serde_json::json!({
                "id": tc.tool_call_id.clone()?,
                "name": tc.name,
                "arguments": serde_json::to_string(&tc.arguments).unwrap_or_else(|_| "{}".to_string()),
            }))
        })
        .collect::<Option<Vec<_>>>()?;

    let content = if text.trim().is_empty() {
        serde_json::Value::Null
    } else {
        serde_json::Value::String(text.trim().to_string())
    };

    let mut obj = serde_json::json!({
        "content": content,
        "tool_calls": calls_json,
    });

    if let Some(rc) = reasoning_content {
        obj.as_object_mut().unwrap().insert(
            "reasoning_content".to_string(),
            serde_json::Value::String(rc.to_string()),
        );
    }

    Some(obj.to_string())
}

fn resolve_display_text(
    response_text: &str,
    parsed_text: &str,
    has_tool_calls: bool,
    has_native_tool_calls: bool,
) -> String {
    if has_tool_calls {
        if !parsed_text.is_empty() {
            return parsed_text.to_string();
        }
        if has_native_tool_calls {
            return response_text.to_string();
        }
        return String::new();
    }

    if parsed_text.is_empty() {
        response_text.to_string()
    } else {
        parsed_text.to_string()
    }
}

#[derive(Debug, Clone)]
pub(crate) struct ParsedToolCall {
    pub(crate) name: String,
    pub(crate) arguments: serde_json::Value,
    pub(crate) tool_call_id: Option<String>,
}

#[derive(Debug)]
pub(crate) struct ToolLoopCancelled;

impl std::fmt::Display for ToolLoopCancelled {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.write_str("tool loop cancelled")
    }
}

impl std::error::Error for ToolLoopCancelled {}

pub(crate) fn is_tool_loop_cancelled(err: &anyhow::Error) -> bool {
    err.chain().any(|source| source.is::<ToolLoopCancelled>())
}

#[derive(Debug)]
pub(crate) struct ModelSwitchRequested {
    pub provider: String,
    pub model: String,
}

impl std::fmt::Display for ModelSwitchRequested {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(
            f,
            "model switch requested to {} {}",
            self.provider, self.model
        )
    }
}

impl std::error::Error for ModelSwitchRequested {}

pub(crate) fn is_model_switch_requested(err: &anyhow::Error) -> Option<(String, String)> {
    err.chain()
        .filter_map(|source| source.downcast_ref::<ModelSwitchRequested>())
        .map(|e| (e.provider.clone(), e.model.clone()))
        .next()
}

#[derive(Debug, Default)]
struct StreamedChatOutcome {
    response_text: String,
    tool_calls: Vec<ToolCall>,
    forwarded_live_deltas: bool,
}

async fn consume_provider_streaming_response(
    provider: &dyn Provider,
    messages: &[ChatMessage],
    request_tools: Option<&[crate::tools::ToolSpec]>,
    model: &str,
    temperature: f64,
    cancellation_token: Option<&CancellationToken>,
    on_delta: Option<&tokio::sync::mpsc::Sender<DraftEvent>>,
) -> Result<StreamedChatOutcome> {
    let mut provider_stream = provider.stream_chat(
        ChatRequest {
            messages,
            tools: request_tools,
        },
        model,
        temperature,
        crate::providers::traits::StreamOptions::new(true),
    );
    let mut outcome = StreamedChatOutcome::default();
    let mut delta_sender = on_delta;
    let mut suppress_forwarding = false;
    let mut marker_window = String::new();

    loop {
        let next_chunk = if let Some(token) = cancellation_token {
            tokio::select! {
                () = token.cancelled() => return Err(ToolLoopCancelled.into()),
                chunk = provider_stream.next() => chunk,
            }
        } else {
            provider_stream.next().await
        };

        let Some(event_result) = next_chunk else {
            break;
        };

        let event = event_result.map_err(|err| anyhow::anyhow!("provider stream error: {err}"))?;
        match event {
            StreamEvent::Final => break,
            StreamEvent::ToolCall(tool_call) => {
                outcome.tool_calls.push(tool_call);
                suppress_forwarding = true;
                if outcome.forwarded_live_deltas {
                    if let Some(tx) = delta_sender {
                        let _ = tx.send(DraftEvent::Clear).await;
                    }
                    outcome.forwarded_live_deltas = false;
                }
            }
            StreamEvent::PreExecutedToolCall { .. } | StreamEvent::PreExecutedToolResult { .. } => {
                // Pre-executed tool events are for observability only.
                // They are forwarded to the gateway via turn_streamed but
                // do not affect the agent's tool dispatch loop.
            }
            StreamEvent::TextDelta(chunk) => {
                if chunk.delta.is_empty() {
                    continue;
                }

                outcome.response_text.push_str(&chunk.delta);
                marker_window.push_str(&chunk.delta);

                if marker_window.len() > STREAM_TOOL_MARKER_WINDOW_CHARS {
                    let keep_from = marker_window.len() - STREAM_TOOL_MARKER_WINDOW_CHARS;
                    let boundary = marker_window
                        .char_indices()
                        .find(|(idx, _)| *idx >= keep_from)
                        .map_or(0, |(idx, _)| idx);
                    marker_window.drain(..boundary);
                }

                if !suppress_forwarding && {
                    let lowered = marker_window.to_ascii_lowercase();
                    lowered.contains("<tool_call")
                        || lowered.contains("<toolcall")
                        || lowered.contains("\"tool_calls\"")
                } {
                    suppress_forwarding = true;
                    if outcome.forwarded_live_deltas {
                        if let Some(tx) = delta_sender {
                            let _ = tx.send(DraftEvent::Clear).await;
                        }
                        outcome.forwarded_live_deltas = false;
                    }
                }

                if suppress_forwarding {
                    continue;
                }

                if let Some(tx) = delta_sender {
                    if !outcome.forwarded_live_deltas {
                        let _ = tx.send(DraftEvent::Clear).await;
                        outcome.forwarded_live_deltas = true;
                    }
                    if tx.send(DraftEvent::Content(chunk.delta)).await.is_err() {
                        delta_sender = None;
                    }
                }
            }
        }
    }

    Ok(outcome)
}

/// Execute a single turn of the agent loop: send messages, parse tool calls,
/// execute tools, and loop until the LLM produces a final text response.
/// When `silent` is true, suppresses stdout (for channel use).
#[allow(clippy::too_many_arguments)]
pub(crate) async fn agent_turn(
    provider: &dyn Provider,
    history: &mut Vec<ChatMessage>,
    tools_registry: &[Box<dyn Tool>],
    observer: &dyn Observer,
    provider_name: &str,
    model: &str,
    temperature: f64,
    silent: bool,
    channel_name: &str,
    channel_reply_target: Option<&str>,
    multimodal_config: &crate::config::MultimodalConfig,
    max_tool_iterations: usize,
    approval: Option<&ApprovalManager>,
    excluded_tools: &[String],
    dedup_exempt_tools: &[String],
    activated_tools: Option<&std::sync::Arc<std::sync::Mutex<crate::tools::ActivatedToolSet>>>,
    model_switch_callback: Option<ModelSwitchCallback>,
) -> Result<String> {
    run_tool_call_loop(
        provider,
        history,
        tools_registry,
        observer,
        provider_name,
        model,
        temperature,
        silent,
        approval,
        channel_name,
        channel_reply_target,
        multimodal_config,
        max_tool_iterations,
        None,
        None,
        None,
        excluded_tools,
        dedup_exempt_tools,
        activated_tools,
        model_switch_callback,
        &crate::config::PacingConfig::default(),
        0,    // max_tool_result_chars: 0 = disabled (legacy callers)
        0,    // context_token_budget: 0 = disabled (legacy callers)
        None, // shared_budget: no shared budget for legacy callers
    )
    .await
}

fn maybe_inject_channel_delivery_defaults(
    tool_name: &str,
    tool_args: &mut serde_json::Value,
    channel_name: &str,
    channel_reply_target: Option<&str>,
) {
    if tool_name != "cron_add" {
        return;
    }

    if !matches!(
        channel_name,
        "telegram" | "discord" | "slack" | "mattermost" | "matrix"
    ) {
        return;
    }

    let Some(reply_target) = channel_reply_target
        .map(str::trim)
        .filter(|value| !value.is_empty())
    else {
        return;
    };

    let Some(args) = tool_args.as_object_mut() else {
        return;
    };

    let is_agent_job = args
        .get("job_type")
        .and_then(serde_json::Value::as_str)
        .is_some_and(|job_type| job_type.eq_ignore_ascii_case("agent"))
        || args
            .get("prompt")
            .and_then(serde_json::Value::as_str)
            .is_some_and(|prompt| !prompt.trim().is_empty());
    if !is_agent_job {
        return;
    }

    let default_delivery = || {
        serde_json::json!({
            "mode": "announce",
            "channel": channel_name,
            "to": reply_target,
        })
    };

    match args.get_mut("delivery") {
        None => {
            args.insert("delivery".to_string(), default_delivery());
        }
        Some(serde_json::Value::Null) => {
            *args.get_mut("delivery").expect("delivery key exists") = default_delivery();
        }
        Some(serde_json::Value::Object(delivery)) => {
            if delivery
                .get("mode")
                .and_then(serde_json::Value::as_str)
                .is_some_and(|mode| mode.eq_ignore_ascii_case("none"))
            {
                return;
            }

            delivery
                .entry("mode".to_string())
                .or_insert_with(|| serde_json::Value::String("announce".to_string()));

            let needs_channel = delivery
                .get("channel")
                .and_then(serde_json::Value::as_str)
                .is_none_or(|value| value.trim().is_empty());
            if needs_channel {
                delivery.insert(
                    "channel".to_string(),
                    serde_json::Value::String(channel_name.to_string()),
                );
            }

            let needs_target = delivery
                .get("to")
                .and_then(serde_json::Value::as_str)
                .is_none_or(|value| value.trim().is_empty());
            if needs_target {
                delivery.insert(
                    "to".to_string(),
                    serde_json::Value::String(reply_target.to_string()),
                );
            }
        }
        Some(_) => {}
    }
}

// ── Agent Tool-Call Loop ──────────────────────────────────────────────────
// Core agentic iteration: send conversation to the LLM, parse any tool
// calls from the response, execute them, append results to history, and
// repeat until the LLM produces a final text-only answer.
//
// Loop invariant: at the start of each iteration, `history` contains the
// full conversation so far (system prompt + user messages + prior tool
// results). The loop exits when:
//   • the LLM returns no tool calls (final answer), or
//   • max_iterations is reached (runaway safety), or
//   • the cancellation token fires (external abort).

/// Execute a single turn of the agent loop: send messages, parse tool calls,
/// execute tools, and loop until the LLM produces a final text response.
#[allow(clippy::too_many_arguments)]
pub(crate) async fn run_tool_call_loop(
    provider: &dyn Provider,
    history: &mut Vec<ChatMessage>,
    tools_registry: &[Box<dyn Tool>],
    observer: &dyn Observer,
    provider_name: &str,
    model: &str,
    temperature: f64,
    silent: bool,
    approval: Option<&ApprovalManager>,
    channel_name: &str,
    channel_reply_target: Option<&str>,
    multimodal_config: &crate::config::MultimodalConfig,
    max_tool_iterations: usize,
    cancellation_token: Option<CancellationToken>,
    on_delta: Option<tokio::sync::mpsc::Sender<DraftEvent>>,
    hooks: Option<&crate::hooks::HookRunner>,
    excluded_tools: &[String],
    dedup_exempt_tools: &[String],
    activated_tools: Option<&std::sync::Arc<std::sync::Mutex<crate::tools::ActivatedToolSet>>>,
    model_switch_callback: Option<ModelSwitchCallback>,
    pacing: &crate::config::PacingConfig,
    max_tool_result_chars: usize,
    context_token_budget: usize,
    shared_budget: Option<Arc<std::sync::atomic::AtomicUsize>>,
) -> Result<String> {
    let max_iterations = if max_tool_iterations == 0 {
        DEFAULT_MAX_TOOL_ITERATIONS
    } else {
        max_tool_iterations
    };

    let turn_id = Uuid::new_v4().to_string();
    let loop_started_at = Instant::now();
    let loop_ignore_tools: HashSet<&str> = pacing
        .loop_ignore_tools
        .iter()
        .map(String::as_str)
        .collect();
    let mut consecutive_identical_outputs: usize = 0;
    let mut last_tool_output_hash: Option<u64> = None;

    let mut loop_detector = crate::agent::loop_detector::LoopDetector::new(
        crate::agent::loop_detector::LoopDetectorConfig {
            enabled: pacing.loop_detection_enabled,
            window_size: pacing.loop_detection_window_size,
            max_repeats: pacing.loop_detection_max_repeats,
        },
    );

    for iteration in 0..max_iterations {
        let mut seen_tool_signatures: HashSet<(String, String)> = HashSet::new();

        if cancellation_token
            .as_ref()
            .is_some_and(CancellationToken::is_cancelled)
        {
            return Err(ToolLoopCancelled.into());
        }

        // Shared iteration budget: parent + subagents share a global counter
        if let Some(ref budget) = shared_budget {
            let remaining = budget.load(std::sync::atomic::Ordering::Relaxed);
            if remaining == 0 {
                tracing::warn!("Shared iteration budget exhausted at iteration {iteration}");
                break;
            }
            budget.fetch_sub(1, std::sync::atomic::Ordering::Relaxed);
        }

        // Preemptive context management: trim history before it overflows
        if context_token_budget > 0 {
            let estimated = estimate_history_tokens(history);
            if estimated > context_token_budget {
                tracing::info!(
                    estimated,
                    budget = context_token_budget,
                    iteration = iteration + 1,
                    "Preemptive context trim: estimated tokens exceed budget"
                );
                let chars_saved = fast_trim_tool_results(history, 4);
                if chars_saved > 0 {
                    tracing::info!(chars_saved, "Preemptive fast-trim applied");
                }
                // If still over budget, use the history pruner for deeper cleanup
                let recheck = estimate_history_tokens(history);
                if recheck > context_token_budget {
                    let stats = crate::agent::history_pruner::prune_history(
                        history,
                        &crate::agent::history_pruner::HistoryPrunerConfig {
                            enabled: true,
                            max_tokens: context_token_budget,
                            keep_recent: 4,
                            collapse_tool_results: true,
                        },
                    );
                    if stats.dropped_messages > 0 || stats.collapsed_pairs > 0 {
                        tracing::info!(
                            collapsed = stats.collapsed_pairs,
                            dropped = stats.dropped_messages,
                            "Preemptive history prune applied"
                        );
                    }
                }
            }
        }

        // Check if model switch was requested via model_switch tool
        if let Some(ref callback) = model_switch_callback {
            if let Ok(guard) = callback.lock() {
                if let Some((new_provider, new_model)) = guard.as_ref() {
                    if new_provider != provider_name || new_model != model {
                        tracing::info!(
                            "Model switch detected: {} {} -> {} {}",
                            provider_name,
                            model,
                            new_provider,
                            new_model
                        );
                        return Err(ModelSwitchRequested {
                            provider: new_provider.clone(),
                            model: new_model.clone(),
                        }
                        .into());
                    }
                }
            }
        }

        // Rebuild tool_specs each iteration so newly activated deferred tools appear.
        let mut tool_specs: Vec<crate::tools::ToolSpec> = tools_registry
            .iter()
            .filter(|tool| !excluded_tools.iter().any(|ex| ex == tool.name()))
            .map(|tool| tool.spec())
            .collect();
        if let Some(at) = activated_tools {
            for spec in at.lock().unwrap().tool_specs() {
                if !excluded_tools.iter().any(|ex| ex == &spec.name) {
                    tool_specs.push(spec);
                }
            }
        }
        let use_native_tools = provider.supports_native_tools() && !tool_specs.is_empty();

        let image_marker_count = multimodal::count_image_markers(history);

        // ── Vision provider routing ──────────────────────────
        // When the default provider lacks vision support but a dedicated
        // vision_provider is configured, create it on demand and use it
        // for this iteration.  Otherwise, preserve the original error.
        let vision_provider_box: Option<Box<dyn Provider>> = if image_marker_count > 0
            && !provider.supports_vision()
        {
            if let Some(ref vp) = multimodal_config.vision_provider {
                let vp_instance = providers::create_provider(vp, None)
                    .map_err(|e| anyhow::anyhow!("failed to create vision provider '{vp}': {e}"))?;
                if !vp_instance.supports_vision() {
                    return Err(ProviderCapabilityError {
                        provider: vp.clone(),
                        capability: "vision".to_string(),
                        message: format!(
                            "configured vision_provider '{vp}' does not support vision input"
                        ),
                    }
                    .into());
                }
                Some(vp_instance)
            } else {
                return Err(ProviderCapabilityError {
                        provider: provider_name.to_string(),
                        capability: "vision".to_string(),
                        message: format!(
                            "received {image_marker_count} image marker(s), but this provider does not support vision input"
                        ),
                    }
                    .into());
            }
        } else {
            None
        };

        let (active_provider, active_provider_name, active_model): (&dyn Provider, &str, &str) =
            if let Some(ref vp_box) = vision_provider_box {
                let vp_name = multimodal_config
                    .vision_provider
                    .as_deref()
                    .unwrap_or(provider_name);
                let vm = multimodal_config.vision_model.as_deref().unwrap_or(model);
                (vp_box.as_ref(), vp_name, vm)
            } else {
                (provider, provider_name, model)
            };

        let prepared_messages =
            multimodal::prepare_messages_for_provider(history, multimodal_config).await?;

        // ── Progress: LLM thinking ────────────────────────────
        if let Some(ref tx) = on_delta {
            let phase = if iteration == 0 {
                "\u{1f914} Thinking...\n".to_string()
            } else {
                format!("\u{1f914} Thinking (round {})...\n", iteration + 1)
            };
            let _ = tx.send(DraftEvent::Progress(phase)).await;
        }

        observer.record_event(&ObserverEvent::LlmRequest {
            provider: active_provider_name.to_string(),
            model: active_model.to_string(),
            messages_count: history.len(),
        });
        runtime_trace::record_event(
            "llm_request",
            Some(channel_name),
            Some(active_provider_name),
            Some(active_model),
            Some(&turn_id),
            None,
            None,
            serde_json::json!({
                "iteration": iteration + 1,
                "messages_count": history.len(),
            }),
        );

        let llm_started_at = Instant::now();

        // Fire void hook before LLM call
        if let Some(hooks) = hooks {
            hooks.fire_llm_input(history, model).await;
        }

        // Budget enforcement — block if limit exceeded (no-op when not scoped)
        if let Some(BudgetCheck::Exceeded {
            current_usd,
            limit_usd,
            period,
        }) = check_tool_loop_budget()
        {
            return Err(anyhow::anyhow!(
                "Budget exceeded: ${:.4} of ${:.2} {:?} limit. Cannot make further API calls until the budget resets.",
                current_usd,
                limit_usd,
                period
            ));
        }

        // Unified path via Provider::chat so provider-specific native tool logic
        // (OpenAI/Anthropic/OpenRouter/compatible adapters) is honored.
        let request_tools = if use_native_tools {
            Some(tool_specs.as_slice())
        } else {
            None
        };
        let should_consume_provider_stream = on_delta.is_some()
            && provider.supports_streaming()
            && (request_tools.is_none() || provider.supports_streaming_tool_events());
        tracing::debug!(
            has_on_delta = on_delta.is_some(),
            supports_streaming = provider.supports_streaming(),
            should_consume_provider_stream,
            "Streaming decision for iteration {}",
            iteration + 1,
        );
        let mut streamed_live_deltas = false;

        let chat_result = if should_consume_provider_stream {
            match consume_provider_streaming_response(
                active_provider,
                &prepared_messages.messages,
                request_tools,
                active_model,
                temperature,
                cancellation_token.as_ref(),
                on_delta.as_ref(),
            )
            .await
            {
                Ok(streamed) => {
                    streamed_live_deltas = streamed.forwarded_live_deltas;
                    Ok(crate::providers::ChatResponse {
                        text: Some(streamed.response_text),
                        tool_calls: streamed.tool_calls,
                        usage: None,
                        reasoning_content: None,
                    })
                }
                Err(stream_err) => {
                    tracing::warn!(
                        provider = active_provider_name,
                        model = active_model,
                        iteration = iteration + 1,
                        "provider streaming failed, falling back to non-streaming chat: {stream_err}"
                    );
                    runtime_trace::record_event(
                        "llm_stream_fallback",
                        Some(channel_name),
                        Some(active_provider_name),
                        Some(active_model),
                        Some(&turn_id),
                        Some(false),
                        Some("provider stream failed; fallback to non-streaming chat"),
                        serde_json::json!({
                            "iteration": iteration + 1,
                            "error": scrub_credentials(&stream_err.to_string()),
                        }),
                    );
                    if let Some(ref tx) = on_delta {
                        let _ = tx.send(DraftEvent::Clear).await;
                    }
                    {
                        let chat_future = active_provider.chat(
                            ChatRequest {
                                messages: &prepared_messages.messages,
                                tools: request_tools,
                            },
                            active_model,
                            temperature,
                        );
                        if let Some(token) = cancellation_token.as_ref() {
                            tokio::select! {
                                () = token.cancelled() => Err(ToolLoopCancelled.into()),
                                result = chat_future => result,
                            }
                        } else {
                            chat_future.await
                        }
                    }
                }
            }
        } else {
            // Non-streaming path: wrap with optional per-step timeout from
            // pacing config to catch hung model responses.
            let chat_future = active_provider.chat(
                ChatRequest {
                    messages: &prepared_messages.messages,
                    tools: request_tools,
                },
                active_model,
                temperature,
            );

            match pacing.step_timeout_secs {
                Some(step_secs) if step_secs > 0 => {
                    let step_timeout = Duration::from_secs(step_secs);
                    if let Some(token) = cancellation_token.as_ref() {
                        tokio::select! {
                            () = token.cancelled() => return Err(ToolLoopCancelled.into()),
                            result = tokio::time::timeout(step_timeout, chat_future) => {
                                match result {
                                    Ok(inner) => inner,
                                    Err(_) => anyhow::bail!(
                                        "LLM inference step timed out after {step_secs}s (step_timeout_secs)"
                                    ),
                                }
                            },
                        }
                    } else {
                        match tokio::time::timeout(step_timeout, chat_future).await {
                            Ok(inner) => inner,
                            Err(_) => anyhow::bail!(
                                "LLM inference step timed out after {step_secs}s (step_timeout_secs)"
                            ),
                        }
                    }
                }
                _ => {
                    if let Some(token) = cancellation_token.as_ref() {
                        tokio::select! {
                            () = token.cancelled() => return Err(ToolLoopCancelled.into()),
                            result = chat_future => result,
                        }
                    } else {
                        chat_future.await
                    }
                }
            }
        };

        let (
            response_text,
            parsed_text,
            tool_calls,
            assistant_history_content,
            native_tool_calls,
            _parse_issue_detected,
            response_streamed_live,
        ) = match chat_result {
            Ok(resp) => {
                let (resp_input_tokens, resp_output_tokens) = resp
                    .usage
                    .as_ref()
                    .map(|u| (u.input_tokens, u.output_tokens))
                    .unwrap_or((None, None));

                observer.record_event(&ObserverEvent::LlmResponse {
                    provider: provider_name.to_string(),
                    model: model.to_string(),
                    duration: llm_started_at.elapsed(),
                    success: true,
                    error_message: None,
                    input_tokens: resp_input_tokens,
                    output_tokens: resp_output_tokens,
                });

                // Record cost via task-local tracker (no-op when not scoped)
                let _ = resp
                    .usage
                    .as_ref()
                    .and_then(|usage| record_tool_loop_cost_usage(provider_name, model, usage));

                let response_text = resp.text_or_empty().to_string();
                // First try native structured tool calls (OpenAI-format).
                // Fall back to text-based parsing (XML tags, markdown blocks,
                // GLM format) only if the provider returned no native calls —
                // this ensures we support both native and prompt-guided models.
                let mut calls: Vec<ParsedToolCall> = resp
                    .tool_calls
                    .iter()
                    .map(|call| ParsedToolCall {
                        name: call.name.clone(),
                        arguments: serde_json::from_str::<serde_json::Value>(&call.arguments)
                            .unwrap_or_else(|_| serde_json::Value::Object(serde_json::Map::new())),
                        tool_call_id: Some(call.id.clone()),
                    })
                    .collect();
                let mut parsed_text = String::new();

                if calls.is_empty() {
                    let (fallback_text, fallback_calls) = parse_tool_calls(&response_text);
                    if !fallback_text.is_empty() {
                        parsed_text = fallback_text;
                    }
                    calls = fallback_calls;
                }

                let parse_issue = detect_tool_call_parse_issue(&response_text, &calls);
                if let Some(ref issue) = parse_issue {
                    runtime_trace::record_event(
                        "tool_call_parse_issue",
                        Some(channel_name),
                        Some(provider_name),
                        Some(model),
                        Some(&turn_id),
                        Some(false),
                        Some(issue.as_str()),
                        serde_json::json!({
                            "iteration": iteration + 1,
                            "response_excerpt": truncate_with_ellipsis(
                                &scrub_credentials(&response_text),
                                600
                            ),
                        }),
                    );
                }

                runtime_trace::record_event(
                    "llm_response",
                    Some(channel_name),
                    Some(provider_name),
                    Some(model),
                    Some(&turn_id),
                    Some(true),
                    None,
                    serde_json::json!({
                        "iteration": iteration + 1,
                        "duration_ms": llm_started_at.elapsed().as_millis(),
                        "input_tokens": resp_input_tokens,
                        "output_tokens": resp_output_tokens,
                        "raw_response": scrub_credentials(&response_text),
                        "native_tool_calls": resp.tool_calls.len(),
                        "parsed_tool_calls": calls.len(),
                    }),
                );

                // Preserve native tool call IDs in assistant history so role=tool
                // follow-up messages can reference the exact call id.
                let reasoning_content = resp.reasoning_content.clone();
                let assistant_history_content = if resp.tool_calls.is_empty() {
                    if use_native_tools {
                        build_native_assistant_history_from_parsed_calls(
                            &response_text,
                            &calls,
                            reasoning_content.as_deref(),
                        )
                        .unwrap_or_else(|| response_text.clone())
                    } else {
                        response_text.clone()
                    }
                } else {
                    build_native_assistant_history(
                        &response_text,
                        &resp.tool_calls,
                        reasoning_content.as_deref(),
                    )
                };

                let native_calls = resp.tool_calls;
                (
                    response_text,
                    parsed_text,
                    calls,
                    assistant_history_content,
                    native_calls,
                    parse_issue.is_some(),
                    streamed_live_deltas,
                )
            }
            Err(e) => {
                let safe_error = crate::providers::sanitize_api_error(&e.to_string());
                observer.record_event(&ObserverEvent::LlmResponse {
                    provider: provider_name.to_string(),
                    model: model.to_string(),
                    duration: llm_started_at.elapsed(),
                    success: false,
                    error_message: Some(safe_error.clone()),
                    input_tokens: None,
                    output_tokens: None,
                });
                runtime_trace::record_event(
                    "llm_response",
                    Some(channel_name),
                    Some(provider_name),
                    Some(model),
                    Some(&turn_id),
                    Some(false),
                    Some(&safe_error),
                    serde_json::json!({
                        "iteration": iteration + 1,
                        "duration_ms": llm_started_at.elapsed().as_millis(),
                    }),
                );

                // Context overflow recovery: trim history and retry
                if crate::providers::reliable::is_context_window_exceeded(&e) {
                    tracing::warn!(
                        iteration = iteration + 1,
                        "Context window exceeded, attempting in-loop recovery"
                    );

                    // Step 1: fast-trim old tool results (cheap)
                    let chars_saved = fast_trim_tool_results(history, 4);
                    if chars_saved > 0 {
                        tracing::info!(
                            chars_saved,
                            "Context recovery: trimmed old tool results, retrying"
                        );
                        continue;
                    }

                    // Step 2: emergency drop oldest non-system messages
                    let dropped = emergency_history_trim(history, 4);
                    if dropped > 0 {
                        tracing::info!(dropped, "Context recovery: dropped old messages, retrying");
                        continue;
                    }

                    // Nothing left to trim — truly unrecoverable
                    tracing::error!("Context overflow unrecoverable: no trimmable messages");
                }

                return Err(e);
            }
        };

        let display_text = if parsed_text.is_empty() {
            response_text.clone()
        } else {
            parsed_text
        };

        // ── Progress: LLM responded ─────────────────────────────
        if let Some(ref tx) = on_delta {
            let llm_secs = llm_started_at.elapsed().as_secs();
            if !tool_calls.is_empty() {
                let _ = tx
                    .send(DraftEvent::Progress(format!(
                        "\u{1f4ac} Got {} tool call(s) ({llm_secs}s)\n",
                        tool_calls.len()
                    )))
                    .await;
            }
        }

        if tool_calls.is_empty() {
            runtime_trace::record_event(
                "turn_final_response",
                Some(channel_name),
                Some(provider_name),
                Some(model),
                Some(&turn_id),
                Some(true),
                None,
                serde_json::json!({
                    "iteration": iteration + 1,
                    "text": scrub_credentials(&display_text),
                }),
            );
            // No tool calls — this is the final response.
            // If a streaming sender is provided, relay the text in small chunks
            // so the channel can progressively update the draft message.
            if let Some(ref tx) = on_delta {
                let should_emit_post_hoc_chunks =
                    !response_streamed_live || display_text != response_text;
                if !should_emit_post_hoc_chunks {
                    history.push(ChatMessage::assistant(response_text.clone()));
                    return Ok(display_text);
                }
                // Clear accumulated progress lines before streaming the final answer.
                let _ = tx.send(DraftEvent::Clear).await;
                // Split on whitespace boundaries, accumulating chunks of at least
                // STREAM_CHUNK_MIN_CHARS characters for progressive draft updates.
                let mut chunk = String::new();
                for word in display_text.split_inclusive(char::is_whitespace) {
                    if cancellation_token
                        .as_ref()
                        .is_some_and(CancellationToken::is_cancelled)
                    {
                        return Err(ToolLoopCancelled.into());
                    }
                    chunk.push_str(word);
                    if chunk.len() >= STREAM_CHUNK_MIN_CHARS
                        && tx
                            .send(DraftEvent::Content(std::mem::take(&mut chunk)))
                            .await
                            .is_err()
                    {
                        break; // receiver dropped
                    }
                }
                if !chunk.is_empty() {
                    let _ = tx.send(DraftEvent::Content(chunk)).await;
                }
            }
            history.push(ChatMessage::assistant(response_text.clone()));
            return Ok(display_text);
        }

        // Native tool-call providers can return assistant text separately from
        // the structured call payload; relay it to draft-capable channels.
        if !display_text.is_empty() {
            if !native_tool_calls.is_empty() {
                if let Some(ref tx) = on_delta {
                    let mut narration = display_text.clone();
                    if !narration.ends_with('\n') {
                        narration.push('\n');
                    }
                    let _ = tx.send(DraftEvent::Content(narration)).await;
                }
            }
            if !silent {
                print!("{display_text}");
                let _ = std::io::stdout().flush();
            }
        }

        // Execute tool calls and build results. `individual_results` tracks per-call output so
        // native-mode history can emit one role=tool message per tool call with the correct ID.
        //
        // When multiple tool calls are present and interactive CLI approval is not needed, run
        // tool executions concurrently for lower wall-clock latency.
        let mut tool_results = String::new();
        let mut individual_results: Vec<(Option<String>, String)> = Vec::new();
        let mut ordered_results: Vec<Option<(String, Option<String>, ToolExecutionOutcome)>> =
            (0..tool_calls.len()).map(|_| None).collect();
        let allow_parallel_execution = should_execute_tools_in_parallel(&tool_calls, approval);
        let mut executable_indices: Vec<usize> = Vec::new();
        let mut executable_calls: Vec<ParsedToolCall> = Vec::new();

        for (idx, call) in tool_calls.iter().enumerate() {
            // ── Hook: before_tool_call (modifying) ──────────
            let mut tool_name = call.name.clone();
            let mut tool_args = call.arguments.clone();
            if let Some(hooks) = hooks {
                match hooks
                    .run_before_tool_call(tool_name.clone(), tool_args.clone())
                    .await
                {
                    crate::hooks::HookResult::Cancel(reason) => {
                        tracing::info!(tool = %call.name, %reason, "tool call cancelled by hook");
                        let cancelled = format!("Cancelled by hook: {reason}");
                        runtime_trace::record_event(
                            "tool_call_result",
                            Some(channel_name),
                            Some(provider_name),
                            Some(model),
                            Some(&turn_id),
                            Some(false),
                            Some(&cancelled),
                            serde_json::json!({
                                "iteration": iteration + 1,
                                "tool": call.name,
                                "arguments": scrub_credentials(&tool_args.to_string()),
                            }),
                        );
                        if let Some(ref tx) = on_delta {
                            let _ = tx
                                .send(DraftEvent::Progress(format!(
                                    "\u{274c} {}: {}\n",
                                    call.name,
                                    truncate_with_ellipsis(&scrub_credentials(&cancelled), 200)
                                )))
                                .await;
                        }
                        ordered_results[idx] = Some((
                            call.name.clone(),
                            call.tool_call_id.clone(),
                            ToolExecutionOutcome {
                                output: cancelled,
                                success: false,
                                error_reason: Some(scrub_credentials(&reason)),
                                duration: Duration::ZERO,
                            },
                        ));
                        continue;
                    }
                    crate::hooks::HookResult::Continue((name, args)) => {
                        tool_name = name;
                        tool_args = args;
                    }
                }
            }

            maybe_inject_channel_delivery_defaults(
                &tool_name,
                &mut tool_args,
                channel_name,
                channel_reply_target,
            );

            // ── Approval hook ────────────────────────────────
            if let Some(mgr) = approval {
                if mgr.needs_approval(&tool_name) {
                    let request = ApprovalRequest {
                        tool_name: tool_name.clone(),
                        arguments: tool_args.clone(),
                    };

                    // Interactive CLI: prompt the operator.
                    // Non-interactive (channels): auto-deny since no operator
                    // is present to approve.
                    let decision = if mgr.is_non_interactive() {
                        ApprovalResponse::No
                    } else {
                        mgr.prompt_cli(&request)
                    };

                    mgr.record_decision(&tool_name, &tool_args, decision, channel_name);

                    if decision == ApprovalResponse::No {
                        let denied = "Denied by user.".to_string();
                        runtime_trace::record_event(
                            "tool_call_result",
                            Some(channel_name),
                            Some(provider_name),
                            Some(model),
                            Some(&turn_id),
                            Some(false),
                            Some(&denied),
                            serde_json::json!({
                                "iteration": iteration + 1,
                                "tool": tool_name.clone(),
                                "arguments": scrub_credentials(&tool_args.to_string()),
                            }),
                        );
                        if let Some(ref tx) = on_delta {
                            let _ = tx
                                .send(DraftEvent::Progress(format!(
                                    "\u{274c} {}: {}\n",
                                    tool_name, denied
                                )))
                                .await;
                        }
                        ordered_results[idx] = Some((
                            tool_name.clone(),
                            call.tool_call_id.clone(),
                            ToolExecutionOutcome {
                                output: denied.clone(),
                                success: false,
                                error_reason: Some(denied),
                                duration: Duration::ZERO,
                            },
                        ));
                        continue;
                    }
                }
            }

            let signature = {
                let canonical_args = canonicalize_json_for_tool_signature(&tool_args);
                let args_json =
                    serde_json::to_string(&canonical_args).unwrap_or_else(|_| "{}".to_string());
                (tool_name.trim().to_ascii_lowercase(), args_json)
            };
            let dedup_exempt = dedup_exempt_tools.iter().any(|e| e == &tool_name);
            if !dedup_exempt && !seen_tool_signatures.insert(signature) {
                let duplicate = format!(
                    "Skipped duplicate tool call '{tool_name}' with identical arguments in this turn."
                );
                runtime_trace::record_event(
                    "tool_call_result",
                    Some(channel_name),
                    Some(provider_name),
                    Some(model),
                    Some(&turn_id),
                    Some(false),
                    Some(&duplicate),
                    serde_json::json!({
                        "iteration": iteration + 1,
                        "tool": tool_name.clone(),
                        "arguments": scrub_credentials(&tool_args.to_string()),
                        "deduplicated": true,
                    }),
                );
                if let Some(ref tx) = on_delta {
                    let _ = tx
                        .send(DraftEvent::Progress(format!(
                            "\u{274c} {}: {}\n",
                            tool_name, duplicate
                        )))
                        .await;
                }
                ordered_results[idx] = Some((
                    tool_name.clone(),
                    call.tool_call_id.clone(),
                    ToolExecutionOutcome {
                        output: duplicate.clone(),
                        success: false,
                        error_reason: Some(duplicate),
                        duration: Duration::ZERO,
                    },
                ));
                continue;
            }

            runtime_trace::record_event(
                "tool_call_start",
                Some(channel_name),
                Some(provider_name),
                Some(model),
                Some(&turn_id),
                None,
                None,
                serde_json::json!({
                    "iteration": iteration + 1,
                    "tool": tool_name.clone(),
                    "arguments": scrub_credentials(&tool_args.to_string()),
                }),
            );

            // ── Progress: tool start ────────────────────────────
            if let Some(ref tx) = on_delta {
                let hint = {
                    let raw = match tool_name.as_str() {
                        "shell" => tool_args.get("command").and_then(|v| v.as_str()),
                        "file_read" | "file_write" => {
                            tool_args.get("path").and_then(|v| v.as_str())
                        }
                        _ => tool_args
                            .get("action")
                            .and_then(|v| v.as_str())
                            .or_else(|| tool_args.get("query").and_then(|v| v.as_str())),
                    };
                    match raw {
                        Some(s) => truncate_with_ellipsis(s, 60),
                        None => String::new(),
                    }
                };
                let progress = if hint.is_empty() {
                    format!("\u{23f3} {}\n", tool_name)
                } else {
                    format!("\u{23f3} {}: {hint}\n", tool_name)
                };
                tracing::debug!(tool = %tool_name, "Sending progress start to draft");
                let _ = tx.send(DraftEvent::Progress(progress)).await;
            }

            executable_indices.push(idx);
            executable_calls.push(ParsedToolCall {
                name: tool_name,
                arguments: tool_args,
                tool_call_id: call.tool_call_id.clone(),
            });
        }

        let executed_outcomes = if allow_parallel_execution && executable_calls.len() > 1 {
            execute_tools_parallel(
                &executable_calls,
                tools_registry,
                activated_tools,
                observer,
                cancellation_token.as_ref(),
            )
            .await?
        } else {
            execute_tools_sequential(
                &executable_calls,
                tools_registry,
                activated_tools,
                observer,
                cancellation_token.as_ref(),
            )
            .await?
        };

        for ((idx, call), outcome) in executable_indices
            .iter()
            .zip(executable_calls.iter())
            .zip(executed_outcomes.into_iter())
        {
            runtime_trace::record_event(
                "tool_call_result",
                Some(channel_name),
                Some(provider_name),
                Some(model),
                Some(&turn_id),
                Some(outcome.success),
                outcome.error_reason.as_deref(),
                serde_json::json!({
                    "iteration": iteration + 1,
                    "tool": call.name.clone(),
                    "duration_ms": outcome.duration.as_millis(),
                    "output": scrub_credentials(&outcome.output),
                }),
            );

            // ── Hook: after_tool_call (void) ─────────────────
            if let Some(hooks) = hooks {
                let tool_result_obj = crate::tools::ToolResult {
                    success: outcome.success,
                    output: outcome.output.clone(),
                    error: None,
                };
                hooks
                    .fire_after_tool_call(&call.name, &tool_result_obj, outcome.duration)
                    .await;
            }

            // ── Progress: tool completion ───────────────────────
            if let Some(ref tx) = on_delta {
                let secs = outcome.duration.as_secs();
                let progress_msg = if outcome.success {
                    format!("\u{2705} {} ({secs}s)\n", call.name)
                } else if let Some(ref reason) = outcome.error_reason {
                    format!(
                        "\u{274c} {} ({secs}s): {}\n",
                        call.name,
                        truncate_with_ellipsis(reason, 200)
                    )
                } else {
                    format!("\u{274c} {} ({secs}s)\n", call.name)
                };
                tracing::debug!(tool = %call.name, secs, "Sending progress complete to draft");
                let _ = tx.send(DraftEvent::Progress(progress_msg)).await;
            }

            ordered_results[*idx] = Some((call.name.clone(), call.tool_call_id.clone(), outcome));
        }

        // Collect tool results and build per-tool output for loop detection.
        // Only non-ignored tool outputs contribute to the identical-output hash.
        let mut detection_relevant_output = String::new();
        // Use enumerate *before* filter_map so result_index stays aligned with
        // tool_calls even when some ordered_results entries are None.
        for (result_index, (tool_name, tool_call_id, outcome)) in ordered_results
            .into_iter()
            .enumerate()
            .filter_map(|(i, opt)| opt.map(|v| (i, v)))
        {
            if !loop_ignore_tools.contains(tool_name.as_str()) {
                detection_relevant_output.push_str(&outcome.output);

                // Feed the pattern-based loop detector with name + args + result.
                let args = tool_calls
                    .get(result_index)
                    .map(|c| &c.arguments)
                    .unwrap_or(&serde_json::Value::Null);
                let det_result = loop_detector.record(&tool_name, args, &outcome.output);
                match det_result {
                    crate::agent::loop_detector::LoopDetectionResult::Ok => {}
                    crate::agent::loop_detector::LoopDetectionResult::Warning(ref msg) => {
                        tracing::warn!(tool = %tool_name, %msg, "loop detector warning");
                        // Inject a system nudge so the LLM adjusts strategy.
                        history.push(ChatMessage::system(format!("[Loop Detection] {msg}")));
                    }
                    crate::agent::loop_detector::LoopDetectionResult::Block(ref msg) => {
                        tracing::warn!(tool = %tool_name, %msg, "loop detector blocked tool call");
                        // Replace the tool output with the block message.
                        // We still continue the loop so the LLM sees the block feedback.
                        history.push(ChatMessage::system(format!(
                            "[Loop Detection — BLOCKED] {msg}"
                        )));
                    }
                    crate::agent::loop_detector::LoopDetectionResult::Break(msg) => {
                        runtime_trace::record_event(
                            "loop_detector_circuit_breaker",
                            Some(channel_name),
                            Some(provider_name),
                            Some(model),
                            Some(&turn_id),
                            Some(false),
                            Some(&msg),
                            serde_json::json!({
                                "iteration": iteration + 1,
                                "tool": tool_name,
                            }),
                        );
                        anyhow::bail!("Agent loop aborted by loop detector: {msg}");
                    }
                }
            }
            let result_output = truncate_tool_result(&outcome.output, max_tool_result_chars);
            individual_results.push((tool_call_id, result_output.clone()));
            let _ = writeln!(
                tool_results,
                "<tool_result name=\"{}\">\n{}\n</tool_result>",
                tool_name, result_output
            );
        }

        // ── Time-gated loop detection ──────────────────────────
        // When pacing.loop_detection_min_elapsed_secs is set, identical-output
        // loop detection activates after the task has been running that long.
        // This avoids false-positive aborts on long-running browser/research
        // workflows while keeping aggressive protection for quick tasks.
        // When not configured, identical-output detection is disabled (preserving
        // existing behavior where only max_iterations prevents runaway loops).
        let loop_detection_active = match pacing.loop_detection_min_elapsed_secs {
            Some(min_secs) => loop_started_at.elapsed() >= Duration::from_secs(min_secs),
            None => false, // disabled when not configured (backwards compatible)
        };

        if loop_detection_active && !detection_relevant_output.is_empty() {
            use std::hash::{Hash, Hasher};
            let mut hasher = std::collections::hash_map::DefaultHasher::new();
            detection_relevant_output.hash(&mut hasher);
            let current_hash = hasher.finish();

            if last_tool_output_hash == Some(current_hash) {
                consecutive_identical_outputs += 1;
            } else {
                consecutive_identical_outputs = 0;
                last_tool_output_hash = Some(current_hash);
            }

            // Bail if we see 3+ consecutive identical tool outputs (clear runaway).
            if consecutive_identical_outputs >= 3 {
                runtime_trace::record_event(
                    "tool_loop_identical_output_abort",
                    Some(channel_name),
                    Some(provider_name),
                    Some(model),
                    Some(&turn_id),
                    Some(false),
                    Some("identical tool output detected 3 consecutive times"),
                    serde_json::json!({
                        "iteration": iteration + 1,
                        "consecutive_identical": consecutive_identical_outputs,
                    }),
                );
                anyhow::bail!(
                    "Agent loop aborted: identical tool output detected {} consecutive times",
                    consecutive_identical_outputs
                );
            }
        }

        // Add assistant message with tool calls + tool results to history.
        // Native mode: use JSON-structured messages so convert_messages() can
        // reconstruct proper OpenAI-format tool_calls and tool result messages.
        // Prompt mode: use XML-based text format as before.
        history.push(ChatMessage::assistant(assistant_history_content));
        if native_tool_calls.is_empty() {
            let all_results_have_ids = use_native_tools
                && !individual_results.is_empty()
                && individual_results
                    .iter()
                    .all(|(tool_call_id, _)| tool_call_id.is_some());
            if all_results_have_ids {
                for (tool_call_id, result) in &individual_results {
                    let tool_msg = serde_json::json!({
                        "tool_call_id": tool_call_id,
                        "content": result,
                    });
                    history.push(ChatMessage::tool(tool_msg.to_string()));
                }
            } else {
                history.push(ChatMessage::user(format!("[Tool results]\n{tool_results}")));
            }
        } else {
            for (native_call, (_, result)) in
                native_tool_calls.iter().zip(individual_results.iter())
            {
                let tool_msg = serde_json::json!({
                    "tool_call_id": native_call.id,
                    "content": result,
                });
                history.push(ChatMessage::tool(tool_msg.to_string()));
            }
        }
    }

    runtime_trace::record_event(
        "tool_loop_exhausted",
        Some(channel_name),
        Some(provider_name),
        Some(model),
        Some(&turn_id),
        Some(false),
        Some("agent exceeded maximum tool iterations"),
        serde_json::json!({
            "max_iterations": max_iterations,
        }),
    );

    // Graceful shutdown: ask the LLM for a final summary without tools
    tracing::warn!(
        max_iterations,
        "Max iterations reached, requesting final summary"
    );
    history.push(ChatMessage::user(
        "You have reached the maximum number of tool iterations. \
         Please provide your best answer based on the work completed so far. \
         Summarize what you accomplished and what remains to be done."
            .to_string(),
    ));

    let summary_request = crate::providers::ChatRequest {
        messages: history,
        tools: None, // No tools — force a text response
    };
    match provider.chat(summary_request, model, temperature).await {
        Ok(resp) => {
            let text = resp.text.unwrap_or_default();
            if text.is_empty() {
                anyhow::bail!("Agent exceeded maximum tool iterations ({max_iterations})")
            }
            Ok(text)
        }
        Err(e) => {
            tracing::warn!(error = %e, "Final summary LLM call failed, bailing");
            anyhow::bail!("Agent exceeded maximum tool iterations ({max_iterations})")
        }
    }
}

/// Build the tool instruction block for the system prompt so the LLM knows
/// how to invoke tools.
pub(crate) fn build_tool_instructions(
    tools_registry: &[Box<dyn Tool>],
    tool_descriptions: Option<&ToolDescriptions>,
) -> String {
    let mut instructions = String::new();
    instructions.push_str("\n## Tool Use Protocol\n\n");
    instructions.push_str("To use a tool, wrap a JSON object in <tool_call></tool_call> tags:\n\n");
    instructions.push_str("```\n<tool_call>\n{\"name\": \"tool_name\", \"arguments\": {\"param\": \"value\"}}\n</tool_call>\n```\n\n");
    instructions.push_str(
        "CRITICAL: Output actual <tool_call> tags—never describe steps or give examples.\n\n",
    );
    instructions.push_str("Example: User says \"what's the date?\". You MUST respond with:\n<tool_call>\n{\"name\":\"shell\",\"arguments\":{\"command\":\"date\"}}\n</tool_call>\n\n");
    instructions.push_str("You may use multiple tool calls in a single response. ");
    instructions.push_str("After tool execution, results appear in <tool_result> tags. ");
    instructions
        .push_str("Continue reasoning with the results until you can give a final answer.\n\n");
    instructions.push_str("### Available Tools\n\n");

    for tool in tools_registry {
        let desc = tool_descriptions
            .and_then(|td| td.get(tool.name()))
            .unwrap_or_else(|| tool.description());
        let _ = writeln!(
            instructions,
            "**{}**: {}\nParameters: `{}`\n",
            tool.name(),
            desc,
            tool.parameters_schema()
        );
    }

    instructions
}

// ── CLI Entrypoint ───────────────────────────────────────────────────────
// Wires up all subsystems (observer, runtime, security, memory, tools,
// provider, hardware RAG, peripherals) and enters either single-shot or
// interactive REPL mode. The interactive loop manages history compaction
// and hard trimming to keep the context window bounded.

#[allow(clippy::too_many_lines)]
pub async fn run(
    config: Config,
    message: Option<String>,
    provider_override: Option<String>,
    model_override: Option<String>,
    temperature: f64,
    interactive: bool,
    session_state_file: Option<PathBuf>,
    allowed_tools: Option<Vec<String>>,
) -> Result<String> {
    // ── Wire up agnostic subsystems ──────────────────────────────
    let base_observer = observability::create_observer(&config.observability);
    let observer: Arc<dyn Observer> = Arc::from(base_observer);
    let runtime: Arc<dyn runtime::RuntimeAdapter> =
        Arc::from(runtime::create_runtime(&config.runtime)?);
    let security = Arc::new(SecurityPolicy::from_config(
        &config.autonomy,
        &config.workspace_dir,
    ));

    // ── Memory (the brain) ────────────────────────────────────────
    let mem: Arc<dyn Memory> = Arc::from(memory::create_memory_with_storage_and_routes(
        &config.memory,
        &config.embedding_routes,
        Some(&config.storage.provider.config),
        &config.workspace_dir,
        config.api_key.as_deref(),
    )?);
    tracing::info!(backend = mem.name(), "Memory initialized");

    // ── Tools ────────────
    let (composio_key, composio_entity_id) = if config.composio.enabled {
        (
            config.composio.api_key.as_deref(),
            Some(config.composio.entity_id.as_str()),
        )
    } else {
        (None, None)
    };
    let (
        mut tools_registry,
        delegate_handle,
        _reaction_handle,
        _channel_map_handle,
        _ask_user_handle,
        _escalate_handle,
    ) = tools::all_tools_with_runtime(
        Arc::new(config.clone()),
        &security,
        runtime,
        mem.clone(),
        composio_key,
        composio_entity_id,
        &config.browser,
        &config.http_request,
        &config.web_fetch,
        &config.workspace_dir,
        &config.agents,
        config.api_key.as_deref(),
        &config,
        None,
    );

    // ── Capability-based tool access control ─────────────────────
    // When `allowed_tools` is `Some(list)`, restrict the tool registry to only
    // those tools whose name appears in the list. Unknown names are silently
    // ignored. When `None`, all tools remain available (backward compatible).
    if let Some(ref allow_list) = allowed_tools {
        tools_registry.retain(|t| allow_list.iter().any(|name| name == t.name()));
        tracing::info!(
            allowed = allow_list.len(),
            retained = tools_registry.len(),
            "Applied capability-based tool access filter"
        );
    }

    // ── Wire MCP tools (non-fatal) — CLI path ────────────────────
    // NOTE: MCP tools are injected after built-in tool filtering
    // (filter_primary_agent_tools_or_fail / agent.allowed_tools / agent.denied_tools).
    // MCP servers are user-declared external integrations; the built-in allow/deny
    // filter is not appropriate for them and would silently drop all MCP tools when
    // a restrictive allowlist is configured. Keep this block after any such filter call.
    //
    // When `deferred_loading` is enabled, MCP tools are NOT added to the registry
    // eagerly. Instead, a `tool_search` built-in is registered so the LLM can
    // fetch schemas on demand. This reduces context window waste.
    let mut deferred_section = String::new();
    let mut activated_handle: Option<
        std::sync::Arc<std::sync::Mutex<crate::tools::ActivatedToolSet>>,
    > = None;
    if config.mcp.enabled && !config.mcp.servers.is_empty() {
        tracing::info!(
            "Initializing MCP client — {} server(s) configured",
            config.mcp.servers.len()
        );
        match crate::tools::McpRegistry::connect_all(&config.mcp.servers).await {
            Ok(registry) => {
                let registry = std::sync::Arc::new(registry);
                if config.mcp.deferred_loading {
                    // Deferred path: build stubs and register tool_search
                    let deferred_set = crate::tools::DeferredMcpToolSet::from_registry(
                        std::sync::Arc::clone(&registry),
                    )
                    .await;
                    tracing::info!(
                        "MCP deferred: {} tool stub(s) from {} server(s)",
                        deferred_set.len(),
                        registry.server_count()
                    );
                    deferred_section =
                        crate::tools::mcp_deferred::build_deferred_tools_section(&deferred_set);
                    let activated = std::sync::Arc::new(std::sync::Mutex::new(
                        crate::tools::ActivatedToolSet::new(),
                    ));
                    activated_handle = Some(std::sync::Arc::clone(&activated));
                    tools_registry.push(Box::new(crate::tools::ToolSearchTool::new(
                        deferred_set,
                        activated,
                    )));
                } else {
                    // Eager path: register all MCP tools directly
                    let names = registry.tool_names();
                    let mut registered = 0usize;
                    for name in names {
                        if let Some(def) = registry.get_tool_def(&name).await {
                            let wrapper: std::sync::Arc<dyn Tool> =
                                std::sync::Arc::new(crate::tools::McpToolWrapper::new(
                                    name,
                                    def,
                                    std::sync::Arc::clone(&registry),
                                ));
                            if let Some(ref handle) = delegate_handle {
                                handle.write().push(std::sync::Arc::clone(&wrapper));
                            }
                            tools_registry.push(Box::new(crate::tools::ArcToolRef(wrapper)));
                            registered += 1;
                        }
                    }
                    tracing::info!(
                        "MCP: {} tool(s) registered from {} server(s)",
                        registered,
                        registry.server_count()
                    );
                }
            }
            Err(e) => {
                tracing::error!("MCP registry failed to initialize: {e:#}");
            }
        }
    }

    // ── Resolve provider ─────────────────────────────────────────
    let mut provider_name = provider_override
        .as_deref()
        .or(config.default_provider.as_deref())
        .unwrap_or("openrouter")
        .to_string();

    let mut model_name = model_override
        .as_deref()
        .or(config.default_model.as_deref())
        .unwrap_or("anthropic/claude-sonnet-4")
        .to_string();

    let provider_runtime_options = providers::provider_runtime_options_from_config(&config);

    let mut provider: Box<dyn Provider> = providers::create_routed_provider_with_options(
        &provider_name,
        config.api_key.as_deref(),
        config.api_url.as_deref(),
        &config.reliability,
        &config.model_routes,
        &model_name,
        &provider_runtime_options,
    )?;

    let model_switch_callback = get_model_switch_state();

    observer.record_event(&ObserverEvent::AgentStart {
        provider: provider_name.to_string(),
        model: model_name.to_string(),
    });

    // ── Load locale-aware tool descriptions ────────────────────────
    let i18n_locale = config
        .locale
        .as_deref()
        .filter(|s| !s.is_empty())
        .map(ToString::to_string)
        .unwrap_or_else(crate::i18n::detect_locale);
    let i18n_search_dirs = crate::i18n::default_search_dirs(&config.workspace_dir);
    let i18n_descs = crate::i18n::ToolDescriptions::load(&i18n_locale, &i18n_search_dirs);

    // ── Build system prompt from workspace MD files (OpenClaw framework) ──
    let skills = crate::skills::load_skills_with_config(&config.workspace_dir, &config);

    // Register skill-defined tools as callable tool specs in the tool registry
    // so the LLM can invoke them via native function calling, not just XML prompts.
    tools::register_skill_tools(&mut tools_registry, &skills, security.clone());

    let mut tool_descs: Vec<(&str, &str)> = vec![
        (
            "shell",
            "Execute terminal commands. Use when: running local checks, build/test commands, diagnostics. Don't use when: a safer dedicated tool exists, or command is destructive without approval.",
        ),
        (
            "file_read",
            "Read file contents. Use when: inspecting project files, configs, logs. Don't use when: a targeted search is enough.",
        ),
        (
            "file_write",
            "Write file contents. Use when: applying focused edits, scaffolding files, updating docs/code. Don't use when: side effects are unclear or file ownership is uncertain.",
        ),
        (
            "memory_store",
            "Save to memory. Use when: preserving durable preferences, decisions, key context. Don't use when: information is transient/noisy/sensitive without need.",
        ),
        (
            "memory_recall",
            "Search memory. Use when: retrieving prior decisions, user preferences, historical context. Don't use when: answer is already in current context.",
        ),
        (
            "memory_forget",
            "Delete a memory entry. Use when: memory is incorrect/stale or explicitly requested for removal. Don't use when: impact is uncertain.",
        ),
    ];
    if matches!(
        config.skills.prompt_injection_mode,
        crate::config::SkillsPromptInjectionMode::Compact
    ) {
        tool_descs.push((
            "read_skill",
            "Load the full source for an available skill by name. Use when: compact mode only shows a summary and you need the complete skill instructions.",
        ));
    }
    tool_descs.push((
        "cron_add",
        "Create a cron job. Supports schedule kinds: cron, at, every; and job types: shell or agent.",
    ));
    tool_descs.push((
        "cron_list",
        "List all cron jobs with schedule, status, and metadata.",
    ));
    tool_descs.push(("cron_remove", "Remove a cron job by job_id."));
    tool_descs.push((
        "cron_update",
        "Patch a cron job (schedule, enabled, command/prompt, model, delivery, session_target).",
    ));
    tool_descs.push((
        "cron_run",
        "Force-run a cron job immediately and record a run history entry.",
    ));
    tool_descs.push(("cron_runs", "Show recent run history for a cron job."));
    tool_descs.push((
        "screenshot",
        "Capture a screenshot of the current screen. Returns file path and base64-encoded PNG. Use when: visual verification, UI inspection, debugging displays.",
    ));
    tool_descs.push((
        "image_info",
        "Read image file metadata (format, dimensions, size) and optionally base64-encode it. Use when: inspecting images, preparing visual data for analysis.",
    ));
    if config.browser.enabled {
        tool_descs.push((
            "browser_open",
            "Open approved HTTPS URLs in system browser (allowlist-only, no scraping)",
        ));
    }
    if config.composio.enabled {
        tool_descs.push((
            "composio",
            "Execute actions on 1000+ apps via Composio (Gmail, Notion, GitHub, Slack, etc.). Use action='list' to discover, 'execute' to run (optionally with connected_account_id), 'connect' to OAuth.",
        ));
    }
    tool_descs.push((
        "schedule",
        "Manage scheduled tasks (create/list/get/cancel/pause/resume). Supports recurring cron and one-shot delays.",
    ));
    tool_descs.push((
        "model_routing_config",
        "Configure default model, scenario routing, and delegate agents. Use for natural-language requests like: 'set conversation to kimi and coding to gpt-5.3-codex'.",
    ));
    if !config.agents.is_empty() {
        tool_descs.push((
            "delegate",
            "Delegate a sub-task to a specialized agent. Use when: task needs different model/capability, or to parallelize work.",
        ));
    }
    let bootstrap_max_chars = if config.agent.compact_context {
        Some(6000)
    } else {
        None
    };
    let native_tools = provider.supports_native_tools();
    let mut system_prompt = crate::channels::build_system_prompt_with_mode_and_autonomy(
        &config.workspace_dir,
        &model_name,
        &tool_descs,
        &skills,
        Some(&config.identity),
        bootstrap_max_chars,
        Some(&config.autonomy),
        native_tools,
        config.skills.prompt_injection_mode,
        config.agent.compact_context,
        config.agent.max_system_prompt_chars,
    );

    // Append structured tool-use instructions with schemas (only for non-native providers)
    if !native_tools {
        system_prompt.push_str(&build_tool_instructions(&tools_registry, Some(&i18n_descs)));
    }

    // Append deferred MCP tool names so the LLM knows what is available
    if !deferred_section.is_empty() {
        system_prompt.push('\n');
        system_prompt.push_str(&deferred_section);
    }

    // ── Approval manager (supervised mode) ───────────────────────
    let approval_manager = if interactive {
        Some(ApprovalManager::from_config(&config.autonomy))
    } else {
        None
    };
    let channel_name = if interactive { "cli" } else { "daemon" };
    let memory_session_id = session_state_file.as_deref().and_then(|path| {
        let raw = path.to_string_lossy().trim().to_string();
        if raw.is_empty() {
            None
        } else {
            Some(format!("cli:{raw}"))
        }
    });

    // ── Execute ──────────────────────────────────────────────────
    let start = Instant::now();

    let mut final_output = String::new();

    // Save the base system prompt before any thinking modifications so
    // the interactive loop can restore it between turns.
    let base_system_prompt = system_prompt.clone();

    if let Some(msg) = message {
        // ── Parse thinking directive from user message ─────────
        let (thinking_directive, effective_msg) =
            match crate::agent::thinking::parse_thinking_directive(&msg) {
                Some((level, remaining)) => {
                    tracing::info!(thinking_level = ?level, "Thinking directive parsed from message");
                    (Some(level), remaining)
                }
                None => (None, msg.clone()),
            };
        let thinking_level = crate::agent::thinking::resolve_thinking_level(
            thinking_directive,
            None,
            &config.agent.thinking,
        );
        let thinking_params = crate::agent::thinking::apply_thinking_level(thinking_level);
        let effective_temperature = crate::agent::thinking::clamp_temperature(
            temperature + thinking_params.temperature_adjustment,
        );

        // Prepend thinking system prompt prefix when present.
        if let Some(ref prefix) = thinking_params.system_prompt_prefix {
            system_prompt = format!("{prefix}\n\n{system_prompt}");
        }

        // Auto-save user message to memory (skip short/trivial messages)
        if config.memory.auto_save
            && effective_msg.chars().count() >= AUTOSAVE_MIN_MESSAGE_CHARS
            && !memory::should_skip_autosave_content(&effective_msg)
        {
            let user_key = autosave_memory_key("user_msg");
            let _ = mem
                .store(
                    &user_key,
                    &effective_msg,
                    MemoryCategory::Conversation,
                    memory_session_id.as_deref(),
                )
                .await;
        }

        // Inject memory + hardware RAG context into user message
        let mem_context = build_context(
            mem.as_ref(),
            &effective_msg,
            config.memory.min_relevance_score,
            memory_session_id.as_deref(),
        )
        .await;
        let context = mem_context;
        let now = chrono::Local::now().format("%Y-%m-%d %H:%M:%S %Z");
        let enriched = if context.is_empty() {
            format!("[{now}] {effective_msg}")
        } else {
            format!("{context}[{now}] {effective_msg}")
        };

        let mut history = vec![
            ChatMessage::system(&system_prompt),
            ChatMessage::user(&enriched),
        ];

        // Prune history for token efficiency (when enabled).
        if config.agent.history_pruning.enabled {
            let _stats = crate::agent::history_pruner::prune_history(
                &mut history,
                &config.agent.history_pruning,
            );
        }

        // Compute per-turn excluded MCP tools from tool_filter_groups.
        let excluded_tools = compute_excluded_mcp_tools(
            &tools_registry,
            &config.agent.tool_filter_groups,
            &effective_msg,
        );

        #[allow(unused_assignments)]
        let mut response = String::new();
        loop {
            match run_tool_call_loop(
                provider.as_ref(),
                &mut history,
                &tools_registry,
                observer.as_ref(),
                &provider_name,
                &model_name,
                effective_temperature,
                false,
                approval_manager.as_ref(),
                channel_name,
                None,
                &config.multimodal,
                config.agent.max_tool_iterations,
                None,
                None,
                None,
                &excluded_tools,
                &config.agent.tool_call_dedup_exempt,
                activated_handle.as_ref(),
                Some(model_switch_callback.clone()),
                &config.pacing,
                config.agent.max_tool_result_chars,
                config.agent.max_context_tokens,
                None, // shared_budget
            )
            .await
            {
                Ok(resp) => {
                    response = resp;
                    break;
                }
                Err(e) => {
                    if let Some((new_provider, new_model)) = is_model_switch_requested(&e) {
                        tracing::info!(
                            "Model switch requested, switching from {} {} to {} {}",
                            provider_name,
                            model_name,
                            new_provider,
                            new_model
                        );

                        provider = providers::create_routed_provider_with_options(
                            &new_provider,
                            config.api_key.as_deref(),
                            config.api_url.as_deref(),
                            &config.reliability,
                            &config.model_routes,
                            &new_model,
                            &provider_runtime_options,
                        )?;

                        provider_name = new_provider;
                        model_name = new_model;

                        clear_model_switch_request();

                        observer.record_event(&ObserverEvent::AgentStart {
                            provider: provider_name.to_string(),
                            model: model_name.to_string(),
                        });

                        continue;
                    }
                    return Err(e);
                }
            }
        }

        // After successful multi-step execution, attempt autonomous skill creation.
        #[cfg(feature = "skill-creation")]
        if config.skills.skill_creation.enabled {
            let tool_calls = crate::skills::creator::extract_tool_calls_from_history(&history);
            if tool_calls.len() >= 2 {
                let creator = crate::skills::creator::SkillCreator::new(
                    config.workspace_dir.clone(),
                    config.skills.skill_creation.clone(),
                );
                match creator.create_from_execution(&msg, &tool_calls, None).await {
                    Ok(Some(slug)) => {
                        tracing::info!(slug, "Auto-created skill from execution");
                    }
                    Ok(None) => {
                        tracing::debug!("Skill creation skipped (duplicate or disabled)");
                    }
                    Err(e) => tracing::warn!("Skill creation failed: {e}"),
                }
            }
        }
        final_output = response.clone();
        println!("{response}");
        observer.record_event(&ObserverEvent::TurnComplete);
    } else {
        println!("🦀 ZeroClaw Interactive Mode");
        println!("Type /help for commands.\n");
        let cli = crate::channels::CliChannel::new();

        // Persistent conversation history across turns
        let mut history = if let Some(path) = session_state_file.as_deref() {
            load_interactive_session_history(path, &system_prompt)?
        } else {
            vec![ChatMessage::system(&system_prompt)]
        };

        loop {
            print!("> ");
            let _ = std::io::stdout().flush();

            // Read raw bytes to avoid UTF-8 validation errors when PTY
            // transport splits multi-byte characters at frame boundaries
            // (e.g. CJK input with spaces over kubectl exec / SSH).
            let mut raw = Vec::new();
            match std::io::BufRead::read_until(&mut std::io::stdin().lock(), b'\n', &mut raw) {
                Ok(0) => break,
                Ok(_) => {}
                Err(e) => {
                    eprintln!("\nError reading input: {e}\n");
                    break;
                }
            }
            let input = String::from_utf8_lossy(&raw).into_owned();

            let user_input = input.trim().to_string();
            if user_input.is_empty() {
                continue;
            }
            match user_input.as_str() {
                "/quit" | "/exit" => break,
                "/help" => {
                    println!("Available commands:");
                    println!("  /help             Show this help message");
                    println!("  /clear /new       Clear conversation history");
                    println!("  /quit /exit       Exit interactive mode");
                    println!(
                        "  /think:<level>    Set reasoning depth (off|minimal|low|medium|high|max)\n"
                    );
                    continue;
                }
                "/clear" | "/new" => {
                    println!(
                        "This will clear the current conversation and delete all session memory."
                    );
                    println!("Core memories (long-term facts/preferences) will be preserved.");
                    print!("Continue? [y/N] ");
                    let _ = std::io::stdout().flush();

                    let mut confirm_raw = Vec::new();
                    if std::io::BufRead::read_until(
                        &mut std::io::stdin().lock(),
                        b'\n',
                        &mut confirm_raw,
                    )
                    .is_err()
                    {
                        continue;
                    }
                    let confirm = String::from_utf8_lossy(&confirm_raw);
                    if !matches!(confirm.trim().to_lowercase().as_str(), "y" | "yes") {
                        println!("Cancelled.\n");
                        continue;
                    }

                    history.clear();
                    history.push(ChatMessage::system(&system_prompt));
                    // Clear conversation and daily memory
                    let mut cleared = 0;
                    for category in [MemoryCategory::Conversation, MemoryCategory::Daily] {
                        let entries = mem.list(Some(&category), None).await.unwrap_or_default();
                        for entry in entries {
                            if mem.forget(&entry.key).await.unwrap_or(false) {
                                cleared += 1;
                            }
                        }
                    }
                    if cleared > 0 {
                        println!("Conversation cleared ({cleared} memory entries removed).\n");
                    } else {
                        println!("Conversation cleared.\n");
                    }
                    if let Some(path) = session_state_file.as_deref() {
                        save_interactive_session_history(path, &history)?;
                    }
                    continue;
                }
                _ => {}
            }

            // ── Parse thinking directive from interactive input ───
            let (thinking_directive, effective_input) =
                match crate::agent::thinking::parse_thinking_directive(&user_input) {
                    Some((level, remaining)) => {
                        tracing::info!(thinking_level = ?level, "Thinking directive parsed");
                        (Some(level), remaining)
                    }
                    None => (None, user_input.clone()),
                };
            let thinking_level = crate::agent::thinking::resolve_thinking_level(
                thinking_directive,
                None,
                &config.agent.thinking,
            );
            let thinking_params = crate::agent::thinking::apply_thinking_level(thinking_level);
            let turn_temperature = crate::agent::thinking::clamp_temperature(
                temperature + thinking_params.temperature_adjustment,
            );

            // For non-Medium levels, temporarily patch the system prompt with prefix.
            let turn_system_prompt;
            if let Some(ref prefix) = thinking_params.system_prompt_prefix {
                turn_system_prompt = format!("{prefix}\n\n{system_prompt}");
                // Update the system message in history for this turn.
                if let Some(sys_msg) = history.first_mut() {
                    if sys_msg.role == "system" {
                        sys_msg.content = turn_system_prompt.clone();
                    }
                }
            }

            // Auto-save conversation turns (skip short/trivial messages)
            if config.memory.auto_save
                && effective_input.chars().count() >= AUTOSAVE_MIN_MESSAGE_CHARS
                && !memory::should_skip_autosave_content(&effective_input)
            {
                let user_key = autosave_memory_key("user_msg");
                let _ = mem
                    .store(
                        &user_key,
                        &effective_input,
                        MemoryCategory::Conversation,
                        memory_session_id.as_deref(),
                    )
                    .await;
            }

            // Inject memory + hardware RAG context into user message
            let mem_context = build_context(
                mem.as_ref(),
                &effective_input,
                config.memory.min_relevance_score,
                memory_session_id.as_deref(),
            )
            .await;
            let context = mem_context;
            let now = chrono::Local::now().format("%Y-%m-%d %H:%M:%S %Z");
            let enriched = if context.is_empty() {
                format!("[{now}] {effective_input}")
            } else {
                format!("{context}[{now}] {effective_input}")
            };

            history.push(ChatMessage::user(&enriched));

            // Compute per-turn excluded MCP tools from tool_filter_groups.
            let excluded_tools = compute_excluded_mcp_tools(
                &tools_registry,
                &config.agent.tool_filter_groups,
                &effective_input,
            );

            // Set up streaming channel so tool progress and response
            // content are printed progressively instead of buffered.
            let (delta_tx, mut delta_rx) = tokio::sync::mpsc::channel::<DraftEvent>(64);
            let content_was_streamed =
                std::sync::Arc::new(std::sync::atomic::AtomicBool::new(false));
            let content_streamed_flag = content_was_streamed.clone();
            let is_tty = std::io::IsTerminal::is_terminal(&std::io::stderr());

            let consumer_handle = tokio::spawn(async move {
                use std::io::Write;
                while let Some(event) = delta_rx.recv().await {
                    match event {
                        DraftEvent::Clear => {
                            let _ = writeln!(std::io::stderr());
                        }
                        DraftEvent::Progress(text) => {
                            if is_tty {
                                let _ = write!(std::io::stderr(), "\x1b[2m{text}\x1b[0m");
                            } else {
                                let _ = write!(std::io::stderr(), "{text}");
                            }
                            let _ = std::io::stderr().flush();
                        }
                        DraftEvent::Content(text) => {
                            content_streamed_flag.store(true, std::sync::atomic::Ordering::Relaxed);
                            print!("{text}");
                            let _ = std::io::stdout().flush();
                        }
                    }
                }
            });

            // Ctrl+C cancels the in-flight turn instead of killing the process.
            let cancel_token = CancellationToken::new();
            let cancel_token_clone = cancel_token.clone();
            let ctrlc_handle = tokio::spawn(async move {
                if tokio::signal::ctrl_c().await.is_ok() {
                    cancel_token_clone.cancel();
                }
            });

            let response = loop {
                match run_tool_call_loop(
                    provider.as_ref(),
                    &mut history,
                    &tools_registry,
                    observer.as_ref(),
                    &provider_name,
                    &model_name,
                    turn_temperature,
                    true,
                    approval_manager.as_ref(),
                    channel_name,
                    None,
                    &config.multimodal,
                    config.agent.max_tool_iterations,
                    Some(cancel_token.clone()),
                    Some(delta_tx.clone()),
                    None,
                    &excluded_tools,
                    &config.agent.tool_call_dedup_exempt,
                    activated_handle.as_ref(),
                    Some(model_switch_callback.clone()),
                    &config.pacing,
                    config.agent.max_tool_result_chars,
                    config.agent.max_context_tokens,
                    None, // shared_budget
                )
                .await
                {
                    Ok(resp) => break resp,
                    Err(e) => {
                        if is_tool_loop_cancelled(&e) {
                            eprintln!("\n\x1b[2m(cancelled)\x1b[0m");
                            break String::new();
                        }
                        if let Some((new_provider, new_model)) = is_model_switch_requested(&e) {
                            tracing::info!(
                                "Model switch requested, switching from {} {} to {} {}",
                                provider_name,
                                model_name,
                                new_provider,
                                new_model
                            );

                            provider = providers::create_routed_provider_with_options(
                                &new_provider,
                                config.api_key.as_deref(),
                                config.api_url.as_deref(),
                                &config.reliability,
                                &config.model_routes,
                                &new_model,
                                &provider_runtime_options,
                            )?;

                            provider_name = new_provider;
                            model_name = new_model;

                            clear_model_switch_request();

                            observer.record_event(&ObserverEvent::AgentStart {
                                provider: provider_name.to_string(),
                                model: model_name.to_string(),
                            });

                            continue;
                        }
                        // Context overflow recovery: compress and retry
                        if crate::providers::reliable::is_context_window_exceeded(&e) {
                            tracing::warn!(
                                "Context overflow in interactive loop, attempting recovery"
                            );
                            let mut compressor =
                                crate::agent::context_compressor::ContextCompressor::new(
                                    config.agent.context_compression.clone(),
                                    config.agent.max_context_tokens,
                                )
                                .with_memory(mem.clone());
                            let error_msg = format!("{e}");
                            match compressor
                                .compress_on_error(
                                    &mut history,
                                    provider.as_ref(),
                                    &model_name,
                                    &error_msg,
                                )
                                .await
                            {
                                Ok(true) => {
                                    tracing::info!(
                                        "Context recovered via compression, retrying turn"
                                    );
                                    continue;
                                }
                                Ok(false) => {
                                    tracing::warn!("Compression ran but couldn't reduce enough");
                                }
                                Err(compress_err) => {
                                    tracing::warn!(
                                        error = %compress_err,
                                        "Compression failed during recovery"
                                    );
                                }
                            }
                        }

                        eprintln!("\nError: {e}\n");
                        break String::new();
                    }
                }
            };

            // Clean up: stop the Ctrl+C listener and flush streaming events.
            ctrlc_handle.abort();
            drop(delta_tx);
            let _ = consumer_handle.await;

            final_output = response.clone();
            if content_was_streamed.load(std::sync::atomic::Ordering::Relaxed) {
                println!();
            } else if let Err(e) = crate::channels::Channel::send(
                &cli,
                &crate::channels::traits::SendMessage::new(format!("\n{response}\n"), "user"),
            )
            .await
            {
                eprintln!("\nError sending CLI response: {e}\n");
            }
            observer.record_event(&ObserverEvent::TurnComplete);

            // Context compression before hard trimming to preserve long-context signal.
            {
                let compressor = crate::agent::context_compressor::ContextCompressor::new(
                    config.agent.context_compression.clone(),
                    config.agent.max_context_tokens,
                )
                .with_memory(mem.clone());
                match compressor
                    .compress_if_needed(&mut history, provider.as_ref(), &model_name)
                    .await
                {
                    Ok(result) if result.compressed => {
                        tracing::info!(
                            passes = result.passes_used,
                            before = result.tokens_before,
                            after = result.tokens_after,
                            "Context compression complete"
                        );
                    }
                    Ok(_) => {} // No compression needed
                    Err(e) => {
                        tracing::warn!(
                            error = %e,
                            "Context compression failed, falling back to history trim"
                        );
                        trim_history(&mut history, config.agent.max_history_messages / 2);
                    }
                }
            }

            // Hard cap as a safety net.
            trim_history(&mut history, config.agent.max_history_messages);

            // Restore base system prompt (remove per-turn thinking prefix).
            if thinking_params.system_prompt_prefix.is_some() {
                if let Some(sys_msg) = history.first_mut() {
                    if sys_msg.role == "system" {
                        sys_msg.content.clone_from(&base_system_prompt);
                    }
                }
            }

            if let Some(path) = session_state_file.as_deref() {
                save_interactive_session_history(path, &history)?;
            }
        }
    }

    let duration = start.elapsed();
    observer.record_event(&ObserverEvent::AgentEnd {
        provider: provider_name.to_string(),
        model: model_name.to_string(),
        duration,
        tokens_used: None,
        cost_usd: None,
    });

    Ok(final_output)
}

/// Process a single message through the full agent (with tools, peripherals, memory).
/// Used by channels (Telegram, Discord, etc.) to enable hardware and tool use.
pub async fn process_message(
    config: Config,
    message: &str,
    session_id: Option<&str>,
) -> Result<String> {
    let observer: Arc<dyn Observer> =
        Arc::from(observability::create_observer(&config.observability));
    let runtime: Arc<dyn runtime::RuntimeAdapter> =
        Arc::from(runtime::create_runtime(&config.runtime)?);
    let security = Arc::new(SecurityPolicy::from_config(
        &config.autonomy,
        &config.workspace_dir,
    ));
    let approval_manager = ApprovalManager::for_non_interactive(&config.autonomy);
    let mem: Arc<dyn Memory> = Arc::from(memory::create_memory_with_storage_and_routes(
        &config.memory,
        &config.embedding_routes,
        Some(&config.storage.provider.config),
        &config.workspace_dir,
        config.api_key.as_deref(),
    )?);

    let (composio_key, composio_entity_id) = if config.composio.enabled {
        (
            config.composio.api_key.as_deref(),
            Some(config.composio.entity_id.as_str()),
        )
    } else {
        (None, None)
    };
    let (
        mut tools_registry,
        delegate_handle_pm,
        _reaction_handle_pm,
        _channel_map_handle_pm,
        _ask_user_handle_pm,
        _escalate_handle_pm,
    ) = tools::all_tools_with_runtime(
        Arc::new(config.clone()),
        &security,
        runtime,
        mem.clone(),
        composio_key,
        composio_entity_id,
        &config.browser,
        &config.http_request,
        &config.web_fetch,
        &config.workspace_dir,
        &config.agents,
        config.api_key.as_deref(),
        &config,
        None,
    );
    // ── Wire MCP tools (non-fatal) — process_message path ────────
    // NOTE: Same ordering contract as the CLI path above — MCP tools must be
    // injected after filter_primary_agent_tools_or_fail (or equivalent built-in
    // tool allow/deny filtering) to avoid MCP tools being silently dropped.
    let mut deferred_section = String::new();
    let mut activated_handle_pm: Option<
        std::sync::Arc<std::sync::Mutex<crate::tools::ActivatedToolSet>>,
    > = None;
    if config.mcp.enabled && !config.mcp.servers.is_empty() {
        tracing::info!(
            "Initializing MCP client — {} server(s) configured",
            config.mcp.servers.len()
        );
        match crate::tools::McpRegistry::connect_all(&config.mcp.servers).await {
            Ok(registry) => {
                let registry = std::sync::Arc::new(registry);
                if config.mcp.deferred_loading {
                    let deferred_set = crate::tools::DeferredMcpToolSet::from_registry(
                        std::sync::Arc::clone(&registry),
                    )
                    .await;
                    tracing::info!(
                        "MCP deferred: {} tool stub(s) from {} server(s)",
                        deferred_set.len(),
                        registry.server_count()
                    );
                    deferred_section =
                        crate::tools::mcp_deferred::build_deferred_tools_section(&deferred_set);
                    let activated = std::sync::Arc::new(std::sync::Mutex::new(
                        crate::tools::ActivatedToolSet::new(),
                    ));
                    activated_handle_pm = Some(std::sync::Arc::clone(&activated));
                    tools_registry.push(Box::new(crate::tools::ToolSearchTool::new(
                        deferred_set,
                        activated,
                    )));
                } else {
                    let names = registry.tool_names();
                    let mut registered = 0usize;
                    for name in names {
                        if let Some(def) = registry.get_tool_def(&name).await {
                            let wrapper: std::sync::Arc<dyn Tool> =
                                std::sync::Arc::new(crate::tools::McpToolWrapper::new(
                                    name,
                                    def,
                                    std::sync::Arc::clone(&registry),
                                ));
                            if let Some(ref handle) = delegate_handle_pm {
                                handle.write().push(std::sync::Arc::clone(&wrapper));
                            }
                            tools_registry.push(Box::new(crate::tools::ArcToolRef(wrapper)));
                            registered += 1;
                        }
                    }
                    tracing::info!(
                        "MCP: {} tool(s) registered from {} server(s)",
                        registered,
                        registry.server_count()
                    );
                }
            }
            Err(e) => {
                tracing::error!("MCP registry failed to initialize: {e:#}");
            }
        }
    }

    let provider_name = config.default_provider.as_deref().unwrap_or("openrouter");
    let model_name = config
        .default_model
        .clone()
        .unwrap_or_else(|| "anthropic/claude-sonnet-4-20250514".into());
    let provider_runtime_options = providers::provider_runtime_options_from_config(&config);
    let provider: Box<dyn Provider> = providers::create_routed_provider_with_options(
        provider_name,
        config.api_key.as_deref(),
        config.api_url.as_deref(),
        &config.reliability,
        &config.model_routes,
        &model_name,
        &provider_runtime_options,
    )?;

    // ── Load locale-aware tool descriptions ────────────────────────
    let i18n_locale = config
        .locale
        .as_deref()
        .filter(|s| !s.is_empty())
        .map(ToString::to_string)
        .unwrap_or_else(crate::i18n::detect_locale);
    let i18n_search_dirs = crate::i18n::default_search_dirs(&config.workspace_dir);
    let i18n_descs = crate::i18n::ToolDescriptions::load(&i18n_locale, &i18n_search_dirs);

    let skills = crate::skills::load_skills_with_config(&config.workspace_dir, &config);

    // Register skill-defined tools as callable tool specs (process_message path).
    tools::register_skill_tools(&mut tools_registry, &skills, security.clone());

    let mut tool_descs: Vec<(&str, &str)> = vec![
        ("shell", "Execute terminal commands."),
        ("file_read", "Read file contents."),
        ("file_write", "Write file contents."),
        ("memory_store", "Save to memory."),
        ("memory_recall", "Search memory."),
        ("memory_forget", "Delete a memory entry."),
        (
            "model_routing_config",
            "Configure default model, scenario routing, and delegate agents.",
        ),
        ("screenshot", "Capture a screenshot."),
        ("image_info", "Read image metadata."),
    ];
    if matches!(
        config.skills.prompt_injection_mode,
        crate::config::SkillsPromptInjectionMode::Compact
    ) {
        tool_descs.push((
            "read_skill",
            "Load the full source for an available skill by name.",
        ));
    }
    if config.browser.enabled {
        tool_descs.push(("browser_open", "Open approved URLs in browser."));
    }
    if config.composio.enabled {
        tool_descs.push(("composio", "Execute actions on 1000+ apps via Composio."));
    }

    // Filter out tools excluded for non-CLI channels (gateway counts as non-CLI).
    // Skip when autonomy is `Full` — full-autonomy agents keep all tools.
    if config.autonomy.level != AutonomyLevel::Full {
        let excluded = &config.autonomy.non_cli_excluded_tools;
        if !excluded.is_empty() {
            tool_descs.retain(|(name, _)| !excluded.iter().any(|ex| ex == name));
        }
    }

    let bootstrap_max_chars = if config.agent.compact_context {
        Some(6000)
    } else {
        None
    };
    let native_tools = provider.supports_native_tools();
    let mut system_prompt = crate::channels::build_system_prompt_with_mode_and_autonomy(
        &config.workspace_dir,
        &model_name,
        &tool_descs,
        &skills,
        Some(&config.identity),
        bootstrap_max_chars,
        Some(&config.autonomy),
        native_tools,
        config.skills.prompt_injection_mode,
        config.agent.compact_context,
        config.agent.max_system_prompt_chars,
    );
    if !native_tools {
        system_prompt.push_str(&build_tool_instructions(&tools_registry, Some(&i18n_descs)));
    }
    if !deferred_section.is_empty() {
        system_prompt.push('\n');
        system_prompt.push_str(&deferred_section);
    }

    // ── Parse thinking directive from user message ─────────────
    let (thinking_directive, effective_message) =
        match crate::agent::thinking::parse_thinking_directive(message) {
            Some((level, remaining)) => {
                tracing::info!(thinking_level = ?level, "Thinking directive parsed from message");
                (Some(level), remaining)
            }
            None => (None, message.to_string()),
        };
    let thinking_level = crate::agent::thinking::resolve_thinking_level(
        thinking_directive,
        None,
        &config.agent.thinking,
    );
    let thinking_params = crate::agent::thinking::apply_thinking_level(thinking_level);
    let effective_temperature = crate::agent::thinking::clamp_temperature(
        config.default_temperature + thinking_params.temperature_adjustment,
    );

    // Prepend thinking system prompt prefix when present.
    if let Some(ref prefix) = thinking_params.system_prompt_prefix {
        system_prompt = format!("{prefix}\n\n{system_prompt}");
    }

    let effective_msg_ref = effective_message.as_str();
    let mem_context = build_context(
        mem.as_ref(),
        effective_msg_ref,
        config.memory.min_relevance_score,
        session_id,
    )
    .await;
    let context = mem_context;
    let now = chrono::Local::now().format("%Y-%m-%d %H:%M:%S %Z");
    let enriched = if context.is_empty() {
        format!("[{now}] {effective_message}")
    } else {
        format!("{context}[{now}] {effective_message}")
    };

    let mut history = vec![
        ChatMessage::system(&system_prompt),
        ChatMessage::user(&enriched),
    ];
    let mut excluded_tools = compute_excluded_mcp_tools(
        &tools_registry,
        &config.agent.tool_filter_groups,
        effective_msg_ref,
    );
    if config.autonomy.level != AutonomyLevel::Full {
        excluded_tools.extend(config.autonomy.non_cli_excluded_tools.iter().cloned());
    }

    agent_turn(
        provider.as_ref(),
        &mut history,
        &tools_registry,
        observer.as_ref(),
        provider_name,
        &model_name,
        effective_temperature,
        true,
        "daemon",
        None,
        &config.multimodal,
        config.agent.max_tool_iterations,
        Some(&approval_manager),
        &excluded_tools,
        &config.agent.tool_call_dedup_exempt,
        activated_handle_pm.as_ref(),
        None,
    )
    .await
}


#[cfg(test)]
#[path = "loop_.test.rs"]
mod tests;
