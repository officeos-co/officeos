    use super::*;
    use crate::memory::{Memory, MemoryCategory, SqliteMemory};
    use tempfile::TempDir;

    fn default_cfg() -> MemoryConfig {
        MemoryConfig::default()
    }

    #[test]
    fn archives_old_daily_memory_files() {
        let tmp = TempDir::new().unwrap();
        let workspace = tmp.path();
        fs::create_dir_all(workspace.join("memory")).unwrap();

        let old = (Local::now().date_naive() - Duration::days(10))
            .format("%Y-%m-%d")
            .to_string();
        let today = Local::now().date_naive().format("%Y-%m-%d").to_string();

        let old_file = workspace.join("memory").join(format!("{old}.md"));
        let today_file = workspace.join("memory").join(format!("{today}.md"));
        fs::write(&old_file, "old note").unwrap();
        fs::write(&today_file, "fresh note").unwrap();

        run_if_due(&default_cfg(), workspace).unwrap();

        assert!(!old_file.exists(), "old daily file should be archived");
        assert!(
            workspace
                .join("memory")
                .join("archive")
                .join(format!("{old}.md"))
                .exists(),
            "old daily file should exist in memory/archive"
        );
        assert!(today_file.exists(), "today file should remain in place");
    }

    #[test]
    fn archives_old_session_files() {
        let tmp = TempDir::new().unwrap();
        let workspace = tmp.path();
        fs::create_dir_all(workspace.join("sessions")).unwrap();

        let old = (Local::now().date_naive() - Duration::days(10))
            .format("%Y-%m-%d")
            .to_string();
        let old_name = format!("{old}-agent.log");
        let old_file = workspace.join("sessions").join(&old_name);
        fs::write(&old_file, "old session").unwrap();

        run_if_due(&default_cfg(), workspace).unwrap();

        assert!(!old_file.exists(), "old session file should be archived");
        assert!(
            workspace
                .join("sessions")
                .join("archive")
                .join(&old_name)
                .exists(),
            "archived session file should exist"
        );
    }

    #[test]
    fn skips_second_run_within_cadence_window() {
        let tmp = TempDir::new().unwrap();
        let workspace = tmp.path();
        fs::create_dir_all(workspace.join("memory")).unwrap();

        let old_a = (Local::now().date_naive() - Duration::days(10))
            .format("%Y-%m-%d")
            .to_string();
        let file_a = workspace.join("memory").join(format!("{old_a}.md"));
        fs::write(&file_a, "first").unwrap();

        run_if_due(&default_cfg(), workspace).unwrap();
        assert!(!file_a.exists(), "first old file should be archived");

        let old_b = (Local::now().date_naive() - Duration::days(9))
            .format("%Y-%m-%d")
            .to_string();
        let file_b = workspace.join("memory").join(format!("{old_b}.md"));
        fs::write(&file_b, "second").unwrap();

        // Should skip because cadence gate prevents a second immediate run.
        run_if_due(&default_cfg(), workspace).unwrap();
        assert!(
            file_b.exists(),
            "second file should remain because run is throttled"
        );
    }

    #[test]
    fn purges_old_memory_archives() {
        let tmp = TempDir::new().unwrap();
        let workspace = tmp.path();
        let archive_dir = workspace.join("memory").join("archive");
        fs::create_dir_all(&archive_dir).unwrap();

        let old = (Local::now().date_naive() - Duration::days(40))
            .format("%Y-%m-%d")
            .to_string();
        let keep = (Local::now().date_naive() - Duration::days(5))
            .format("%Y-%m-%d")
            .to_string();

        let old_file = archive_dir.join(format!("{old}.md"));
        let keep_file = archive_dir.join(format!("{keep}.md"));
        fs::write(&old_file, "expired").unwrap();
        fs::write(&keep_file, "recent").unwrap();

        run_if_due(&default_cfg(), workspace).unwrap();

        assert!(!old_file.exists(), "old archived file should be purged");
        assert!(keep_file.exists(), "recent archived file should remain");
    }

    #[tokio::test]
    async fn prunes_old_conversation_rows_in_sqlite_backend() {
        let tmp = TempDir::new().unwrap();
        let workspace = tmp.path();

        let mem = SqliteMemory::new(workspace).unwrap();
        mem.store("conv_old", "outdated", MemoryCategory::Conversation, None)
            .await
            .unwrap();
        mem.store("core_keep", "durable", MemoryCategory::Core, None)
            .await
            .unwrap();
        drop(mem);

        let db_path = workspace.join("memory").join("brain.db");
        let conn = Connection::open(&db_path).unwrap();
        let old_cutoff = (Local::now() - Duration::days(60)).to_rfc3339();
        conn.execute(
            "UPDATE memories SET created_at = ?1, updated_at = ?1 WHERE key = 'conv_old'",
            params![old_cutoff],
        )
        .unwrap();
        drop(conn);

        let mut cfg = default_cfg();
        cfg.archive_after_days = 0;
        cfg.purge_after_days = 0;
        cfg.conversation_retention_days = 30;

        run_if_due(&cfg, workspace).unwrap();

        let mem2 = SqliteMemory::new(workspace).unwrap();
        assert!(
            mem2.get("conv_old").await.unwrap().is_none(),
            "old conversation rows should be pruned"
        );
        assert!(
            mem2.get("core_keep").await.unwrap().is_some(),
            "core memory should remain"
        );
    }
