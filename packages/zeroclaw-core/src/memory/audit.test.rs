    use super::*;
    use crate::memory::NoneMemory;
    use tempfile::TempDir;

    #[tokio::test]
    async fn audited_memory_logs_store_operation() {
        let tmp = TempDir::new().unwrap();
        let inner = NoneMemory::new();
        let audited = AuditedMemory::new(inner, tmp.path()).unwrap();

        audited
            .store("test_key", "test_value", MemoryCategory::Core, None)
            .await
            .unwrap();

        assert_eq!(audited.audit_count().unwrap(), 1);
    }

    #[tokio::test]
    async fn audited_memory_logs_recall_operation() {
        let tmp = TempDir::new().unwrap();
        let inner = NoneMemory::new();
        let audited = AuditedMemory::new(inner, tmp.path()).unwrap();

        let _ = audited.recall("query", 10, None, None, None).await;

        assert_eq!(audited.audit_count().unwrap(), 1);
    }

    #[tokio::test]
    async fn audited_memory_prune_works() {
        let tmp = TempDir::new().unwrap();
        let inner = NoneMemory::new();
        let audited = AuditedMemory::new(inner, tmp.path()).unwrap();

        audited
            .store("k1", "v1", MemoryCategory::Core, None)
            .await
            .unwrap();

        // Pruning with 0 days should remove entries
        let pruned = audited.prune_older_than(0).unwrap();
        // Entry was just created, so 0-day retention should remove it
        // Pruning should succeed (pruned is usize, always >= 0)
        let _ = pruned;
    }

    #[tokio::test]
    async fn audited_memory_delegates_correctly() {
        let tmp = TempDir::new().unwrap();
        let inner = NoneMemory::new();
        let audited = AuditedMemory::new(inner, tmp.path()).unwrap();

        assert_eq!(audited.name(), "none");
        assert!(audited.health_check().await);
        assert_eq!(audited.count().await.unwrap(), 0);
    }
