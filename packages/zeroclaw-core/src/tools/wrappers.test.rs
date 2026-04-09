use super::*;
use crate::security::{AutonomyLevel, SecurityPolicy};
use async_trait::async_trait;
use std::sync::atomic::{AtomicUsize, Ordering};

// ── Helpers ───────────────────────────────────────────────────────────────

fn policy(autonomy: AutonomyLevel) -> Arc<SecurityPolicy> {
    Arc::new(SecurityPolicy {
        autonomy,
        workspace_dir: std::env::temp_dir(),
        ..SecurityPolicy::default()
    })
}

/// A minimal tool that records how many times `execute` was called.
struct CountingTool {
    calls: Arc<AtomicUsize>,
}

impl CountingTool {
    fn new() -> (Self, Arc<AtomicUsize>) {
        let counter = Arc::new(AtomicUsize::new(0));
        (
            CountingTool {
                calls: counter.clone(),
            },
            counter,
        )
    }
}

#[async_trait]
impl Tool for CountingTool {
    fn name(&self) -> &str {
        "counting"
    }
    fn description(&self) -> &str {
        "counts calls"
    }
    fn parameters_schema(&self) -> serde_json::Value {
        serde_json::json!({})
    }
    async fn execute(&self, _args: serde_json::Value) -> anyhow::Result<ToolResult> {
        self.calls.fetch_add(1, Ordering::SeqCst);
        Ok(ToolResult {
            success: true,
            output: "ok".into(),
            error: None,
        })
    }
}

// ── RateLimitedTool tests ─────────────────────────────────────────────────

#[tokio::test]
async fn rate_limited_allows_call_within_budget() {
    let (inner, counter) = CountingTool::new();
    let tool = RateLimitedTool::new(inner, policy(AutonomyLevel::Full));
    let result = tool
        .execute(serde_json::json!({}))
        .await
        .expect("should succeed");
    assert!(result.success);
    assert_eq!(counter.load(Ordering::SeqCst), 1);
}

#[tokio::test]
async fn rate_limited_delegates_name_and_schema() {
    let (inner, _) = CountingTool::new();
    let tool = RateLimitedTool::new(inner, policy(AutonomyLevel::Full));
    assert_eq!(tool.name(), "counting");
    assert_eq!(tool.description(), "counts calls");
    assert!(tool.parameters_schema().is_object());
}

#[tokio::test]
async fn rate_limited_blocks_when_exhausted() {
    // Use a policy with a tiny action budget (1 action per window).
    let sec = Arc::new(SecurityPolicy {
        autonomy: AutonomyLevel::Full,
        workspace_dir: std::env::temp_dir(),
        max_actions_per_hour: 1,
        ..SecurityPolicy::default()
    });
    let (inner, counter) = CountingTool::new();
    let tool = RateLimitedTool::new(inner, sec);

    let r1 = tool.execute(serde_json::json!({})).await.unwrap();
    assert!(r1.success, "first call should succeed");

    let r2 = tool.execute(serde_json::json!({})).await.unwrap();
    assert!(!r2.success, "second call should be rate-limited");
    assert!(r2.error.unwrap().contains("Rate limit exceeded"));
    // Inner tool must NOT have been called on the blocked attempt.
    assert_eq!(counter.load(Ordering::SeqCst), 1);
}

// ── PathGuardedTool tests ─────────────────────────────────────────────────

#[tokio::test]
async fn path_guard_allows_safe_path() {
    let (inner, counter) = CountingTool::new();
    let tool = PathGuardedTool::new(inner, policy(AutonomyLevel::Full));
    let result = tool
        .execute(serde_json::json!({"path": "src/main.rs"}))
        .await
        .unwrap();
    assert!(result.success);
    assert_eq!(counter.load(Ordering::SeqCst), 1);
}

#[tokio::test]
async fn path_guard_blocks_forbidden_path() {
    let (inner, counter) = CountingTool::new();
    let tool = PathGuardedTool::new(inner, policy(AutonomyLevel::Full));
    let result = tool
        .execute(serde_json::json!({"command": "cat /etc/passwd"}))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.unwrap().contains("Path blocked"));
    assert_eq!(
        counter.load(Ordering::SeqCst),
        0,
        "inner must not be called"
    );
}

#[tokio::test]
async fn path_guard_no_path_arg_passes_through() {
    let (inner, counter) = CountingTool::new();
    let tool = PathGuardedTool::new(inner, policy(AutonomyLevel::Full));
    // No recognised path field — wrapper must not block.
    let result = tool
        .execute(serde_json::json!({"value": "hello"}))
        .await
        .unwrap();
    assert!(result.success);
    assert_eq!(counter.load(Ordering::SeqCst), 1);
}

#[tokio::test]
async fn path_guard_custom_extractor() {
    let (inner, counter) = CountingTool::new();
    let tool = PathGuardedTool::new(inner, policy(AutonomyLevel::Full)).with_extractor(|args| {
        args.get("target")
            .and_then(|v| v.as_str())
            .map(String::from)
    });
    let result = tool
        .execute(serde_json::json!({"target": "/etc/shadow"}))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.unwrap().contains("Path blocked"));
    assert_eq!(counter.load(Ordering::SeqCst), 0);
}

// ── Composition test ──────────────────────────────────────────────────────

#[tokio::test]
async fn composed_wrappers_both_enforce() {
    // RateLimited(PathGuarded(CountingTool)) — path check happens inside
    // the rate-limit window, so a forbidden path must still be blocked
    // (and not consume a rate-limit slot).
    let sec = policy(AutonomyLevel::Full);
    let (inner, counter) = CountingTool::new();
    let tool = RateLimitedTool::new(PathGuardedTool::new(inner, sec.clone()), sec);

    let blocked = tool
        .execute(serde_json::json!({"path": "/etc/passwd"}))
        .await
        .unwrap();
    assert!(!blocked.success);
    assert_eq!(counter.load(Ordering::SeqCst), 0);
}
