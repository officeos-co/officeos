//! The [`Observer`] trait and the [`ObserverEvent`] enum.
//!
//! This file defines [`ObserverEvent`] at line 10 and the [`Observer`] trait
//! at line 156. Together they form the contract every observability backend
//! in ZeroClaw implements.
//!
//! [`ObserverEvent`] is an enum covering every notable agent event. Concrete
//! variants include `TurnStarted`, `TurnEnded`, `ToolCallStarted`,
//! `ToolCallFinished`, `ProviderCallStarted`, `ProviderCallFinished`,
//! `CacheHit`, `CacheMiss`, `MemoryStored`, `MemoryRecalled`,
//! `ChannelMessageReceived`, and `ChannelMessageSent`. Variants carry just
//! enough context for tracing and diagnostics without leaking sensitive
//! prompt or response content.
//!
//! The [`Observer`] trait exposes `record_event(&self, event: &ObserverEvent)`
//! plus span start/end hooks for distributed tracing. It is bounded
//! `Send + Sync + 'static` so the agent can store it in `Arc<dyn Observer>`
//! and share it across threads and tokio tasks. Every implementor is
//! responsible for its own buffering, batching, and export — the agent just
//! fires events and moves on, so a slow backend never blocks the orchestration
//! loop. Events are structured and cheap to clone/copy, eliminating event
//! loss on slow consumers.
//!
//! Call sites are spread across the codebase: see `Agent::turn`,
//! `Agent::execute_tools`, `AnthropicProvider::chat`, `SqliteMemory::store`,
//! and `TelegramChannel::send` for representative emit points. Almost every
//! subsystem fires at least one event.
//!
//! ## Key types
//! - [`ObserverEvent`] — every lifecycle event the agent emits
//! - [`Observer`] — trait implemented by all backends
//!
//! ## Related
//! - `src/observability/mod.rs` — module index, factory, re-exports
//! - `src/observability/multi.rs` — `MultiObserver` fan-out
//! - `src/agent/mod.rs` — main `record_event` call sites

use std::time::Duration;

/// Discrete events emitted by the agent runtime for observability.
///
/// Each variant represents a lifecycle event that observers can record,
/// aggregate, or forward to external monitoring systems. Events carry
/// just enough context for tracing and diagnostics without exposing
/// sensitive prompt or response content.
#[derive(Debug, Clone)]
pub enum ObserverEvent {
    /// The agent orchestration loop has started a new session.
    AgentStart { provider: String, model: String },
    /// A request is about to be sent to an LLM provider.
    ///
    /// This is emitted immediately before a provider call so observers can print
    /// user-facing progress without leaking prompt contents.
    LlmRequest {
        provider: String,
        model: String,
        messages_count: usize,
    },
    /// Result of a single LLM provider call.
    LlmResponse {
        provider: String,
        model: String,
        duration: Duration,
        success: bool,
        error_message: Option<String>,
        input_tokens: Option<u64>,
        output_tokens: Option<u64>,
    },
    /// The agent session has finished.
    ///
    /// Carries aggregate usage data (tokens, cost) when the provider reports it.
    AgentEnd {
        provider: String,
        model: String,
        duration: Duration,
        tokens_used: Option<u64>,
        cost_usd: Option<f64>,
    },
    /// A tool call is about to be executed.
    ToolCallStart {
        tool: String,
        arguments: Option<String>,
    },
    /// A tool call has completed with a success/failure outcome.
    ToolCall {
        tool: String,
        duration: Duration,
        success: bool,
    },
    /// The agent produced a final answer for the current user message.
    TurnComplete,
    /// A message was sent or received through a channel.
    ChannelMessage {
        /// Channel name (e.g., `"telegram"`, `"discord"`).
        channel: String,
        /// `"inbound"` or `"outbound"`.
        direction: String,
    },
    /// Periodic heartbeat tick from the runtime keep-alive loop.
    HeartbeatTick,
    /// Response cache hit — an LLM call was avoided.
    CacheHit {
        /// `"hot"` (in-memory) or `"warm"` (SQLite).
        cache_type: String,
        /// Estimated tokens saved by this cache hit.
        tokens_saved: u64,
    },
    /// Response cache miss — the prompt was not found in cache.
    CacheMiss {
        /// `"response"` cache layer that was checked.
        cache_type: String,
    },
    /// An error occurred in a named component.
    Error {
        /// Subsystem where the error originated (e.g., `"provider"`, `"gateway"`).
        component: String,
        /// Human-readable error description. Must not contain secrets or tokens.
        message: String,
    },
    /// A hand has started execution.
    HandStarted { hand_name: String },
    /// A hand has completed execution successfully.
    HandCompleted {
        hand_name: String,
        duration_ms: u64,
        findings_count: usize,
    },
    /// A hand has failed during execution.
    HandFailed {
        hand_name: String,
        error: String,
        duration_ms: u64,
    },
    /// A deployment has started.
    DeploymentStarted {
        /// Identifier for the deployment (e.g., commit SHA or release tag).
        deploy_id: String,
    },
    /// A deployment has completed successfully.
    DeploymentCompleted {
        deploy_id: String,
        /// Commit SHA that was deployed.
        commit_sha: String,
    },
    /// A deployment has failed.
    DeploymentFailed {
        deploy_id: String,
        /// Human-readable failure reason.
        reason: String,
    },
    /// Recovery from a failed deployment has completed.
    RecoveryCompleted { deploy_id: String },
}

