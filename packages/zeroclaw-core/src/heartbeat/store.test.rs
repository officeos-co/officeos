    use super::*;
    use chrono::Duration as ChronoDuration;
    use tempfile::TempDir;

    #[test]
    fn record_and_list_runs() {
        let tmp = TempDir::new().unwrap();
        let base = Utc::now();

        for i in 0..3 {
            let start = base + ChronoDuration::seconds(i);
            let end = start + ChronoDuration::milliseconds(100);
            record_run(
                tmp.path(),
                &format!("Task {i}"),
                "medium",
                start,
                end,
                "ok",
                Some("done"),
                100,
                50,
            )
            .unwrap();
        }

        let runs = list_runs(tmp.path(), 10).unwrap();
        assert_eq!(runs.len(), 3);
        // Most recent first
        assert!(runs[0].task_text.contains('2'));
    }

    #[test]
    fn prunes_old_runs() {
        let tmp = TempDir::new().unwrap();
        let base = Utc::now();

        for i in 0..5 {
            let start = base + ChronoDuration::seconds(i);
            let end = start + ChronoDuration::milliseconds(50);
            record_run(
                tmp.path(),
                "Task",
                "high",
                start,
                end,
                "ok",
                None,
                50,
                2, // keep only 2
            )
            .unwrap();
        }

        let runs = list_runs(tmp.path(), 10).unwrap();
        assert_eq!(runs.len(), 2);
    }

    #[test]
    fn run_stats_counts_correctly() {
        let tmp = TempDir::new().unwrap();
        let now = Utc::now();

        record_run(tmp.path(), "A", "high", now, now, "ok", None, 10, 50).unwrap();
        record_run(
            tmp.path(),
            "B",
            "low",
            now,
            now,
            "error",
            Some("fail"),
            20,
            50,
        )
        .unwrap();
        record_run(tmp.path(), "C", "medium", now, now, "ok", None, 15, 50).unwrap();

        let (total, ok, err) = run_stats(tmp.path()).unwrap();
        assert_eq!(total, 3);
        assert_eq!(ok, 2);
        assert_eq!(err, 1);
    }

    #[test]
    fn truncates_large_output() {
        let tmp = TempDir::new().unwrap();
        let now = Utc::now();
        let big = "x".repeat(MAX_OUTPUT_BYTES + 512);

        record_run(
            tmp.path(),
            "T",
            "medium",
            now,
            now,
            "ok",
            Some(&big),
            10,
            50,
        )
        .unwrap();

        let runs = list_runs(tmp.path(), 1).unwrap();
        let stored = runs[0].output.as_deref().unwrap_or_default();
        assert!(stored.ends_with(TRUNCATED_MARKER));
        assert!(stored.len() <= MAX_OUTPUT_BYTES);
    }
