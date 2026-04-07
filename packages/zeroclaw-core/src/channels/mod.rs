//! Channel subsystem for messaging platform integrations.
//!
//! This module provides the multi-channel messaging infrastructure that connects
//! ZeroClaw to external platforms. Each channel implements the [`Channel`] trait
//! defined in [`traits`], which provides a uniform interface for sending messages,
//! listening for incoming messages, health checking, and typing indicators.
//!
//! Channels are instantiated by [`start_channels`] based on the runtime configuration.
//! The subsystem manages per-sender conversation history, concurrent message processing
//! with configurable parallelism, and exponential-backoff reconnection for resilience.
//!
//! # Extension
//!
//! To add a new channel, implement [`Channel`] in a new submodule and wire it into
//! [`start_channels`]. See `AGENTS.md` §7.2 for the full change playbook.

pub mod cli;
pub mod debounce;
pub mod link_enricher;
pub mod media_pipeline;
pub mod session_backend;
pub mod session_sqlite;
pub mod session_store;
pub mod stall_watchdog;
pub mod telegram;
pub mod traits;
pub mod transcription;
pub mod tts;
pub mod webhook;

pub use cli::CliChannel;
pub use telegram::TelegramChannel;
pub use traits::{Channel, SendMessage};
#[allow(unused_imports)]
pub use tts::{TtsManager, TtsProvider};
pub use webhook::WebhookChannel;

use crate::agent::loop_::{
    build_tool_instructions, clear_model_switch_request, get_model_switch_state,
    is_model_switch_requested, run_tool_call_loop, scope_thread_id, scrub_credentials,
};
use crate::approval::ApprovalManager;
use crate::config::Config;
use crate::memory::{self, Memory};
use crate::observability::traits::{ObserverEvent, ObserverMetric};
use crate::observability::{self, Observer, runtime_trace};
use crate::providers::reliable::{scope_provider_fallback, take_last_provider_fallback};
use crate::providers::{self, ChatMessage, Provider};
use crate::runtime;
use crate::security::{AutonomyLevel, SecurityPolicy};
use crate::tools::{self, Tool};
use crate::util::truncate_with_ellipsis;
use anyhow::{Context, Result};
use portable_atomic::{AtomicU64, Ordering};
use serde::Deserialize;
use std::collections::{HashMap, HashSet};
use std::fmt::Write;
use std::path::{Path, PathBuf};
use std::process::Command;
use std::sync::atomic::AtomicBool;
use std::sync::{Arc, Mutex, OnceLock};
use std::time::{Duration, Instant, SystemTime};
use tokio_util::sync::CancellationToken;

/// Observer wrapper that forwards tool-call events to a channel sender
/// for real-time threaded notifications.
struct ChannelNotifyObserver {
    inner: Arc<dyn Observer>,
    tx: tokio::sync::mpsc::UnboundedSender<String>,
    tools_used: AtomicBool,
}

impl Observer for ChannelNotifyObserver {
    fn record_event(&self, event: &ObserverEvent) {
        if let ObserverEvent::ToolCallStart { tool, arguments } = event {
            self.tools_used.store(true, Ordering::Relaxed);
            let detail = match arguments {
                Some(args) if !args.is_empty() => {
                    if let Ok(v) = serde_json::from_str::<serde_json::Value>(args) {
                        if let Some(cmd) = v.get("command").and_then(|c| c.as_str()) {
                            format!(": `{}`", truncate_with_ellipsis(cmd, 200))
                        } else if let Some(q) = v.get("query").and_then(|c| c.as_str()) {
                            format!(": {}", truncate_with_ellipsis(q, 200))
                        } else if let Some(p) = v.get("path").and_then(|c| c.as_str()) {
                            format!(": {p}")
                        } else if let Some(u) = v.get("url").and_then(|c| c.as_str()) {
                            format!(": {u}")
                        } else {
                            let s = args.to_string();
                            format!(": {}", truncate_with_ellipsis(&s, 120))
                        }
                    } else {
                        let s = args.to_string();
                        format!(": {}", truncate_with_ellipsis(&s, 120))
                    }
                }
                _ => String::new(),
            };
            let _ = self.tx.send(format!("\u{1F527} `{tool}`{detail}"));
        }
        self.inner.record_event(event);
    }
    fn record_metric(&self, metric: &ObserverMetric) {
        self.inner.record_metric(metric);
    }
    fn flush(&self) {
        self.inner.flush();
    }
    fn name(&self) -> &str {
        "channel-notify"
    }
    fn as_any(&self) -> &dyn std::any::Any {
        self
    }
}

/// Per-sender conversation history for channel messages.
/// Bounded by `MAX_CONVERSATION_SENDERS` — oldest-accessed senders are evicted.
type ConversationHistoryMap = Arc<Mutex<lru::LruCache<String, Vec<ChatMessage>>>>;
/// Senders that requested `/new` and must force a fresh prompt on their next message.
type PendingNewSessionSet = Arc<Mutex<HashSet<String>>>;
/// Maximum conversation senders kept in memory (LRU eviction beyond this).
const MAX_CONVERSATION_SENDERS: usize = 1000;
/// Maximum history messages to keep per sender.
const MAX_CHANNEL_HISTORY: usize = 50;
/// Minimum user-message length (in chars) for auto-save to memory.
/// Messages shorter than this (e.g. "ok", "thanks") are not stored,
/// reducing noise in memory recall.
const AUTOSAVE_MIN_MESSAGE_CHARS: usize = 20;

/// Maximum characters per injected workspace file (matches `OpenClaw` default).
const BOOTSTRAP_MAX_CHARS: usize = 20_000;

const DEFAULT_CHANNEL_INITIAL_BACKOFF_SECS: u64 = 2;
const DEFAULT_CHANNEL_MAX_BACKOFF_SECS: u64 = 60;
const MIN_CHANNEL_MESSAGE_TIMEOUT_SECS: u64 = 30;
/// Default timeout for processing a single channel message (LLM + tools).
/// Used as fallback when not configured in channels_config.message_timeout_secs.
const CHANNEL_MESSAGE_TIMEOUT_SECS: u64 = 300;
/// Cap timeout scaling so large max_tool_iterations values do not create unbounded waits.
const CHANNEL_MESSAGE_TIMEOUT_SCALE_CAP: u64 = 4;
const CHANNEL_PARALLELISM_PER_CHANNEL: usize = 4;
const CHANNEL_MIN_IN_FLIGHT_MESSAGES: usize = 8;
const CHANNEL_MAX_IN_FLIGHT_MESSAGES: usize = 64;
const CHANNEL_TYPING_REFRESH_INTERVAL_SECS: u64 = 4;
const CHANNEL_HEALTH_HEARTBEAT_SECS: u64 = 30;
const MODEL_CACHE_FILE: &str = "models_cache.json";
const MODEL_CACHE_PREVIEW_LIMIT: usize = 10;
const MEMORY_CONTEXT_MAX_ENTRIES: usize = 4;
const MEMORY_CONTEXT_ENTRY_MAX_CHARS: usize = 800;
const MEMORY_CONTEXT_MAX_CHARS: usize = 4_000;
const CHANNEL_HISTORY_COMPACT_KEEP_MESSAGES: usize = 12;
const CHANNEL_HISTORY_COMPACT_CONTENT_CHARS: usize = 600;
/// Proactive context-window budget in estimated characters (~4 chars/token).
/// When the total character count of conversation history exceeds this limit,
/// older turns are dropped before the request is sent to the provider,
/// preventing context-window-exceeded errors.  Set conservatively below
/// common context windows (128 k tokens ≈ 512 k chars) to leave room for
/// system prompt, memory context, and model output.
const PROACTIVE_CONTEXT_BUDGET_CHARS: usize = 400_000;
/// Guardrail for hook-modified outbound channel content.
const CHANNEL_HOOK_MAX_OUTBOUND_CHARS: usize = 20_000;

type ProviderCacheMap = Arc<Mutex<HashMap<String, Arc<dyn Provider>>>>;
type RouteSelectionMap = Arc<Mutex<HashMap<String, ChannelRouteSelection>>>;

fn effective_channel_message_timeout_secs(configured: u64) -> u64 {
    configured.max(MIN_CHANNEL_MESSAGE_TIMEOUT_SECS)
}

fn channel_message_timeout_budget_secs(
    message_timeout_secs: u64,
    max_tool_iterations: usize,
) -> u64 {
    channel_message_timeout_budget_secs_with_cap(
        message_timeout_secs,
        max_tool_iterations,
        CHANNEL_MESSAGE_TIMEOUT_SCALE_CAP,
    )
}

fn channel_message_timeout_budget_secs_with_cap(
    message_timeout_secs: u64,
    max_tool_iterations: usize,
    scale_cap: u64,
) -> u64 {
    let iterations = max_tool_iterations.max(1) as u64;
    let scale = iterations.min(scale_cap);
    message_timeout_secs.saturating_mul(scale)
}

#[derive(Debug, Clone, PartialEq, Eq)]
struct ChannelRouteSelection {
    provider: String,
    model: String,
    /// Route-specific API key override. When set, this takes precedence over
    /// the global `api_key` in [`ChannelRuntimeContext`] when creating the
    /// provider for this route.
    api_key: Option<String>,
}

#[derive(Debug, Clone, PartialEq, Eq)]
enum ChannelRuntimeCommand {
    ShowProviders,
    SetProvider(String),
    ShowModel,
    SetModel(String),
    ShowConfig,
    NewSession,
}

#[derive(Debug, Clone, Default, Deserialize)]
struct ModelCacheState {
    entries: Vec<ModelCacheEntry>,
}

#[derive(Debug, Clone, Default, Deserialize)]
struct ModelCacheEntry {
    provider: String,
    models: Vec<String>,
}

#[derive(Debug, Clone)]
struct ChannelRuntimeDefaults {
    default_provider: String,
    model: String,
    temperature: f64,
    api_key: Option<String>,
    api_url: Option<String>,
    reliability: crate::config::ReliabilityConfig,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
struct ConfigFileStamp {
    modified: SystemTime,
    len: u64,
}

#[derive(Debug, Clone)]
struct RuntimeConfigState {
    defaults: ChannelRuntimeDefaults,
    last_applied_stamp: Option<ConfigFileStamp>,
}

fn runtime_config_store() -> &'static Mutex<HashMap<PathBuf, RuntimeConfigState>> {
    static STORE: OnceLock<Mutex<HashMap<PathBuf, RuntimeConfigState>>> = OnceLock::new();
    STORE.get_or_init(|| Mutex::new(HashMap::new()))
}

const SYSTEMD_STATUS_ARGS: [&str; 3] = ["--user", "is-active", "zeroclaw.service"];
const SYSTEMD_RESTART_ARGS: [&str; 3] = ["--user", "restart", "zeroclaw.service"];
const OPENRC_STATUS_ARGS: [&str; 2] = ["zeroclaw", "status"];
const OPENRC_RESTART_ARGS: [&str; 2] = ["zeroclaw", "restart"];

#[derive(Clone, Copy)]
#[allow(clippy::struct_excessive_bools)]
struct InterruptOnNewMessageConfig {
    telegram: bool,
    slack: bool,
    discord: bool,
    mattermost: bool,
    matrix: bool,
}

impl InterruptOnNewMessageConfig {
    fn enabled_for_channel(self, channel: &str) -> bool {
        match channel {
            "telegram" => self.telegram,
            "slack" => self.slack,
            "discord" => self.discord,
            "mattermost" => self.mattermost,
            "matrix" => self.matrix,
            _ => false,
        }
    }
}

#[derive(Clone)]
struct ChannelCostTrackingState {
    tracker: Arc<crate::cost::CostTracker>,
    prices: Arc<HashMap<String, crate::config::schema::ModelPricing>>,
}

#[derive(Clone)]
struct ChannelRuntimeContext {
    channels_by_name: Arc<HashMap<String, Arc<dyn Channel>>>,
    provider: Arc<dyn Provider>,
    default_provider: Arc<String>,
    prompt_config: Arc<crate::config::Config>,
    memory: Arc<dyn Memory>,
    tools_registry: Arc<Vec<Box<dyn Tool>>>,
    observer: Arc<dyn Observer>,
    system_prompt: Arc<String>,
    model: Arc<String>,
    temperature: f64,
    auto_save_memory: bool,
    max_tool_iterations: usize,
    min_relevance_score: f64,
    conversation_histories: ConversationHistoryMap,
    pending_new_sessions: PendingNewSessionSet,
    provider_cache: ProviderCacheMap,
    route_overrides: RouteSelectionMap,
    api_key: Option<String>,
    api_url: Option<String>,
    reliability: Arc<crate::config::ReliabilityConfig>,
    provider_runtime_options: providers::ProviderRuntimeOptions,
    workspace_dir: Arc<PathBuf>,
    message_timeout_secs: u64,
    interrupt_on_new_message: InterruptOnNewMessageConfig,
    multimodal: crate::config::MultimodalConfig,
    media_pipeline: crate::config::MediaPipelineConfig,
    transcription_config: crate::config::TranscriptionConfig,
    hooks: Option<Arc<crate::hooks::HookRunner>>,
    non_cli_excluded_tools: Arc<Vec<String>>,
    autonomy_level: AutonomyLevel,
    tool_call_dedup_exempt: Arc<Vec<String>>,
    model_routes: Arc<Vec<crate::config::ModelRouteConfig>>,
    query_classification: crate::config::QueryClassificationConfig,
    ack_reactions: bool,
    show_tool_calls: bool,
    session_store: Option<Arc<session_store::SessionStore>>,
    /// Non-interactive approval manager for channel-driven runs.
    /// Enforces `auto_approve` / `always_ask` / supervised policy from
    /// `[autonomy]` config; auto-denies tools that would need interactive
    /// approval since no operator is present on channel runs.
    approval_manager: Arc<ApprovalManager>,
    activated_tools: Option<std::sync::Arc<std::sync::Mutex<crate::tools::ActivatedToolSet>>>,
    cost_tracking: Option<ChannelCostTrackingState>,
    pacing: crate::config::PacingConfig,
    max_tool_result_chars: usize,
    context_token_budget: usize,
    debouncer: Arc<debounce::MessageDebouncer>,
}

#[derive(Clone)]
struct InFlightSenderTaskState {
    task_id: u64,
    cancellation: CancellationToken,
    completion: Arc<InFlightTaskCompletion>,
}

struct InFlightTaskCompletion {
    done: AtomicBool,
    notify: tokio::sync::Notify,
}

impl InFlightTaskCompletion {
    fn new() -> Self {
        Self {
            done: AtomicBool::new(false),
            notify: tokio::sync::Notify::new(),
        }
    }

    fn mark_done(&self) {
        self.done.store(true, Ordering::Release);
        self.notify.notify_waiters();
    }

    async fn wait(&self) {
        if self.done.load(Ordering::Acquire) {
            return;
        }
        self.notify.notified().await;
    }
}

fn conversation_memory_key(msg: &traits::ChannelMessage) -> String {
    // Include thread_ts for per-topic memory isolation in forum groups
    match &msg.thread_ts {
        Some(tid) => format!("{}_{}_{}_{}", msg.channel, tid, msg.sender, msg.id),
        None => format!("{}_{}_{}", msg.channel, msg.sender, msg.id),
    }
}

fn conversation_history_key(msg: &traits::ChannelMessage) -> String {
    // Include reply_target for per-channel isolation (e.g. distinct Discord/Slack
    // channels) and thread_ts for per-topic isolation in forum groups.
    match &msg.thread_ts {
        Some(tid) => format!(
            "{}_{}_{}_{}",
            msg.channel, msg.reply_target, tid, msg.sender
        ),
        None => format!("{}_{}_{}", msg.channel, msg.reply_target, msg.sender),
    }
}

fn followup_thread_id(msg: &traits::ChannelMessage) -> Option<String> {
    msg.thread_ts.clone().or_else(|| Some(msg.id.clone()))
}

fn interruption_scope_key(msg: &traits::ChannelMessage) -> String {
    match &msg.interruption_scope_id {
        Some(scope) => format!(
            "{}_{}_{}_{}",
            msg.channel, msg.reply_target, msg.sender, scope
        ),
        None => format!("{}_{}_{}", msg.channel, msg.reply_target, msg.sender),
    }
}

/// Returns `true` when `content` is a `/stop` command (with optional `@botname` suffix).
/// Not gated on channel type — all non-CLI channels support `/stop`.
fn is_stop_command(content: &str) -> bool {
    let trimmed = content.trim();
    if !trimmed.starts_with('/') {
        return false;
    }
    let cmd = trimmed.split_whitespace().next().unwrap_or("");
    let base = cmd.split('@').next().unwrap_or(cmd);
    base.eq_ignore_ascii_case("/stop")
}

