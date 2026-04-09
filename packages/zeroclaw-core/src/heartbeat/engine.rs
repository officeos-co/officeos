use crate::config::HeartbeatConfig;
use crate::observability::{Observer, ObserverEvent};
use anyhow::Result;
use chrono::{DateTime, Utc};
use parking_lot::Mutex as ParkingMutex;
use serde::{Deserialize, Serialize};
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

/// A structured heartbeat task with priority and status metadata.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct HeartbeatTask {
    pub text: String,
    pub priority: TaskPriority,
    pub status: TaskStatus,
}

impl HeartbeatTask {
    pub fn is_runnable(&self) -> bool {
        self.status == TaskStatus::Active
    }
}

impl fmt::Display for HeartbeatTask {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "[{}] {}", self.priority, self.text)
    }
}

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

    /// Parse tasks from HEARTBEAT.md with structured metadata support.
    ///
    /// Supports both legacy flat format and new structured format:
    ///
    /// Legacy:
    ///   `- Check email`  →  medium priority, active status
    ///
    /// Structured:
    ///   `- [high] Check email`           →  high priority, active
    ///   `- [low|paused] Review old PRs`  →  low priority, paused
    ///   `- [completed] Old task`         →  medium priority, completed
    fn parse_tasks(content: &str) -> Vec<HeartbeatTask> {
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
                    };
                }
            }
        }
        // No metadata — default to medium/active
        HeartbeatTask {
            text: text.to_string(),
            priority: TaskPriority::Medium,
            status: TaskStatus::Active,
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
             Be conservative — skip if tasks are routine and not time-sensitive.",
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
                           # Examples:\n\
                           # - [high] Check my email for important messages\n\
                           # - Review my calendar for upcoming events\n\
                           # - [low|paused] Check the weather forecast\n";
            tokio::fs::write(&path, default).await?;
        }
        Ok(())
    }
}

#[cfg(test)]
#[path = "engine.test.rs"]
mod tests;
