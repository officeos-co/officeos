use crate::config::HeartbeatConfig;
use crate::observability::{Observer, ObserverEvent};
use anyhow::Result;
use chrono::{DateTime, Utc};
use parking_lot::Mutex as ParkingMutex;
use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::fmt;
use std::path::Path;
use std::sync::Arc;
use tokio::time::{self, Duration};
use tracing::{info, warn};

// ── Structured task types ────────────────────────────────────────

/// Priority level for a heartbeat task.
#[derive(Debug, Clone, Copy, PartialEq, Eq, PartialOrd, Ord, Serialize, Deserialize)]
#[serde(rename_all = "lowercase")]
pub enum TaskPriority {
    Low,
    Medium,
    High,
}

impl fmt::Display for TaskPriority {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            Self::Low => write!(f, "low"),
            Self::Medium => write!(f, "medium"),
            Self::High => write!(f, "high"),
        }
    }
}

/// Status of a heartbeat task.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "lowercase")]
pub enum TaskStatus {
    Active,
    Paused,
    Completed,
}

impl fmt::Display for TaskStatus {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            Self::Active => write!(f, "active"),
            Self::Paused => write!(f, "paused"),
            Self::Completed => write!(f, "completed"),
        }
    }
}

/// A structured heartbeat task with priority, status, optional per-task interval,
/// and an optional name for tracking.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct HeartbeatTask {
    pub text: String,
    pub priority: TaskPriority,
    pub status: TaskStatus,
    /// Optional unique name for this task (used for per-task state tracking).
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub name: Option<String>,
    /// Optional per-task interval in seconds. When set, this task only runs
    /// when at least this many seconds have elapsed since its last execution.
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub interval_secs: Option<u64>,
    /// Optional override prompt for this task. When set, this prompt is used
    /// instead of the task `text` when invoking the LLM.
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub prompt: Option<String>,
}

impl HeartbeatTask {
    pub fn is_runnable(&self) -> bool {
        self.status == TaskStatus::Active
    }

    /// Return the display name: explicit name, or a slugified version of the text.
    pub fn display_name(&self) -> &str {
        self.name.as_deref().unwrap_or(&self.text)
    }

    /// Return the effective prompt: explicit prompt, or the task text.
    pub fn effective_prompt(&self) -> &str {
        self.prompt.as_deref().unwrap_or(&self.text)
    }
}

impl fmt::Display for HeartbeatTask {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        if let Some(ref name) = self.name {
            write!(f, "[{}] {} ({})", self.priority, name, self.text)
        } else {
            write!(f, "[{}] {}", self.priority, self.text)
        }
    }
}

// ── Per-task state tracking ────────────────────────────────────

/// Persisted state for a single heartbeat task, keyed by task name.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct HeartbeatTaskState {
    pub last_run_at: Option<DateTime<Utc>>,
    pub run_count: u64,
}

impl Default for HeartbeatTaskState {
    fn default() -> Self {
        Self {
            last_run_at: None,
            run_count: 0,
        }
    }
}

/// Persistent store for per-task heartbeat state.
#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct HeartbeatState {
    pub tasks: HashMap<String, HeartbeatTaskState>,
}

impl HeartbeatState {
    /// Load state from the workspace JSON file, or return default.
    pub fn load(workspace_dir: &Path) -> Self {
        let path = workspace_dir.join("heartbeat").join("task_state.json");
        match std::fs::read_to_string(&path) {
            Ok(content) => serde_json::from_str(&content).unwrap_or_default(),
            Err(_) => Self::default(),
        }
    }

    /// Persist state to the workspace JSON file.
    pub fn save(&self, workspace_dir: &Path) -> Result<()> {
        let dir = workspace_dir.join("heartbeat");
        std::fs::create_dir_all(&dir)?;
        let path = dir.join("task_state.json");
        let content = serde_json::to_string_pretty(self)?;
        std::fs::write(&path, content)?;
        Ok(())
    }

    /// Record that a task ran at the given time.
    pub fn record_run(&mut self, task_key: &str, at: DateTime<Utc>) {
        let entry = self.tasks.entry(task_key.to_string()).or_default();
        entry.last_run_at = Some(at);
        entry.run_count += 1;
    }

