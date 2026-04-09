use super::*;
use async_trait::async_trait;
use std::sync::Arc;
use std::sync::atomic::{AtomicU32, Ordering};

/// A hook that records how many times void events fire.
struct CountingHook {
    name: String,
    priority: i32,
    fire_count: Arc<AtomicU32>,
}

impl CountingHook {
    fn new(name: &str, priority: i32) -> (Self, Arc<AtomicU32>) {
        let count = Arc::new(AtomicU32::new(0));
        (
            Self {
                name: name.to_string(),
                priority,
                fire_count: count.clone(),
            },
            count,
        )
    }
}

#[async_trait]
impl HookHandler for CountingHook {
    fn name(&self) -> &str {
        &self.name
    }
    fn priority(&self) -> i32 {
        self.priority
    }
    async fn on_heartbeat_tick(&self) {
        self.fire_count.fetch_add(1, Ordering::SeqCst);
    }
}

/// A modifying hook that uppercases the prompt.
struct UppercasePromptHook {
    name: String,
    priority: i32,
}

#[async_trait]
impl HookHandler for UppercasePromptHook {
    fn name(&self) -> &str {
        &self.name
    }
    fn priority(&self) -> i32 {
        self.priority
    }
    async fn before_prompt_build(&self, prompt: String) -> HookResult<String> {
        HookResult::Continue(prompt.to_uppercase())
    }
}

/// A modifying hook that cancels before_prompt_build.
struct CancelPromptHook {
    name: String,
    priority: i32,
}

#[async_trait]
impl HookHandler for CancelPromptHook {
    fn name(&self) -> &str {
        &self.name
    }
    fn priority(&self) -> i32 {
        self.priority
    }
    async fn before_prompt_build(&self, _prompt: String) -> HookResult<String> {
        HookResult::Cancel("blocked by policy".into())
    }
}

/// A modifying hook that appends a suffix to the prompt.
struct SuffixPromptHook {
    name: String,
    priority: i32,
    suffix: String,
}

#[async_trait]
impl HookHandler for SuffixPromptHook {
    fn name(&self) -> &str {
        &self.name
    }
    fn priority(&self) -> i32 {
        self.priority
    }
    async fn before_prompt_build(&self, prompt: String) -> HookResult<String> {
        HookResult::Continue(format!("{}{}", prompt, self.suffix))
    }
}

#[test]
fn register_and_sort_by_priority() {
    let mut runner = HookRunner::new();
    let (low, _) = CountingHook::new("low", 1);
    let (high, _) = CountingHook::new("high", 10);
    let (mid, _) = CountingHook::new("mid", 5);

    runner.register(Box::new(low));
    runner.register(Box::new(high));
    runner.register(Box::new(mid));

    let names: Vec<&str> = runner.handlers.iter().map(|h| h.name()).collect();
    assert_eq!(names, vec!["high", "mid", "low"]);
}

#[tokio::test]
async fn void_hooks_fire_all_handlers() {
    let mut runner = HookRunner::new();
    let (h1, c1) = CountingHook::new("hook_a", 0);
    let (h2, c2) = CountingHook::new("hook_b", 0);

    runner.register(Box::new(h1));
    runner.register(Box::new(h2));

    runner.fire_heartbeat_tick().await;

    assert_eq!(c1.load(Ordering::SeqCst), 1);
    assert_eq!(c2.load(Ordering::SeqCst), 1);
}

#[tokio::test]
async fn modifying_hook_can_cancel() {
    let mut runner = HookRunner::new();
    runner.register(Box::new(CancelPromptHook {
        name: "blocker".into(),
        priority: 10,
    }));
    runner.register(Box::new(UppercasePromptHook {
        name: "upper".into(),
        priority: 0,
    }));

    let result = runner.run_before_prompt_build("hello".into()).await;
    assert!(result.is_cancel());
}

#[tokio::test]
async fn modifying_hook_pipelines_data() {
    let mut runner = HookRunner::new();

    // Priority 10 runs first: uppercases
    runner.register(Box::new(UppercasePromptHook {
        name: "upper".into(),
        priority: 10,
    }));
    // Priority 0 runs second: appends suffix
    runner.register(Box::new(SuffixPromptHook {
        name: "suffix".into(),
        priority: 0,
        suffix: "_done".into(),
    }));

    match runner.run_before_prompt_build("hello".into()).await {
        HookResult::Continue(result) => assert_eq!(result, "HELLO_done"),
        HookResult::Cancel(_) => panic!("should not cancel"),
    }
}
