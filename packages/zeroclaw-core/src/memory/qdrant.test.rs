    use super::*;

    #[test]
    fn category_to_str_maps_known_categories() {
        assert_eq!(QdrantMemory::category_to_str(&MemoryCategory::Core), "core");
        assert_eq!(
            QdrantMemory::category_to_str(&MemoryCategory::Daily),
            "daily"
        );
        assert_eq!(
            QdrantMemory::category_to_str(&MemoryCategory::Conversation),
            "conversation"
        );
        assert_eq!(
            QdrantMemory::category_to_str(&MemoryCategory::Custom("notes".into())),
            "notes"
        );
    }

    #[test]
    fn parse_category_maps_known_and_custom_values() {
        assert_eq!(QdrantMemory::parse_category("core"), MemoryCategory::Core);
        assert_eq!(QdrantMemory::parse_category("daily"), MemoryCategory::Daily);
        assert_eq!(
            QdrantMemory::parse_category("conversation"),
            MemoryCategory::Conversation
        );
        assert_eq!(
            QdrantMemory::parse_category("custom_notes"),
            MemoryCategory::Custom("custom_notes".into())
        );
    }

    #[test]
    fn memory_payload_serializes_correctly() {
        let payload = MemoryPayload {
            key: "test_key".into(),
            content: "test content".into(),
            category: "core".into(),
            timestamp: "2026-02-20T00:00:00Z".into(),
            session_id: Some("session-1".into()),
        };

        let json = serde_json::to_string(&payload).unwrap();
        assert!(json.contains("test_key"));
        assert!(json.contains("test content"));
        assert!(json.contains("session-1"));
    }

    #[test]
    fn memory_payload_skips_none_session_id() {
        let payload = MemoryPayload {
            key: "test_key".into(),
            content: "test content".into(),
            category: "core".into(),
            timestamp: "2026-02-20T00:00:00Z".into(),
            session_id: None,
        };

        let json = serde_json::to_string(&payload).unwrap();
        assert!(!json.contains("session_id"));
    }
