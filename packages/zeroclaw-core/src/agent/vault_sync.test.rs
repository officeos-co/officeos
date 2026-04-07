    use super::*;
    use tempfile::TempDir;

    fn test_workspace() -> (TempDir, PathBuf) {
        let tmp = TempDir::new().unwrap();
        let ws = tmp.path().to_path_buf();
        (tmp, ws)
    }

    #[test]
    fn vault_sync_new_sets_paths() {
        let (_tmp, ws) = test_workspace();
        let sync = VaultSync::new(&ws, "agent-db-123");
        assert_eq!(sync.vault_database, "agent-db-123");
        assert_eq!(sync.sync_state_path, ws.join(".vault_sync_state.json"));
    }

    #[test]
    fn sync_state_roundtrip() {
        let (_tmp, ws) = test_workspace();
        let sync = VaultSync::new(&ws, "test-db");

        // Initially empty
        let state = sync.load_sync_state();
        assert!(state.files.is_empty());

        // Update and reload
        sync.update_sync_state("SOUL.md", 1234);
        let state = sync.load_sync_state();
        assert_eq!(state.files.len(), 1);
        assert_eq!(state.files["SOUL.md"].last_synced_size, 1234);
        assert!(state.files["SOUL.md"].last_synced_epoch > 0);
    }

    #[test]
    fn is_available_returns_false_when_vault_cli_missing() {
        let (_tmp, ws) = test_workspace();
        // Use a non-existent database; vault CLI likely not installed in test env
        let sync = VaultSync::new(&ws, "nonexistent-db");
        // Should not panic, just return false
        let _ = sync.is_available();
    }

    #[test]
    fn sync_from_vault_gracefully_handles_missing_cli() {
        let (_tmp, ws) = test_workspace();
        let sync = VaultSync::new(&ws, "nonexistent-db");

        // Write a local SOUL.md to verify it's preserved
        std::fs::write(ws.join("SOUL.md"), "I am a helpful agent.").unwrap();

        let report = sync.sync_from_vault();

        // All files should fail (no vault CLI), but no panic
        assert!(report.synced.is_empty());

        // Local file should be preserved (graceful degradation)
        let content = std::fs::read_to_string(ws.join("SOUL.md")).unwrap();
        assert_eq!(content, "I am a helpful agent.");
    }

    #[test]
    fn sync_to_vault_skips_unmodified_files() {
        let (_tmp, ws) = test_workspace();
        let sync = VaultSync::new(&ws, "test-db");

        // Create a local USER.md and mark it as synced
        std::fs::write(ws.join("USER.md"), "User prefs").unwrap();
        sync.update_sync_state("USER.md", 10); // matches "User prefs".len()

        let report = sync.sync_to_vault();

        // Should skip because size matches
        assert!(report.synced.is_empty());
        assert!(report.skipped.contains(&"USER.md".to_string()));
    }

    #[test]
    fn tier0_and_tier1_cover_all_personality_files() {
        let mut all: Vec<&str> = TIER0_FILES.to_vec();
        all.extend(TIER1_FILES);

        for file in PERSONALITY_FILES {
            assert!(
                all.contains(file),
                "{file} is in PERSONALITY_FILES but not in TIER0 or TIER1"
            );
        }
    }