/// Numeric metrics emitted by the agent runtime.
///
/// Observers can aggregate these into dashboards, alerts, or structured logs.
/// Each variant carries a single scalar value with implicit units.
#[derive(Debug, Clone)]
pub enum ObserverMetric {
    /// Time elapsed for a single LLM or tool request.
    RequestLatency(Duration),
    /// Number of tokens consumed by an LLM call.
    TokensUsed(u64),
    /// Current number of active concurrent sessions.
    ActiveSessions(u64),
    /// Current depth of the inbound message queue.
    QueueDepth(u64),
    /// Duration of a single hand run.
    HandRunDuration {
        hand_name: String,
        duration: Duration,
    },
    /// Number of findings produced by a hand run.
    HandFindingsCount { hand_name: String, count: u64 },
    /// Records a hand run outcome for success-rate tracking.
    HandSuccessRate { hand_name: String, success: bool },
    /// Time elapsed from commit to deployment (lead time for changes).
    DeploymentLeadTime(Duration),
    /// Time elapsed to recover from a failed deployment.
    RecoveryTime(Duration),
}

/// Core observability trait for recording agent runtime telemetry.
///
/// Implement this trait to integrate with any monitoring backend (structured
/// logging, Prometheus, OpenTelemetry, etc.). The agent runtime holds one or
/// more `Observer` instances and calls [`record_event`](Observer::record_event)
/// and [`record_metric`](Observer::record_metric) at key lifecycle points.
///
/// Implementations must be `Send + Sync + 'static` because the observer is
/// shared across async tasks via `Arc`.
pub trait Observer: Send + Sync + 'static {
    /// Record a discrete lifecycle event.
    ///
    /// Called synchronously on the hot path; implementations should avoid
    /// blocking I/O. Buffer events internally and flush asynchronously
    /// when possible.
    fn record_event(&self, event: &ObserverEvent);

    /// Record a numeric metric sample.
    ///
    /// Called synchronously; same non-blocking guidance as
    /// [`record_event`](Observer::record_event).
    fn record_metric(&self, metric: &ObserverMetric);

    /// Flush any buffered telemetry data to the backend.
    ///
    /// The runtime calls this during graceful shutdown. The default
    /// implementation is a no-op, which is appropriate for backends
    /// that write synchronously.
    fn flush(&self) {}

    /// Return the human-readable name of this observer backend.
    ///
    /// Used in logs and diagnostics (e.g., `"console"`, `"prometheus"`,
    /// `"opentelemetry"`).
    fn name(&self) -> &str;

    /// Downcast to `Any` for backend-specific operations.
    ///
    /// Enables callers to access concrete observer types when needed
    /// (e.g., retrieving a Prometheus registry handle for custom metrics).
    fn as_any(&self) -> &dyn std::any::Any;
}


#[cfg(test)]
#[path = "traits.test.rs"]
mod tests;
