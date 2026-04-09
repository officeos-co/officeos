use super::*;
use crate::security::{AutonomyLevel, SecurityPolicy};

fn test_security(workspace: std::path::PathBuf) -> Arc<SecurityPolicy> {
    Arc::new(SecurityPolicy {
        autonomy: AutonomyLevel::Supervised,
        workspace_dir: workspace,
        ..SecurityPolicy::default()
    })
}

fn test_security_with(
    workspace: std::path::PathBuf,
    autonomy: AutonomyLevel,
    max_actions_per_hour: u32,
) -> Arc<SecurityPolicy> {
    Arc::new(SecurityPolicy {
        autonomy,
        workspace_dir: workspace,
        max_actions_per_hour,
        ..SecurityPolicy::default()
    })
}

#[test]
fn file_write_name() {
    let tool = FileWriteTool::new(test_security(std::env::temp_dir()));
    assert_eq!(tool.name(), "file_write");
}

#[test]
fn file_write_schema_has_path_and_content() {
    let tool = FileWriteTool::new(test_security(std::env::temp_dir()));
    let schema = tool.parameters_schema();
    assert!(schema["properties"]["path"].is_object());
    assert!(schema["properties"]["content"].is_object());
    let required = schema["required"].as_array().unwrap();
    assert!(required.contains(&json!("path")));
    assert!(required.contains(&json!("content")));
}

#[tokio::test]
async fn file_write_creates_file() {
    let dir = std::env::temp_dir().join("zeroclaw_test_file_write");
    let _ = tokio::fs::remove_dir_all(&dir).await;
    tokio::fs::create_dir_all(&dir).await.unwrap();

    let tool = FileWriteTool::new(test_security(dir.clone()));
    let result = tool
        .execute(json!({"path": "out.txt", "content": "written!"}))
        .await
        .unwrap();
    assert!(result.success);
    assert!(result.output.contains("8 bytes"));

    let content = tokio::fs::read_to_string(dir.join("out.txt"))
        .await
        .unwrap();
    assert_eq!(content, "written!");

    let _ = tokio::fs::remove_dir_all(&dir).await;
}

#[tokio::test]
async fn file_write_creates_parent_dirs() {
    let dir = std::env::temp_dir().join("zeroclaw_test_file_write_nested");
    let _ = tokio::fs::remove_dir_all(&dir).await;
    tokio::fs::create_dir_all(&dir).await.unwrap();

    let tool = FileWriteTool::new(test_security(dir.clone()));
    let result = tool
        .execute(json!({"path": "a/b/c/deep.txt", "content": "deep"}))
        .await
        .unwrap();
    assert!(result.success);

    let content = tokio::fs::read_to_string(dir.join("a/b/c/deep.txt"))
        .await
        .unwrap();
    assert_eq!(content, "deep");

    let _ = tokio::fs::remove_dir_all(&dir).await;
}

#[tokio::test]
async fn file_write_normalizes_workspace_prefixed_relative_path() {
    let root = std::env::temp_dir().join("zeroclaw_test_file_write_workspace_prefixed");
    let workspace = root.join("workspace");
    let _ = tokio::fs::remove_dir_all(&root).await;
    tokio::fs::create_dir_all(&workspace).await.unwrap();

    let tool = FileWriteTool::new(test_security(workspace.clone()));
    let workspace_prefixed = workspace
        .strip_prefix(std::path::Path::new("/"))
        .unwrap()
        .join("nested/out.txt");
    let result = tool
        .execute(json!({
            "path": workspace_prefixed.to_string_lossy(),
            "content": "written!"
        }))
        .await
        .unwrap();
    assert!(result.success);

    let content = tokio::fs::read_to_string(workspace.join("nested/out.txt"))
        .await
        .unwrap();
    assert_eq!(content, "written!");
    assert!(!workspace.join(workspace_prefixed).exists());

    let _ = tokio::fs::remove_dir_all(&root).await;
}

#[tokio::test]
async fn file_write_overwrites_existing() {
    let dir = std::env::temp_dir().join("zeroclaw_test_file_write_overwrite");
    let _ = tokio::fs::remove_dir_all(&dir).await;
    tokio::fs::create_dir_all(&dir).await.unwrap();
    tokio::fs::write(dir.join("exist.txt"), "old")
        .await
        .unwrap();

    let tool = FileWriteTool::new(test_security(dir.clone()));
    let result = tool
        .execute(json!({"path": "exist.txt", "content": "new"}))
        .await
        .unwrap();
    assert!(result.success);

    let content = tokio::fs::read_to_string(dir.join("exist.txt"))
        .await
        .unwrap();
    assert_eq!(content, "new");

    let _ = tokio::fs::remove_dir_all(&dir).await;
}