    /// Check if a task is due based on its interval.
    pub fn is_due(&self, task_key: &str, interval_secs: u64) -> bool {
        match self.tasks.get(task_key) {
            None => true, // never run
            Some(state) => match state.last_run_at {
                None => true,
                Some(last) => {
                    let elapsed = Utc::now().signed_duration_since(last);
                    elapsed.num_seconds() >= interval_secs as i64
                }
            },
        }
    }
}

// ── HEARTBEAT_OK response contract ─────────────────────────────

/// Default max chars for suppressing near-empty heartbeat responses.
const DEFAULT_ACK_MAX_CHARS: usize = 300;

/// Strip `HEARTBEAT_OK` from a response and suppress if remaining content
/// is short enough. Returns `None` if the message should be suppressed.
pub fn filter_heartbeat_response(response: &str, ack_max_chars: Option<usize>) -> Option<String> {
    let max = ack_max_chars.unwrap_or(DEFAULT_ACK_MAX_CHARS);
    let cleaned = response.replace("HEARTBEAT_OK", "").trim().to_string();
    if cleaned.len() <= max {
        return None;
    }
    Some(cleaned)
}

/// Default heartbeat prompt suffix instructing the LLM to respond with
/// HEARTBEAT_OK if nothing needs attention.
pub const HEARTBEAT_OK_SUFFIX: &str =
    "\n\nIf nothing needs attention, reply HEARTBEAT_OK.";

// ── Health Metrics ───────────────────────────────────────────────

/// Live health metrics for the heartbeat subsystem.
///
/// Shared via `Arc<ParkingMutex<>>` between the heartbeat worker,
/// deadman watcher, and API consumers.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct HeartbeatMetrics {
    /// Monotonic uptime since the heartbeat loop started.
    pub uptime_secs: u64,
    /// Consecutive successful ticks (resets on failure).
    pub consecutive_successes: u64,
    /// Consecutive failed ticks (resets on success).
    pub consecutive_failures: u64,
    /// Timestamp of the most recent tick (UTC RFC 3339).
    pub last_tick_at: Option<DateTime<Utc>>,
    /// Exponential moving average of tick durations in milliseconds.
    pub avg_tick_duration_ms: f64,
    /// Total number of ticks executed since startup.
    pub total_ticks: u64,
}

impl Default for HeartbeatMetrics {
    fn default() -> Self {
        Self {
            uptime_secs: 0,
            consecutive_successes: 0,
            consecutive_failures: 0,
            last_tick_at: None,
            avg_tick_duration_ms: 0.0,
            total_ticks: 0,
        }
    }
}

impl HeartbeatMetrics {
    /// Record a successful tick with the given duration.
    pub fn record_success(&mut self, duration_ms: f64) {
        self.consecutive_successes += 1;
        self.consecutive_failures = 0;
        self.last_tick_at = Some(Utc::now());
        self.total_ticks += 1;
        self.update_avg_duration(duration_ms);
    }

    /// Record a failed tick with the given duration.
    pub fn record_failure(&mut self, duration_ms: f64) {
        self.consecutive_failures += 1;
        self.consecutive_successes = 0;
        self.last_tick_at = Some(Utc::now());
        self.total_ticks += 1;
        self.update_avg_duration(duration_ms);
    }

    fn update_avg_duration(&mut self, duration_ms: f64) {
        const ALPHA: f64 = 0.3; // EMA smoothing factor
        if self.total_ticks == 1 {
            self.avg_tick_duration_ms = duration_ms;
        } else {
            self.avg_tick_duration_ms =
                ALPHA * duration_ms + (1.0 - ALPHA) * self.avg_tick_duration_ms;
        }
    }
}

/// Compute the adaptive interval for the next heartbeat tick.
///
/// Strategy:
/// - On failures: exponential back-off `base * 2^failures` capped at `max_interval`.
/// - When high-priority tasks are present: use `min_interval` for faster reaction.
/// - Otherwise: use `base_interval`.
pub fn compute_adaptive_interval(
    base_minutes: u32,
    min_minutes: u32,
    max_minutes: u32,
    consecutive_failures: u64,
    has_high_priority_tasks: bool,
) -> u32 {
    if consecutive_failures > 0 {
        let backoff = base_minutes.saturating_mul(
            1u32.checked_shl(consecutive_failures.min(10) as u32)
                .unwrap_or(u32::MAX),
        );
        return backoff.min(max_minutes).max(min_minutes);
    }

    if has_high_priority_tasks {
        return min_minutes.max(5); // never go below 5 minutes
    }

    base_minutes.clamp(min_minutes, max_minutes)
}

