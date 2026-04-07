    use super::*;
    use std::path::PathBuf;

    fn test_profile() -> WorkspaceProfile {
        WorkspaceProfile {
            name: "client_a".to_string(),
            allowed_domains: vec!["api.example.com".to_string()],
            credential_profile: None,
            memory_namespace: Some("client_a".to_string()),
            audit_namespace: Some("client_a".to_string()),
            tool_restrictions: vec!["shell".to_string()],
        }
    }

    #[test]
    fn boundary_inactive_allows_everything() {
        let boundary = WorkspaceBoundary::inactive();
        assert_eq!(boundary.check_tool_access("shell"), BoundaryVerdict::Allow);
        assert_eq!(
            boundary.check_domain_access("any.domain"),
            BoundaryVerdict::Allow
        );
        assert!(!boundary.is_active());
    }

    #[test]
    fn boundary_denies_restricted_tool() {
        let boundary = WorkspaceBoundary::new(Some(test_profile()), false);
        assert!(matches!(
            boundary.check_tool_access("shell"),
            BoundaryVerdict::Deny(_)
        ));
        assert_eq!(
            boundary.check_tool_access("file_read"),
            BoundaryVerdict::Allow
        );
    }

    #[test]
    fn boundary_denies_unlisted_domain() {
        let boundary = WorkspaceBoundary::new(Some(test_profile()), false);
        assert_eq!(
            boundary.check_domain_access("api.example.com"),
            BoundaryVerdict::Allow
        );
        assert!(matches!(
            boundary.check_domain_access("evil.com"),
            BoundaryVerdict::Deny(_)
        ));
    }

    #[test]
    fn boundary_denies_cross_workspace_path_access() {
        let boundary = WorkspaceBoundary::new(Some(test_profile()), false);
        let base = PathBuf::from("/home/zeroclaw_user/.zeroclaw/workspaces");

        // Access to own workspace is allowed
        let own_path = base.join("client_a").join("data.db");
        assert_eq!(
            boundary.check_path_access(&own_path, &base),
            BoundaryVerdict::Allow
        );

        // Access to other workspace is denied
        let other_path = base.join("client_b").join("data.db");
        assert!(matches!(
            boundary.check_path_access(&other_path, &base),
            BoundaryVerdict::Deny(_)
        ));
    }

    #[test]
    fn boundary_allows_cross_workspace_when_enabled() {
        let boundary = WorkspaceBoundary::new(Some(test_profile()), true);
        let base = PathBuf::from("/home/zeroclaw_user/.zeroclaw/workspaces");
        let other_path = base.join("client_b").join("data.db");

        assert_eq!(
            boundary.check_path_access(&other_path, &base),
            BoundaryVerdict::Allow
        );
    }

    #[test]
    fn boundary_allows_paths_outside_workspaces_dir() {
        let boundary = WorkspaceBoundary::new(Some(test_profile()), false);
        let base = PathBuf::from("/home/zeroclaw_user/.zeroclaw/workspaces");
        let outside_path = PathBuf::from("/tmp/something");

        assert_eq!(
            boundary.check_path_access(&outside_path, &base),
            BoundaryVerdict::Allow
        );
    }