#[tokio::test]
async fn file_write_blocks_path_traversal() {
    let dir = std::env::temp_dir().join("zeroclaw_test_file_write_traversal");
    let _ = tokio::fs::remove_dir_all(&dir).await;
    tokio::fs::create_dir_all(&dir).await.unwrap();

    let tool = FileWriteTool::new(test_security(dir.clone()));
    let result = tool
        .execute(json!({"path": "../../etc/evil", "content": "bad"}))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.as_ref().unwrap().contains("not allowed"));

    let _ = tokio::fs::remove_dir_all(&dir).await;
}

#[tokio::test]
async fn file_write_blocks_absolute_path() {
    let tool = FileWriteTool::new(test_security(std::env::temp_dir()));
    let result = tool
        .execute(json!({"path": "/etc/evil", "content": "bad"}))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.as_ref().unwrap().contains("not allowed"));
}

#[tokio::test]
async fn file_write_missing_path_param() {
    let tool = FileWriteTool::new(test_security(std::env::temp_dir()));
    let result = tool.execute(json!({"content": "data"})).await;
    assert!(result.is_err());
}

#[tokio::test]
async fn file_write_missing_content_param() {
    let tool = FileWriteTool::new(test_security(std::env::temp_dir()));
    let result = tool.execute(json!({"path": "file.txt"})).await;
    assert!(result.is_err());
}

#[tokio::test]
async fn file_write_empty_content() {
    let dir = std::env::temp_dir().join("zeroclaw_test_file_write_empty");
    let _ = tokio::fs::remove_dir_all(&dir).await;
    tokio::fs::create_dir_all(&dir).await.unwrap();

    let tool = FileWriteTool::new(test_security(dir.clone()));
    let result = tool
        .execute(json!({"path": "empty.txt", "content": ""}))
        .await
        .unwrap();
    assert!(result.success);
    assert!(result.output.contains("0 bytes"));

    let _ = tokio::fs::remove_dir_all(&dir).await;
}

#[cfg(unix)]
#[tokio::test]
async fn file_write_blocks_symlink_escape() {
    use std::os::unix::fs::symlink;

    let root = std::env::temp_dir().join("zeroclaw_test_file_write_symlink_escape");
    let workspace = root.join("workspace");
    let outside = root.join("outside");

    let _ = tokio::fs::remove_dir_all(&root).await;
    tokio::fs::create_dir_all(&workspace).await.unwrap();
    tokio::fs::create_dir_all(&outside).await.unwrap();

    symlink(&outside, workspace.join("escape_dir")).unwrap();

    let tool = FileWriteTool::new(test_security(workspace.clone()));
    let result = tool
        .execute(json!({"path": "escape_dir/hijack.txt", "content": "bad"}))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(
        result
            .error
            .as_deref()
            .unwrap_or("")
            .contains("escapes workspace")
    );
    assert!(!outside.join("hijack.txt").exists());

    let _ = tokio::fs::remove_dir_all(&root).await;
}

#[tokio::test]
async fn file_write_blocks_readonly_mode() {
    let dir = std::env::temp_dir().join("zeroclaw_test_file_write_readonly");
    let _ = tokio::fs::remove_dir_all(&dir).await;
    tokio::fs::create_dir_all(&dir).await.unwrap();

    let tool = FileWriteTool::new(test_security_with(dir.clone(), AutonomyLevel::ReadOnly, 20));
    let result = tool
        .execute(json!({"path": "out.txt", "content": "should-block"}))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(result.error.as_deref().unwrap_or("").contains("read-only"));
    assert!(!dir.join("out.txt").exists());

    let _ = tokio::fs::remove_dir_all(&dir).await;
}

#[tokio::test]
async fn file_write_blocks_when_rate_limited() {
    let dir = std::env::temp_dir().join("zeroclaw_test_file_write_rate_limited");
    let _ = tokio::fs::remove_dir_all(&dir).await;
    tokio::fs::create_dir_all(&dir).await.unwrap();

    let tool = FileWriteTool::new(test_security_with(
        dir.clone(),
        AutonomyLevel::Supervised,
        0,
    ));
    let result = tool
        .execute(json!({"path": "out.txt", "content": "should-block"}))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(
        result
            .error
            .as_deref()
            .unwrap_or("")
            .contains("Rate limit exceeded")
    );
    assert!(!dir.join("out.txt").exists());

    let _ = tokio::fs::remove_dir_all(&dir).await;
}