// ── Duration parsing ────────────────────────────────────────────

/// Parse a human-readable duration string into seconds.
/// Supports: `30m`, `6h`, `1d`, `90s`, `2h30m`, `1.5h`, or plain seconds.
pub fn parse_duration_str(s: &str) -> u64 {
    let s = s.trim().to_ascii_lowercase();
    let mut total_secs: u64 = 0;
    let mut num_buf = String::new();

    for ch in s.chars() {
        if ch.is_ascii_digit() || ch == '.' {
            num_buf.push(ch);
        } else {
            let n: f64 = num_buf.parse().unwrap_or(0.0);
            num_buf.clear();
            match ch {
                's' => total_secs += n as u64,
                'm' => total_secs += (n * 60.0) as u64,
                'h' => total_secs += (n * 3600.0) as u64,
                'd' => total_secs += (n * 86400.0) as u64,
                _ => {}
            }
        }
    }

    // If only digits remain, treat as seconds
    if !num_buf.is_empty() {
        if let Ok(n) = num_buf.parse::<u64>() {
            total_secs += n;
        }
    }

    total_secs.max(1) // minimum 1 second
}

/// Serialize a duration in seconds to a human-readable string.
pub fn format_duration_secs(secs: u64) -> String {
    if secs >= 86400 && secs % 86400 == 0 {
        format!("{}d", secs / 86400)
    } else if secs >= 3600 && secs % 3600 == 0 {
        format!("{}h", secs / 3600)
    } else if secs >= 60 && secs % 60 == 0 {
        format!("{}m", secs / 60)
    } else {
        format!("{secs}s")
    }
}

// ── Engine ───────────────────────────────────────────────────────

/// Heartbeat engine — reads HEARTBEAT.md and executes tasks periodically
pub struct HeartbeatEngine {
    config: HeartbeatConfig,
    workspace_dir: std::path::PathBuf,
    observer: Arc<dyn Observer>,
    metrics: Arc<ParkingMutex<HeartbeatMetrics>>,
}

impl HeartbeatEngine {
    pub fn new(
        config: HeartbeatConfig,
        workspace_dir: std::path::PathBuf,
        observer: Arc<dyn Observer>,
    ) -> Self {
        Self {
            config,
            workspace_dir,
            observer,
            metrics: Arc::new(ParkingMutex::new(HeartbeatMetrics::default())),
        }
    }

    /// Get a shared handle to the live heartbeat metrics.
    pub fn metrics(&self) -> Arc<ParkingMutex<HeartbeatMetrics>> {
        Arc::clone(&self.metrics)
    }

    /// Start the heartbeat loop (runs until cancelled)
    pub async fn run(&self) -> Result<()> {
        if !self.config.enabled {
            info!("Heartbeat disabled");
            return Ok(());
        }

        let interval_mins = self.config.interval_minutes.max(1);
        info!("💓 Heartbeat started: every {} minutes", interval_mins);

        let mut interval = time::interval(Duration::from_secs(u64::from(interval_mins) * 60));

        loop {
            interval.tick().await;
            self.observer.record_event(&ObserverEvent::HeartbeatTick);

            match self.tick().await {
                Ok(tasks) => {
                    if tasks > 0 {
                        info!("💓 Heartbeat: processed {} tasks", tasks);
                    }
                }
                Err(e) => {
                    warn!("💓 Heartbeat error: {}", e);
                    self.observer.record_event(&ObserverEvent::Error {
                        component: "heartbeat".into(),
                        message: e.to_string(),
                    });
                }
            }
        }
    }

    /// Single heartbeat tick — read HEARTBEAT.md and return task count
    async fn tick(&self) -> Result<usize> {
        Ok(self.collect_tasks().await?.len())
    }

    /// Read HEARTBEAT.md and return all parsed structured tasks.
    pub async fn collect_tasks(&self) -> Result<Vec<HeartbeatTask>> {
        let heartbeat_path = self.workspace_dir.join("HEARTBEAT.md");
        if !heartbeat_path.exists() {
            return Ok(Vec::new());
        }
        let content = tokio::fs::read_to_string(&heartbeat_path).await?;
        Ok(Self::parse_tasks(&content))
    }