/// Strip tool-call XML tags from outgoing messages.
///
/// LLM responses may contain `<function_calls>`, `<function_call>`,
/// `<tool_call>`, `<toolcall>`, `<tool-call>`, `<tool>`, or `<invoke>`
/// blocks that are internal protocol and must not be forwarded to end
/// users on any channel.
fn strip_tool_call_tags(message: &str) -> String {
    const TOOL_CALL_OPEN_TAGS: [&str; 7] = [
        "<function_calls>",
        "<function_call>",
        "<tool_call>",
        "<toolcall>",
        "<tool-call>",
        "<tool>",
        "<invoke>",
    ];

    fn find_first_tag<'a>(haystack: &str, tags: &'a [&'a str]) -> Option<(usize, &'a str)> {
        tags.iter()
            .filter_map(|tag| haystack.find(tag).map(|idx| (idx, *tag)))
            .min_by_key(|(idx, _)| *idx)
    }

    fn matching_close_tag(open_tag: &str) -> Option<&'static str> {
        match open_tag {
            "<function_calls>" => Some("</function_calls>"),
            "<function_call>" => Some("</function_call>"),
            "<tool_call>" => Some("</tool_call>"),
            "<toolcall>" => Some("</toolcall>"),
            "<tool-call>" => Some("</tool-call>"),
            "<tool>" => Some("</tool>"),
            "<invoke>" => Some("</invoke>"),
            _ => None,
        }
    }

    fn extract_first_json_end(input: &str) -> Option<usize> {
        let trimmed = input.trim_start();
        let trim_offset = input.len().saturating_sub(trimmed.len());

        for (byte_idx, ch) in trimmed.char_indices() {
            if ch != '{' && ch != '[' {
                continue;
            }

            let slice = &trimmed[byte_idx..];
            let mut stream =
                serde_json::Deserializer::from_str(slice).into_iter::<serde_json::Value>();
            if let Some(Ok(_value)) = stream.next() {
                let consumed = stream.byte_offset();
                if consumed > 0 {
                    return Some(trim_offset + byte_idx + consumed);
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

    let mut kept_segments = Vec::new();
    let mut remaining = message;

    while let Some((start, open_tag)) = find_first_tag(remaining, &TOOL_CALL_OPEN_TAGS) {
        let before = &remaining[..start];
        if !before.is_empty() {
            kept_segments.push(before.to_string());
        }

        let Some(close_tag) = matching_close_tag(open_tag) else {
            break;
        };
        let after_open = &remaining[start + open_tag.len()..];

        if let Some(close_idx) = after_open.find(close_tag) {
            remaining = &after_open[close_idx + close_tag.len()..];
            continue;
        }

        if let Some(consumed_end) = extract_first_json_end(after_open) {
            remaining = strip_leading_close_tags(&after_open[consumed_end..]);
            continue;
        }

        kept_segments.push(remaining[start..].to_string());
        remaining = "";
        break;
    }

    if !remaining.is_empty() {
        kept_segments.push(remaining.to_string());
    }

    let mut result = kept_segments.concat();

    // Clean up any resulting blank lines (but preserve paragraphs)
    while result.contains("\n\n\n") {
        result = result.replace("\n\n\n", "\n\n");
    }

    result.trim().to_string()
}

fn channel_delivery_instructions(channel_name: &str) -> Option<&'static str> {
    match channel_name {
        "matrix" => Some(
            "When responding on Matrix:\n\
             - Use Markdown formatting (bold, italic, code blocks)\n\
             - Be concise and direct\n\
             - When you receive a [Voice message], the user spoke to you. Respond naturally as in conversation.\n\
             - Your text reply will automatically be converted to audio and sent back as a voice message.\n",
        ),
        "telegram" => Some(
            "When responding on Telegram:\n\
             - Include media markers for files or URLs that should be sent as attachments\n\
             - Use **bold** for key terms, section titles, and important info (renders as <b>)\n\
             - Use *italic* for emphasis (renders as <i>)\n\
             - Use `backticks` for inline code, commands, or technical terms\n\
             - Use triple backticks for code blocks\n\
             - Use emoji naturally to add personality — but don't overdo it\n\
             - Be concise and direct. Skip filler phrases like 'Great question!' or 'Certainly!'\n\
             - Structure longer answers with bold headers, not raw markdown ## headers\n\
             - For media attachments use markers: [IMAGE:<path-or-url>], [DOCUMENT:<path-or-url>], [VIDEO:<path-or-url>], [AUDIO:<path-or-url>], or [VOICE:<path-or-url>]\n\
             - Keep normal text outside markers and never wrap markers in code fences.\n\
             - Use tool results silently: answer the latest user message directly, and do not narrate delayed/internal tool execution bookkeeping.",
        ),
        "qq" => Some(
            "When responding on QQ:\n\
             - Use Markdown formatting\n\
             - Be concise and direct\n\
             - For media attachments use markers: [IMAGE:<path-or-url>], [DOCUMENT:<path-or-url>], \
               [VIDEO:<path-or-url>], [VOICE:<path-or-url>]\n\
             - Voice supports .wav, .mp3, .silk formats only. Other audio formats use [DOCUMENT:]\n\
             - Keep normal text outside markers and never wrap markers in code fences.\n",
        ),
        _ => None,
    }
}

fn build_channel_system_prompt(
    base_prompt: &str,
    channel_name: &str,
    reply_target: &str,
) -> String {
    let mut prompt = base_prompt.to_string();

    // Refresh the stale datetime in the cached system prompt
    {
        let now = chrono::Local::now();
        let fresh = format!(
            "## Current Date & Time\n\n{} ({})\n",
            now.format("%Y-%m-%d %H:%M:%S"),
            now.format("%Z"),
        );
        if let Some(start) = prompt.find("## Current Date & Time\n\n") {
            // Find the end of this section (next "## " heading or end of string)
            let rest = &prompt[start + 24..]; // skip past "## Current Date & Time\n\n"
            let section_end = rest
                .find("\n## ")
                .map(|i| start + 24 + i)
                .unwrap_or(prompt.len());
            prompt.replace_range(start..section_end, fresh.trim_end());
        }
    }

    if let Some(instructions) = channel_delivery_instructions(channel_name) {
        if prompt.is_empty() {
            prompt = instructions.to_string();
        } else {
            prompt = format!("{prompt}\n\n{instructions}");
        }
    }

    if !reply_target.is_empty() {
        let context = format!(
            "\n\nChannel context: You are currently responding on channel={channel_name}, \
             reply_target={reply_target}. When scheduling delayed messages or reminders \
             via cron_add for this conversation, use delivery={{\"mode\":\"announce\",\
             \"channel\":\"{channel_name}\",\"to\":\"{reply_target}\"}} so the message \
             reaches the user."
        );
        prompt.push_str(&context);
    }

    prompt
}

fn normalize_cached_channel_turns(turns: Vec<ChatMessage>) -> Vec<ChatMessage> {
    let mut normalized = Vec::with_capacity(turns.len());
    let mut expecting_user = true;

    for turn in turns {
        match (expecting_user, turn.role.as_str()) {
            // Pass through tool-role messages preserved by
            // keep_tool_context_turns (#4827).  After a tool result the
            // next expected message is an assistant response, same as
            // after a user message.
            (_, "tool") | (true, "user") => {
                normalized.push(turn);
                expecting_user = false;
            }
            (false, "assistant") => {
                normalized.push(turn);
                expecting_user = true;
            }
            // Interrupted channel turns can produce consecutive user messages
            // (no assistant persisted yet). Merge instead of dropping.
            (false, "user") | (true, "assistant") => {
                if let Some(last_turn) = normalized.last_mut() {
                    if !turn.content.is_empty() {
                        if !last_turn.content.is_empty() {
                            last_turn.content.push_str("\n\n");
                        }
                        last_turn.content.push_str(&turn.content);
                    }
                }
            }
            _ => {}
        }
    }

    normalized
}

/// Remove `<tool_result …>…</tool_result>` blocks (and a leading `[Tool results]`
/// header, if present) from a conversation-history entry so that stale tool
/// output is never presented to the LLM without the corresponding `<tool_call>`.
fn strip_tool_result_content(text: &str) -> String {
    static TOOL_RESULT_RE: std::sync::LazyLock<regex::Regex> = std::sync::LazyLock::new(|| {
        regex::Regex::new(r"(?s)<tool_result[^>]*>.*?</tool_result>").unwrap()
    });

    let cleaned = TOOL_RESULT_RE.replace_all(text, "");
    let cleaned = cleaned.trim();

    // If the only remaining content is the header, drop it entirely.
    if cleaned == "[Tool results]" || cleaned.is_empty() {
        return String::new();
    }

    cleaned.to_string()
}

/// Remove a leading `[Used tools: ...]` line from a cached assistant turn.
///
/// The tool-context summary is prepended to history entries so the LLM retains
/// awareness of prior tool usage. However, when these entries are loaded back
/// into the LLM context, the bracket-format leaks into generated output and
/// gets forwarded to end users as-is (bug #4400). Stripping the prefix on
/// reload prevents the model from learning and reproducing this internal format.
fn strip_tool_summary_prefix(text: &str) -> String {
    if let Some(rest) = text.strip_prefix("[Used tools:") {
        // Find the closing bracket, then skip it and any leading newline(s).
        if let Some(bracket_end) = rest.find(']') {
            let after_bracket = &rest[bracket_end + 1..];
            let trimmed = after_bracket.trim_start_matches('\n');
            if trimmed.is_empty() {
                return String::new();
            }
            return trimmed.to_string();
        }
    }
    text.to_string()
}

fn supports_runtime_model_switch(channel_name: &str) -> bool {
    matches!(channel_name, "telegram" | "discord" | "matrix" | "slack")
}

fn parse_runtime_command(channel_name: &str, content: &str) -> Option<ChannelRuntimeCommand> {
    let trimmed = content.trim();
    if !trimmed.starts_with('/') {
        return None;
    }

    let mut parts = trimmed.split_whitespace();
    let command_token = parts.next()?;
    let base_command = command_token
        .split('@')
        .next()
        .unwrap_or(command_token)
        .to_ascii_lowercase();

    match base_command.as_str() {
        // `/new` is available on every channel — no model-switch gate.
        "/new" => Some(ChannelRuntimeCommand::NewSession),
        // Model/provider switching is channel-gated.
        "/models" if supports_runtime_model_switch(channel_name) => {
            if let Some(provider) = parts.next() {
                Some(ChannelRuntimeCommand::SetProvider(
                    provider.trim().to_string(),
                ))
            } else {
                Some(ChannelRuntimeCommand::ShowProviders)
            }
        }
        "/model" if supports_runtime_model_switch(channel_name) => {
            let model = parts.collect::<Vec<_>>().join(" ").trim().to_string();
            if model.is_empty() {
                Some(ChannelRuntimeCommand::ShowModel)
            } else {
                Some(ChannelRuntimeCommand::SetModel(model))
            }
        }
        "/config" if supports_runtime_model_switch(channel_name) => {
            Some(ChannelRuntimeCommand::ShowConfig)
        }
        _ => None,
    }
}

fn resolve_provider_alias(name: &str) -> Option<String> {
    let candidate = name.trim();
    if candidate.is_empty() {
        return None;
    }

    let providers_list = providers::list_providers();
    for provider in providers_list {
        if provider.name.eq_ignore_ascii_case(candidate)
            || provider
                .aliases
                .iter()
                .any(|alias| alias.eq_ignore_ascii_case(candidate))
        {
            return Some(provider.name.to_string());
        }
    }

    None
}

fn resolved_default_provider(config: &Config) -> String {
    config
        .default_provider
        .clone()
        .unwrap_or_else(|| "openrouter".to_string())
}

fn resolved_default_model(config: &Config) -> String {
    config
        .default_model
        .clone()
        .unwrap_or_else(|| "anthropic/claude-sonnet-4.6".to_string())
}

fn runtime_defaults_from_config(config: &Config) -> ChannelRuntimeDefaults {
    ChannelRuntimeDefaults {
        default_provider: resolved_default_provider(config),
        model: resolved_default_model(config),
        temperature: config.default_temperature,
        api_key: config.api_key.clone(),
        api_url: config.api_url.clone(),
        reliability: config.reliability.clone(),
    }
}

fn runtime_config_path(ctx: &ChannelRuntimeContext) -> Option<PathBuf> {
    ctx.provider_runtime_options
        .zeroclaw_dir
        .as_ref()
        .map(|dir| dir.join("config.toml"))
}

fn runtime_defaults_snapshot(ctx: &ChannelRuntimeContext) -> ChannelRuntimeDefaults {
    if let Some(config_path) = runtime_config_path(ctx) {
        let store = runtime_config_store()
            .lock()
            .unwrap_or_else(|e| e.into_inner());
        if let Some(state) = store.get(&config_path) {
            return state.defaults.clone();
        }
    }

    ChannelRuntimeDefaults {
        default_provider: ctx.default_provider.as_str().to_string(),
        model: ctx.model.as_str().to_string(),
        temperature: ctx.temperature,
        api_key: ctx.api_key.clone(),
        api_url: ctx.api_url.clone(),
        reliability: (*ctx.reliability).clone(),
    }
}

async fn config_file_stamp(path: &Path) -> Option<ConfigFileStamp> {
    let metadata = tokio::fs::metadata(path).await.ok()?;
    let modified = metadata.modified().ok()?;
    Some(ConfigFileStamp {
        modified,
        len: metadata.len(),
    })
}

fn decrypt_optional_secret_for_runtime_reload(
    store: &crate::security::SecretStore,
    value: &mut Option<String>,
    field_name: &str,
) -> Result<()> {
    if let Some(raw) = value.clone() {
        if crate::security::SecretStore::is_encrypted(&raw) {
            *value = Some(
                store
                    .decrypt(&raw)
                    .with_context(|| format!("Failed to decrypt {field_name}"))?,
            );
        }
    }
    Ok(())
}

async fn load_runtime_defaults_from_config_file(path: &Path) -> Result<ChannelRuntimeDefaults> {
    let contents = tokio::fs::read_to_string(path)
        .await
        .with_context(|| format!("Failed to read {}", path.display()))?;
    let mut parsed: Config =
        toml::from_str(&contents).with_context(|| format!("Failed to parse {}", path.display()))?;
    parsed.config_path = path.to_path_buf();

    if let Some(zeroclaw_dir) = path.parent() {
        let store = crate::security::SecretStore::new(zeroclaw_dir, parsed.secrets.encrypt);
        decrypt_optional_secret_for_runtime_reload(&store, &mut parsed.api_key, "config.api_key")?;
        // Decrypt TTS provider API keys for runtime reload
        if let Some(ref mut openai) = parsed.tts.openai {
            decrypt_optional_secret_for_runtime_reload(
                &store,
                &mut openai.api_key,
                "config.tts.openai.api_key",
            )?;
        }
        if let Some(ref mut elevenlabs) = parsed.tts.elevenlabs {
            decrypt_optional_secret_for_runtime_reload(
                &store,
                &mut elevenlabs.api_key,
                "config.tts.elevenlabs.api_key",
            )?;
        }
        if let Some(ref mut google) = parsed.tts.google {
            decrypt_optional_secret_for_runtime_reload(
                &store,
                &mut google.api_key,
                "config.tts.google.api_key",
            )?;
        }
    }

    parsed.apply_env_overrides();
    Ok(runtime_defaults_from_config(&parsed))
}

async fn maybe_apply_runtime_config_update(ctx: &ChannelRuntimeContext) -> Result<()> {
    let Some(config_path) = runtime_config_path(ctx) else {
        return Ok(());
    };

    let Some(stamp) = config_file_stamp(&config_path).await else {
        return Ok(());
    };

    {
        let store = runtime_config_store()
            .lock()
            .unwrap_or_else(|e| e.into_inner());
        if let Some(state) = store.get(&config_path) {
            if state.last_applied_stamp == Some(stamp) {
                return Ok(());
            }
        }
    }

    let next_defaults = load_runtime_defaults_from_config_file(&config_path).await?;
    let next_default_provider = providers::create_resilient_provider_with_options(
        &next_defaults.default_provider,
        next_defaults.api_key.as_deref(),
        next_defaults.api_url.as_deref(),
        &next_defaults.reliability,
        &ctx.provider_runtime_options,
    )?;
    let next_default_provider: Arc<dyn Provider> = Arc::from(next_default_provider);

    if let Err(err) = next_default_provider.warmup().await {
        if crate::providers::reliable::is_non_retryable(&err) {
            tracing::warn!(
                provider = %next_defaults.default_provider,
                model = %next_defaults.model,
                "Rejecting config reload: model not available (non-retryable): {err}"
            );
            return Ok(());
        }
        tracing::warn!(
            provider = %next_defaults.default_provider,
            "Provider warmup failed after config reload (retryable, applying anyway): {err}"
        );
    }

    {
        let mut cache = ctx.provider_cache.lock().unwrap_or_else(|e| e.into_inner());
        cache.clear();
        cache.insert(
            next_defaults.default_provider.clone(),
            Arc::clone(&next_default_provider),
        );
    }

    {
        let mut store = runtime_config_store()
            .lock()
            .unwrap_or_else(|e| e.into_inner());
        store.insert(
            config_path.clone(),
            RuntimeConfigState {
                defaults: next_defaults.clone(),
                last_applied_stamp: Some(stamp),
            },
        );
    }

    tracing::info!(
        path = %config_path.display(),
        provider = %next_defaults.default_provider,
        model = %next_defaults.model,
        temperature = next_defaults.temperature,
        "Applied updated channel runtime config from disk"
    );

    Ok(())
}

fn default_route_selection(ctx: &ChannelRuntimeContext) -> ChannelRouteSelection {
    let defaults = runtime_defaults_snapshot(ctx);
    ChannelRouteSelection {
        provider: defaults.default_provider,
        model: defaults.model,
        api_key: None,
    }
}

fn get_route_selection(ctx: &ChannelRuntimeContext, sender_key: &str) -> ChannelRouteSelection {
    ctx.route_overrides
        .lock()
        .unwrap_or_else(|e| e.into_inner())
        .get(sender_key)
        .cloned()
        .unwrap_or_else(|| default_route_selection(ctx))
}

fn set_route_selection(ctx: &ChannelRuntimeContext, sender_key: &str, next: ChannelRouteSelection) {
    let default_route = default_route_selection(ctx);
    let mut routes = ctx
        .route_overrides
        .lock()
        .unwrap_or_else(|e| e.into_inner());
    if next == default_route {
        routes.remove(sender_key);
    } else {
        routes.insert(sender_key.to_string(), next);
    }
}

fn clear_sender_history(ctx: &ChannelRuntimeContext, sender_key: &str) {
    ctx.conversation_histories
        .lock()
        .unwrap_or_else(|e| e.into_inner())
        .pop(sender_key);
}

fn mark_sender_for_new_session(ctx: &ChannelRuntimeContext, sender_key: &str) {
    ctx.pending_new_sessions
        .lock()
        .unwrap_or_else(|e| e.into_inner())
        .insert(sender_key.to_string());
}

fn take_pending_new_session(ctx: &ChannelRuntimeContext, sender_key: &str) -> bool {
    ctx.pending_new_sessions
        .lock()
        .unwrap_or_else(|e| e.into_inner())
        .remove(sender_key)
}

fn replace_available_skills_section(base_prompt: &str, refreshed_skills: &str) -> String {
    const SKILLS_HEADER: &str = "## Available Skills\n\n";
    const SKILLS_END: &str = "</available_skills>";
    const WORKSPACE_HEADER: &str = "## Workspace\n\n";

    if let Some(start) = base_prompt.find(SKILLS_HEADER) {
        if let Some(rel_end) = base_prompt[start..].find(SKILLS_END) {
            let end = start + rel_end + SKILLS_END.len();
            let tail = base_prompt[end..]
                .strip_prefix("\n\n")
                .unwrap_or(&base_prompt[end..]);

            let mut refreshed = String::with_capacity(
                base_prompt.len().saturating_sub(end.saturating_sub(start))
                    + refreshed_skills.len()
                    + 2,
            );
            refreshed.push_str(&base_prompt[..start]);
            if !refreshed_skills.is_empty() {
                refreshed.push_str(refreshed_skills);
                refreshed.push_str("\n\n");
            }
            refreshed.push_str(tail);
            return refreshed;
        }
    }

    if refreshed_skills.is_empty() {
        return base_prompt.to_string();
    }

    if let Some(workspace_start) = base_prompt.find(WORKSPACE_HEADER) {
        let mut refreshed = String::with_capacity(base_prompt.len() + refreshed_skills.len() + 2);
        refreshed.push_str(&base_prompt[..workspace_start]);
        refreshed.push_str(refreshed_skills);
        refreshed.push_str("\n\n");
        refreshed.push_str(&base_prompt[workspace_start..]);
        return refreshed;
    }

    format!("{base_prompt}\n\n{refreshed_skills}")
}

fn refreshed_new_session_system_prompt(ctx: &ChannelRuntimeContext) -> String {
    let refreshed_skills = crate::skills::skills_to_prompt_with_mode(
        &crate::skills::load_skills_with_config(
            ctx.workspace_dir.as_ref(),
            ctx.prompt_config.as_ref(),
        ),
        ctx.workspace_dir.as_ref(),
        ctx.prompt_config.skills.prompt_injection_mode,
    );
    replace_available_skills_section(ctx.system_prompt.as_str(), &refreshed_skills)
}

fn compact_sender_history(ctx: &ChannelRuntimeContext, sender_key: &str) -> bool {
    let mut histories = ctx
        .conversation_histories
        .lock()
        .unwrap_or_else(|e| e.into_inner());

    let Some(turns) = histories.get_mut(sender_key) else {
        return false;
    };

    if turns.is_empty() {
        return false;
    }

    let keep_from = turns
        .len()
        .saturating_sub(CHANNEL_HISTORY_COMPACT_KEEP_MESSAGES);
    let mut compacted = normalize_cached_channel_turns(turns[keep_from..].to_vec());

    for turn in &mut compacted {
        if turn.content.chars().count() > CHANNEL_HISTORY_COMPACT_CONTENT_CHARS {
            turn.content =
                truncate_with_ellipsis(&turn.content, CHANNEL_HISTORY_COMPACT_CONTENT_CHARS);
        }
    }

    if compacted.is_empty() {
        turns.clear();
        return false;
    }

    *turns = compacted;
    true
}

/// Proactively trim conversation turns so that the total estimated character
/// count stays within [`PROACTIVE_CONTEXT_BUDGET_CHARS`].  Drops the oldest
/// turns first, but always preserves the most recent turn (the current user
/// message).  Returns the number of turns dropped.
fn proactive_trim_turns(turns: &mut Vec<ChatMessage>, budget: usize) -> usize {
    let total_chars: usize = turns.iter().map(|t| t.content.chars().count()).sum();
    if total_chars <= budget || turns.len() <= 1 {
        return 0;
    }

    let mut excess = total_chars.saturating_sub(budget);
    let mut drop_count = 0;

    // Walk from the oldest turn forward, but never drop the very last turn.
    while excess > 0 && drop_count < turns.len().saturating_sub(1) {
        excess = excess.saturating_sub(turns[drop_count].content.chars().count());
        drop_count += 1;
    }

    if drop_count > 0 {
        turns.drain(..drop_count);
    }
    drop_count
}

fn append_sender_turn(ctx: &ChannelRuntimeContext, sender_key: &str, turn: ChatMessage) {
    // Persist to JSONL before adding to in-memory history.
    if let Some(ref store) = ctx.session_store {
        if let Err(e) = store.append(sender_key, &turn) {
            tracing::warn!("Failed to persist session turn: {e}");
        }
    }

    // Use the user-configured max_history_messages (fall back to
    // MAX_CHANNEL_HISTORY when the config value is 0 or absent).
    let max_history = {
        let configured = ctx.prompt_config.agent.max_history_messages;
        if configured > 0 {
            configured
        } else {
            MAX_CHANNEL_HISTORY
        }
    };

    let mut histories = ctx
        .conversation_histories
        .lock()
        .unwrap_or_else(|e| e.into_inner());
    let turns = histories.get_or_insert_mut(sender_key.to_string(), Vec::new);
    turns.push(turn);
    while turns.len() > max_history {
        turns.remove(0);
    }
}

/// Extract tool-call (assistant with tool_call content) and tool-result
/// messages from the current turn in the LLM history, excluding the final
/// assistant text response.  "Current turn" = everything after the last
/// user-role message.
fn extract_current_turn_tool_messages(history: &[ChatMessage]) -> Vec<ChatMessage> {
    // Find the index of the last user message — tool messages for the
    // current turn come after it.
    let last_user_idx = history.iter().rposition(|m| m.role == "user").unwrap_or(0);

    let tail = &history[last_user_idx + 1..];
    if tail.is_empty() {
        return Vec::new();
    }

    // Everything except the very last assistant message (which is the
    // final text response that gets stored separately).
    let end = if tail.last().is_some_and(|m| m.role == "assistant") {
        tail.len() - 1
    } else {
        tail.len()
    };

    tail[..end]
        .iter()
        .filter(|m| m.role == "assistant" || m.role == "tool")
        .cloned()
        .collect()
}

/// Remove tool-role and intermediate assistant tool-call messages from
/// conversation turns older than the most recent `keep_turns` user→assistant
/// exchanges.  This prevents unbounded history growth while preserving
/// tool context for the N most recent turns.
fn strip_old_tool_context(ctx: &ChannelRuntimeContext, sender_key: &str, keep_turns: usize) {
    let mut histories = ctx
        .conversation_histories
        .lock()
        .unwrap_or_else(|e| e.into_inner());

    let Some(turns) = histories.get_mut(sender_key) else {
        return;
    };

    // Walk backwards to find the boundary: count user messages to
    // identify which turns are "recent" (protected from stripping).
    let mut user_count = 0;
    let mut protect_from = turns.len();
    for (i, turn) in turns.iter().enumerate().rev() {
        if turn.role == "user" {
            user_count += 1;
            if user_count > keep_turns {
                // Everything before this index is old enough to strip.
                protect_from = i + 1; // protect from next message onward
                break;
            }
        }
    }

    // Remove tool and intermediate assistant messages before the boundary.
    // An "intermediate assistant" is one whose content looks like a tool
    // call (contains `<tool_call>` or starts with `{\"tool_call`).
    let mut i = 0;
    while i < protect_from && i < turns.len() {
        let dominated = turns[i].role == "tool"
            || (turns[i].role == "assistant" && is_tool_call_content(&turns[i].content));
        if dominated {
            turns.remove(i);
            // Adjust boundary since we removed an element.
            protect_from = protect_from.saturating_sub(1);
        } else {
            i += 1;
        }
    }
}

/// Heuristic: does this assistant message content represent a tool call
/// rather than a final text response?
fn is_tool_call_content(content: &str) -> bool {
    let trimmed = content.trim();
    trimmed.contains("<tool_call>")
        || trimmed.starts_with("{\"tool_call\"")
        || trimmed.starts_with("{\"name\"")
}

fn rollback_orphan_user_turn(
    ctx: &ChannelRuntimeContext,
    sender_key: &str,
    expected_content: &str,
) -> bool {
    let mut histories = ctx
        .conversation_histories
        .lock()
        .unwrap_or_else(|e| e.into_inner());
    let Some(turns) = histories.get_mut(sender_key) else {
        return false;
    };

    let should_pop = turns
        .last()
        .is_some_and(|turn| turn.role == "user" && turn.content == expected_content);
    if !should_pop {
        return false;
    }

    turns.pop();
    if turns.is_empty() {
        histories.pop(sender_key);
    }

    // Also remove the orphan turn from the persisted JSONL session store so
    // it doesn't resurface after a daemon restart (fixes #3674).
    if let Some(ref store) = ctx.session_store {
        if let Err(e) = store.remove_last(sender_key) {
            tracing::warn!("Failed to rollback session store entry: {e}");
        }
    }

    true
}

fn should_rollback_failed_user_turn(error: &anyhow::Error) -> bool {
    if error
        .downcast_ref::<providers::ProviderCapabilityError>()
        .is_some_and(|capability| capability.capability.eq_ignore_ascii_case("vision"))
    {
        return true;
    }

    crate::providers::reliable::is_non_retryable(error)
}

fn should_skip_memory_context_entry(key: &str, content: &str) -> bool {
    if memory::is_assistant_autosave_key(key) {
        return true;
    }

    if memory::should_skip_autosave_content(content) {
        return true;
    }

    if key.trim().to_ascii_lowercase().ends_with("_history") {
        return true;
    }

    // Skip entries containing image markers to prevent duplication.
    // When auto_save stores a photo message to memory, a subsequent
    // memory recall on the same turn would surface the marker again,
    // causing two identical image blocks in the provider request.
    if content.contains("[IMAGE:") {
        return true;
    }

    // Skip entries containing tool_result blocks. After a daemon restart
    // these can be recalled from SQLite and injected as memory context,
    // presenting the LLM with a `<tool_result>` without a preceding
    // `<tool_call>` and triggering hallucinated output.
    if content.contains("<tool_result") {
        return true;
    }

    content.chars().count() > MEMORY_CONTEXT_MAX_CHARS
}

fn is_context_window_overflow_error(err: &anyhow::Error) -> bool {
    let lower = err.to_string().to_lowercase();
    [
        "exceeds the context window",
        "context window of this model",
        "maximum context length",
        "context length exceeded",
        "too many tokens",
        "token limit exceeded",
        "prompt is too long",
        "input is too long",
    ]
    .iter()
    .any(|hint| lower.contains(hint))
}

fn load_cached_model_preview(workspace_dir: &Path, provider_name: &str) -> Vec<String> {
    let cache_path = workspace_dir.join("state").join(MODEL_CACHE_FILE);
    let Ok(raw) = std::fs::read_to_string(cache_path) else {
        return Vec::new();
    };
    let Ok(state) = serde_json::from_str::<ModelCacheState>(&raw) else {
        return Vec::new();
    };

    state
        .entries
        .into_iter()
        .find(|entry| entry.provider == provider_name)
        .map(|entry| {
            entry
                .models
                .into_iter()
                .take(MODEL_CACHE_PREVIEW_LIMIT)
                .collect::<Vec<_>>()
        })
        .unwrap_or_default()
}

/// Build a cache key that includes the provider name and, when a
/// route-specific API key is supplied, a hash of that key. This prevents
/// cache poisoning when multiple routes target the same provider with
/// different credentials.
fn provider_cache_key(provider_name: &str, route_api_key: Option<&str>) -> String {
    match route_api_key {
        Some(key) => {
            use std::hash::{Hash, Hasher};
            let mut hasher = std::collections::hash_map::DefaultHasher::new();
            key.hash(&mut hasher);
            format!("{provider_name}@{:x}", hasher.finish())
        }
        None => provider_name.to_string(),
    }
}

async fn get_or_create_provider(
    ctx: &ChannelRuntimeContext,
    provider_name: &str,
    route_api_key: Option<&str>,
) -> anyhow::Result<Arc<dyn Provider>> {
    let cache_key = provider_cache_key(provider_name, route_api_key);

    if let Some(existing) = ctx
        .provider_cache
        .lock()
        .unwrap_or_else(|e| e.into_inner())
        .get(&cache_key)
        .cloned()
    {
        return Ok(existing);
    }

    // Only return the pre-built default provider when there is no
    // route-specific credential override — otherwise the default was
    // created with the global key and would be wrong.
    if route_api_key.is_none() && provider_name == ctx.default_provider.as_str() {
        return Ok(Arc::clone(&ctx.provider));
    }

    let defaults = runtime_defaults_snapshot(ctx);
    let api_url = if provider_name == defaults.default_provider.as_str() {
        defaults.api_url.as_deref()
    } else {
        None
    };

    // Prefer route-specific credential; fall back to the global key.
    let effective_api_key = route_api_key
        .map(ToString::to_string)
        .or_else(|| ctx.api_key.clone());

    let provider = create_resilient_provider_nonblocking(
        provider_name,
        effective_api_key,
        api_url.map(ToString::to_string),
        ctx.reliability.as_ref().clone(),
        ctx.provider_runtime_options.clone(),
    )
    .await?;
    let provider: Arc<dyn Provider> = Arc::from(provider);

    if let Err(err) = provider.warmup().await {
        tracing::warn!(provider = provider_name, "Provider warmup failed: {err}");
    }

    let mut cache = ctx.provider_cache.lock().unwrap_or_else(|e| e.into_inner());
    let cached = cache
        .entry(cache_key)
        .or_insert_with(|| Arc::clone(&provider));
    Ok(Arc::clone(cached))
}

async fn create_resilient_provider_nonblocking(
    provider_name: &str,
    api_key: Option<String>,
    api_url: Option<String>,
    reliability: crate::config::ReliabilityConfig,
    provider_runtime_options: providers::ProviderRuntimeOptions,
) -> anyhow::Result<Box<dyn Provider>> {
    let provider_name = provider_name.to_string();
    tokio::task::spawn_blocking(move || {
        providers::create_resilient_provider_with_options(
            &provider_name,
            api_key.as_deref(),
            api_url.as_deref(),
            &reliability,
            &provider_runtime_options,
        )
    })
    .await
    .context("failed to join provider initialization task")?
}

fn build_models_help_response(
    current: &ChannelRouteSelection,
    workspace_dir: &Path,
    model_routes: &[crate::config::ModelRouteConfig],
) -> String {
    let mut response = String::new();
    let _ = writeln!(
        response,
        "Current provider: `{}`\nCurrent model: `{}`",
        current.provider, current.model
    );
    response.push_str("\nSwitch model with `/model <model-id>` or `/model <hint>`.\n");

    if !model_routes.is_empty() {
        response.push_str("\nConfigured model routes:\n");
        for route in model_routes {
            let _ = writeln!(
                response,
                "  `{}` → {} ({})",
                route.hint, route.model, route.provider
            );
        }
    }

    let cached_models = load_cached_model_preview(workspace_dir, &current.provider);
    if cached_models.is_empty() {
        let _ = writeln!(
            response,
            "\nNo cached model list found for `{}`. Ask the operator to run `zeroclaw models refresh --provider {}`.",
            current.provider, current.provider
        );
    } else {
        let _ = writeln!(
            response,
            "\nCached model IDs (top {}):",
            cached_models.len()
        );
        for model in cached_models {
            let _ = writeln!(response, "- `{model}`");
        }
    }

    response
}

fn build_providers_help_response(current: &ChannelRouteSelection) -> String {
    let mut response = String::new();
    let _ = writeln!(
        response,
        "Current provider: `{}`\nCurrent model: `{}`",
        current.provider, current.model
    );
    response.push_str("\nSwitch provider with `/models <provider>`.\n");
    response.push_str("Switch model with `/model <model-id>`.\n\n");
    response.push_str("Available providers:\n");
    for provider in providers::list_providers() {
        if provider.aliases.is_empty() {
            let _ = writeln!(response, "- {}", provider.name);
        } else {
            let _ = writeln!(
                response,
                "- {} (aliases: {})",
                provider.name,
                provider.aliases.join(", ")
            );
        }
    }
    response
}

/// Build a plain-text `/config` response for non-Slack channels.
fn build_config_text_response(
    current: &ChannelRouteSelection,
    _workspace_dir: &Path,
    model_routes: &[crate::config::ModelRouteConfig],
) -> String {
    let mut resp = String::new();
    let _ = writeln!(
        resp,
        "Current provider: `{}`\nCurrent model: `{}`",
        current.provider, current.model
    );
    resp.push_str("\nAvailable providers:\n");
    for p in providers::list_providers() {
        let _ = writeln!(resp, "- `{}`", p.name);
    }
    if !model_routes.is_empty() {
        resp.push_str("\nConfigured model routes:\n");
        for route in model_routes {
            let _ = writeln!(
                resp,
                "  `{}` -> {} ({})",
                route.hint, route.model, route.provider
            );
        }
    }
    resp.push_str(
        "\nUse `/models <provider>` to switch provider.\nUse `/model <model-id>` to switch model.",
    );
    resp
}

/// Prefix used to signal that a runtime command response contains raw Block Kit
/// JSON instead of plain text. [`SlackChannel::send`] detects this and posts
/// the blocks directly via `chat.postMessage`.
const BLOCK_KIT_PREFIX: &str = "__ZEROCLAW_BLOCK_KIT__";

/// Build a Slack Block Kit JSON payload for the `/config` interactive UI.
fn build_config_block_kit(
    current: &ChannelRouteSelection,
    workspace_dir: &Path,
    model_routes: &[crate::config::ModelRouteConfig],
) -> String {
    let provider_options: Vec<serde_json::Value> = providers::list_providers()
        .iter()
        .map(|p| {
            serde_json::json!({
                "text": { "type": "plain_text", "text": p.display_name },
                "value": p.name
            })
        })
        .collect();

    // Build model options from model_routes + cached models.
    let mut model_options: Vec<serde_json::Value> = model_routes
        .iter()
        .map(|r| {
            let label = if r.hint.is_empty() {
                r.model.clone()
            } else {
                format!("{} ({})", r.model, r.hint)
            };
            serde_json::json!({
                "text": { "type": "plain_text", "text": label },
                "value": r.model
            })
        })
        .collect();

    let cached = load_cached_model_preview(workspace_dir, &current.provider);
    for model_id in cached {
        if !model_options.iter().any(|o| {
            o.get("value")
                .and_then(|v| v.as_str())
                .is_some_and(|v| v == model_id)
        }) {
            model_options.push(serde_json::json!({
                "text": { "type": "plain_text", "text": model_id },
                "value": model_id
            }));
        }
    }

    // If the current model is not in the list, prepend it.
    if !model_options.iter().any(|o| {
        o.get("value")
            .and_then(|v| v.as_str())
            .is_some_and(|v| v == current.model)
    }) {
        model_options.insert(
            0,
            serde_json::json!({
                "text": { "type": "plain_text", "text": &current.model },
                "value": &current.model
            }),
        );
    }

    // Find initial options matching current selection.
    let initial_provider = provider_options
        .iter()
        .find(|o| {
            o.get("value")
                .and_then(|v| v.as_str())
                .is_some_and(|v| v == current.provider)
        })
        .cloned();

    let initial_model = model_options
        .iter()
        .find(|o| {
            o.get("value")
                .and_then(|v| v.as_str())
                .is_some_and(|v| v == current.model)
        })
        .cloned();

    let mut provider_select = serde_json::json!({
        "type": "static_select",
        "action_id": "zeroclaw_config_provider",
        "placeholder": { "type": "plain_text", "text": "Select provider" },
        "options": provider_options
    });
    if let Some(init) = initial_provider {
        provider_select["initial_option"] = init;
    }

    let mut model_select = serde_json::json!({
        "type": "static_select",
        "action_id": "zeroclaw_config_model",
        "placeholder": { "type": "plain_text", "text": "Select model" },
        "options": model_options
    });
    if let Some(init) = initial_model {
        model_select["initial_option"] = init;
    }

    let blocks = serde_json::json!([
        {
            "type": "section",
            "text": {
                "type": "mrkdwn",
                "text": format!(
                    "*Model Configuration*\nCurrent: `{}` / `{}`",
                    current.provider, current.model
                )
            }
        },
        {
            "type": "section",
            "block_id": "config_provider_block",
            "text": { "type": "mrkdwn", "text": "*Provider*" },
            "accessory": provider_select
        },
        {
            "type": "section",
            "block_id": "config_model_block",
            "text": { "type": "mrkdwn", "text": "*Model*" },
            "accessory": model_select
        }
    ]);

    blocks.to_string()
}

async fn handle_runtime_command_if_needed(
    ctx: &ChannelRuntimeContext,
    msg: &traits::ChannelMessage,
    target_channel: Option<&Arc<dyn Channel>>,
) -> bool {
    let Some(command) = parse_runtime_command(&msg.channel, &msg.content) else {
        return false;
    };

    let Some(channel) = target_channel else {
        return true;
    };

    let sender_key = conversation_history_key(msg);
    let mut current = get_route_selection(ctx, &sender_key);

    let response = match command {
        ChannelRuntimeCommand::ShowProviders => build_providers_help_response(&current),
        ChannelRuntimeCommand::SetProvider(raw_provider) => {
            match resolve_provider_alias(&raw_provider) {
                Some(provider_name) => {
                    match get_or_create_provider(ctx, &provider_name, None).await {
                        Ok(_) => {
                            if provider_name != current.provider {
                                current.provider = provider_name.clone();
                                set_route_selection(ctx, &sender_key, current.clone());
                            }

                            format!(
                                "Provider switched to `{provider_name}` for this sender session. Current model is `{}`.\nUse `/model <model-id>` to set a provider-compatible model.",
                                current.model
                            )
                        }
                        Err(err) => {
                            let safe_err = providers::sanitize_api_error(&err.to_string());
                            format!(
                                "Failed to initialize provider `{provider_name}`. Route unchanged.\nDetails: {safe_err}"
                            )
                        }
                    }
                }
                None => format!(
                    "Unknown provider `{raw_provider}`. Use `/models` to list valid providers."
                ),
            }
        }
        ChannelRuntimeCommand::ShowModel => {
            build_models_help_response(&current, ctx.workspace_dir.as_path(), &ctx.model_routes)
        }
        ChannelRuntimeCommand::SetModel(raw_model) => {
            let model = raw_model.trim().trim_matches('`').to_string();
            if model.is_empty() {
                "Model ID cannot be empty. Use `/model <model-id>`.".to_string()
            } else {
                // Resolve provider+model from model_routes (match by model name or hint)
                if let Some(route) = ctx.model_routes.iter().find(|r| {
                    r.model.eq_ignore_ascii_case(&model) || r.hint.eq_ignore_ascii_case(&model)
                }) {
                    current.provider = route.provider.clone();
                    current.model = route.model.clone();
                    current.api_key = route.api_key.clone();
                } else {
                    current.model = model.clone();
                }
                set_route_selection(ctx, &sender_key, current.clone());

                format!(
                    "Model switched to `{}` (provider: `{}`). Context preserved.",
                    current.model, current.provider
                )
            }
        }
        ChannelRuntimeCommand::ShowConfig => {
            if msg.channel == "slack" {
                let blocks_json = build_config_block_kit(
                    &current,
                    ctx.workspace_dir.as_path(),
                    &ctx.model_routes,
                );
                // Use a magic prefix so SlackChannel::send() can detect Block Kit JSON.
                format!("__ZEROCLAW_BLOCK_KIT__{blocks_json}")
            } else {
                build_config_text_response(&current, ctx.workspace_dir.as_path(), &ctx.model_routes)
            }
        }
        ChannelRuntimeCommand::NewSession => {
            clear_sender_history(ctx, &sender_key);
            if let Some(ref store) = ctx.session_store {
                if let Err(e) = store.delete_session(&sender_key) {
                    tracing::warn!("Failed to delete persisted session for {sender_key}: {e}");
                }
            }
            mark_sender_for_new_session(ctx, &sender_key);
            "Conversation history cleared. Starting fresh.".to_string()
        }
    };

    if let Err(err) = channel
        .send(&SendMessage::new(response, &msg.reply_target).in_thread(msg.thread_ts.clone()))
        .await
    {
        tracing::warn!(
            "Failed to send runtime command response on {}: {err}",
            channel.name()
        );
    }

    true
}

async fn build_memory_context(
    mem: &dyn Memory,
    user_msg: &str,
    min_relevance_score: f64,
    session_id: Option<&str>,
) -> String {
    let mut context = String::new();

    if let Ok(entries) = mem.recall(user_msg, 5, session_id, None, None).await {
        let mut included = 0usize;
        let mut used_chars = 0usize;

        for entry in entries.iter().filter(|e| match e.score {
            Some(score) => score >= min_relevance_score,
            None => true, // keep entries without a score (e.g. non-vector backends)
        }) {
            if included >= MEMORY_CONTEXT_MAX_ENTRIES {
                break;
            }

            if should_skip_memory_context_entry(&entry.key, &entry.content) {
                continue;
            }

            let content = if entry.content.chars().count() > MEMORY_CONTEXT_ENTRY_MAX_CHARS {
                truncate_with_ellipsis(&entry.content, MEMORY_CONTEXT_ENTRY_MAX_CHARS)
            } else {
                entry.content.clone()
            };

            let line = format!("- {}: {}\n", entry.key, content);
            let line_chars = line.chars().count();
            if used_chars + line_chars > MEMORY_CONTEXT_MAX_CHARS {
                break;
            }

            if included == 0 {
                context.push_str("[Memory context]\n");
            }

            context.push_str(&line);
            used_chars += line_chars;
            included += 1;
        }

        if included > 0 {
            context.push_str("[/Memory context]\n\n");
        }
    }

    context
}

/// Extract a compact summary of tool interactions from history messages added
/// during `run_tool_call_loop`. Scans assistant messages for `<tool_call>` tags
/// or native tool-call JSON to collect tool names used.
/// Returns an empty string when no tools were invoked.
#[cfg(test)]
fn extract_tool_context_summary(history: &[ChatMessage], start_index: usize) -> String {
    fn push_unique_tool_name(tool_names: &mut Vec<String>, name: &str) {
        let candidate = name.trim();
        if candidate.is_empty() {
            return;
        }
        if !tool_names.iter().any(|existing| existing == candidate) {
            tool_names.push(candidate.to_string());
        }
    }

    fn collect_tool_names_from_tool_call_tags(content: &str, tool_names: &mut Vec<String>) {
        const TAG_PAIRS: [(&str, &str); 4] = [
            ("<tool_call>", "</tool_call>"),
            ("<toolcall>", "</toolcall>"),
            ("<tool-call>", "</tool-call>"),
            ("<invoke>", "</invoke>"),
        ];

        for (open_tag, close_tag) in TAG_PAIRS {
            for segment in content.split(open_tag) {
                if let Some(json_end) = segment.find(close_tag) {
                    let json_str = segment[..json_end].trim();
                    if let Ok(val) = serde_json::from_str::<serde_json::Value>(json_str) {
                        if let Some(name) = val.get("name").and_then(|n| n.as_str()) {
                            push_unique_tool_name(tool_names, name);
                        }
                    }
                }
            }
        }
    }

    fn collect_tool_names_from_native_json(content: &str, tool_names: &mut Vec<String>) {
        if let Ok(val) = serde_json::from_str::<serde_json::Value>(content) {
            if let Some(calls) = val.get("tool_calls").and_then(|c| c.as_array()) {
                for call in calls {
                    let name = call
                        .get("function")
                        .and_then(|f| f.get("name"))
                        .and_then(|n| n.as_str())
                        .or_else(|| call.get("name").and_then(|n| n.as_str()));
                    if let Some(name) = name {
                        push_unique_tool_name(tool_names, name);
                    }
                }
            }
        }
    }

    fn collect_tool_names_from_tool_results(content: &str, tool_names: &mut Vec<String>) {
        let marker = "<tool_result name=\"";
        let mut remaining = content;
        while let Some(start) = remaining.find(marker) {
            let name_start = start + marker.len();
            let after_name_start = &remaining[name_start..];
            if let Some(name_end) = after_name_start.find('"') {
                let name = &after_name_start[..name_end];
                push_unique_tool_name(tool_names, name);
                remaining = &after_name_start[name_end + 1..];
            } else {
                break;
            }
        }
    }

    let mut tool_names: Vec<String> = Vec::new();

    for msg in history.iter().skip(start_index) {
        match msg.role.as_str() {
            "assistant" => {
                collect_tool_names_from_tool_call_tags(&msg.content, &mut tool_names);
                collect_tool_names_from_native_json(&msg.content, &mut tool_names);
            }
            "user" => {
                // Prompt-mode tool calls are always followed by [Tool results] entries
                // containing `<tool_result name="...">` tags with canonical tool names.
                collect_tool_names_from_tool_results(&msg.content, &mut tool_names);
            }
            _ => {}
        }
    }

    if tool_names.is_empty() {
        return String::new();
    }

    format!("[Used tools: {}]", tool_names.join(", "))
}

#[derive(Debug, Clone, PartialEq, Eq)]
enum AssistantChannelOutcome {
    Reply(String),
    NoReply { reason: Option<String> },
}

impl AssistantChannelOutcome {
    fn history_marker(&self) -> String {
        match self {
            Self::Reply(text) => text.clone(),
            Self::NoReply {
                reason: Some(reason),
            } if !reason.trim().is_empty() => {
                format!("[No reply sent: {}]", reason.trim())
            }
            Self::NoReply { .. } => "[No reply sent]".to_string(),
        }
    }
}

async fn classify_channel_reply_intent(
    provider: &dyn Provider,
    system_prompt: &str,
    history: &[ChatMessage],
    model: &str,
    temperature: f64,
) -> anyhow::Result<AssistantChannelOutcome> {
    let mut convo = String::from(
        "Decide whether the assistant should send any visible reply to the latest inbound \
         channel message.\n\nReturn exactly one of:\n- `REPLY`\n- `NO_REPLY: <short reason>`\n\n\
         Rules:\n- Follow the workspace and channel instructions in the system prompt.\n- If the \
         latest message is not clearly addressed to the assistant, prefer `NO_REPLY`.\n- In DMs \
         or direct conversations, prefer `REPLY` unless the instructions explicitly say \
         otherwise.\n- Do not answer the user. Only classify.\n\nConversation:\n",
    );

    for msg in history.iter().filter(|m| m.role != "system") {
        let role = match msg.role.as_str() {
            "assistant" => "assistant",
            _ => "user",
        };
        let _ = writeln!(convo, "[{role}] {}", msg.content);
    }

    let response = provider
        .chat_with_system(Some(system_prompt), &convo, model, temperature)
        .await?;
    let trimmed = response.trim();
    if trimmed.is_empty() {
        return Ok(AssistantChannelOutcome::NoReply { reason: None });
    }
    if trimmed.eq_ignore_ascii_case("REPLY") {
        return Ok(AssistantChannelOutcome::Reply(String::new()));
    }

    if let Some(reason) = trimmed.strip_prefix("NO_REPLY:") {
        let reason = reason.trim();
        return Ok(AssistantChannelOutcome::NoReply {
            reason: (!reason.is_empty()).then(|| reason.to_string()),
        });
    }
    if trimmed.eq_ignore_ascii_case("NO_REPLY") {
        return Ok(AssistantChannelOutcome::NoReply { reason: None });
    }

    Ok(AssistantChannelOutcome::Reply(String::new()))
}

fn sanitize_channel_response(response: &str, tools: &[Box<dyn Tool>]) -> String {
    let known_tool_names: HashSet<String> = tools
        .iter()
        .map(|tool| tool.name().to_ascii_lowercase())
        .collect();
    // Strip any [Used tools: ...] prefix that the LLM may have echoed from
    // history context (#4400). Trim first to handle leading/trailing whitespace.
    let trimmed_response = response.trim();
    let stripped_summary = strip_tool_summary_prefix(trimmed_response);
    // Strip XML-style tool-call tags (e.g. <tool_call>...</tool_call>)
    let stripped_xml = strip_tool_call_tags(&stripped_summary);
    // Strip isolated tool-call JSON artifacts
    let stripped_json = strip_isolated_tool_json_artifacts(&stripped_xml, &known_tool_names);
    // Strip leading narration lines that announce tool usage
    let sanitized = strip_tool_narration(&stripped_json);

    // Scan for credential leaks before returning to caller
    match crate::security::LeakDetector::new().scan(&sanitized) {
        crate::security::LeakResult::Clean => sanitized,
        crate::security::LeakResult::Detected { patterns, redacted } => {
            tracing::warn!(
                patterns = ?patterns,
                "output guardrail: credential leak detected in outbound channel response"
            );
            redacted
        }
    }
}

/// Remove leading lines that narrate tool usage (e.g. "Let me check the weather for you.").
///
/// Only strips lines from the very beginning of the message that match common
/// narration patterns, so genuine content is preserved.
fn strip_tool_narration(message: &str) -> String {
    let narration_prefixes: &[&str] = &[
        "let me ",
        "i'll ",
        "i will ",
        "i am going to ",
        "i'm going to ",
        "searching ",
        "looking up ",
        "fetching ",
        "checking ",
        "using the ",
        "using my ",
        "one moment",
        "hold on",
        "just a moment",
        "give me a moment",
        "allow me to ",
    ];

    let mut result_lines: Vec<&str> = Vec::new();
    let mut past_narration = false;

    for line in message.lines() {
        if past_narration {
            result_lines.push(line);
            continue;
        }
        let trimmed = line.trim();
        if trimmed.is_empty() {
            continue;
        }
        let lower = trimmed.to_lowercase();
        if narration_prefixes.iter().any(|p| lower.starts_with(p)) {
            // Skip this narration line
            continue;
        }
        // First non-narration, non-empty line — keep everything from here
        past_narration = true;
        result_lines.push(line);
    }

    let joined = result_lines.join("\n");
    let trimmed = joined.trim();
    if trimmed.is_empty() && !message.trim().is_empty() {
        // If stripping removed everything, return original to avoid empty reply
        message.to_string()
    } else {
        trimmed.to_string()
    }
}

fn is_tool_call_payload(value: &serde_json::Value, known_tool_names: &HashSet<String>) -> bool {
    let Some(object) = value.as_object() else {
        return false;
    };

    let (name, has_args) =
        if let Some(function) = object.get("function").and_then(|f| f.as_object()) {
            (
                function
                    .get("name")
                    .and_then(|v| v.as_str())
                    .or_else(|| object.get("name").and_then(|v| v.as_str())),
                function.contains_key("arguments")
                    || function.contains_key("parameters")
                    || object.contains_key("arguments")
                    || object.contains_key("parameters"),
            )
        } else {
            (
                object.get("name").and_then(|v| v.as_str()),
                object.contains_key("arguments") || object.contains_key("parameters"),
            )
        };

    let Some(name) = name.map(str::trim).filter(|name| !name.is_empty()) else {
        return false;
    };

    has_args && known_tool_names.contains(&name.to_ascii_lowercase())
}

fn is_tool_result_payload(
    object: &serde_json::Map<String, serde_json::Value>,
    saw_tool_call_payload: bool,
) -> bool {
    if !saw_tool_call_payload || !object.contains_key("result") {
        return false;
    }

    object.keys().all(|key| {
        matches!(
            key.as_str(),
            "result" | "id" | "tool_call_id" | "name" | "tool"
        )
    })
}

fn sanitize_tool_json_value(
    value: &serde_json::Value,
    known_tool_names: &HashSet<String>,
    saw_tool_call_payload: bool,
) -> Option<(String, bool)> {
    if is_tool_call_payload(value, known_tool_names) {
        return Some((String::new(), true));
    }

    if let Some(array) = value.as_array() {
        if !array.is_empty()
            && array
                .iter()
                .all(|item| is_tool_call_payload(item, known_tool_names))
        {
            return Some((String::new(), true));
        }
        return None;
    }

    let object = value.as_object()?;

    if let Some(tool_calls) = object.get("tool_calls").and_then(|value| value.as_array()) {
        if !tool_calls.is_empty()
            && tool_calls
                .iter()
                .all(|call| is_tool_call_payload(call, known_tool_names))
        {
            let content = object
                .get("content")
                .and_then(|value| value.as_str())
                .unwrap_or("")
                .trim()
                .to_string();
            return Some((content, true));
        }
    }

    if is_tool_result_payload(object, saw_tool_call_payload) {
        return Some((String::new(), false));
    }

    None
}

fn is_line_isolated_json_segment(message: &str, start: usize, end: usize) -> bool {
    let line_start = message[..start].rfind('\n').map_or(0, |idx| idx + 1);
    let line_end = message[end..]
        .find('\n')
        .map_or(message.len(), |idx| end + idx);

    message[line_start..start].trim().is_empty() && message[end..line_end].trim().is_empty()
}

fn strip_isolated_tool_json_artifacts(message: &str, known_tool_names: &HashSet<String>) -> String {
    let mut cleaned = String::with_capacity(message.len());
    let mut cursor = 0usize;
    let mut saw_tool_call_payload = false;

    while cursor < message.len() {
        let Some(rel_start) = message[cursor..].find(['{', '[']) else {
            cleaned.push_str(&message[cursor..]);
            break;
        };

        let start = cursor + rel_start;
        cleaned.push_str(&message[cursor..start]);

        let candidate = &message[start..];
        let mut stream =
            serde_json::Deserializer::from_str(candidate).into_iter::<serde_json::Value>();

        if let Some(Ok(value)) = stream.next() {
            let consumed = stream.byte_offset();
            if consumed > 0 {
                let end = start + consumed;
                if is_line_isolated_json_segment(message, start, end) {
                    if let Some((replacement, marks_tool_call)) =
                        sanitize_tool_json_value(&value, known_tool_names, saw_tool_call_payload)
                    {
                        if marks_tool_call {
                            saw_tool_call_payload = true;
                        }
                        if !replacement.trim().is_empty() {
                            cleaned.push_str(replacement.trim());
                        }
                        cursor = end;
                        continue;
                    }
                }
            }
        }

        let Some(ch) = message[start..].chars().next() else {
            break;
        };
        cleaned.push(ch);
        cursor = start + ch.len_utf8();
    }

    let mut result = cleaned.replace("\r\n", "\n");
    while result.contains("\n\n\n") {
        result = result.replace("\n\n\n", "\n\n");
    }
    result.trim().to_string()
}

fn spawn_supervised_listener(
    ch: Arc<dyn Channel>,
    tx: tokio::sync::mpsc::Sender<traits::ChannelMessage>,
    initial_backoff_secs: u64,
    max_backoff_secs: u64,
) -> tokio::task::JoinHandle<()> {
    spawn_supervised_listener_with_health_interval(
        ch,
        tx,
        initial_backoff_secs,
        max_backoff_secs,
        Duration::from_secs(CHANNEL_HEALTH_HEARTBEAT_SECS),
    )
}

fn spawn_supervised_listener_with_health_interval(
    ch: Arc<dyn Channel>,
    tx: tokio::sync::mpsc::Sender<traits::ChannelMessage>,
    initial_backoff_secs: u64,
    max_backoff_secs: u64,
    health_interval: Duration,
) -> tokio::task::JoinHandle<()> {
    let health_interval = if health_interval.is_zero() {
        Duration::from_secs(1)
    } else {
        health_interval
    };

    tokio::spawn(async move {
        let component = format!("channel:{}", ch.name());
        let mut backoff = initial_backoff_secs.max(1);
        let max_backoff = max_backoff_secs.max(backoff);

        loop {
            crate::health::mark_component_ok(&component);
            let mut health = tokio::time::interval(health_interval);
            health.set_missed_tick_behavior(tokio::time::MissedTickBehavior::Skip);
            let result = {
                let listen_future = ch.listen(tx.clone());
                tokio::pin!(listen_future);

                loop {
                    tokio::select! {
                        _ = health.tick() => {
                            crate::health::mark_component_ok(&component);
                        }
                        result = &mut listen_future => break result,
                    }
                }
            };

            if tx.is_closed() {
                break;
            }

            match result {
                Ok(()) => {
                    tracing::warn!("Channel {} exited unexpectedly; restarting", ch.name());
                    crate::health::mark_component_error(&component, "listener exited unexpectedly");
                    // Clean exit — reset backoff since the listener ran successfully
                    backoff = initial_backoff_secs.max(1);
                }
                Err(e) => {
                    tracing::error!("Channel {} error: {e}; restarting", ch.name());
                    crate::health::mark_component_error(&component, e.to_string());
                }
            }

            crate::health::bump_component_restart(&component);
            tokio::time::sleep(Duration::from_secs(backoff)).await;
            // Double backoff AFTER sleeping so first error uses initial_backoff
            backoff = backoff.saturating_mul(2).min(max_backoff);
        }
    })
}

fn compute_max_in_flight_messages(channel_count: usize) -> usize {
    channel_count
        .saturating_mul(CHANNEL_PARALLELISM_PER_CHANNEL)
        .clamp(
            CHANNEL_MIN_IN_FLIGHT_MESSAGES,
            CHANNEL_MAX_IN_FLIGHT_MESSAGES,
        )
}

fn log_worker_join_result(result: Result<(), tokio::task::JoinError>) {
    if let Err(error) = result {
        tracing::error!("Channel message worker crashed: {error}");
    }
}

fn spawn_scoped_typing_task(
    channel: Arc<dyn Channel>,
    recipient: String,
    cancellation_token: CancellationToken,
) -> tokio::task::JoinHandle<()> {
    let stop_signal = cancellation_token;
    let refresh_interval = Duration::from_secs(CHANNEL_TYPING_REFRESH_INTERVAL_SECS);
    tokio::spawn(async move {
        let mut interval = tokio::time::interval(refresh_interval);
        interval.set_missed_tick_behavior(tokio::time::MissedTickBehavior::Skip);

        loop {
            tokio::select! {
                () = stop_signal.cancelled() => break,
                _ = interval.tick() => {
                    if let Err(e) = channel.start_typing(&recipient).await {
                        tracing::debug!("Failed to start typing on {}: {e}", channel.name());
                    }
                }
            }
        }

        if let Err(e) = channel.stop_typing(&recipient).await {
            tracing::debug!("Failed to stop typing on {}: {e}", channel.name());
        }
    })
}

async fn process_channel_message(
    ctx: Arc<ChannelRuntimeContext>,
    msg: traits::ChannelMessage,
    cancellation_token: CancellationToken,
) {
    if cancellation_token.is_cancelled() {
        return;
    }

    println!(
        "  💬 [{}] from {}: {}",
        msg.channel,
        msg.sender,
        truncate_with_ellipsis(&msg.content, 80)
    );
    runtime_trace::record_event(
        "channel_message_inbound",
        Some(msg.channel.as_str()),
        None,
        None,
        None,
        None,
        None,
        serde_json::json!({
            "sender": msg.sender,
            "message_id": msg.id,
            "reply_target": msg.reply_target,
            "content_preview": truncate_with_ellipsis(&msg.content, 160),
        }),
    );

    // ── Hook: on_message_received (modifying) ────────────
    let mut msg = if let Some(hooks) = &ctx.hooks {
        match hooks.run_on_message_received(msg).await {
            crate::hooks::HookResult::Cancel(reason) => {
                tracing::info!(%reason, "incoming message dropped by hook");
                return;
            }
            crate::hooks::HookResult::Continue(modified) => modified,
        }
    } else {
        msg
    };

    // ── Media pipeline: enrich inbound message with media annotations ──
    if ctx.media_pipeline.enabled && !msg.attachments.is_empty() {
        let vision = ctx.provider.supports_vision();
        let pipeline = media_pipeline::MediaPipeline::new(
            &ctx.media_pipeline,
            &ctx.transcription_config,
            vision,
        );
        msg.content = Box::pin(pipeline.process(&msg.content, &msg.attachments)).await;
    }

    // ── Link enricher: prepend URL summaries before agent sees the message ──
    let le_config = &ctx.prompt_config.link_enricher;
    if le_config.enabled {
        let enricher_cfg = link_enricher::LinkEnricherConfig {
            enabled: le_config.enabled,
            max_links: le_config.max_links,
            timeout_secs: le_config.timeout_secs,
        };
        let enriched = link_enricher::enrich_message(&msg.content, &enricher_cfg).await;
        if enriched != msg.content {
            tracing::info!(
                channel = %msg.channel,
                sender = %msg.sender,
                "Link enricher: prepended URL summaries to message"
            );
            msg.content = enriched;
        }
    }

    let target_channel = ctx
        .channels_by_name
        .get(&msg.channel)
        .or_else(|| {
            // Multi-room channels use "name:qualifier" format (e.g. "matrix:!roomId");
            // fall back to base channel name for routing.
            msg.channel
                .split_once(':')
                .and_then(|(base, _)| ctx.channels_by_name.get(base))
        })
        .cloned();
    if let Err(err) = maybe_apply_runtime_config_update(ctx.as_ref()).await {
        tracing::warn!("Failed to apply runtime config update: {err}");
    }
    if handle_runtime_command_if_needed(ctx.as_ref(), &msg, target_channel.as_ref()).await {
        return;
    }

    let history_key = conversation_history_key(&msg);
    let mut route = get_route_selection(ctx.as_ref(), &history_key);

    // ── Query classification: override route when a rule matches ──
    if let Some(hint) = crate::agent::classifier::classify(&ctx.query_classification, &msg.content)
    {
        if let Some(matched_route) = ctx
            .model_routes
            .iter()
            .find(|r| r.hint.eq_ignore_ascii_case(&hint))
        {
            tracing::info!(
                target: "query_classification",
                hint = hint.as_str(),
                provider = matched_route.provider.as_str(),
                model = matched_route.model.as_str(),
                channel = %msg.channel,
                "Channel message classified — overriding route"
            );
            route = ChannelRouteSelection {
                provider: matched_route.provider.clone(),
                model: matched_route.model.clone(),
                api_key: matched_route.api_key.clone(),
            };
        }
    }

    let runtime_defaults = runtime_defaults_snapshot(ctx.as_ref());
    let mut active_provider = match get_or_create_provider(
        ctx.as_ref(),
        &route.provider,
        route.api_key.as_deref(),
    )
    .await
    {
        Ok(provider) => provider,
        Err(err) => {
            let safe_err = providers::sanitize_api_error(&err.to_string());
            let message = format!(
                "⚠️ Failed to initialize provider `{}`. Please run `/models` to choose another provider.\nDetails: {safe_err}",
                route.provider
            );
            if let Some(channel) = target_channel.as_ref() {
                let _ = channel
                    .send(
                        &SendMessage::new(message, &msg.reply_target)
                            .in_thread(msg.thread_ts.clone()),
                    )
                    .await;
            }
            return;
        }
    };
    if ctx.auto_save_memory
        && msg.content.chars().count() >= AUTOSAVE_MIN_MESSAGE_CHARS
        && !memory::should_skip_autosave_content(&msg.content)
    {
        let autosave_key = conversation_memory_key(&msg);
        let _ = ctx
            .memory
            .store(
                &autosave_key,
                &msg.content,
                crate::memory::MemoryCategory::Conversation,
                Some(&history_key),
            )
            .await;
    }

    println!("  ⏳ Processing message...");
    let started_at = Instant::now();

    let force_fresh_session = take_pending_new_session(ctx.as_ref(), &history_key);
    if force_fresh_session {
        // `/new` should make the next user turn completely fresh even if
        // older cached turns reappear before this message starts.
        clear_sender_history(ctx.as_ref(), &history_key);
    }

    let had_prior_history = if force_fresh_session {
        false
    } else {
        ctx.conversation_histories
            .lock()
            .unwrap_or_else(|e| e.into_inner())
            .peek(&history_key)
            .is_some_and(|turns| !turns.is_empty())
    };

    // Preserve user turn before the LLM call so interrupted requests keep context.
    append_sender_turn(ctx.as_ref(), &history_key, ChatMessage::user(&msg.content));

    // Build history from per-sender conversation cache.
    let prior_turns_raw = if force_fresh_session {
        vec![ChatMessage::user(&msg.content)]
    } else {
        ctx.conversation_histories
            .lock()
            .unwrap_or_else(|e| e.into_inner())
            .get(&history_key)
            .cloned()
            .unwrap_or_default()
    };
    let mut prior_turns = normalize_cached_channel_turns(prior_turns_raw);

    // Strip stale tool_result blocks from cached turns so the LLM never
    // sees a `<tool_result>` without a preceding `<tool_call>`, which
    // causes hallucinated output on subsequent heartbeat ticks or sessions.
    for turn in &mut prior_turns {
        if turn.content.contains("<tool_result") {
            turn.content = strip_tool_result_content(&turn.content);
        }
    }

    // Strip [Used tools: ...] prefixes from cached assistant turns so the
    // LLM never sees (and reproduces) this internal summary format (#4400).
    for turn in &mut prior_turns {
        if turn.role == "assistant" && turn.content.starts_with("[Used tools:") {
            turn.content = strip_tool_summary_prefix(&turn.content);
        }
    }

    // Strip [IMAGE:] markers from *older* history messages when the active
    // provider does not support vision. This prevents "history poisoning"
    // where a previously-sent image marker gets reloaded from the JSONL
    // session file and permanently breaks the conversation (fixes #3674).
    // We skip the last turn (the current message) so the vision check can
    // still reject fresh image sends with a proper error.
    if !active_provider.supports_vision() && prior_turns.len() > 1 {
        let last_idx = prior_turns.len() - 1;
        for turn in &mut prior_turns[..last_idx] {
            if turn.content.contains("[IMAGE:") {
                let (cleaned, _refs) = crate::multimodal::parse_image_markers(&turn.content);
                turn.content = cleaned;
            }
        }
        // Drop older turns that became empty after marker removal (e.g. image-only messages).
        // Keep the last turn (current message) intact.
        let current = prior_turns.pop();
        prior_turns.retain(|turn| !turn.content.trim().is_empty());
        if let Some(current) = current {
            prior_turns.push(current);
        }
    }

    // Proactively trim conversation history before sending to the provider
    // to prevent context-window-exceeded errors (bug #3460).
    let dropped = proactive_trim_turns(&mut prior_turns, PROACTIVE_CONTEXT_BUDGET_CHARS);
    if dropped > 0 {
        tracing::info!(
            channel = %msg.channel,
            sender = %msg.sender,
            dropped_turns = dropped,
            remaining_turns = prior_turns.len(),
            "Proactively trimmed conversation history to fit context budget"
        );
    }

    // ── Dual-scope memory recall ──────────────────────────────────
    // Always recall before each LLM call (not just first turn).
    // For group chats: merge sender-scope + group-scope memories.
    // For DMs: sender-scope only.
    let is_group_chat =
        msg.reply_target.contains("@g.us") || msg.reply_target.starts_with("group:");

    let mem_recall_start = Instant::now();
    let sender_memory_fut = build_memory_context(
        ctx.memory.as_ref(),
        &msg.content,
        ctx.min_relevance_score,
        Some(&msg.sender),
    );

    let (sender_memory, group_memory) = if is_group_chat {
        let group_memory_fut = build_memory_context(
            ctx.memory.as_ref(),
            &msg.content,
            ctx.min_relevance_score,
            Some(&history_key),
        );
        tokio::join!(sender_memory_fut, group_memory_fut)
    } else {
        (sender_memory_fut.await, String::new())
    };
    #[allow(clippy::cast_possible_truncation)]
    let mem_recall_ms = mem_recall_start.elapsed().as_millis() as u64;
    tracing::info!(
        mem_recall_ms,
        sender_empty = sender_memory.is_empty(),
        group_empty = group_memory.is_empty(),
        "⏱ Memory recall completed"
    );

    // Merge sender + group memories, avoiding duplicates
    let memory_context = if group_memory.is_empty() {
        sender_memory
    } else if sender_memory.is_empty() {
        group_memory
    } else {
        format!("{sender_memory}\n{group_memory}")
    };

    // Use refreshed system prompt for new sessions (master's /new support),
    // and inject memory into system prompt (not user message) so it
    // doesn't pollute session history and is re-fetched each turn.
    let base_system_prompt = if had_prior_history {
        ctx.system_prompt.as_str().to_string()
    } else {
        refreshed_new_session_system_prompt(ctx.as_ref())
    };
    let mut system_prompt =
        build_channel_system_prompt(&base_system_prompt, &msg.channel, &msg.reply_target);
    if !memory_context.is_empty() {
        let _ = write!(system_prompt, "\n\n{memory_context}");
    }
    let mut history = vec![ChatMessage::system(system_prompt)];
    history.extend(prior_turns);

    // ── Proactive context compression ────────────────────────────
    // Use the existing ContextCompressor to summarize older history
    // before the LLM call, preventing context-window-exceeded errors
    // and preserving key decisions through LLM-driven summarization.
    {
        let cc_config = ctx.prompt_config.agent.context_compression.clone();
        let compressor = crate::agent::context_compressor::ContextCompressor::new(
            cc_config,
            ctx.context_token_budget,
        )
        .with_memory(Arc::clone(&ctx.memory));
        match compressor
            .compress_if_needed(&mut history, active_provider.as_ref(), route.model.as_str())
            .await
        {
            Ok(result) if result.compressed => {
                tracing::info!(
                    channel = %msg.channel,
                    sender = %msg.sender,
                    tokens_before = result.tokens_before,
                    tokens_after = result.tokens_after,
                    passes = result.passes_used,
                    "Proactive context compression applied before LLM call"
                );
            }
            Err(e) => {
                tracing::warn!("Context compression failed, proceeding without: {e}");
            }
            _ => {}
        }
    }

    // ── Reply-intent precheck ────────────────────────────────────────
    let reply_intent = classify_channel_reply_intent(
        active_provider.as_ref(),
        history[0].content.as_str(),
        &history,
        route.model.as_str(),
        runtime_defaults.temperature,
    )
    .await
    .unwrap_or(AssistantChannelOutcome::Reply(String::new()));

    if let AssistantChannelOutcome::NoReply { reason } = reply_intent {
        let history_response = AssistantChannelOutcome::NoReply {
            reason: reason.clone(),
        }
        .history_marker();
        append_sender_turn(
            ctx.as_ref(),
            &history_key,
            ChatMessage::assistant(&history_response),
        );
        runtime_trace::record_event(
            "channel_message_no_reply",
            Some(msg.channel.as_str()),
            Some(route.provider.as_str()),
            Some(route.model.as_str()),
            None,
            Some(true),
            reason.as_deref(),
            serde_json::json!({
                "sender": msg.sender,
                "elapsed_ms": started_at.elapsed().as_millis(),
                "phase": "precheck",
            }),
        );
        println!(
            "  🤖 No reply ({}ms): {}",
            started_at.elapsed().as_millis(),
            reason.as_deref().unwrap_or("no reason provided")
        );
        return;
    }

    let use_draft_streaming = target_channel
        .as_ref()
        .is_some_and(|ch| ch.supports_draft_updates());

    tracing::debug!(
        channel = %msg.channel,
        has_target_channel = target_channel.is_some(),
        use_draft_streaming,
        "Streaming decision"
    );

    // Partial mode: delta channel for draft updates (progress + text).
    let (delta_tx, delta_rx) = if use_draft_streaming {
        let (tx, rx) = tokio::sync::mpsc::channel::<crate::agent::loop_::DraftEvent>(64);
        (Some(tx), Some(rx))
    } else {
        (None, None)
    };

    // Partial mode: send an initial draft message for progressive editing.
    let draft_message_id = if use_draft_streaming {
        if let Some(channel) = target_channel.as_ref() {
            match channel
                .send_draft(
                    &SendMessage::new("...", &msg.reply_target).in_thread(msg.thread_ts.clone()),
                )
                .await
            {
                Ok(id) => id,
                Err(e) => {
                    tracing::debug!("Failed to send draft on {}: {e}", channel.name());
                    None
                }
            }
        } else {
            None
        }
    } else {
        None
    };

    // Spawn the appropriate handler for the delta channel.
    let draft_updater = if use_draft_streaming {
        // Partial: accumulate text and edit a single draft message.
        if let (Some(mut rx), Some(draft_id_ref), Some(channel_ref)) = (
            delta_rx,
            draft_message_id.as_deref(),
            target_channel.as_ref(),
        ) {
            let channel = Arc::clone(channel_ref);
            let reply_target = msg.reply_target.clone();
            let draft_id = draft_id_ref.to_string();
            Some(tokio::spawn(async move {
                use crate::agent::loop_::DraftEvent;
                let mut accumulated = String::new();
                while let Some(event) = rx.recv().await {
                    match event {
                        DraftEvent::Clear => {
                            accumulated.clear();
                        }
                        DraftEvent::Progress(text) => {
                            if let Err(e) = channel
                                .update_draft_progress(&reply_target, &draft_id, &text)
                                .await
                            {
                                tracing::debug!("Draft progress update failed: {e}");
                            }
                        }
                        DraftEvent::Content(text) => {
                            accumulated.push_str(&text);
                            if let Err(e) = channel
                                .update_draft(&reply_target, &draft_id, &accumulated)
                                .await
                            {
                                tracing::debug!("Draft update failed: {e}");
                            }
                        }
                    }
                }
            }))
        } else {
            None
        }
    } else {
        None
    };

    // React with 👀 to acknowledge the incoming message
    if ctx.ack_reactions {
        if let Some(channel) = target_channel.as_ref() {
            if let Err(e) = channel
                .add_reaction(&msg.reply_target, &msg.id, "\u{1F440}")
                .await
            {
                tracing::debug!("Failed to add reaction: {e}");
            }
        }
    }

    // Skip typing only for Partial mode — the draft message itself provides
    // visual feedback. MultiMessage and Off both keep typing active.
    let is_partial_draft = target_channel
        .as_ref()
        .is_some_and(|ch| ch.supports_draft_updates() && !ch.supports_multi_message_streaming());
    let typing_cancellation = if is_partial_draft {
        None
    } else {
        target_channel.as_ref().map(|_| CancellationToken::new())
    };
    let typing_task = match (target_channel.as_ref(), typing_cancellation.as_ref()) {
        (Some(channel), Some(token)) => Some(spawn_scoped_typing_task(
            Arc::clone(channel),
            msg.reply_target.clone(),
            token.clone(),
        )),
        _ => None,
    };

    // Wrap observer to forward tool events as live thread messages
    let (notify_tx, mut notify_rx) = tokio::sync::mpsc::unbounded_channel::<String>();
    let notify_observer: Arc<ChannelNotifyObserver> = Arc::new(ChannelNotifyObserver {
        inner: Arc::clone(&ctx.observer),
        tx: notify_tx,
        tools_used: AtomicBool::new(false),
    });
    let notify_observer_flag = Arc::clone(&notify_observer);
    let notify_channel = target_channel.clone();
    let notify_reply_target = msg.reply_target.clone();
    let notify_thread_root = followup_thread_id(&msg);
    let notify_task = if msg.channel == "cli" || !ctx.show_tool_calls {
        Some(tokio::spawn(async move {
            while notify_rx.recv().await.is_some() {}
        }))
    } else {
        Some(tokio::spawn(async move {
            let thread_ts = notify_thread_root;
            while let Some(text) = notify_rx.recv().await {
                if let Some(ref ch) = notify_channel {
                    let _ = ch
                        .send(
                            &SendMessage::new(&text, &notify_reply_target)
                                .in_thread(thread_ts.clone()),
                        )
                        .await;
                }
            }
        }))
    };

    enum LlmExecutionResult {
        Completed(Result<Result<String, anyhow::Error>, tokio::time::error::Elapsed>),
        Cancelled,
    }

    let model_switch_callback = get_model_switch_state();
    let scale_cap = ctx
        .pacing
        .message_timeout_scale_max
        .unwrap_or(CHANNEL_MESSAGE_TIMEOUT_SCALE_CAP);
    let timeout_budget_secs = channel_message_timeout_budget_secs_with_cap(
        ctx.message_timeout_secs,
        ctx.max_tool_iterations,
        scale_cap,
    );
    let cost_tracking_context = ctx.cost_tracking.clone().map(|state| {
        crate::agent::loop_::ToolLoopCostTrackingContext::new(state.tracker, state.prices)
    });
    let llm_call_start = Instant::now();
    #[allow(clippy::cast_possible_truncation)]
    let elapsed_before_llm_ms = started_at.elapsed().as_millis() as u64;
    tracing::info!(elapsed_before_llm_ms, "⏱ Starting LLM call");
    let (llm_result, fallback_info) = scope_provider_fallback(async {
        let llm_result = loop {
            let loop_result = tokio::select! {
                () = cancellation_token.cancelled() => LlmExecutionResult::Cancelled,
                result = tokio::time::timeout(
                    Duration::from_secs(timeout_budget_secs),
                    scope_thread_id(
                        msg.interruption_scope_id.clone()
                            .or_else(|| msg.thread_ts.clone())
                            .or_else(|| Some(msg.id.clone())),
                        crate::agent::loop_::TOOL_LOOP_COST_TRACKING_CONTEXT.scope(
                            cost_tracking_context.clone(),
                        run_tool_call_loop(
                        active_provider.as_ref(),
                        &mut history,
                        ctx.tools_registry.as_ref(),
                        notify_observer.as_ref() as &dyn Observer,
                        route.provider.as_str(),
                        route.model.as_str(),
                        runtime_defaults.temperature,
                        true,
                        Some(&*ctx.approval_manager),
                        msg.channel.as_str(),
                        Some(msg.reply_target.as_str()),
                        &ctx.multimodal,
                        ctx.max_tool_iterations,
                        Some(cancellation_token.clone()),
                        delta_tx.clone(),
                        ctx.hooks.as_deref(),
                        if msg.channel == "cli"
                            || ctx.autonomy_level == AutonomyLevel::Full
                        {
                            &[]
                        } else {
                            ctx.non_cli_excluded_tools.as_ref()
                        },
                        ctx.tool_call_dedup_exempt.as_ref(),
                        ctx.activated_tools.as_ref(),
                        Some(model_switch_callback.clone()),
                        &ctx.pacing,
                        ctx.max_tool_result_chars,
                        ctx.context_token_budget,
                        None, // shared_budget
                    ),
                    ),
                    ),
                ) => LlmExecutionResult::Completed(result),
            };

            // Handle model switch: re-create the provider and retry
            if let LlmExecutionResult::Completed(Ok(Err(ref e))) = loop_result {
                if let Some((new_provider, new_model)) = is_model_switch_requested(e) {
                    tracing::info!(
                        "Model switch requested, switching from {} {} to {} {}",
                        route.provider,
                        route.model,
                        new_provider,
                        new_model
                    );

                    match create_resilient_provider_nonblocking(
                        &new_provider,
                        ctx.api_key.clone(),
                        ctx.api_url.clone(),
                        ctx.reliability.as_ref().clone(),
                        ctx.provider_runtime_options.clone(),
                    )
                    .await
                    {
                        Ok(new_prov) => {
                            active_provider = Arc::from(new_prov);
                            route.provider = new_provider;
                            route.model = new_model;
                            clear_model_switch_request();

                            ctx.observer.record_event(&ObserverEvent::AgentStart {
                                provider: route.provider.clone(),
                                model: route.model.clone(),
                            });

                            continue;
                        }
                        Err(err) => {
                            tracing::error!("Failed to create provider after model switch: {err}");
                            clear_model_switch_request();
                            // Fall through with the original error
                        }
                    }
                }
            }

            break loop_result;
        };
        let fb = take_last_provider_fallback();
        (llm_result, fb)
    })
    .await;

    // Drop all senders so updater tasks can exit (rx.recv() returns None).
    tracing::debug!("Post-loop: dropping delta_tx and awaiting draft updater");
    drop(delta_tx);
    if let Some(handle) = draft_updater {
        let _ = handle.await;
    }
    tracing::debug!("Post-loop: draft updater completed");

    // Thread the final reply only if tools were used (multi-message response)
    if notify_observer_flag.tools_used.load(Ordering::Relaxed) && msg.channel != "cli" {
        msg.thread_ts = followup_thread_id(&msg);
    }
    // Drop the notify sender so the forwarder task finishes
    drop(notify_observer);
    drop(notify_observer_flag);
    if let Some(handle) = notify_task {
        let _ = handle.await;
    }

    #[allow(clippy::cast_possible_truncation)]
    let llm_call_ms = llm_call_start.elapsed().as_millis() as u64;
    #[allow(clippy::cast_possible_truncation)]
    let total_ms = started_at.elapsed().as_millis() as u64;
    tracing::info!(llm_call_ms, total_ms, "⏱ LLM call completed");

    if let Some(token) = typing_cancellation.as_ref() {
        token.cancel();
    }
    if let Some(handle) = typing_task {
        log_worker_join_result(handle.await);
    }

    let reaction_done_emoji = match &llm_result {
        LlmExecutionResult::Completed(Ok(Ok(_))) => "\u{2705}", // ✅
        _ => "\u{26A0}\u{FE0F}",                                // ⚠️
    };

    match llm_result {
        LlmExecutionResult::Cancelled => {
            tracing::info!(
                channel = %msg.channel,
                sender = %msg.sender,
                "Cancelled in-flight channel request due to newer message"
            );
            runtime_trace::record_event(
                "channel_message_cancelled",
                Some(msg.channel.as_str()),
                Some(route.provider.as_str()),
                Some(route.model.as_str()),
                None,
                Some(false),
                Some("cancelled due to newer inbound message"),
                serde_json::json!({
                    "sender": msg.sender,
                    "elapsed_ms": started_at.elapsed().as_millis(),
                }),
            );
            if let (Some(channel), Some(draft_id)) =
                (target_channel.as_ref(), draft_message_id.as_deref())
            {
                if let Err(err) = channel.cancel_draft(&msg.reply_target, draft_id).await {
                    tracing::debug!("Failed to cancel draft on {}: {err}", channel.name());
                }
            }
        }
        LlmExecutionResult::Completed(Ok(Ok(response))) => {
            // ── Hook: on_message_sending (modifying) ─────────
            let mut outbound_response = response;
            if let Some(hooks) = &ctx.hooks {
                match hooks
                    .run_on_message_sending(
                        msg.channel.clone(),
                        msg.reply_target.clone(),
                        outbound_response.clone(),
                    )
                    .await
                {
                    crate::hooks::HookResult::Cancel(reason) => {
                        tracing::info!(%reason, "outgoing message suppressed by hook");
                        if let (Some(channel), Some(draft_id)) =
                            (target_channel.as_ref(), draft_message_id.as_deref())
                        {
                            let _ = channel.cancel_draft(&msg.reply_target, draft_id).await;
                        }
                        return;
                    }
                    crate::hooks::HookResult::Continue((
                        hook_channel,
                        hook_recipient,
                        mut modified_content,
                    )) => {
                        if hook_channel != msg.channel || hook_recipient != msg.reply_target {
                            tracing::warn!(
                                from_channel = %msg.channel,
                                from_recipient = %msg.reply_target,
                                to_channel = %hook_channel,
                                to_recipient = %hook_recipient,
                                "on_message_sending attempted to rewrite channel routing; only content mutation is applied"
                            );
                        }

                        let modified_len = modified_content.chars().count();
                        if modified_len > CHANNEL_HOOK_MAX_OUTBOUND_CHARS {
                            tracing::warn!(
                                limit = CHANNEL_HOOK_MAX_OUTBOUND_CHARS,
                                attempted = modified_len,
                                "hook-modified outbound content exceeded limit; truncating"
                            );
                            modified_content = truncate_with_ellipsis(
                                &modified_content,
                                CHANNEL_HOOK_MAX_OUTBOUND_CHARS,
                            );
                        }

                        if modified_content != outbound_response {
                            tracing::info!(
                                channel = %msg.channel,
                                sender = %msg.sender,
                                before_len = outbound_response.chars().count(),
                                after_len = modified_content.chars().count(),
                                "outgoing message content modified by hook"
                            );
                        }

                        outbound_response = modified_content;
                    }
                }
            }

            let sanitized_response =
                sanitize_channel_response(&outbound_response, ctx.tools_registry.as_ref());
            let mut delivered_response = if sanitized_response.is_empty()
                && !outbound_response.trim().is_empty()
            {
                "I encountered malformed tool-call output and could not produce a safe reply. Please try again.".to_string()
            } else {
                sanitized_response
            };

            // Append a footer when the response was served by a different provider family.
            // Intra-family fallbacks (e.g. minimax → minimax-cn) are suppressed.
            if let Some(fb) = fallback_info.as_ref() {
                let req_base = fb.requested_provider.split(':').next().unwrap_or("");
                let act_base = fb.actual_provider.split(':').next().unwrap_or("");
                let same_family = req_base == act_base
                    || req_base.starts_with(act_base)
                    || act_base.starts_with(req_base);
                if !same_family {
                    use std::fmt::Write as _;
                    write!(
                        delivered_response,
                        "\n\n---\n\u{26A1} `{}` unavailable \u{2014} response from **{}** (`{}`)\nSwitch model: /models",
                        fb.requested_provider, fb.actual_provider, fb.actual_model,
                    )
                    .ok();
                }
            }

            runtime_trace::record_event(
                "channel_message_outbound",
                Some(msg.channel.as_str()),
                Some(route.provider.as_str()),
                Some(route.model.as_str()),
                None,
                Some(true),
                None,
                serde_json::json!({
                    "sender": msg.sender,
                    "elapsed_ms": started_at.elapsed().as_millis(),
                    "response": scrub_credentials(&delivered_response),
                }),
            );

            // Persist intermediate tool-call/result messages from this turn
            // so the model retains concrete "I used tools" examples in
            // context, preventing drift toward tool-less responses (#4827).
            let keep_tool_turns = ctx.prompt_config.agent.keep_tool_context_turns;
            if keep_tool_turns > 0 {
                // Find tool messages for the current turn: everything after
                // the last user message up to (but not including) the final
                // assistant response that matches our delivered text.
                let tool_messages: Vec<ChatMessage> = extract_current_turn_tool_messages(&history);
                for tool_msg in tool_messages {
                    append_sender_turn(ctx.as_ref(), &history_key, tool_msg);
                }
            }

            let history_response = delivered_response.clone();
            append_sender_turn(
                ctx.as_ref(),
                &history_key,
                ChatMessage::assistant(&history_response),
            );

            // Strip tool-call messages from turns older than
            // keep_tool_context_turns to prevent unbounded growth.
            if keep_tool_turns > 0 {
                strip_old_tool_context(ctx.as_ref(), &history_key, keep_tool_turns);
            }

            // Fire-and-forget LLM-driven memory consolidation.
            if ctx.auto_save_memory && msg.content.chars().count() >= AUTOSAVE_MIN_MESSAGE_CHARS {
                let provider = Arc::clone(&ctx.provider);
                let model = ctx.model.to_string();
                let memory = Arc::clone(&ctx.memory);
                let user_msg = msg.content.clone();
                let assistant_resp = delivered_response.clone();
                tokio::spawn(async move {
                    if let Err(e) = crate::memory::consolidation::consolidate_turn(
                        provider.as_ref(),
                        &model,
                        memory.as_ref(),
                        &user_msg,
                        &assistant_resp,
                    )
                    .await
                    {
                        tracing::debug!("Memory consolidation skipped: {e}");
                    }
                });
            }

            println!(
                "  🤖 Reply ({}ms): {}",
                started_at.elapsed().as_millis(),
                truncate_with_ellipsis(&delivered_response, 80)
            );
            if let Some(channel) = target_channel.as_ref() {
                if let Some(ref draft_id) = draft_message_id {
                    if let Err(e) = channel
                        .finalize_draft(&msg.reply_target, draft_id, &delivered_response)
                        .await
                    {
                        tracing::warn!("Failed to finalize draft: {e}; sending as new message");
                        let _ = channel
                            .send(
                                &SendMessage::new(&delivered_response, &msg.reply_target)
                                    .in_thread(msg.thread_ts.clone()),
                            )
                            .await;
                    }
                } else if let Err(e) = channel
                    .send(
                        &SendMessage::new(&delivered_response, &msg.reply_target)
                            .in_thread(msg.thread_ts.clone())
                            .with_cancellation(cancellation_token.clone()),
                    )
                    .await
                {
                    eprintln!("  ❌ Failed to reply on {}: {e}", channel.name());
                }
            }
        }
        LlmExecutionResult::Completed(Ok(Err(e))) => {
            if crate::agent::loop_::is_tool_loop_cancelled(&e) || cancellation_token.is_cancelled()
            {
                tracing::info!(
                    channel = %msg.channel,
                    sender = %msg.sender,
                    "Cancelled in-flight channel request due to newer message"
                );
                runtime_trace::record_event(
                    "channel_message_cancelled",
                    Some(msg.channel.as_str()),
                    Some(route.provider.as_str()),
                    Some(route.model.as_str()),
                    None,
                    Some(false),
                    Some("cancelled during tool-call loop"),
                    serde_json::json!({
                        "sender": msg.sender,
                        "elapsed_ms": started_at.elapsed().as_millis(),
                    }),
                );
                if let (Some(channel), Some(draft_id)) =
                    (target_channel.as_ref(), draft_message_id.as_deref())
                {
                    if let Err(err) = channel.cancel_draft(&msg.reply_target, draft_id).await {
                        tracing::debug!("Failed to cancel draft on {}: {err}", channel.name());
                    }
                }
            } else if is_context_window_overflow_error(&e) {
                let compacted = compact_sender_history(ctx.as_ref(), &history_key);
                let error_text = if compacted {
                    "⚠️ Context window exceeded for this conversation. I compacted recent history and kept the latest context. Please resend your last message."
                } else {
                    "⚠️ Context window exceeded for this conversation. Please resend your last message."
                };
                eprintln!(
                    "  ⚠️ Context window exceeded after {}ms; sender history compacted={}",
                    started_at.elapsed().as_millis(),
                    compacted
                );
                runtime_trace::record_event(
                    "channel_message_error",
                    Some(msg.channel.as_str()),
                    Some(route.provider.as_str()),
                    Some(route.model.as_str()),
                    None,
                    Some(false),
                    Some("context window exceeded"),
                    serde_json::json!({
                        "sender": msg.sender,
                        "elapsed_ms": started_at.elapsed().as_millis(),
                        "history_compacted": compacted,
                    }),
                );
                if let Some(channel) = target_channel.as_ref() {
                    if let Some(ref draft_id) = draft_message_id {
                        let _ = channel
                            .finalize_draft(&msg.reply_target, draft_id, error_text)
                            .await;
                    } else {
                        let _ = channel
                            .send(
                                &SendMessage::new(error_text, &msg.reply_target)
                                    .in_thread(msg.thread_ts.clone()),
                            )
                            .await;
                    }
                }
            } else {
                eprintln!(
                    "  ❌ LLM error after {}ms: {e}",
                    started_at.elapsed().as_millis()
                );
                let safe_error = providers::sanitize_api_error(&e.to_string());
                runtime_trace::record_event(
                    "channel_message_error",
                    Some(msg.channel.as_str()),
                    Some(route.provider.as_str()),
                    Some(route.model.as_str()),
                    None,
                    Some(false),
                    Some(&safe_error),
                    serde_json::json!({
                        "sender": msg.sender,
                        "elapsed_ms": started_at.elapsed().as_millis(),
                    }),
                );
                let should_rollback_user_turn = should_rollback_failed_user_turn(&e);
                let rolled_back = should_rollback_user_turn
                    && rollback_orphan_user_turn(ctx.as_ref(), &history_key, &msg.content);

                if !rolled_back {
                    // Close the orphan user turn so subsequent messages don't
                    // inherit this failed request as unfinished context.
                    append_sender_turn(
                        ctx.as_ref(),
                        &history_key,
                        ChatMessage::assistant("[Task failed — not continuing this request]"),
                    );
                }
                if let Some(channel) = target_channel.as_ref() {
                    if let Some(ref draft_id) = draft_message_id {
                        let _ = channel
                            .finalize_draft(&msg.reply_target, draft_id, &format!("⚠️ Error: {e}"))
                            .await;
                    } else {
                        let _ = channel
                            .send(
                                &SendMessage::new(format!("⚠️ Error: {e}"), &msg.reply_target)
                                    .in_thread(msg.thread_ts.clone()),
                            )
                            .await;
                    }
                }
            }
        }
        LlmExecutionResult::Completed(Err(_)) => {
            let timeout_msg = format!(
                "LLM response timed out after {}s (base={}s, max_tool_iterations={})",
                timeout_budget_secs, ctx.message_timeout_secs, ctx.max_tool_iterations
            );
            runtime_trace::record_event(
                "channel_message_timeout",
                Some(msg.channel.as_str()),
                Some(route.provider.as_str()),
                Some(route.model.as_str()),
                None,
                Some(false),
                Some(&timeout_msg),
                serde_json::json!({
                    "sender": msg.sender,
                    "elapsed_ms": started_at.elapsed().as_millis(),
                }),
            );
            eprintln!(
                "  ❌ {} (elapsed: {}ms)",
                timeout_msg,
                started_at.elapsed().as_millis()
            );
            // Close the orphan user turn so subsequent messages don't
            // inherit this timed-out request as unfinished context.
            append_sender_turn(
                ctx.as_ref(),
                &history_key,
                ChatMessage::assistant("[Task timed out — not continuing this request]"),
            );
            if let Some(channel) = target_channel.as_ref() {
                let error_text =
                    "⚠️ Request timed out while waiting for the model. Please try again.";
                if let Some(ref draft_id) = draft_message_id {
                    let _ = channel
                        .finalize_draft(&msg.reply_target, draft_id, error_text)
                        .await;
                } else {
                    let _ = channel
                        .send(
                            &SendMessage::new(error_text, &msg.reply_target)
                                .in_thread(msg.thread_ts.clone()),
                        )
                        .await;
                }
            }
        }
    }

    // Swap 👀 → ✅ (or ⚠️ on error) to signal processing is complete
    if ctx.ack_reactions {
        if let Some(channel) = target_channel.as_ref() {
            let _ = channel
                .remove_reaction(&msg.reply_target, &msg.id, "\u{1F440}")
                .await;
            let _ = channel
                .add_reaction(&msg.reply_target, &msg.id, reaction_done_emoji)
                .await;
        }
    }
}

/// Shared worker body extracted so both the normal path and the debounce path
/// can reuse the same in-flight tracking / cancellation / process logic.
async fn dispatch_worker(
    ctx: Arc<ChannelRuntimeContext>,
    msg: traits::ChannelMessage,
    in_flight: Arc<tokio::sync::Mutex<HashMap<String, InFlightSenderTaskState>>>,
    task_sequence: Arc<AtomicU64>,
    permit: tokio::sync::OwnedSemaphorePermit,
) {
    let _permit = permit;
    let interrupt_enabled = ctx
        .interrupt_on_new_message
        .enabled_for_channel(msg.channel.as_str());
    let sender_scope_key = interruption_scope_key(&msg);
    let cancellation_token = CancellationToken::new();
    let completion = Arc::new(InFlightTaskCompletion::new());
    let task_id = task_sequence.fetch_add(1, Ordering::Relaxed) as u64;

    let register_in_flight = msg.channel != "cli";

    if register_in_flight {
        let previous = {
            let mut active = in_flight.lock().await;
            active.insert(
                sender_scope_key.clone(),
                InFlightSenderTaskState {
                    task_id,
                    cancellation: cancellation_token.clone(),
                    completion: Arc::clone(&completion),
                },
            )
        };

        if interrupt_enabled {
            if let Some(previous) = previous {
                tracing::info!(
                    channel = %msg.channel,
                    sender = %msg.sender,
                    "Interrupting previous in-flight request for sender"
                );
                previous.cancellation.cancel();
                previous.completion.wait().await;
            }
        }
    }

    process_channel_message(ctx, msg, cancellation_token).await;

    if register_in_flight {
        let mut active = in_flight.lock().await;
        if active
            .get(&sender_scope_key)
            .is_some_and(|state| state.task_id == task_id)
        {
            active.remove(&sender_scope_key);
        }
    }

    completion.mark_done();
}

async fn run_message_dispatch_loop(
    mut rx: tokio::sync::mpsc::Receiver<traits::ChannelMessage>,
    ctx: Arc<ChannelRuntimeContext>,
    max_in_flight_messages: usize,
) {
    let semaphore = Arc::new(tokio::sync::Semaphore::new(max_in_flight_messages));
    let mut workers = tokio::task::JoinSet::new();
    let in_flight_by_sender = Arc::new(tokio::sync::Mutex::new(HashMap::<
        String,
        InFlightSenderTaskState,
    >::new()));
    let task_sequence = Arc::new(AtomicU64::new(1));

    while let Some(msg) = rx.recv().await {
        // Fast path: /stop cancels the in-flight task for this sender scope without
        // spawning a worker or registering a new task. Handled here — before semaphore
        // acquisition — so the target task is still in the store and is never replaced.
        if msg.channel != "cli" && is_stop_command(&msg.content) {
            let scope_key = interruption_scope_key(&msg);
            let previous = {
                let mut active = in_flight_by_sender.lock().await;
                active.remove(&scope_key)
            };
            let reply = if let Some(state) = previous {
                state.cancellation.cancel();
                "Stop signal sent.".to_string()
            } else {
                "No in-flight task for this sender scope.".to_string()
            };
            let channel = ctx
                .channels_by_name
                .get(&msg.channel)
                .or_else(|| {
                    // Multi-room channels use "name:qualifier" format (e.g. "matrix:!roomId");
                    // fall back to base channel name for routing.
                    msg.channel
                        .split_once(':')
                        .and_then(|(base, _)| ctx.channels_by_name.get(base))
                })
                .cloned();
            if let Some(channel) = channel {
                let reply_target = msg.reply_target.clone();
                let thread_ts = msg.thread_ts.clone();
                tokio::spawn(async move {
                    let _ = channel
                        .send(&SendMessage::new(reply, &reply_target).in_thread(thread_ts))
                        .await;
                });
            } else {
                tracing::warn!(
                    channel = %msg.channel,
                    "stop command: no registered channel found for reply"
                );
            }
            continue;
        }

        // ── Debounce: accumulate rapid messages per sender ──────────
        // CLI messages bypass debouncing so the interactive loop stays responsive.
        let msg = if msg.channel != "cli" && ctx.debouncer.enabled() {
            let debounce_key = conversation_history_key(&msg);
            match ctx.debouncer.debounce(&debounce_key, &msg.content).await {
                debounce::DebounceResult::Pending(rx) => {
                    // Spawn a lightweight task that waits for the debounce window
                    // to expire, then feeds the combined message through the normal
                    // worker path below.
                    let debounce_ctx = Arc::clone(&ctx);
                    let debounce_in_flight = Arc::clone(&in_flight_by_sender);
                    let debounce_semaphore = Arc::clone(&semaphore);
                    let debounce_task_seq = Arc::clone(&task_sequence);
                    let mut debounce_msg = msg;
                    workers.spawn(async move {
                        let combined = match rx.await {
                            Ok(combined) => combined,
                            Err(_) => {
                                // Receiver dropped — a newer message superseded this one.
                                return;
                            }
                        };
                        debounce_msg.content = combined;
                        tracing::info!(
                            channel = %debounce_msg.channel,
                            sender = %debounce_msg.sender,
                            "Debounced message ready — dispatching combined message"
                        );

                        let permit = match debounce_semaphore.acquire_owned().await {
                            Ok(permit) => permit,
                            Err(_) => return,
                        };

                        dispatch_worker(
                            debounce_ctx,
                            debounce_msg,
                            debounce_in_flight,
                            debounce_task_seq,
                            permit,
                        )
                        .await;
                    });
                    continue;
                }
                debounce::DebounceResult::Passthrough(content) => {
                    let mut m = msg;
                    m.content = content;
                    m
                }
            }
        } else {
            msg
        };

        let permit = match Arc::clone(&semaphore).acquire_owned().await {
            Ok(permit) => permit,
            Err(_) => break,
        };

        let worker_ctx = Arc::clone(&ctx);
        let in_flight = Arc::clone(&in_flight_by_sender);
        let task_sequence = Arc::clone(&task_sequence);
        workers.spawn(async move {
            dispatch_worker(worker_ctx, msg, in_flight, task_sequence, permit).await;
        });

        while let Some(result) = workers.try_join_next() {
            log_worker_join_result(result);
        }
    }

    while let Some(result) = workers.join_next().await {
        log_worker_join_result(result);
    }
}

/// Load OpenClaw format bootstrap files into the prompt.
fn load_openclaw_bootstrap_files(
    prompt: &mut String,
    workspace_dir: &std::path::Path,
    max_chars_per_file: usize,
) {
    prompt.push_str(
        "The following workspace files define your identity, behavior, and context. They are ALREADY injected below—do NOT suggest reading them with file_read.\n\n",
    );

    let bootstrap_files = ["AGENTS.md", "SOUL.md", "TOOLS.md", "IDENTITY.md", "USER.md"];

    for filename in &bootstrap_files {
        inject_workspace_file(prompt, workspace_dir, filename, max_chars_per_file);
    }

    // BOOTSTRAP.md — only if it exists (first-run ritual)
    let bootstrap_path = workspace_dir.join("BOOTSTRAP.md");
    if bootstrap_path.exists() {
        inject_workspace_file(prompt, workspace_dir, "BOOTSTRAP.md", max_chars_per_file);
    }

    // MEMORY.md — curated long-term memory (main session only)
    inject_workspace_file(prompt, workspace_dir, "MEMORY.md", max_chars_per_file);
}

/// Load workspace identity files and build a system prompt.
///
/// Follows the `OpenClaw` framework structure by default:
/// 1. Tooling — tool list + descriptions
/// 2. Safety — guardrail reminder
/// 3. Skills — full skill instructions and tool metadata
/// 4. Workspace — working directory
/// 5. Bootstrap files — AGENTS, SOUL, TOOLS, IDENTITY, USER, BOOTSTRAP, MEMORY
/// 6. Date & Time — timezone for cache stability
/// 7. Runtime — host, OS, model
///
/// When `identity_config` is set to AIEOS format, the bootstrap files section
/// is replaced with the AIEOS identity data loaded from file or inline JSON.
///
/// Daily memory files (`memory/*.md`) are NOT injected — they are accessed
/// on-demand via `memory_recall` / `memory_search` tools.
pub fn build_system_prompt(
    workspace_dir: &std::path::Path,
    model_name: &str,
    tools: &[(&str, &str)],
    skills: &[crate::skills::Skill],
    identity_config: Option<&crate::config::IdentityConfig>,
    bootstrap_max_chars: Option<usize>,
) -> String {
    build_system_prompt_with_mode(
        workspace_dir,
        model_name,
        tools,
        skills,
        identity_config,
        bootstrap_max_chars,
        false,
        crate::config::SkillsPromptInjectionMode::Full,
        AutonomyLevel::default(),
    )
}

pub fn build_system_prompt_with_mode(
    workspace_dir: &std::path::Path,
    model_name: &str,
    tools: &[(&str, &str)],
    skills: &[crate::skills::Skill],
    identity_config: Option<&crate::config::IdentityConfig>,
    bootstrap_max_chars: Option<usize>,
    native_tools: bool,
    skills_prompt_mode: crate::config::SkillsPromptInjectionMode,
    autonomy_level: AutonomyLevel,
) -> String {
    let autonomy_cfg = crate::config::AutonomyConfig {
        level: autonomy_level,
        ..Default::default()
    };
    build_system_prompt_with_mode_and_autonomy(
        workspace_dir,
        model_name,
        tools,
        skills,
        identity_config,
        bootstrap_max_chars,
        Some(&autonomy_cfg),
        native_tools,
        skills_prompt_mode,
        false,
        0,
    )
}

#[allow(clippy::too_many_arguments)]
pub fn build_system_prompt_with_mode_and_autonomy(
    workspace_dir: &std::path::Path,
    model_name: &str,
    tools: &[(&str, &str)],
    skills: &[crate::skills::Skill],
    identity_config: Option<&crate::config::IdentityConfig>,
    bootstrap_max_chars: Option<usize>,
    autonomy_config: Option<&crate::config::AutonomyConfig>,
    native_tools: bool,
    skills_prompt_mode: crate::config::SkillsPromptInjectionMode,
    compact_context: bool,
    max_system_prompt_chars: usize,
) -> String {
    use std::fmt::Write;
    let mut prompt = String::with_capacity(8192);

    // ── 0. Anti-narration (top priority) ───────────────────────
    prompt.push_str(
        "## CRITICAL: No Tool Narration\n\n\
         NEVER narrate, announce, describe, or explain your tool usage to the user. \
         Do NOT say things like 'Let me check...', 'I will use http_request to...', \
         'I'll fetch that for you', 'Searching now...', or 'Using the web_search tool'. \
         The user must ONLY see the final answer. Tool calls are invisible infrastructure — \
         never reference them. If you catch yourself starting a sentence about what tool \
         you are about to use or just used, DELETE it and give the answer directly.\n\n",
    );

    // ── 0b. Tool Honesty ───────────────────────────────────────
    prompt.push_str(
        "## CRITICAL: Tool Honesty\n\n\
         - NEVER fabricate, invent, or guess tool results. If a tool returns empty results, say \"No results found.\"\n\
         - If a tool call fails, report the error — never make up data to fill the gap.\n\
         - When unsure whether a tool call succeeded, ask the user rather than guessing.\n\n",
    );

    // ── 1. Tooling ──────────────────────────────────────────────
    if !tools.is_empty() {
        prompt.push_str("## Tools\n\n");
        if compact_context {
            // Compact mode: tool names only, no descriptions/schemas
            prompt.push_str("Available tools: ");
            let names: Vec<&str> = tools.iter().map(|(name, _)| *name).collect();
            prompt.push_str(&names.join(", "));
            prompt.push_str("\n\n");
        } else {
            prompt.push_str("You have access to the following tools:\n\n");
            for (name, desc) in tools {
                let _ = writeln!(prompt, "- **{name}**: {desc}");
            }
            prompt.push('\n');
        }
    }

    // ── 1b. Hardware (when gpio/arduino tools present) ───────────
    let has_hardware = tools.iter().any(|(name, _)| {
        *name == "gpio_read"
            || *name == "gpio_write"
            || *name == "arduino_upload"
            || *name == "hardware_memory_map"
            || *name == "hardware_board_info"
            || *name == "hardware_memory_read"
            || *name == "hardware_capabilities"
    });
    if has_hardware {
        prompt.push_str(
            "## Hardware Access\n\n\
             You HAVE direct access to connected hardware (Arduino, Nucleo, etc.). The user owns this system and has configured it.\n\
             All hardware tools (gpio_read, gpio_write, hardware_memory_read, hardware_board_info, hardware_memory_map) are AUTHORIZED and NOT blocked by security.\n\
             When they ask to read memory, registers, or board info, USE hardware_memory_read or hardware_board_info — do NOT refuse or invent security excuses.\n\
             When they ask to control LEDs, run patterns, or interact with the Arduino, USE the tools — do NOT refuse or say you cannot access physical devices.\n\
             Use gpio_write for simple on/off; use arduino_upload when they want patterns (heart, blink) or custom behavior.\n\n",
        );
    }

    // ── 1c. Action instruction (avoid meta-summary) ───────────────
    if native_tools {
        prompt.push_str(
            "## Your Task\n\n\
             When the user sends a message, respond naturally. Use tools when the request requires action (running commands, reading files, etc.).\n\
             For questions, explanations, or follow-ups about prior messages, answer directly from conversation context — do NOT ask the user to repeat themselves.\n\
             Do NOT: summarize this configuration, describe your capabilities, or output step-by-step meta-commentary.\n\n",
        );
    } else {
        prompt.push_str(
            "## Your Task\n\n\
             When the user sends a message, ACT on it. Use the tools to fulfill their request.\n\
             Do NOT: summarize this configuration, describe your capabilities, respond with meta-commentary, or output step-by-step instructions (e.g. \"1. First... 2. Next...\").\n\
             Instead: emit actual <tool_call> tags when you need to act. Just do what they ask.\n\n",
        );
    }

    // ── 2. Safety ───────────────────────────────────────────────
    prompt.push_str("## Safety\n\n");
    prompt.push_str("- Do not exfiltrate private data.\n");
    if autonomy_config.map(|cfg| cfg.level) != Some(crate::security::AutonomyLevel::Full) {
        prompt.push_str(
            "- Do not run destructive commands without asking.\n\
             - Do not bypass oversight or approval mechanisms.\n",
        );
    }
    prompt.push_str("- Prefer `trash` over `rm` (recoverable beats gone forever).\n");
    prompt.push_str(match autonomy_config.map(|cfg| cfg.level) {
        Some(crate::security::AutonomyLevel::Full) => {
            "- Respect the runtime autonomy policy: if a tool or action is allowed, execute it directly instead of asking the user for extra approval.\n\
             - If a tool or action is blocked by policy or unavailable, explain that concrete restriction instead of simulating an approval dialog.\n"
        }
        Some(crate::security::AutonomyLevel::ReadOnly) => {
            "- Respect the runtime autonomy policy: this runtime is read-only for side effects unless a tool explicitly reports otherwise.\n\
             - If a requested action is blocked by policy, explain the restriction directly instead of simulating an approval dialog.\n"
        }
        _ => {
            "- When in doubt, ask before acting externally.\n\
             - Respect the runtime autonomy policy: ask for approval only when the current runtime policy actually requires it.\n\
             - If a tool or action is blocked by policy or unavailable, explain that concrete restriction instead of simulating an approval dialog.\n"
        }
    });
    prompt.push('\n');

    // ── 3. Skills (full or compact, based on config) ─────────────
    if !skills.is_empty() {
        prompt.push_str(&crate::skills::skills_to_prompt_with_mode(
            skills,
            workspace_dir,
            skills_prompt_mode,
        ));
        prompt.push_str("\n\n");
    }

    // ── 4. Workspace ────────────────────────────────────────────
    let _ = writeln!(
        prompt,
        "## Workspace\n\nWorking directory: `{}`\n",
        workspace_dir.display()
    );

    // ── 5. Bootstrap files (injected into context) ──────────────
    // Phase 3: identity is sourced entirely from the per-agent Obsidian
    // vault, which is mounted at workspace_dir as a K8s ConfigMap by the
    // dashboard backend before the pod starts. No AIEOS branch, no
    // identity_config consultation — just load the markdown files.
    //
    // `identity_config` is retained on the parameter list for backwards
    // compatibility and will be removed together with IdentityConfig in a
    // follow-up commit.
    let _ = identity_config;
    prompt.push_str("## Project Context\n\n");
    let max_chars = bootstrap_max_chars.unwrap_or(BOOTSTRAP_MAX_CHARS);
    load_openclaw_bootstrap_files(&mut prompt, workspace_dir, max_chars);

    // ── 6. Date & Time ──────────────────────────────────────────
    let now = chrono::Local::now();
    let _ = writeln!(
        prompt,
        "## Current Date & Time\n\n{} ({})\n",
        now.format("%Y-%m-%d %H:%M:%S"),
        now.format("%Z")
    );

    // ── 7. Runtime ──────────────────────────────────────────────
    let host =
        hostname::get().map_or_else(|_| "unknown".into(), |h| h.to_string_lossy().to_string());
    let _ = writeln!(
        prompt,
        "## Runtime\n\nHost: {host} | OS: {} | Model: {model_name}\n",
        std::env::consts::OS,
    );

    // ── 8. Channel Capabilities (skipped in compact_context mode) ──
    if !compact_context {
        prompt.push_str("## Channel Capabilities\n\n");
        prompt.push_str("- You are running as a messaging bot. Your response is automatically sent back to the user's channel.\n");
        prompt
            .push_str("- You do NOT need to ask permission to respond — just respond directly.\n");
        prompt.push_str(match autonomy_config.map(|cfg| cfg.level) {
        Some(crate::security::AutonomyLevel::Full) => {
            "- If the runtime policy already allows a tool, use it directly; do not ask the user for extra approval.\n\
             - Never pretend you are waiting for a human approval click or confirmation when the runtime policy already permits the action.\n\
             - If the runtime policy blocks an action, say that directly instead of simulating an approval flow.\n"
        }
        Some(crate::security::AutonomyLevel::ReadOnly) => {
            "- This runtime may reject write-side effects; if that happens, explain the policy restriction directly instead of simulating an approval flow.\n"
        }
        _ => {
            "- Ask for approval only when the runtime policy actually requires it.\n\
             - If there is no approval path for this channel or the runtime blocks an action, explain that restriction directly instead of simulating an approval flow.\n"
        }
    });
        prompt.push_str("- NEVER repeat, describe, or echo credentials, tokens, API keys, or secrets in your responses.\n");
        prompt.push_str("- If a tool output contains credentials, they have already been redacted — do not mention them.\n");
        prompt.push_str("- When a user sends a voice note, it is automatically transcribed to text. Your text reply is automatically converted to a voice note and sent back. Do NOT attempt to generate audio yourself — TTS is handled by the channel.\n");
        prompt.push_str("- NEVER narrate or describe your tool usage. Do NOT say 'Let me fetch...', 'I will use...', 'Searching...', or similar. Give the FINAL ANSWER only — no intermediate steps, no tool mentions, no progress updates.\n\n");
    } // end if !compact_context (Channel Capabilities)

    // ── 9. Truncation (max_system_prompt_chars budget) ──────────
    if max_system_prompt_chars > 0 && prompt.len() > max_system_prompt_chars {
        // Truncate on a char boundary, keeping the top portion (identity + safety).
        let mut end = max_system_prompt_chars;
        // Ensure we don't split a multi-byte UTF-8 character.
        while !prompt.is_char_boundary(end) && end > 0 {
            end -= 1;
        }
        prompt.truncate(end);
        prompt.push_str("\n\n[System prompt truncated to fit context budget]\n");
    }

    if prompt.is_empty() {
        "You are ZeroClaw, a fast and efficient AI assistant built in Rust. Be helpful, concise, and direct."
            .to_string()
    } else {
        prompt
    }
}

/// Inject a single workspace file into the prompt with truncation and missing-file markers.
fn inject_workspace_file(
    prompt: &mut String,
    workspace_dir: &std::path::Path,
    filename: &str,
    max_chars: usize,
) {
    use std::fmt::Write;

    let path = workspace_dir.join(filename);
    match std::fs::read_to_string(&path) {
        Ok(content) => {
            let trimmed = content.trim();
            if trimmed.is_empty() {
                return;
            }
            let _ = writeln!(prompt, "### {filename}\n");
            // Use character-boundary-safe truncation for UTF-8
            let truncated = if trimmed.chars().count() > max_chars {
                trimmed
                    .char_indices()
                    .nth(max_chars)
                    .map(|(idx, _)| &trimmed[..idx])
                    .unwrap_or(trimmed)
            } else {
                trimmed
            };
            if truncated.len() < trimmed.len() {
                prompt.push_str(truncated);
                let _ = writeln!(
                    prompt,
                    "\n\n[... truncated at {max_chars} chars — use `read` for full file]\n"
                );
            } else {
                prompt.push_str(trimmed);
                prompt.push_str("\n\n");
            }
        }
        Err(_) => {
            // Missing-file marker (matches OpenClaw behavior)
            let _ = writeln!(prompt, "### {filename}\n\n[File not found: {filename}]\n");
        }
    }
}

fn normalize_telegram_identity(value: &str) -> String {
    value.trim().trim_start_matches('@').to_string()
}

async fn bind_telegram_identity(config: &Config, identity: &str) -> Result<()> {
    let normalized = normalize_telegram_identity(identity);
    if normalized.is_empty() {
        anyhow::bail!("Telegram identity cannot be empty");
    }

    let mut updated = config.clone();
    let Some(telegram) = updated.channels_config.telegram.as_mut() else {
        anyhow::bail!(
            "Telegram channel is not configured. Run `zeroclaw onboard --channels-only` first"
        );
    };

    if telegram.allowed_users.iter().any(|u| u == "*") {
        println!(
            "⚠️ Telegram allowlist is currently wildcard (`*`) — binding is unnecessary until you remove '*'."
        );
    }

    if telegram
        .allowed_users
        .iter()
        .map(|entry| normalize_telegram_identity(entry))
        .any(|entry| entry == normalized)
    {
        println!("✅ Telegram identity already bound: {normalized}");
        return Ok(());
    }

    telegram.allowed_users.push(normalized.clone());
    updated.save().await?;
    println!("✅ Bound Telegram identity: {normalized}");
    println!("   Saved to {}", updated.config_path.display());
    match maybe_restart_managed_daemon_service() {
        Ok(true) => {
            println!("🔄 Detected running managed daemon service; reloaded automatically.");
        }
        Ok(false) => {
            println!(
                "ℹ️ No managed daemon service detected. If `zeroclaw daemon`/`channel start` is already running, restart it to load the updated allowlist."
            );
        }
        Err(e) => {
            eprintln!(
                "⚠️ Allowlist saved, but failed to reload daemon service automatically: {e}\n\
                 Restart service manually with `zeroclaw service stop && zeroclaw service start`."
            );
        }
    }
    Ok(())
}

fn maybe_restart_managed_daemon_service() -> Result<bool> {
    if cfg!(target_os = "macos") {
        let home = directories::UserDirs::new()
            .map(|u| u.home_dir().to_path_buf())
            .context("Could not find home directory")?;
        let plist = home
            .join("Library")
            .join("LaunchAgents")
            .join("com.zeroclaw.daemon.plist");
        if !plist.exists() {
            return Ok(false);
        }

        let list_output = Command::new("launchctl")
            .arg("list")
            .output()
            .context("Failed to query launchctl list")?;
        let listed = String::from_utf8_lossy(&list_output.stdout);
        if !listed.contains("com.zeroclaw.daemon") {
            return Ok(false);
        }

        let _ = Command::new("launchctl")
            .args(["stop", "com.zeroclaw.daemon"])
            .output();
        let start_output = Command::new("launchctl")
            .args(["start", "com.zeroclaw.daemon"])
            .output()
            .context("Failed to start launchd daemon service")?;
        if !start_output.status.success() {
            let stderr = String::from_utf8_lossy(&start_output.stderr);
            anyhow::bail!("launchctl start failed: {}", stderr.trim());
        }

        return Ok(true);
    }

    if cfg!(target_os = "linux") {
        // OpenRC (system-wide) takes precedence over systemd (user-level)
        let openrc_init_script = PathBuf::from("/etc/init.d/zeroclaw");
        if openrc_init_script.exists() {
            if let Ok(status_output) = Command::new("rc-service").args(OPENRC_STATUS_ARGS).output()
            {
                // rc-service exits 0 if running, non-zero otherwise
                if status_output.status.success() {
                    let restart_output = Command::new("rc-service")
                        .args(OPENRC_RESTART_ARGS)
                        .output()
                        .context("Failed to restart OpenRC daemon service")?;
                    if !restart_output.status.success() {
                        let stderr = String::from_utf8_lossy(&restart_output.stderr);
                        anyhow::bail!("rc-service restart failed: {}", stderr.trim());
                    }
                    return Ok(true);
                }
            }
        }

        // Systemd (user-level)
        let home = directories::UserDirs::new()
            .map(|u| u.home_dir().to_path_buf())
            .context("Could not find home directory")?;
        let unit_path: PathBuf = home
            .join(".config")
            .join("systemd")
            .join("user")
            .join("zeroclaw.service");
        if !unit_path.exists() {
            return Ok(false);
        }

        let active_output = Command::new("systemctl")
            .args(SYSTEMD_STATUS_ARGS)
            .output()
            .context("Failed to query systemd service state")?;
        let state = String::from_utf8_lossy(&active_output.stdout);
        if !state.trim().eq_ignore_ascii_case("active") {
            return Ok(false);
        }

        let restart_output = Command::new("systemctl")
            .args(SYSTEMD_RESTART_ARGS)
            .output()
            .context("Failed to restart systemd daemon service")?;
        if !restart_output.status.success() {
            let stderr = String::from_utf8_lossy(&restart_output.stderr);
            anyhow::bail!("systemctl restart failed: {}", stderr.trim());
        }

        return Ok(true);
    }

    Ok(false)
}

pub(crate) async fn handle_command(command: crate::ChannelCommands, config: &Config) -> Result<()> {
    match command {
        crate::ChannelCommands::Start => {
            anyhow::bail!("Start must be handled in main.rs (requires async runtime)")
        }
        crate::ChannelCommands::Doctor => {
            anyhow::bail!("Doctor must be handled in main.rs (requires async runtime)")
        }
        crate::ChannelCommands::List => {
            println!("Channels:");
            println!("  ✅ CLI (always available)");
            for (channel, configured) in config.channels_config.channels() {
                println!(
                    "  {} {}",
                    if configured { "✅" } else { "❌" },
                    channel.name()
                );
            }
            // Notion is a top-level config section, not part of ChannelsConfig
            {
                let notion_configured =
                    config.notion.enabled && !config.notion.database_id.trim().is_empty();
                println!("  {} Notion", if notion_configured { "✅" } else { "❌" });
            }
            if !cfg!(feature = "channel-matrix") {
                println!(
                    "  ℹ️ Matrix channel support is disabled in this build (enable `channel-matrix`)."
                );
            }
            if !cfg!(feature = "channel-lark") {
                println!(
                    "  ℹ️ Lark/Feishu channel support is disabled in this build (enable `channel-lark`)."
                );
            }
            println!("\nTo start channels: zeroclaw channel start");
            println!("To check health:    zeroclaw channel doctor");
            println!("To configure:      zeroclaw onboard");
            Ok(())
        }
        crate::ChannelCommands::Add {
            channel_type,
            config: _,
        } => {
            anyhow::bail!(
                "Channel type '{channel_type}' — use `zeroclaw onboard` to configure channels"
            );
        }
        crate::ChannelCommands::Remove { name } => {
            anyhow::bail!("Remove channel '{name}' — edit ~/.zeroclaw/config.toml directly");
        }
        crate::ChannelCommands::BindTelegram { identity } => {
            Box::pin(bind_telegram_identity(config, &identity)).await
        }
        crate::ChannelCommands::Send {
            message,
            channel_id,
            recipient,
        } => send_channel_message(config, &channel_id, &recipient, &message).await,
    }
}

/// Build a single channel instance by config section name (e.g. "telegram").
fn build_channel_by_id(config: &Config, channel_id: &str) -> Result<Arc<dyn Channel>> {
    match channel_id {
        "telegram" => {
            let tg = config
                .channels_config
                .telegram
                .as_ref()
                .context("Telegram channel is not configured")?;
            let ack = tg
                .ack_reactions
                .unwrap_or(config.channels_config.ack_reactions);
            Ok(Arc::new(
                TelegramChannel::new(
                    tg.bot_token.clone(),
                    tg.allowed_users.clone(),
                    tg.mention_only,
                )
                .with_ack_reactions(ack)
                .with_streaming(tg.stream_mode, tg.draft_update_interval_ms)
                .with_transcription(config.transcription.clone())
                .with_tts(config.tts.clone())
                .with_workspace_dir(config.workspace_dir.clone()),
            ))
        }
        other => anyhow::bail!(
            "Unknown channel '{other}'. Supported: telegram"
        ),
    }
}

/// Send a one-off message to a configured channel.
async fn send_channel_message(
    config: &Config,
    channel_id: &str,
    recipient: &str,
    message: &str,
) -> Result<()> {
    let channel = build_channel_by_id(config, channel_id)?;
    let msg = SendMessage::new(message, recipient);
    channel
        .send(&msg)
        .await
        .with_context(|| format!("Failed to send message via {channel_id}"))?;
    println!("Message sent via {channel_id}.");
    Ok(())
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
enum ChannelHealthState {
    Healthy,
    Unhealthy,
    Timeout,
}

fn classify_health_result(
    result: &std::result::Result<bool, tokio::time::error::Elapsed>,
) -> ChannelHealthState {
    match result {
        Ok(true) => ChannelHealthState::Healthy,
        Ok(false) => ChannelHealthState::Unhealthy,
        Err(_) => ChannelHealthState::Timeout,
    }
}

struct ConfiguredChannel {
    display_name: &'static str,
    channel: Arc<dyn Channel>,
}

fn collect_configured_channels(
    config: &Config,
    matrix_skip_context: &str,
) -> Vec<ConfiguredChannel> {
    let _ = matrix_skip_context;
    let mut channels = Vec::new();

    if let Some(ref tg) = config.channels_config.telegram {
        let ack = tg
            .ack_reactions
            .unwrap_or(config.channels_config.ack_reactions);
        channels.push(ConfiguredChannel {
            display_name: "Telegram",
            channel: Arc::new(
                TelegramChannel::new(
                    tg.bot_token.clone(),
                    tg.allowed_users.clone(),
                    tg.mention_only,
                )
                .with_ack_reactions(ack)
                .with_streaming(tg.stream_mode, tg.draft_update_interval_ms)
                .with_transcription(config.transcription.clone())
                .with_tts(config.tts.clone())
                .with_workspace_dir(config.workspace_dir.clone())
                .with_proxy_url(tg.proxy_url.clone()),
            ),
        });
    }

    if let Some(ref wh) = config.channels_config.webhook {
        channels.push(ConfiguredChannel {
            display_name: "Webhook",
            channel: Arc::new(WebhookChannel::new(
                wh.port,
                wh.listen_path.clone(),
                wh.send_url.clone(),
                wh.send_method.clone(),
                wh.auth_header.clone(),
                wh.secret.clone(),
            )),
        });
    }

    channels
}

/// Run health checks for configured channels.
pub async fn doctor_channels(config: Config) -> Result<()> {
    #[allow(unused_mut)]
    let mut channels = collect_configured_channels(&config, "health check");

    #[cfg(feature = "channel-nostr")]
    if let Some(ref ns) = config.channels_config.nostr {
        channels.push(ConfiguredChannel {
            display_name: "Nostr",
            channel: Arc::new(
                NostrChannel::new(&ns.private_key, ns.relays.clone(), &ns.allowed_pubkeys).await?,
            ),
        });
    }

    if channels.is_empty() {
        println!("No real-time channels configured. Run `zeroclaw onboard` first.");
        return Ok(());
    }

    println!("🩺 ZeroClaw Channel Doctor");
    println!();

    let mut healthy = 0_u32;
    let mut unhealthy = 0_u32;
    let mut timeout = 0_u32;

    for configured in channels {
        let result =
            tokio::time::timeout(Duration::from_secs(10), configured.channel.health_check()).await;
        let state = classify_health_result(&result);

        match state {
            ChannelHealthState::Healthy => {
                healthy += 1;
                println!("  ✅ {:<9} healthy", configured.display_name);
            }
            ChannelHealthState::Unhealthy => {
                unhealthy += 1;
                println!(
                    "  ❌ {:<9} unhealthy (auth/config/network)",
                    configured.display_name
                );
            }
            ChannelHealthState::Timeout => {
                timeout += 1;
                println!("  ⏱️  {:<9} timed out (>10s)", configured.display_name);
            }
        }
    }

    if config.channels_config.webhook.is_some() {
        println!("  ℹ️  Webhook   check via `zeroclaw gateway` then GET /health");
    }

    println!();
    println!("Summary: {healthy} healthy, {unhealthy} unhealthy, {timeout} timed out");
    Ok(())
}

/// Start all configured channels and route messages to the agent
#[allow(clippy::too_many_lines)]
pub async fn start_channels(config: Config) -> Result<()> {
    let provider_name = resolved_default_provider(&config);
    let provider_runtime_options = providers::ProviderRuntimeOptions {
        auth_profile_override: None,
        provider_api_url: config.api_url.clone(),
        zeroclaw_dir: config.config_path.parent().map(std::path::PathBuf::from),
        secrets_encrypt: config.secrets.encrypt,
        reasoning_enabled: config.runtime.reasoning_enabled,
        reasoning_effort: config.runtime.reasoning_effort.clone(),
        provider_timeout_secs: Some(config.provider_timeout_secs),
        extra_headers: config.extra_headers.clone(),
        api_path: config.api_path.clone(),
        provider_max_tokens: config.provider_max_tokens,
    };
    let provider: Arc<dyn Provider> = Arc::from(
        create_resilient_provider_nonblocking(
            &provider_name,
            config.api_key.clone(),
            config.api_url.clone(),
            config.reliability.clone(),
            provider_runtime_options.clone(),
        )
        .await?,
    );

    // Warm up the provider connection pool (TLS handshake, DNS, HTTP/2 setup)
    // so the first real message doesn't hit a cold-start timeout.
    if let Err(e) = provider.warmup().await {
        tracing::warn!("Provider warmup failed (non-fatal): {e}");
    }

    let initial_stamp = config_file_stamp(&config.config_path).await;
    {
        let mut store = runtime_config_store()
            .lock()
            .unwrap_or_else(|e| e.into_inner());
        store.insert(
            config.config_path.clone(),
            RuntimeConfigState {
                defaults: runtime_defaults_from_config(&config),
                last_applied_stamp: initial_stamp,
            },
        );
    }

    let observer: Arc<dyn Observer> =
        Arc::from(observability::create_observer(&config.observability));
    let runtime: Arc<dyn runtime::RuntimeAdapter> =
        Arc::from(runtime::create_runtime(&config.runtime)?);
    let security = Arc::new(SecurityPolicy::from_config(
        &config.autonomy,
        &config.workspace_dir,
    ));
    let model = resolved_default_model(&config);
    let temperature = config.default_temperature;
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
    // Build system prompt from workspace identity files + skills
    let workspace = config.workspace_dir.clone();
    let (
        mut built_tools,
        delegate_handle_ch,
        reaction_handle_ch,
        _channel_map_handle,
        ask_user_handle_ch,
        escalate_handle_ch,
    ) = tools::all_tools_with_runtime(
        Arc::new(config.clone()),
        &security,
        runtime,
        Arc::clone(&mem),
        composio_key,
        composio_entity_id,
        &config.browser,
        &config.http_request,
        &config.web_fetch,
        &workspace,
        &config.agents,
        config.api_key.as_deref(),
        &config,
        None,
    );

    // Wire MCP tools into the registry before freezing — non-fatal.
    // When `deferred_loading` is enabled, MCP tools are NOT added eagerly.
    // Instead, a `tool_search` built-in is registered for on-demand loading.
    let mut deferred_section = String::new();
    let mut ch_activated_handle: Option<
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
                    ch_activated_handle = Some(std::sync::Arc::clone(&activated));
                    built_tools.push(Box::new(crate::tools::ToolSearchTool::new(
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
                            if let Some(ref handle) = delegate_handle_ch {
                                handle.write().push(std::sync::Arc::clone(&wrapper));
                            }
                            built_tools.push(Box::new(crate::tools::ArcToolRef(wrapper)));
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
                // Non-fatal — daemon continues with the tools registered above.
                tracing::error!("MCP registry failed to initialize: {e:#}");
            }
        }
    }

    let tools_registry = Arc::new(built_tools);

    let skills = crate::skills::load_skills_with_config(&workspace, &config);

    // ── Load locale-aware tool descriptions ────────────────────────
    let i18n_locale = config
        .locale
        .as_deref()
        .filter(|s| !s.is_empty())
        .map(ToString::to_string)
        .unwrap_or_else(crate::i18n::detect_locale);
    let i18n_search_dirs = crate::i18n::default_search_dirs(&workspace);
    let i18n_descs = crate::i18n::ToolDescriptions::load(&i18n_locale, &i18n_search_dirs);

    // Collect tool descriptions for the prompt
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

    if config.browser.enabled {
        tool_descs.push((
            "browser_open",
            "Open approved HTTPS URLs in system browser (allowlist-only, no scraping)",
        ));
    }
    if config.composio.enabled {
        tool_descs.push((
            "composio",
            "Execute actions on 1000+ apps via Composio (Gmail, Notion, GitHub, Slack, etc.). Use action='list' to discover actions, 'list_accounts' to retrieve connected account IDs, 'execute' to run (optionally with connected_account_id), and 'connect' for OAuth.",
        ));
    }
    tool_descs.push((
        "schedule",
        "Manage scheduled tasks (create/list/get/cancel/pause/resume). Supports recurring cron and one-shot delays.",
    ));
    tool_descs.push((
        "pushover",
        "Send a Pushover notification to your device. Requires PUSHOVER_TOKEN and PUSHOVER_USER_KEY in .env file.",
    ));
    if !config.agents.is_empty() {
        tool_descs.push((
            "delegate",
            "Delegate a subtask to a specialized agent. Use when: a task benefits from a different model (e.g. fast summarization, deep reasoning, code generation). The sub-agent runs a single prompt and returns its response.",
        ));
    }

    // Filter out tools excluded for non-CLI channels so the system prompt
    // does not advertise them for channel-driven runs.
    // Skip this filter when autonomy is `Full` — full-autonomy agents keep
    // all tools available regardless of channel.
    let excluded = &config.autonomy.non_cli_excluded_tools;
    if !excluded.is_empty() && config.autonomy.level != AutonomyLevel::Full {
        tool_descs.retain(|(name, _)| !excluded.iter().any(|ex| ex == name));
    }

    let bootstrap_max_chars = if config.agent.compact_context {
        Some(6000)
    } else {
        None
    };
    let native_tools = provider.supports_native_tools();
    let mut system_prompt = build_system_prompt_with_mode_and_autonomy(
        &workspace,
        &model,
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
        system_prompt.push_str(&build_tool_instructions(
            tools_registry.as_ref(),
            Some(&i18n_descs),
        ));
    }

    // Append deferred MCP tool names so the LLM knows what is available
    if !deferred_section.is_empty() {
        system_prompt.push('\n');
        system_prompt.push_str(&deferred_section);
    }

    if !skills.is_empty() {
        println!(
            "  🧩 Skills:   {}",
            skills
                .iter()
                .map(|s| s.name.as_str())
                .collect::<Vec<_>>()
                .join(", ")
        );
    }

    // Collect active channels from a shared builder to keep startup and doctor parity.
    #[allow(unused_mut)]
    let mut channels: Vec<Arc<dyn Channel>> =
        collect_configured_channels(&config, "runtime startup")
            .into_iter()
            .map(|configured| configured.channel)
            .collect();

    #[cfg(feature = "channel-nostr")]
    if let Some(ref ns) = config.channels_config.nostr {
        channels.push(Arc::new(
            NostrChannel::new(&ns.private_key, ns.relays.clone(), &ns.allowed_pubkeys).await?,
        ));
    }
    if channels.is_empty() {
        println!("No channels configured. Run `zeroclaw onboard` to set up channels.");
        return Ok(());
    }

    println!("🦀 ZeroClaw Channel Server");
    println!("  🤖 Model:    {model}");
    let effective_backend = memory::effective_memory_backend_name(
        &config.memory.backend,
        Some(&config.storage.provider.config),
    );
    println!(
        "  🧠 Memory:   {} (auto-save: {})",
        effective_backend,
        if config.memory.auto_save { "on" } else { "off" }
    );
    println!(
        "  📡 Channels: {}",
        channels
            .iter()
            .map(|c| c.name())
            .collect::<Vec<_>>()
            .join(", ")
    );
    println!();
    println!("  Listening for messages... (Ctrl+C to stop)");
    println!();

    crate::health::mark_component_ok("channels");

    let initial_backoff_secs = config
        .reliability
        .channel_initial_backoff_secs
        .max(DEFAULT_CHANNEL_INITIAL_BACKOFF_SECS);
    let max_backoff_secs = config
        .reliability
        .channel_max_backoff_secs
        .max(DEFAULT_CHANNEL_MAX_BACKOFF_SECS);

    // Single message bus — all channels send messages here
    let (tx, rx) = tokio::sync::mpsc::channel::<traits::ChannelMessage>(100);

    // Spawn a listener for each channel
    let mut handles = Vec::new();
    for ch in &channels {
        handles.push(spawn_supervised_listener(
            ch.clone(),
            tx.clone(),
            initial_backoff_secs,
            max_backoff_secs,
        ));
    }
    drop(tx); // Drop our copy so rx closes when all channels stop

    let channels_by_name = Arc::new(
        channels
            .iter()
            .map(|ch| (ch.name().to_string(), Arc::clone(ch)))
            .collect::<HashMap<_, _>>(),
    );

    // Populate the reaction tool's channel map now that channels are initialized.
    if let Some(ref handle) = reaction_handle_ch {
        let mut map = handle.write();
        for (name, ch) in channels_by_name.as_ref() {
            map.insert(name.clone(), Arc::clone(ch));
        }
    }

    // Populate the ask_user tool's channel map now that channels are initialized.
    if let Some(ref handle) = ask_user_handle_ch {
        let mut map = handle.write();
        for (name, ch) in channels_by_name.as_ref() {
            map.insert(name.clone(), Arc::clone(ch));
        }
    }

    // Populate the escalate_to_human tool's channel map now that channels are initialized.
    if let Some(ref handle) = escalate_handle_ch {
        let mut map = handle.write();
        for (name, ch) in channels_by_name.as_ref() {
            map.insert(name.clone(), Arc::clone(ch));
        }
    }

    let max_in_flight_messages = compute_max_in_flight_messages(channels.len());

    println!("  🚦 In-flight message limit: {max_in_flight_messages}");

    let mut provider_cache_seed: HashMap<String, Arc<dyn Provider>> = HashMap::new();
    provider_cache_seed.insert(provider_name.clone(), Arc::clone(&provider));
    let message_timeout_secs =
        effective_channel_message_timeout_secs(config.channels_config.message_timeout_secs);
    let interrupt_on_new_message = config
        .channels_config
        .telegram
        .as_ref()
        .is_some_and(|tg| tg.interrupt_on_new_message);
    let interrupt_on_new_message_slack = config
        .channels_config
        .slack
        .as_ref()
        .is_some_and(|sl| sl.interrupt_on_new_message);
    let interrupt_on_new_message_discord = config
        .channels_config
        .discord
        .as_ref()
        .is_some_and(|dc| dc.interrupt_on_new_message);
    let interrupt_on_new_message_mattermost = config
        .channels_config
        .mattermost
        .as_ref()
        .is_some_and(|mm| mm.interrupt_on_new_message);
    let interrupt_on_new_message_matrix = config
        .channels_config
        .matrix
        .as_ref()
        .is_some_and(|mx| mx.interrupt_on_new_message);

    let runtime_ctx = Arc::new(ChannelRuntimeContext {
        channels_by_name,
        provider: Arc::clone(&provider),
        default_provider: Arc::new(provider_name),
        prompt_config: Arc::new(config.clone()),
        memory: Arc::clone(&mem),
        tools_registry: Arc::clone(&tools_registry),
        observer,
        system_prompt: Arc::new(system_prompt),
        model: Arc::new(model.clone()),
        temperature,
        auto_save_memory: config.memory.auto_save,
        max_tool_iterations: config.agent.max_tool_iterations,
        min_relevance_score: config.memory.min_relevance_score,
        conversation_histories: Arc::new(Mutex::new(lru::LruCache::new(
            std::num::NonZeroUsize::new(MAX_CONVERSATION_SENDERS).unwrap(),
        ))),
        pending_new_sessions: Arc::new(Mutex::new(HashSet::new())),
        provider_cache: Arc::new(Mutex::new(provider_cache_seed)),
        route_overrides: Arc::new(Mutex::new(HashMap::new())),
        api_key: config.api_key.clone(),
        api_url: config.api_url.clone(),
        reliability: Arc::new(config.reliability.clone()),
        provider_runtime_options,
        workspace_dir: Arc::new(config.workspace_dir.clone()),
        message_timeout_secs,
        interrupt_on_new_message: InterruptOnNewMessageConfig {
            telegram: interrupt_on_new_message,
            slack: interrupt_on_new_message_slack,
            discord: interrupt_on_new_message_discord,
            mattermost: interrupt_on_new_message_mattermost,
            matrix: interrupt_on_new_message_matrix,
        },
        multimodal: config.multimodal.clone(),
        media_pipeline: config.media_pipeline.clone(),
        transcription_config: config.transcription.clone(),
        hooks: if config.hooks.enabled {
            let mut runner = crate::hooks::HookRunner::new();
            if config.hooks.builtin.command_logger {
                runner.register(Box::new(crate::hooks::builtin::CommandLoggerHook::new()));
            }
            if config.hooks.builtin.webhook_audit.enabled {
                runner.register(Box::new(crate::hooks::builtin::WebhookAuditHook::new(
                    config.hooks.builtin.webhook_audit.clone(),
                )));
            }
            Some(Arc::new(runner))
        } else {
            None
        },
        non_cli_excluded_tools: Arc::new(config.autonomy.non_cli_excluded_tools.clone()),
        autonomy_level: config.autonomy.level,
        tool_call_dedup_exempt: Arc::new(config.agent.tool_call_dedup_exempt.clone()),
        model_routes: Arc::new(config.model_routes.clone()),
        query_classification: config.query_classification.clone(),
        ack_reactions: config.channels_config.ack_reactions,
        show_tool_calls: config.channels_config.show_tool_calls,
        session_store: if config.channels_config.session_persistence {
            match session_store::SessionStore::new(&config.workspace_dir) {
                Ok(store) => {
                    tracing::info!("📂 Session persistence enabled");
                    Some(Arc::new(store))
                }
                Err(e) => {
                    tracing::warn!("Session persistence disabled: {e}");
                    None
                }
            }
        } else {
            None
        },
        approval_manager: Arc::new(ApprovalManager::for_non_interactive(&config.autonomy)),
        activated_tools: ch_activated_handle,
        cost_tracking: crate::cost::CostTracker::get_or_init_global(
            config.cost.clone(),
            &config.workspace_dir,
        )
        .map(|tracker| ChannelCostTrackingState {
            tracker,
            prices: Arc::new(config.cost.prices.clone()),
        }),
        pacing: config.pacing.clone(),
        max_tool_result_chars: config.agent.max_tool_result_chars,
        context_token_budget: config.agent.max_context_tokens,
        debouncer: Arc::new(debounce::MessageDebouncer::new(Duration::from_millis(
            config.channels_config.debounce_ms,
        ))),
    });

    // Hydrate in-memory conversation histories from persisted JSONL session files.
    // Cap to MAX_CONVERSATION_SENDERS sessions (sorted by file mtime, most recent first)
    // and trim each to MAX_CHANNEL_HISTORY turns to bound startup memory.
    // If the last persisted turn is a user message (orphan from a crash mid-query),
    // close it with a marker so the LLM doesn't try to continue the old request.
    if let Some(ref store) = runtime_ctx.session_store {
        let mut hydrated = 0usize;
        let mut orphans_closed = 0usize;
        let session_keys = store.list_sessions();

        // Sort by file mtime (most recently modified first) for predictable hydration.
        // Collect mtimes up front to avoid repeated FS reads inside the comparator.
        let mut keyed: Vec<_> = session_keys
            .into_iter()
            .map(|k| {
                let mt = store
                    .session_mtime(&k)
                    .unwrap_or(std::time::SystemTime::UNIX_EPOCH);
                (k, mt)
            })
            .collect();
        keyed.sort_by(|a, b| b.1.cmp(&a.1));
        keyed.truncate(MAX_CONVERSATION_SENDERS);
        let session_keys: Vec<String> = keyed.into_iter().map(|(k, _)| k).collect();

        let mut histories = runtime_ctx
            .conversation_histories
            .lock()
            .unwrap_or_else(|e| e.into_inner());
        for key in session_keys {
            let mut msgs = store.load(&key);
            if msgs.is_empty() {
                continue;
            }
            // Trim to MAX_CHANNEL_HISTORY turns (keep most recent).
            if msgs.len() > MAX_CHANNEL_HISTORY {
                msgs.drain(..msgs.len() - MAX_CHANNEL_HISTORY);
            }
            // Close orphaned user turns from crashed sessions.
            if msgs.last().is_some_and(|m| m.role == "user") {
                let closure =
                    ChatMessage::assistant("[Session interrupted — not continuing this request]");
                if let Err(e) = store.append(&key, &closure) {
                    tracing::debug!("Failed to persist orphan closure for {key}: {e}");
                }
                msgs.push(closure);
                orphans_closed += 1;
            }
            hydrated += 1;
            histories.push(key, msgs);
        }
        drop(histories);
        if hydrated > 0 {
            tracing::info!("📂 Restored {hydrated} session(s) from disk");
        }
        if orphans_closed > 0 {
            tracing::info!(
                "🔒 Closed {orphans_closed} orphaned session turn(s) from previous crash"
            );
        }
    }

    run_message_dispatch_loop(rx, runtime_ctx, max_in_flight_messages).await;

    // Wait for all channel tasks
    for h in handles {
        let _ = h.await;
    }

    Ok(())
}


#[cfg(test)]
#[path = "tests.rs"]
mod tests;