// ── §5.1 TOCTOU / symlink file write protection tests ────

#[cfg(unix)]
#[tokio::test]
async fn file_write_blocks_symlink_target_file() {
    use std::os::unix::fs::symlink;

    let root = std::env::temp_dir().join("zeroclaw_test_file_write_symlink_target");
    let workspace = root.join("workspace");
    let outside = root.join("outside");

    let _ = tokio::fs::remove_dir_all(&root).await;
    tokio::fs::create_dir_all(&workspace).await.unwrap();
    tokio::fs::create_dir_all(&outside).await.unwrap();

    // Create a file outside and symlink to it inside workspace
    tokio::fs::write(outside.join("target.txt"), "original")
        .await
        .unwrap();
    symlink(outside.join("target.txt"), workspace.join("linked.txt")).unwrap();

    let tool = FileWriteTool::new(test_security(workspace.clone()));
    let result = tool
        .execute(json!({"path": "linked.txt", "content": "overwritten"}))
        .await
        .unwrap();

    assert!(!result.success, "writing through symlink must be blocked");
    assert!(
        result.error.as_deref().unwrap_or("").contains("symlink"),
        "error should mention symlink"
    );

    // Verify original file was not modified
    let content = tokio::fs::read_to_string(outside.join("target.txt"))
        .await
        .unwrap();
    assert_eq!(content, "original", "original file must not be modified");

    let _ = tokio::fs::remove_dir_all(&root).await;
}

#[tokio::test]
async fn file_write_absolute_path_in_workspace() {
    let dir = std::env::temp_dir().join("zeroclaw_test_file_write_abs_path");
    let _ = tokio::fs::remove_dir_all(&dir).await;
    tokio::fs::create_dir_all(&dir).await.unwrap();

    // Canonicalize so the workspace dir matches resolved paths on macOS (/private/var/…)
    let dir = tokio::fs::canonicalize(&dir).await.unwrap();

    let tool = FileWriteTool::new(test_security(dir.clone()));

    // Pass an absolute path that is within the workspace
    let abs_path = dir.join("abs_test.txt");
    let result = tool
        .execute(json!({"path": abs_path.to_string_lossy().to_string(), "content": "absolute!"}))
        .await
        .unwrap();

    assert!(
        result.success,
        "writing via absolute workspace path should succeed, error: {:?}",
        result.error
    );

    let content = tokio::fs::read_to_string(dir.join("abs_test.txt"))
        .await
        .unwrap();
    assert_eq!(content, "absolute!");

    let _ = tokio::fs::remove_dir_all(&dir).await;
}

#[tokio::test]
async fn file_write_blocks_null_byte_in_path() {
    let dir = std::env::temp_dir().join("zeroclaw_test_file_write_null");
    let _ = tokio::fs::remove_dir_all(&dir).await;
    tokio::fs::create_dir_all(&dir).await.unwrap();

    let tool = FileWriteTool::new(test_security(dir.clone()));
    let result = tool
        .execute(json!({"path": "file\u{0000}.txt", "content": "bad"}))
        .await
        .unwrap();
    assert!(!result.success, "paths with null bytes must be blocked");

    let _ = tokio::fs::remove_dir_all(&dir).await;
}

#[tokio::test]
async fn file_write_blocks_runtime_config_path() {
    let root = std::env::temp_dir().join("zeroclaw_test_file_write_runtime_config");
    let workspace = root.join("workspace");
    let config_path = root.join("config.toml");
    let _ = tokio::fs::remove_dir_all(&root).await;
    tokio::fs::create_dir_all(&workspace).await.unwrap();

    let security = Arc::new(SecurityPolicy {
        autonomy: AutonomyLevel::Supervised,
        workspace_dir: workspace.clone(),
        workspace_only: false,
        allowed_roots: vec![root.clone()],
        forbidden_paths: vec![],
        ..SecurityPolicy::default()
    });
    let tool = FileWriteTool::new(security);
    let result = tool
        .execute(json!({
            "path": config_path.to_string_lossy(),
            "content": "auto_approve = [\"cron_add\"]"
        }))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(
        result
            .error
            .unwrap_or_default()
            .contains("runtime config/state file")
    );

    let _ = tokio::fs::remove_dir_all(&root).await;
}