    /// Collect only runnable (active) tasks, sorted by priority (high first).
    pub async fn collect_runnable_tasks(&self) -> Result<Vec<HeartbeatTask>> {
        let mut tasks: Vec<HeartbeatTask> = self
            .collect_tasks()
            .await?
            .into_iter()
            .filter(HeartbeatTask::is_runnable)
            .collect();
        // Sort by priority descending (High > Medium > Low)
        tasks.sort_by(|a, b| b.priority.cmp(&a.priority));
        Ok(tasks)
    }

    /// Collect only tasks that are both runnable and due (per-task interval check).
    /// Tasks without an interval are always considered due.
    pub async fn collect_due_tasks(&self) -> Result<Vec<HeartbeatTask>> {
        let state = HeartbeatState::load(&self.workspace_dir);
        let tasks = self.collect_runnable_tasks().await?;
        let due: Vec<HeartbeatTask> = tasks
            .into_iter()
            .filter(|task| {
                match task.interval_secs {
                    None => true, // no interval = always due
                    Some(interval) => {
                        let key = task.name.as_deref().unwrap_or(&task.text);
                        state.is_due(key, interval)
                    }
                }
            })
            .collect();
        Ok(due)
    }

    /// Parse tasks from HEARTBEAT.md with structured metadata support.
    ///
    /// Supports three formats:
    ///
    /// 1. Legacy flat:
    ///    `- Check email`  →  medium priority, active status
    ///
    /// 2. Structured metadata:
    ///    `- [high] Check email`           →  high priority, active
    ///    `- [low|paused] Review old PRs`  →  low priority, paused
    ///
    /// 3. YAML `tasks:` block:
    ///    ```yaml
    ///    tasks:
    ///    - name: inbox-triage
    ///      interval: 30m
    ///      prompt: "Check for urgent unread emails"
    ///    ```
    pub fn parse_tasks(content: &str) -> Vec<HeartbeatTask> {
        // Check for YAML tasks block
        let yaml_tasks = Self::parse_yaml_tasks(content);
        if !yaml_tasks.is_empty() {
            return yaml_tasks;
        }

        // Legacy/structured format
        content
            .lines()
            .filter_map(|line| {
                let trimmed = line.trim();
                let text = trimmed.strip_prefix("- ")?;
                if text.is_empty() {
                    return None;
                }
                Some(Self::parse_task_line(text))
            })
            .collect()
    }

    /// Parse a YAML-style `tasks:` block from HEARTBEAT.md content.
    ///
    /// Supports format:
    /// ```
    /// tasks:
    /// - name: inbox-triage
    ///   interval: 30m
    ///   prompt: "Check for urgent unread emails"
    ///   priority: high
    ///   status: active
    /// ```
    fn parse_yaml_tasks(content: &str) -> Vec<HeartbeatTask> {
        let mut tasks = Vec::new();
        let lines: Vec<&str> = content.lines().collect();

        // Find the "tasks:" line
        let tasks_start = lines.iter().position(|l| l.trim() == "tasks:");
        let tasks_start = match tasks_start {
            Some(i) => i + 1,
            None => return tasks,
        };

        let mut i = tasks_start;
        while i < lines.len() {
            let line = lines[i].trim();

            // Stop at non-indented non-empty line that isn't a YAML list item
            if !line.is_empty() && !line.starts_with('-') && !line.starts_with(' ') && !line.starts_with('#') {
                break;
            }

            // Look for list item start "- name: ..." or "- ..."
            if let Some(rest) = line.strip_prefix("- ") {
                let mut name: Option<String> = None;
                let mut interval_secs: Option<u64> = None;
                let mut prompt: Option<String> = None;
                let mut priority = TaskPriority::Medium;
                let mut status = TaskStatus::Active;
                let mut text = String::new();

                // Parse the first line of the item
                Self::parse_yaml_kv(rest, &mut name, &mut interval_secs, &mut prompt, &mut priority, &mut status, &mut text);

                // Parse continuation lines (indented properties)
                i += 1;
                while i < lines.len() {
                    let cont = lines[i];
                    let cont_trimmed = cont.trim();
                    // Continuation must be indented and not a new list item
                    if cont_trimmed.is_empty() {
                        i += 1;
                        continue;
                    }
                    if cont_trimmed.starts_with("- ") || (!cont.starts_with(' ') && !cont.starts_with('\t')) {
                        break;
                    }
                    Self::parse_yaml_kv(cont_trimmed, &mut name, &mut interval_secs, &mut prompt, &mut priority, &mut status, &mut text);
                    i += 1;
                }

                // Build the task
                let effective_text = if text.is_empty() {
                    prompt.clone().or_else(|| name.clone()).unwrap_or_default()
                } else {
                    text
                };

                if !effective_text.is_empty() || name.is_some() {
                    tasks.push(HeartbeatTask {
                        text: effective_text,
                        priority,
                        status,
                        name,
                        interval_secs,
                        prompt,
                    });
                }
                continue;
            }

            i += 1;
        }

        tasks
    }

