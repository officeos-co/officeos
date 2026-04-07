    use super::*;

    #[tokio::test]
    async fn none_memory_is_noop() {
        let memory = NoneMemory::new();

        memory
            .store("k", "v", MemoryCategory::Core, None)
            .await
            .unwrap();

        assert!(memory.get("k").await.unwrap().is_none());
        assert!(
            memory
                .recall("k", 10, None, None, None)
                .await
                .unwrap()
                .is_empty()
        );
        assert!(memory.list(None, None).await.unwrap().is_empty());
        assert!(!memory.forget("k").await.unwrap());
        assert_eq!(memory.count().await.unwrap(), 0);
        assert!(memory.health_check().await);
    }
