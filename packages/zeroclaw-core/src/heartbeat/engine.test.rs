    use super::*;

    #[test]
    fn parse_tasks_basic() {
        let content = "# Tasks\n\n- Check email\n- Review calendar\nNot a task\n- Third task";
        let tasks = HeartbeatEngine::parse_tasks(content);
        assert_eq!(tasks.len(), 3);
        assert_eq!(tasks[0].text, "Check email");
        assert_eq!(tasks[0].priority, TaskPriority::Medium);
        assert_eq!(tasks[0].status, TaskStatus::Active);
    }

    #[test]
    fn parse_tasks_empty_content() {
        assert!(HeartbeatEngine::parse_tasks("").is_empty());
    }

    #[test]
    fn parse_tasks_only_comments() {
        let tasks = HeartbeatEngine::parse_tasks("# No tasks here\n\nJust comments\n# Another");
        assert!(tasks.is_empty());
    }

    #[test]
    fn parse_tasks_with_leading_whitespace() {
        let content = "  - Indented task\n\t- Tab indented";
        let tasks = HeartbeatEngine::parse_tasks(content);
        assert_eq!(tasks.len(), 2);
        assert_eq!(tasks[0].text, "Indented task");
        assert_eq!(tasks[1].text, "Tab indented");
    }

    #[test]
    fn parse_tasks_dash_without_space_ignored() {
        let content = "- Real task\n-\n- Another";
        let tasks = HeartbeatEngine::parse_tasks(content);
        assert_eq!(tasks.len(), 2);
        assert_eq!(tasks[0].text, "Real task");
        assert_eq!(tasks[1].text, "Another");
    }

    #[test]
    fn parse_tasks_trailing_space_bullet_trimmed_to_dash() {
        let content = "- ";
        let tasks = HeartbeatEngine::parse_tasks(content);
        assert_eq!(tasks.len(), 0);
    }

    #[test]
    fn parse_tasks_bullet_with_content_after_spaces() {
        let content = "- hello  ";
        let tasks = HeartbeatEngine::parse_tasks(content);
        assert_eq!(tasks.len(), 1);
        assert_eq!(tasks[0].text, "hello");
    }

    #[test]
    fn parse_tasks_unicode() {
        let content = "- Check email 📧\n- Review calendar 📅\n- 日本語タスク";
        let tasks = HeartbeatEngine::parse_tasks(content);
        assert_eq!(tasks.len(), 3);
        assert!(tasks[0].text.contains('📧'));
        assert!(tasks[2].text.contains("日本語"));
    }

    #[test]
    fn parse_tasks_mixed_markdown() {
        let content = "# Periodic Tasks\n\n## Quick\n- Task A\n\n## Long\n- Task B\n\n* Not a dash bullet\n1. Not numbered";
        let tasks = HeartbeatEngine::parse_tasks(content);
        assert_eq!(tasks.len(), 2);
        assert_eq!(tasks[0].text, "Task A");
        assert_eq!(tasks[1].text, "Task B");
    }

    #[test]
    fn parse_tasks_single_task() {
        let tasks = HeartbeatEngine::parse_tasks("- Only one");
        assert_eq!(tasks.len(), 1);
        assert_eq!(tasks[0].text, "Only one");
    }

    #[test]
    fn parse_tasks_many_tasks() {
        let content: String = (0..100).fold(String::new(), |mut s, i| {
            use std::fmt::Write;
            let _ = writeln!(s, "- Task {i}");
            s
        });
        let tasks = HeartbeatEngine::parse_tasks(&content);
        assert_eq!(tasks.len(), 100);
        assert_eq!(tasks[99].text, "Task 99");
    }

    // ── Structured task parsing tests ────────────────────────────

    #[test]
    fn parse_task_with_high_priority() {
        let content = "- [high] Urgent email check";
        let tasks = HeartbeatEngine::parse_tasks(content);
        assert_eq!(tasks.len(), 1);
        assert_eq!(tasks[0].text, "Urgent email check");
        assert_eq!(tasks[0].priority, TaskPriority::High);
        assert_eq!(tasks[0].status, TaskStatus::Active);
    }

    #[test]
    fn parse_task_with_low_paused() {
        let content = "- [low|paused] Review old PRs";
        let tasks = HeartbeatEngine::parse_tasks(content);
        assert_eq!(tasks.len(), 1);
        assert_eq!(tasks[0].text, "Review old PRs");
        assert_eq!(tasks[0].priority, TaskPriority::Low);
        assert_eq!(tasks[0].status, TaskStatus::Paused);
    }

    #[test]
    fn parse_task_completed() {
        let content = "- [completed] Old task";
        let tasks = HeartbeatEngine::parse_tasks(content);
        assert_eq!(tasks.len(), 1);
        assert_eq!(tasks[0].priority, TaskPriority::Medium);
        assert_eq!(tasks[0].status, TaskStatus::Completed);
    }

    #[test]
    fn parse_task_without_metadata_defaults() {
        let content = "- Plain task";
        let tasks = HeartbeatEngine::parse_tasks(content);
        assert_eq!(tasks.len(), 1);
        assert_eq!(tasks[0].text, "Plain task");
        assert_eq!(tasks[0].priority, TaskPriority::Medium);
        assert_eq!(tasks[0].status, TaskStatus::Active);
    }

    #[test]
    fn parse_mixed_structured_and_legacy() {
        let content = "- [high] Urgent\n- Normal task\n- [low|paused] Later";
        let tasks = HeartbeatEngine::parse_tasks(content);
        assert_eq!(tasks.len(), 3);
        assert_eq!(tasks[0].priority, TaskPriority::High);
        assert_eq!(tasks[1].priority, TaskPriority::Medium);
        assert_eq!(tasks[2].priority, TaskPriority::Low);
        assert_eq!(tasks[2].status, TaskStatus::Paused);
    }

    #[test]
    fn runnable_filters_paused_and_completed() {
        let content = "- [high] Active\n- [low|paused] Paused\n- [completed] Done";
        let tasks = HeartbeatEngine::parse_tasks(content);
        let runnable: Vec<_> = tasks
            .into_iter()
            .filter(HeartbeatTask::is_runnable)
            .collect();
        assert_eq!(runnable.len(), 1);
        assert_eq!(runnable[0].text, "Active");
    }

    // ── Two-phase decision tests ────────────────────────────────

    #[test]
    fn decision_prompt_includes_all_tasks() {
        let tasks = vec![
            HeartbeatTask {
                text: "Check email".into(),
                priority: TaskPriority::High,
                status: TaskStatus::Active,
            },
            HeartbeatTask {
                text: "Review calendar".into(),
                priority: TaskPriority::Medium,
                status: TaskStatus::Active,
            },
        ];
        let prompt = HeartbeatEngine::build_decision_prompt(&tasks);
        assert!(prompt.contains("1. [high] Check email"));
        assert!(prompt.contains("2. [medium] Review calendar"));
        assert!(prompt.contains("skip"));
        assert!(prompt.contains("run:"));
    }

    #[test]
    fn parse_decision_skip() {
        let indices = HeartbeatEngine::parse_decision_response("skip", 3);
        assert!(indices.is_empty());
    }

    #[test]
    fn parse_decision_skip_with_reason() {
        let indices =
            HeartbeatEngine::parse_decision_response("skip — nothing urgent right now", 3);
        assert!(indices.is_empty());
    }

    #[test]
    fn parse_decision_run_single() {
        let indices = HeartbeatEngine::parse_decision_response("run: 1", 3);
        assert_eq!(indices, vec![0]);
    }

    #[test]
    fn parse_decision_run_multiple() {
        let indices = HeartbeatEngine::parse_decision_response("run: 1, 3", 3);
        assert_eq!(indices, vec![0, 2]);
    }

    #[test]
    fn parse_decision_run_out_of_range_ignored() {
        let indices = HeartbeatEngine::parse_decision_response("run: 1, 5, 2", 3);
        assert_eq!(indices, vec![0, 1]);
    }

    #[test]
    fn parse_decision_run_zero_ignored() {
        let indices = HeartbeatEngine::parse_decision_response("run: 0, 1", 3);
        assert_eq!(indices, vec![0]);
    }

    // ── Task display ────────────────────────────────────────────

    #[test]
    fn task_display_format() {
        let task = HeartbeatTask {
            text: "Check email".into(),
            priority: TaskPriority::High,
            status: TaskStatus::Active,
        };
        assert_eq!(format!("{task}"), "[high] Check email");
    }

    #[test]
    fn priority_ordering() {
        assert!(TaskPriority::High > TaskPriority::Medium);
        assert!(TaskPriority::Medium > TaskPriority::Low);
    }

    // ── Async tests ─────────────────────────────────────────────

    #[tokio::test]
    async fn ensure_heartbeat_file_creates_file() {
        let dir = std::env::temp_dir().join("zeroclaw_test_heartbeat");
        let _ = tokio::fs::remove_dir_all(&dir).await;
        tokio::fs::create_dir_all(&dir).await.unwrap();

        HeartbeatEngine::ensure_heartbeat_file(&dir).await.unwrap();

        let path = dir.join("HEARTBEAT.md");
        assert!(path.exists());
        let content = tokio::fs::read_to_string(&path).await.unwrap();
        assert!(content.contains("Periodic Tasks"));
        assert!(content.contains("[high]"));

        let _ = tokio::fs::remove_dir_all(&dir).await;
    }

    #[tokio::test]
    async fn ensure_heartbeat_file_does_not_overwrite() {
        let dir = std::env::temp_dir().join("zeroclaw_test_heartbeat_no_overwrite");
        let _ = tokio::fs::remove_dir_all(&dir).await;
        tokio::fs::create_dir_all(&dir).await.unwrap();

        let path = dir.join("HEARTBEAT.md");
        tokio::fs::write(&path, "- My custom task").await.unwrap();

        HeartbeatEngine::ensure_heartbeat_file(&dir).await.unwrap();

        let content = tokio::fs::read_to_string(&path).await.unwrap();
        assert_eq!(content, "- My custom task");

        let _ = tokio::fs::remove_dir_all(&dir).await;
    }

    #[tokio::test]
    async fn tick_returns_zero_when_no_file() {
        let dir = std::env::temp_dir().join("zeroclaw_test_tick_no_file");
        let _ = tokio::fs::remove_dir_all(&dir).await;
        tokio::fs::create_dir_all(&dir).await.unwrap();

        let observer: Arc<dyn Observer> = Arc::new(crate::observability::NoopObserver);
        let engine = HeartbeatEngine::new(
            HeartbeatConfig {
                enabled: true,
                interval_minutes: 30,
                ..HeartbeatConfig::default()
            },
            dir.clone(),
            observer,
        );
        let count = engine.tick().await.unwrap();
        assert_eq!(count, 0);

        let _ = tokio::fs::remove_dir_all(&dir).await;
    }

    #[tokio::test]
    async fn tick_counts_tasks_from_file() {
        let dir = std::env::temp_dir().join("zeroclaw_test_tick_count");
        let _ = tokio::fs::remove_dir_all(&dir).await;
        tokio::fs::create_dir_all(&dir).await.unwrap();

        tokio::fs::write(dir.join("HEARTBEAT.md"), "- A\n- B\n- C")
            .await
            .unwrap();

        let observer: Arc<dyn Observer> = Arc::new(crate::observability::NoopObserver);
        let engine = HeartbeatEngine::new(
            HeartbeatConfig {
                enabled: true,
                interval_minutes: 30,
                ..HeartbeatConfig::default()
            },
            dir.clone(),
            observer,
        );
        let count = engine.tick().await.unwrap();
        assert_eq!(count, 3);

        let _ = tokio::fs::remove_dir_all(&dir).await;
    }

    #[tokio::test]
    async fn run_returns_immediately_when_disabled() {
        let observer: Arc<dyn Observer> = Arc::new(crate::observability::NoopObserver);
        let engine = HeartbeatEngine::new(
            HeartbeatConfig {
                enabled: false,
                interval_minutes: 30,
                ..HeartbeatConfig::default()
            },
            std::env::temp_dir(),
            observer,
        );
        // Should return Ok immediately, not loop forever
        let result = engine.run().await;
        assert!(result.is_ok());
    }

    #[tokio::test]
    async fn collect_runnable_tasks_sorts_by_priority() {
        let dir = std::env::temp_dir().join("zeroclaw_test_runnable_sort");
        let _ = tokio::fs::remove_dir_all(&dir).await;
        tokio::fs::create_dir_all(&dir).await.unwrap();

        tokio::fs::write(
            dir.join("HEARTBEAT.md"),
            "- [low] Low task\n- [high] High task\n- Medium task\n- [low|paused] Skip me",
        )
        .await
        .unwrap();

        let observer: Arc<dyn Observer> = Arc::new(crate::observability::NoopObserver);
        let engine = HeartbeatEngine::new(
            HeartbeatConfig {
                enabled: true,
                interval_minutes: 30,
                ..HeartbeatConfig::default()
            },
            dir.clone(),
            observer,
        );

        let tasks = engine.collect_runnable_tasks().await.unwrap();
        assert_eq!(tasks.len(), 3); // paused one excluded
        assert_eq!(tasks[0].priority, TaskPriority::High);
        assert_eq!(tasks[1].priority, TaskPriority::Medium);
        assert_eq!(tasks[2].priority, TaskPriority::Low);

        let _ = tokio::fs::remove_dir_all(&dir).await;
    }

    // ── HeartbeatMetrics tests ───────────────────────────────────

    #[test]
    fn metrics_record_success_updates_fields() {
        let mut m = HeartbeatMetrics::default();
        m.record_success(100.0);
        assert_eq!(m.consecutive_successes, 1);
        assert_eq!(m.consecutive_failures, 0);
        assert_eq!(m.total_ticks, 1);
        assert!(m.last_tick_at.is_some());
        assert!((m.avg_tick_duration_ms - 100.0).abs() < f64::EPSILON);
    }

    #[test]
    fn metrics_record_failure_resets_successes() {
        let mut m = HeartbeatMetrics::default();
        m.record_success(50.0);
        m.record_success(50.0);
        m.record_failure(200.0);
        assert_eq!(m.consecutive_successes, 0);
        assert_eq!(m.consecutive_failures, 1);
        assert_eq!(m.total_ticks, 3);
    }

    #[test]
    fn metrics_ema_smoothing() {
        let mut m = HeartbeatMetrics::default();
        m.record_success(100.0);
        assert!((m.avg_tick_duration_ms - 100.0).abs() < f64::EPSILON);
        m.record_success(200.0);
        // EMA: 0.3 * 200 + 0.7 * 100 = 130
        assert!((m.avg_tick_duration_ms - 130.0).abs() < f64::EPSILON);
    }

    // ── Adaptive interval tests ─────────────────────────────────

    #[test]
    fn adaptive_uses_base_when_no_failures() {
        let result = compute_adaptive_interval(30, 5, 120, 0, false);
        assert_eq!(result, 30);
    }

    #[test]
    fn adaptive_uses_min_for_high_priority() {
        let result = compute_adaptive_interval(30, 5, 120, 0, true);
        assert_eq!(result, 5);
    }

    #[test]
    fn adaptive_backs_off_on_failures() {
        // 1 failure: 30 * 2 = 60
        assert_eq!(compute_adaptive_interval(30, 5, 120, 1, false), 60);
        // 2 failures: 30 * 4 = 120 (capped at max)
        assert_eq!(compute_adaptive_interval(30, 5, 120, 2, false), 120);
        // 3 failures: 30 * 8 = 240 → capped at 120
        assert_eq!(compute_adaptive_interval(30, 5, 120, 3, false), 120);
    }

    #[test]
    fn adaptive_backoff_respects_min() {
        // Even with failures, must be >= min
        assert!(compute_adaptive_interval(5, 10, 120, 0, false) >= 10);
    }

    // ── Engine metrics accessor ─────────────────────────────────

    #[test]
    fn engine_exposes_shared_metrics() {
        let observer: Arc<dyn Observer> = Arc::new(crate::observability::NoopObserver);
        let engine =
            HeartbeatEngine::new(HeartbeatConfig::default(), std::env::temp_dir(), observer);
        let metrics = engine.metrics();
        assert_eq!(metrics.lock().total_ticks, 0);
    }
