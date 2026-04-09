use super::*;

fn test_mappings() -> Vec<RoleMapping> {
    vec![
        RoleMapping {
            nevis_role: "admin".into(),
            zeroclaw_permissions: vec!["all".into()],
            workspace_access: vec!["all".into()],
        },
        RoleMapping {
            nevis_role: "operator".into(),
            zeroclaw_permissions: vec![
                "shell".into(),
                "file_read".into(),
                "file_write".into(),
                "memory_search".into(),
            ],
            workspace_access: vec!["production".into(), "staging".into()],
        },
        RoleMapping {
            nevis_role: "viewer".into(),
            zeroclaw_permissions: vec!["file_read".into(), "memory_search".into()],
            workspace_access: vec!["staging".into()],
        },
    ]
}

fn identity_with_roles(roles: Vec<&str>) -> NevisIdentity {
    NevisIdentity {
        user_id: "zeroclaw_user".into(),
        roles: roles.into_iter().map(String::from).collect(),
        scopes: vec!["openid".into()],
        mfa_verified: true,
        session_expiry: u64::MAX,
    }
}

#[test]
fn admin_gets_all_tools() {
    let policy = IamPolicy::from_mappings(&test_mappings()).unwrap();
    let identity = identity_with_roles(vec!["admin"]);

    assert!(policy.evaluate_tool_access(&identity, "shell").is_allowed());
    assert!(
        policy
            .evaluate_tool_access(&identity, "file_read")
            .is_allowed()
    );
    assert!(
        policy
            .evaluate_tool_access(&identity, "any_tool_name")
            .is_allowed()
    );
}

#[test]
fn admin_gets_all_workspaces() {
    let policy = IamPolicy::from_mappings(&test_mappings()).unwrap();
    let identity = identity_with_roles(vec!["admin"]);

    assert!(
        policy
            .evaluate_workspace_access(&identity, "production")
            .is_allowed()
    );
    assert!(
        policy
            .evaluate_workspace_access(&identity, "any_workspace")
            .is_allowed()
    );
}

#[test]
fn operator_gets_subset_of_tools() {
    let policy = IamPolicy::from_mappings(&test_mappings()).unwrap();
    let identity = identity_with_roles(vec!["operator"]);

    assert!(policy.evaluate_tool_access(&identity, "shell").is_allowed());
    assert!(
        policy
            .evaluate_tool_access(&identity, "file_read")
            .is_allowed()
    );
    assert!(
        !policy
            .evaluate_tool_access(&identity, "browser")
            .is_allowed()
    );
}

#[test]
fn operator_workspace_access_is_scoped() {
    let policy = IamPolicy::from_mappings(&test_mappings()).unwrap();
    let identity = identity_with_roles(vec!["operator"]);

    assert!(
        policy
            .evaluate_workspace_access(&identity, "production")
            .is_allowed()
    );
    assert!(
        policy
            .evaluate_workspace_access(&identity, "staging")
            .is_allowed()
    );
    assert!(
        !policy
            .evaluate_workspace_access(&identity, "development")
            .is_allowed()
    );
}

#[test]
fn viewer_is_read_only() {
    let policy = IamPolicy::from_mappings(&test_mappings()).unwrap();
    let identity = identity_with_roles(vec!["viewer"]);

    assert!(
        policy
            .evaluate_tool_access(&identity, "file_read")
            .is_allowed()
    );
    assert!(
        policy
            .evaluate_tool_access(&identity, "memory_search")
            .is_allowed()
    );
    assert!(!policy.evaluate_tool_access(&identity, "shell").is_allowed());
    assert!(
        !policy
            .evaluate_tool_access(&identity, "file_write")
            .is_allowed()
    );
}

#[test]
fn deny_by_default_for_unknown_role() {
    let policy = IamPolicy::from_mappings(&test_mappings()).unwrap();
    let identity = identity_with_roles(vec!["unknown_role"]);

    assert!(!policy.evaluate_tool_access(&identity, "shell").is_allowed());
    assert!(
        !policy
            .evaluate_workspace_access(&identity, "production")
            .is_allowed()
    );
}