    /// Parse a single YAML key-value pair from a line.
    fn parse_yaml_kv(
        line: &str,
        name: &mut Option<String>,
        interval_secs: &mut Option<u64>,
        prompt: &mut Option<String>,
        priority: &mut TaskPriority,
        status: &mut TaskStatus,
        text: &mut String,
    ) {
        if let Some((key, val)) = line.split_once(':') {
            let key = key.trim();
            let val = val.trim().trim_matches('"').trim_matches('\'');
            match key {
                "name" => *name = Some(val.to_string()),
                "interval" => *interval_secs = Some(parse_duration_str(val)),
                "prompt" => *prompt = Some(val.to_string()),
                "priority" => {
                    *priority = match val.to_ascii_lowercase().as_str() {
                        "high" => TaskPriority::High,
                        "low" => TaskPriority::Low,
                        _ => TaskPriority::Medium,
                    };
                }
                "status" => {
                    *status = match val.to_ascii_lowercase().as_str() {
                        "paused" | "pause" => TaskStatus::Paused,
                        "completed" | "complete" | "done" => TaskStatus::Completed,
                        _ => TaskStatus::Active,
                    };
                }
                "text" => *text = val.to_string(),
                _ => {}
            }
        }
    }

    /// Parse a single task line into a structured `HeartbeatTask`.
    ///
    /// Format: `[priority|status] task text` or just `task text`.
    fn parse_task_line(text: &str) -> HeartbeatTask {
        if let Some(rest) = text.strip_prefix('[') {
            if let Some((meta, task_text)) = rest.split_once(']') {
                let task_text = task_text.trim();
                if !task_text.is_empty() {
                    let (priority, status) = Self::parse_meta(meta);
                    return HeartbeatTask {
                        text: task_text.to_string(),
                        priority,
                        status,
                        name: None,
                        interval_secs: None,
                        prompt: None,
                    };
                }
            }
        }
        // No metadata — default to medium/active
        HeartbeatTask {
            text: text.to_string(),
            priority: TaskPriority::Medium,
            status: TaskStatus::Active,
            name: None,
            interval_secs: None,
            prompt: None,
        }
    }

    /// Parse metadata tags like `high`, `low|paused`, `completed`.
    fn parse_meta(meta: &str) -> (TaskPriority, TaskStatus) {
        let mut priority = TaskPriority::Medium;
        let mut status = TaskStatus::Active;

        for part in meta.split('|') {
            match part.trim().to_ascii_lowercase().as_str() {
                "high" => priority = TaskPriority::High,
                "medium" | "med" => priority = TaskPriority::Medium,
                "low" => priority = TaskPriority::Low,
                "active" => status = TaskStatus::Active,
                "paused" | "pause" => status = TaskStatus::Paused,
                "completed" | "complete" | "done" => status = TaskStatus::Completed,
                _ => {}
            }
        }

        (priority, status)
    }

    /// Build the Phase 1 LLM decision prompt for two-phase heartbeat.
    pub fn build_decision_prompt(tasks: &[HeartbeatTask]) -> String {
        let mut prompt = String::from(
            "You are a heartbeat scheduler. Review the following periodic tasks and decide \
             whether any should be executed right now.\n\n\
             Consider:\n\
             - Task priority (high tasks are more urgent)\n\
             - Whether the task is time-sensitive or can wait\n\
             - Whether running the task now would provide value\n\n\
             Tasks:\n",
        );

        for (i, task) in tasks.iter().enumerate() {
            use std::fmt::Write;
            let _ = writeln!(prompt, "{}. [{}] {}", i + 1, task.priority, task.text);
        }

        prompt.push_str(
            "\nRespond with ONLY one of:\n\
             - `run: 1,2,3` (comma-separated task numbers to execute)\n\
             - `skip` (nothing needs to run right now)\n\n\
             Be conservative — skip if tasks are routine and not time-sensitive.\n\
             If nothing needs attention, reply HEARTBEAT_OK.",
        );

        prompt
    }

