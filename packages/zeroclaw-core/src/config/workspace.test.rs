use super::*;
use tempfile::TempDir;

fn sample_profile(name: &str) -> WorkspaceProfile {
    WorkspaceProfile {
        name: name.to_string(),
        allowed_domains: vec!["example.com".to_string()],
        credential_profile: Some("test-creds".to_string()),
        memory_namespace: Some(format!("{name}_mem")),
        audit_namespace: Some(format!("{name}_audit")),
        tool_restrictions: vec!["shell".to_string()],
    }
}

#[test]
fn workspace_profile_tool_restriction_check() {
    let profile = sample_profile("client_a");
    assert!(profile.is_tool_restricted("shell"));
    assert!(profile.is_tool_restricted("Shell"));
    assert!(!profile.is_tool_restricted("file_read"));
}

#[test]
fn workspace_profile_domain_allowlist_empty_allows_all() {
    let mut profile = sample_profile("client_a");
    profile.allowed_domains.clear();
    assert!(profile.is_domain_allowed("anything.com"));
}

#[test]
fn workspace_profile_domain_allowlist_enforced() {
    let profile = sample_profile("client_a");
    assert!(profile.is_domain_allowed("example.com"));
    assert!(!profile.is_domain_allowed("other.com"));
}

#[test]
fn workspace_profile_effective_namespaces() {
    let profile = sample_profile("client_a");
    assert_eq!(profile.effective_memory_namespace(), "client_a_mem");
    assert_eq!(profile.effective_audit_namespace(), "client_a_audit");

    let fallback = WorkspaceProfile {
        name: "test_ws".to_string(),
        memory_namespace: None,
        audit_namespace: None,
        ..sample_profile("test_ws")
    };
    assert_eq!(fallback.effective_memory_namespace(), "test_ws");
    assert_eq!(fallback.effective_audit_namespace(), "test_ws");
}

#[tokio::test]
async fn workspace_manager_create_and_list() {
    let tmp = TempDir::new().unwrap();
    let mut mgr = WorkspaceManager::new(tmp.path().to_path_buf());

    mgr.create("client_alpha").await.unwrap();
    mgr.create("client_beta").await.unwrap();

    let names = mgr.list();
    assert_eq!(names, vec!["client_alpha", "client_beta"]);
}

#[tokio::test]
async fn workspace_manager_create_rejects_duplicate() {
    let tmp = TempDir::new().unwrap();
    let mut mgr = WorkspaceManager::new(tmp.path().to_path_buf());

    mgr.create("client_a").await.unwrap();
    let result = mgr.create("client_a").await;
    assert!(result.is_err());
}

#[tokio::test]
async fn workspace_manager_create_rejects_invalid_name() {
    let tmp = TempDir::new().unwrap();
    let mut mgr = WorkspaceManager::new(tmp.path().to_path_buf());

    assert!(mgr.create("").await.is_err());
    assert!(mgr.create("bad name").await.is_err());
    assert!(mgr.create("../escape").await.is_err());
}

#[tokio::test]
async fn workspace_manager_switch_and_active() {
    let tmp = TempDir::new().unwrap();
    let mut mgr = WorkspaceManager::new(tmp.path().to_path_buf());

    mgr.create("ws_one").await.unwrap();
    assert!(mgr.active_profile().is_none());

    mgr.switch("ws_one").unwrap();
    assert_eq!(mgr.active_name(), Some("ws_one"));
    assert!(mgr.active_profile().is_some());
}

#[test]
fn workspace_manager_switch_nonexistent_fails() {
    let mgr = WorkspaceManager::new(PathBuf::from("/tmp/nonexistent"));
    let mut mgr = mgr;
    assert!(mgr.switch("no_such_ws").is_err());
}

#[tokio::test]
async fn workspace_manager_load_profiles_from_disk() {
    let tmp = TempDir::new().unwrap();
    let mut mgr = WorkspaceManager::new(tmp.path().to_path_buf());

    // Create a workspace via the manager
    mgr.create("loaded_ws").await.unwrap();

    // Create a fresh manager and load from disk
    let mut mgr2 = WorkspaceManager::new(tmp.path().to_path_buf());
    mgr2.load_profiles().await.unwrap();

    assert_eq!(mgr2.list(), vec!["loaded_ws"]);
    let profile = mgr2.get("loaded_ws").unwrap();
    assert_eq!(profile.name, "loaded_ws");
}

#[tokio::test]
async fn workspace_manager_export_redacts_credentials() {
    let tmp = TempDir::new().unwrap();
    let mut mgr = WorkspaceManager::new(tmp.path().to_path_buf());
    mgr.create("export_test").await.unwrap();

    // Manually set a credential profile
    if let Some(profile) = mgr.profiles.get_mut("export_test") {
        profile.credential_profile = Some("secret-cred-id".to_string());
    }

    let exported = mgr.export("export_test").unwrap();
    assert!(exported.contains("***"));
    assert!(!exported.contains("secret-cred-id"));
}
