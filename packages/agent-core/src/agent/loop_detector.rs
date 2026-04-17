//! Loop detection guardrail for the agent tool-call loop.
//! Port of `packages/zeroclaw-core/src/agent/loop_detector.rs`.
//! See API.md §6.2: 3 patterns, window size 20, hardcoded config.

use std::collections::hash_map::DefaultHasher;
use std::collections::{HashSet, VecDeque};
use std::hash::{Hash, Hasher};

// ── Result enum ──────────────────────────────────────────────────

/// Outcome of a loop-detection check after recording a tool call.
#[derive(Debug, Clone, PartialEq, Eq)]
pub enum LoopDetectionResult {
    /// No pattern detected.
    Ok,
    /// Suspicious pattern — caller should nudge the model.
    Warning(String),
    /// Tool call should be refused (replace output with error).
    Block(String),
    /// Turn should be terminated immediately.
    Break(String),
}

// ── Internal types ───────────────────────────────────────────────

#[derive(Debug, Clone)]
struct ToolCallRecord {
    name: String,
    args_hash: u64,
    result_hash: u64,
}

fn hash_value(value: &serde_json::Value) -> u64 {
    let mut hasher = DefaultHasher::new();
    let canonical = serde_json::to_string(&canonicalise(value)).unwrap_or_default();
    canonical.hash(&mut hasher);
    hasher.finish()
}

fn canonicalise(value: &serde_json::Value) -> serde_json::Value {
    match value {
        serde_json::Value::Object(map) => {
            let mut sorted: Vec<(&String, &serde_json::Value)> = map.iter().collect();
            sorted.sort_by_key(|(k, _)| *k);
            let new_map: serde_json::Map<String, serde_json::Value> = sorted
                .into_iter()
                .map(|(k, v)| (k.clone(), canonicalise(v)))
                .collect();
            serde_json::Value::Object(new_map)
        }
        serde_json::Value::Array(arr) => {
            serde_json::Value::Array(arr.iter().map(canonicalise).collect())
        }
        other => other.clone(),
    }
}

fn hash_str(s: &str) -> u64 {
    let mut hasher = DefaultHasher::new();
    s.hash(&mut hasher);
    hasher.finish()
}

// ── Detector ─────────────────────────────────────────────────────

const WINDOW_SIZE: usize = 20;
const MAX_REPEATS: usize = 3;

/// Stateful loop detector — lives for the duration of a single turn.
pub struct LoopDetector {
    window: VecDeque<ToolCallRecord>,
}

impl Default for LoopDetector {
    fn default() -> Self {
        Self::new()
    }
}

impl LoopDetector {
    pub fn new() -> Self {
        Self {
            window: VecDeque::with_capacity(WINDOW_SIZE),
        }
    }

    /// Record a completed tool call and check for loop patterns.
    pub fn record(
        &mut self,
        name: &str,
        args: &serde_json::Value,
        result: &str,
    ) -> LoopDetectionResult {
        let record = ToolCallRecord {
            name: name.to_string(),
            args_hash: hash_value(args),
            result_hash: hash_str(result),
        };

        if self.window.len() >= WINDOW_SIZE {
            self.window.pop_front();
        }
        self.window.push_back(record);

        // Most severe first.
        if let Some(r) = self.detect_exact_repeat() {
            return r;
        }
        if let Some(r) = self.detect_ping_pong() {
            return r;
        }
        if let Some(r) = self.detect_no_progress() {
            return r;
        }

        LoopDetectionResult::Ok
    }

    /// Pattern 1: Same tool + same args N+ times consecutively.
    fn detect_exact_repeat(&self) -> Option<LoopDetectionResult> {
        if self.window.len() < MAX_REPEATS {
            return None;
        }
        let last = self.window.back()?;
        let consecutive = self
            .window
            .iter()
            .rev()
            .take_while(|r| r.name == last.name && r.args_hash == last.args_hash)
            .count();

        if consecutive >= MAX_REPEATS + 2 {
            Some(LoopDetectionResult::Break(format!(
                "Circuit breaker: tool '{}' called {} times with identical arguments",
                last.name, consecutive
            )))
        } else if consecutive > MAX_REPEATS {
            Some(LoopDetectionResult::Block(format!(
                "Blocked: tool '{}' called {} times with identical arguments",
                last.name, consecutive
            )))
        } else if consecutive >= MAX_REPEATS {
            Some(LoopDetectionResult::Warning(format!(
                "Warning: tool '{}' called {} times with identical arguments. Try a different approach.",
                last.name, consecutive
            )))
        } else {
            None
        }
    }

    /// Pattern 2: Two tools alternating A->B->A->B for 4+ cycles.
    fn detect_ping_pong(&self) -> Option<LoopDetectionResult> {
        const MIN_CYCLES: usize = 4;
        let needed = MIN_CYCLES * 2;
        if self.window.len() < needed {
            return None;
        }

        let tail: Vec<&ToolCallRecord> = self.window.iter().rev().take(needed).collect();
        let a_name = &tail[0].name;
        let b_name = &tail[1].name;
        if a_name == b_name {
            return None;
        }

        let is_ping_pong = tail.iter().enumerate().all(|(i, r)| {
            if i % 2 == 0 {
                &r.name == a_name
            } else {
                &r.name == b_name
            }
        });
        if !is_ping_pong {
            return None;
        }

        Some(LoopDetectionResult::Warning(format!(
            "Warning: tools '{}' and '{}' alternating for {} cycles.",
            a_name, b_name, MIN_CYCLES
        )))
    }

    /// Pattern 3: Same tool 5+ times, different args, identical result hash.
    fn detect_no_progress(&self) -> Option<LoopDetectionResult> {
        const MIN_CALLS: usize = 5;
        if self.window.len() < MIN_CALLS {
            return None;
        }

        let last = self.window.back()?;
        let same_tool_same_result: Vec<&ToolCallRecord> = self
            .window
            .iter()
            .rev()
            .take_while(|r| r.name == last.name && r.result_hash == last.result_hash)
            .collect();

        let count = same_tool_same_result.len();
        if count < MIN_CALLS {
            return None;
        }

        let unique_args: HashSet<u64> = same_tool_same_result.iter().map(|r| r.args_hash).collect();
        if unique_args.len() < 2 {
            return None; // exact-repeat handles same-args case
        }

        Some(LoopDetectionResult::Warning(format!(
            "Warning: tool '{}' called {} times with different args but identical results.",
            last.name, count
        )))
    }
}