    /// Parse the Phase 1 LLM decision response.
    ///
    /// Returns indices of tasks to run, or empty vec if skipped.
    pub fn parse_decision_response(response: &str, task_count: usize) -> Vec<usize> {
        let trimmed = response.trim().to_ascii_lowercase();

        if trimmed == "skip" || trimmed.starts_with("skip") {
            return Vec::new();
        }

        // Look for "run: 1,2,3" pattern
        let numbers_part = if let Some(after_run) = trimmed.strip_prefix("run:") {
            after_run.trim()
        } else if let Some(after_run) = trimmed.strip_prefix("run ") {
            after_run.trim()
        } else {
            // Try to parse as bare numbers
            trimmed.as_str()
        };

        numbers_part
            .split(',')
            .filter_map(|s| {
                let n: usize = s.trim().parse().ok()?;
                if n >= 1 && n <= task_count {
                    Some(n - 1) // Convert to 0-indexed
                } else {
                    None
                }
            })
            .collect()
    }

    /// Create a default HEARTBEAT.md if it doesn't exist
    pub async fn ensure_heartbeat_file(workspace_dir: &Path) -> Result<()> {
        let path = workspace_dir.join("HEARTBEAT.md");
        if !path.exists() {
            let default = "# Periodic Tasks\n\n\
                           # Add tasks below (one per line, starting with `- `)\n\
                           # The agent will check this file on each heartbeat tick.\n\
                           #\n\
                           # Format: - [priority|status] Task description\n\
                           #   priority: high, medium (default), low\n\
                           #   status:   active (default), paused, completed\n\
                           #\n\
                           # Or use YAML tasks: block for per-task intervals:\n\
                           #   tasks:\n\
                           #   - name: inbox-triage\n\
                           #     interval: 30m\n\
                           #     prompt: \"Check for urgent unread emails\"\n\
                           #     priority: high\n\
                           #\n\
                           # Examples:\n\
                           # - [high] Check my email for important messages\n\
                           # - Review my calendar for upcoming events\n\
                           # - [low|paused] Check the weather forecast\n";
            tokio::fs::write(&path, default).await?;
        }
        Ok(())
    }

    /// Serialize tasks back to HEARTBEAT.md format (YAML tasks: block).
    pub fn serialize_tasks(tasks: &[HeartbeatTask]) -> String {
        let mut out = String::from("# Periodic Tasks\n\ntasks:\n");
        for task in tasks {
            let name = task.name.as_deref().unwrap_or(&task.text);
            out.push_str(&format!("- name: \"{}\"\n", name));
            if let Some(interval) = task.interval_secs {
                out.push_str(&format!("  interval: {}\n", format_duration_secs(interval)));
            }
            if let Some(ref prompt) = task.prompt {
                out.push_str(&format!("  prompt: \"{}\"\n", prompt));
            } else {
                out.push_str(&format!("  prompt: \"{}\"\n", task.text));
            }
            out.push_str(&format!("  priority: {}\n", task.priority));
            out.push_str(&format!("  status: {}\n", task.status));
        }
        out
    }

    /// Read tasks from HEARTBEAT.md at the given workspace path.
    pub fn read_tasks_from_file(workspace_dir: &Path) -> Result<Vec<HeartbeatTask>> {
        let path = workspace_dir.join("HEARTBEAT.md");
        if !path.exists() {
            return Ok(Vec::new());
        }
        let content = std::fs::read_to_string(&path)?;
        Ok(Self::parse_tasks(&content))
    }

    /// Write tasks to HEARTBEAT.md at the given workspace path.
    pub fn write_tasks_to_file(workspace_dir: &Path, tasks: &[HeartbeatTask]) -> Result<()> {
        let path = workspace_dir.join("HEARTBEAT.md");
        let content = Self::serialize_tasks(tasks);
        std::fs::write(&path, content)?;
        Ok(())
    }
}

#[cfg(test)]
#[path = "engine.test.rs"]
mod tests;