#[test]
fn deny_by_default_for_no_roles() {
    let policy = IamPolicy::from_mappings(&test_mappings()).unwrap();
    let identity = identity_with_roles(vec![]);

    assert!(
        !policy
            .evaluate_tool_access(&identity, "file_read")
            .is_allowed()
    );
}

#[test]
fn multiple_roles_union_permissions() {
    let policy = IamPolicy::from_mappings(&test_mappings()).unwrap();
    let identity = identity_with_roles(vec!["viewer", "operator"]);

    // viewer has file_read, operator has shell — both should be accessible
    assert!(
        policy
            .evaluate_tool_access(&identity, "file_read")
            .is_allowed()
    );
    assert!(policy.evaluate_tool_access(&identity, "shell").is_allowed());
}

#[test]
fn role_matching_is_case_insensitive() {
    let policy = IamPolicy::from_mappings(&test_mappings()).unwrap();
    let identity = identity_with_roles(vec!["ADMIN"]);

    assert!(policy.evaluate_tool_access(&identity, "shell").is_allowed());
}

#[test]
fn tool_matching_is_case_insensitive() {
    let policy = IamPolicy::from_mappings(&test_mappings()).unwrap();
    let identity = identity_with_roles(vec!["operator"]);

    assert!(policy.evaluate_tool_access(&identity, "SHELL").is_allowed());
    assert!(
        policy
            .evaluate_tool_access(&identity, "File_Read")
            .is_allowed()
    );
}

#[test]
fn empty_tool_name_is_denied() {
    let policy = IamPolicy::from_mappings(&test_mappings()).unwrap();
    let identity = identity_with_roles(vec!["admin"]);

    assert!(!policy.evaluate_tool_access(&identity, "").is_allowed());
    assert!(!policy.evaluate_tool_access(&identity, "  ").is_allowed());
}

#[test]
fn empty_workspace_name_is_denied() {
    let policy = IamPolicy::from_mappings(&test_mappings()).unwrap();
    let identity = identity_with_roles(vec!["admin"]);

    assert!(!policy.evaluate_workspace_access(&identity, "").is_allowed());
}

#[test]
fn empty_mappings_deny_everything() {
    let policy = IamPolicy::from_mappings(&[]).unwrap();
    let identity = identity_with_roles(vec!["admin"]);

    assert!(policy.is_empty());
    assert!(!policy.evaluate_tool_access(&identity, "shell").is_allowed());
}

#[test]
fn policy_decision_deny_contains_reason() {
    let policy = IamPolicy::from_mappings(&test_mappings()).unwrap();
    let identity = identity_with_roles(vec!["viewer"]);

    let decision = policy.evaluate_tool_access(&identity, "shell");
    match decision {
        PolicyDecision::Deny(reason) => {
            assert!(reason.contains("shell"));
        }
        PolicyDecision::Allow => panic!("expected deny"),
    }
}

#[test]
fn duplicate_normalized_roles_are_rejected() {
    let mappings = vec![
        RoleMapping {
            nevis_role: "admin".into(),
            zeroclaw_permissions: vec!["all".into()],
            workspace_access: vec!["all".into()],
        },
        RoleMapping {
            nevis_role: " ADMIN ".into(),
            zeroclaw_permissions: vec!["file_read".into()],
            workspace_access: vec![],
        },
    ];
    let err = IamPolicy::from_mappings(&mappings).unwrap_err();
    assert!(
        err.to_string().contains("duplicate role mapping"),
        "Expected duplicate role error, got: {err}"
    );
}

#[test]
fn empty_role_name_in_mapping_is_skipped() {
    let mappings = vec![RoleMapping {
        nevis_role: "  ".into(),
        zeroclaw_permissions: vec!["all".into()],
        workspace_access: vec![],
    }];
    let policy = IamPolicy::from_mappings(&mappings).unwrap();
    assert!(policy.is_empty());
}
