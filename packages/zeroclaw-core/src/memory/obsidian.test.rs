    use super::*;

    #[test]
    fn note_path_convention() {
        assert_eq!(
            ObsidianMemory::note_path("user_lang", &MemoryCategory::Core),
            "memory/core/user_lang.md"
        );
        assert_eq!(
            ObsidianMemory::note_path("today_note", &MemoryCategory::Daily),
            "memory/daily/today_note.md"
        );
        assert_eq!(
            ObsidianMemory::note_path("ctx_1", &MemoryCategory::Conversation),
            "memory/conversation/ctx_1.md"
        );
        assert_eq!(
            ObsidianMemory::note_path("proj", &MemoryCategory::Custom("project".into())),
            "memory/project/proj.md"
        );
    }

    #[test]
    fn note_path_sanitizes_special_chars() {
        assert_eq!(
            ObsidianMemory::note_path("my key/with spaces", &MemoryCategory::Core),
            "memory/core/my_key_with_spaces.md"
        );
    }

    #[test]
    fn build_note_includes_frontmatter() {
        let note = ObsidianMemory::build_note("lang", "User prefers Rust", &MemoryCategory::Core, None);
        assert!(note.starts_with("---\n"));
        assert!(note.contains("key: \"lang\""));
        assert!(note.contains("category: \"core\""));
        assert!(note.contains("created_at:"));
        assert!(note.contains("User prefers Rust"));
    }

    #[test]
    fn build_note_with_session_id() {
        let note = ObsidianMemory::build_note(
            "ctx",
            "Some context",
            &MemoryCategory::Conversation,
            Some("sess-123"),
        );
        assert!(note.contains("session_id: \"sess-123\""));
    }

    #[test]
    fn parse_note_content_roundtrip() {
        let note = ObsidianMemory::build_note("lang", "Prefers Rust", &MemoryCategory::Core, None);
        let (body, key, category) = parse_note_content(&note);
        assert_eq!(body, "Prefers Rust");
        assert_eq!(key.as_deref(), Some("lang"));
        assert_eq!(category, MemoryCategory::Core);
    }

    #[test]
    fn parse_note_without_frontmatter() {
        let (body, key, category) = parse_note_content("Just plain text");
        assert_eq!(body, "Just plain text");
        assert!(key.is_none());
        assert_eq!(category, MemoryCategory::Core);
    }

    #[tokio::test]
    async fn cache_store_and_get() {
        let mem = ObsidianMemory::new();
        mem.store("lang", "Prefers Rust", MemoryCategory::Core, None)
            .await
            .unwrap();

        let entry = mem.get("lang").await.unwrap();
        assert!(entry.is_some());
        assert_eq!(entry.unwrap().content, "Prefers Rust");
    }

    #[tokio::test]
    async fn cache_recall_finds_match() {
        let mem = ObsidianMemory::new();
        mem.store("lang", "User prefers Rust", MemoryCategory::Core, None)
            .await
            .unwrap();
        mem.store("tz", "Timezone is EST", MemoryCategory::Core, None)
            .await
            .unwrap();

        let results = mem.recall("Rust", 5, None, None, None).await.unwrap();
        assert_eq!(results.len(), 1);
        assert!(results[0].content.contains("Rust"));
    }

    #[tokio::test]
    async fn cache_recall_respects_limit() {
        let mem = ObsidianMemory::new();
        for i in 0..10 {
            mem.store(&format!("k{i}"), &format!("Rust fact {i}"), MemoryCategory::Core, None)
                .await
                .unwrap();
        }

        let results = mem.recall("Rust", 3, None, None, None).await.unwrap();
        assert_eq!(results.len(), 3);
    }

    #[tokio::test]
    async fn forget_removes_from_cache() {
        let mem = ObsidianMemory::new();
        mem.store("lang", "Rust", MemoryCategory::Core, None)
            .await
            .unwrap();

        let removed = mem.forget("lang").await.unwrap();
        assert!(removed);

        let entry = mem.get("lang").await.unwrap();
        // Entry is gone from cache (vault fallback won't work in tests)
        assert!(entry.is_none());
    }

    #[tokio::test]
    async fn count_reflects_cache() {
        let mem = ObsidianMemory::new();
        assert_eq!(mem.count().await.unwrap(), 0);

        mem.store("a", "A", MemoryCategory::Core, None).await.unwrap();
        mem.store("b", "B", MemoryCategory::Core, None).await.unwrap();
        assert_eq!(mem.count().await.unwrap(), 2);
    }

    #[test]
    fn name_is_obsidian() {
        let mem = ObsidianMemory::new();
        assert_eq!(mem.name(), "obsidian");
    }

    #[tokio::test]
    async fn list_filters_by_category() {
        let mem = ObsidianMemory::new();
        mem.store("a", "Core A", MemoryCategory::Core, None).await.unwrap();
        mem.store("b", "Daily B", MemoryCategory::Daily, None).await.unwrap();

        let core = mem.list(Some(&MemoryCategory::Core), None).await.unwrap();
        assert_eq!(core.len(), 1);
        assert_eq!(core[0].key, "a");
    }

    #[test]
    fn cache_eviction_respects_limit() {
        let mem = ObsidianMemory::with_cache_size(3);

        // Insert 5 entries synchronously into cache
        for i in 0..5 {
            let entry = MemoryEntry {
                id: format!("id-{i}"),
                key: format!("k{i}"),
                content: format!("content {i}"),
                category: MemoryCategory::Core,
                timestamp: format!("2026-01-0{i}T00:00:00Z", i = i + 1),
                session_id: None,
                score: None,
                namespace: "default".to_string(),
                importance: None,
                superseded_by: None,
            };
            mem.cache.lock().insert(format!("k{i}"), entry);
        }

        mem.evict_cache();
        assert!(mem.cache.lock().len() <= 3);
    }
