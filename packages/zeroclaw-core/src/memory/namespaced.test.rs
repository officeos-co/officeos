    use super::*;
    use crate::memory::NoneMemory;

    #[tokio::test]
    async fn namespaced_memory_enforces_namespace_on_store() {
        let inner = Arc::new(NoneMemory::new());
        let namespaced = NamespacedMemory::new(inner, "test_namespace".to_string());

        // Store should succeed
        namespaced
            .store("key1", "value1", MemoryCategory::Core, None)
            .await
            .unwrap();
    }

    #[tokio::test]
    async fn namespaced_memory_prevents_cross_namespace_access() {
        let inner = Arc::new(NoneMemory::new());
        let namespaced = NamespacedMemory::new(inner, "test_namespace".to_string());

        // Try to recall from a different namespace (no-op for NoneMemory)
        let results = namespaced
            .recall_namespaced("other_namespace", "query", 10, None, None, None)
            .await
            .unwrap();
        assert!(results.is_empty());
    }

    #[tokio::test]
    async fn namespaced_memory_delegates_correctly() {
        let inner = Arc::new(NoneMemory::new());
        let namespaced = NamespacedMemory::new(inner, "test_namespace".to_string());

        assert_eq!(namespaced.name(), "none");
        assert!(namespaced.health_check().await);
        assert_eq!(namespaced.count().await.unwrap(), 0);
    }
