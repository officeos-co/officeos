    use super::*;
    use std::collections::HashMap;

    fn empty_policy() -> MemoryPolicyConfig {
        MemoryPolicyConfig::default()
    }

    #[test]
    fn default_policy_allows_everything() {
        let enforcer = PolicyEnforcer::new(&empty_policy());
        assert!(!enforcer.is_read_only("default"));
        assert!(
            enforcer
                .validate_store("default", &MemoryCategory::Core)
                .is_ok()
        );
        assert!(enforcer.check_namespace_limit(100).is_ok());
        assert!(enforcer.check_category_limit(100).is_ok());
    }

    #[test]
    fn read_only_namespace_blocks_writes() {
        let policy = MemoryPolicyConfig {
            read_only_namespaces: vec!["archive".into()],
            ..empty_policy()
        };
        let enforcer = PolicyEnforcer::new(&policy);

        assert!(enforcer.is_read_only("archive"));
        assert!(!enforcer.is_read_only("default"));
        assert!(
            enforcer
                .validate_store("archive", &MemoryCategory::Core)
                .is_err()
        );
        assert!(
            enforcer
                .validate_store("default", &MemoryCategory::Core)
                .is_ok()
        );
    }

    #[test]
    fn namespace_quota_enforced() {
        let policy = MemoryPolicyConfig {
            max_entries_per_namespace: 10,
            ..empty_policy()
        };
        let enforcer = PolicyEnforcer::new(&policy);

        assert!(enforcer.check_namespace_limit(5).is_ok());
        assert!(enforcer.check_namespace_limit(10).is_err());
        assert!(enforcer.check_namespace_limit(15).is_err());
    }

    #[test]
    fn category_quota_enforced() {
        let policy = MemoryPolicyConfig {
            max_entries_per_category: 50,
            ..empty_policy()
        };
        let enforcer = PolicyEnforcer::new(&policy);

        assert!(enforcer.check_category_limit(25).is_ok());
        assert!(enforcer.check_category_limit(50).is_err());
    }

    #[test]
    fn per_category_retention_overrides_default() {
        let mut retention = HashMap::new();
        retention.insert("core".into(), 365);
        retention.insert("conversation".into(), 7);

        let policy = MemoryPolicyConfig {
            retention_days_by_category: retention,
            ..empty_policy()
        };
        let enforcer = PolicyEnforcer::new(&policy);

        assert_eq!(
            enforcer.retention_days_for_category(&MemoryCategory::Core, 30),
            365
        );
        assert_eq!(
            enforcer.retention_days_for_category(&MemoryCategory::Conversation, 30),
            7
        );
        assert_eq!(
            enforcer.retention_days_for_category(&MemoryCategory::Daily, 30),
            30
        );
    }
