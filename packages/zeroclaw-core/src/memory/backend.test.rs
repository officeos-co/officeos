    use super::*;

    #[test]
    fn classify_known_backends() {
        assert_eq!(classify_memory_backend("sqlite"), MemoryBackendKind::Sqlite);
        assert_eq!(classify_memory_backend("lucid"), MemoryBackendKind::Lucid);
        assert_eq!(
            classify_memory_backend("markdown"),
            MemoryBackendKind::Markdown
        );
        assert_eq!(classify_memory_backend("none"), MemoryBackendKind::None);
    }

    #[test]
    fn classify_unknown_backend() {
        assert_eq!(classify_memory_backend("redis"), MemoryBackendKind::Unknown);
    }

    #[test]
    fn selectable_backends_are_ordered_for_onboarding() {
        let backends = selectable_memory_backends();
        assert_eq!(backends.len(), 4);
        assert_eq!(backends[0].key, "sqlite");
        assert_eq!(backends[1].key, "lucid");
        assert_eq!(backends[2].key, "markdown");
        assert_eq!(backends[3].key, "none");
    }

    #[test]
    fn lucid_profile_is_sqlite_based_optional_backend() {
        let profile = memory_backend_profile("lucid");
        assert!(profile.sqlite_based);
        assert!(profile.optional_dependency);
        assert!(profile.uses_sqlite_hygiene);
    }

    #[test]
    fn unknown_profile_preserves_extensibility_defaults() {
        let profile = memory_backend_profile("custom-memory");
        assert_eq!(profile.key, "custom");
        assert!(profile.auto_save_default);
        assert!(!profile.uses_sqlite_hygiene);
    }
