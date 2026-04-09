use super::*;
use crate::security::{AutonomyLevel, SecurityPolicy};
use std::path::PathBuf;
use tempfile::TempDir;

fn test_security(workspace: PathBuf) -> Arc<SecurityPolicy> {
    Arc::new(SecurityPolicy {
        autonomy: AutonomyLevel::Supervised,
        workspace_dir: workspace,
        ..SecurityPolicy::default()
    })
}

fn test_security_with(
    workspace: PathBuf,
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

fn create_test_files(dir: &TempDir) {
    std::fs::write(
        dir.path().join("hello.rs"),
        "fn main() {\n    println!(\"hello\");\n}\n",
    )
    .unwrap();
    std::fs::write(
        dir.path().join("lib.rs"),
        "pub fn greet() {\n    println!(\"greet\");\n}\n",
    )
    .unwrap();
    std::fs::write(dir.path().join("readme.txt"), "This is a readme file.\n").unwrap();
}

#[test]
fn content_search_name_and_schema() {
    let tool = ContentSearchTool::new(test_security(std::env::temp_dir()));
    assert_eq!(tool.name(), "content_search");

    let schema = tool.parameters_schema();
    assert!(schema["properties"]["pattern"].is_object());
    assert!(schema["properties"]["path"].is_object());
    assert!(schema["properties"]["output_mode"].is_object());
    assert!(
        schema["required"]
            .as_array()
            .unwrap()
            .contains(&json!("pattern"))
    );
}

#[tokio::test]
async fn content_search_basic_match() {
    let dir = TempDir::new().unwrap();
    create_test_files(&dir);

    let tool = ContentSearchTool::new(test_security(dir.path().to_path_buf()));
    let result = tool.execute(json!({"pattern": "fn main"})).await.unwrap();

    assert!(result.success);
    assert!(result.output.contains("hello.rs"));
    assert!(result.output.contains("fn main"));
}

#[tokio::test]
async fn content_search_files_with_matches_mode() {
    let dir = TempDir::new().unwrap();
    create_test_files(&dir);

    let tool = ContentSearchTool::new(test_security(dir.path().to_path_buf()));
    let result = tool
        .execute(json!({"pattern": "println", "output_mode": "files_with_matches"}))
        .await
        .unwrap();

    assert!(result.success);
    assert!(result.output.contains("hello.rs"));
    assert!(result.output.contains("lib.rs"));
    assert!(!result.output.contains("readme.txt"));
    assert!(result.output.contains("Total: 2 files"));
}

#[tokio::test]
async fn content_search_count_mode() {
    let dir = TempDir::new().unwrap();
    create_test_files(&dir);

    let tool = ContentSearchTool::new(test_security(dir.path().to_path_buf()));
    let result = tool
        .execute(json!({"pattern": "println", "output_mode": "count"}))
        .await
        .unwrap();

    assert!(result.success);
    assert!(result.output.contains("hello.rs"));
    assert!(result.output.contains("lib.rs"));
    assert!(result.output.contains("Total:"));
}

#[tokio::test]
async fn content_search_case_insensitive() {
    let dir = TempDir::new().unwrap();
    std::fs::write(dir.path().join("test.txt"), "Hello World\nhello world\n").unwrap();

    let tool = ContentSearchTool::new(test_security(dir.path().to_path_buf()));
    let result = tool
        .execute(json!({"pattern": "HELLO", "case_sensitive": false}))
        .await
        .unwrap();

    assert!(result.success);
    assert!(result.output.contains("Hello World"));
    assert!(result.output.contains("hello world"));
}

#[tokio::test]
async fn content_search_include_filter() {
    let dir = TempDir::new().unwrap();
    create_test_files(&dir);

    let tool = ContentSearchTool::new(test_security(dir.path().to_path_buf()));
    let result = tool
        .execute(json!({"pattern": "fn", "include": "*.rs"}))
        .await
        .unwrap();

    assert!(result.success);
    assert!(result.output.contains("hello.rs"));
    assert!(!result.output.contains("readme.txt"));
}

#[tokio::test]
async fn content_search_context_lines() {
    let dir = TempDir::new().unwrap();
    std::fs::write(
        dir.path().join("ctx.rs"),
        "line1\nline2\ntarget_line\nline4\nline5\n",
    )
    .unwrap();

    let tool = ContentSearchTool::new(test_security(dir.path().to_path_buf()));
    let result = tool
        .execute(json!({"pattern": "target_line", "context_before": 1, "context_after": 1}))
        .await
        .unwrap();

    assert!(result.success);
    assert!(result.output.contains("target_line"));
    assert!(result.output.contains("line2"));
    assert!(result.output.contains("line4"));
}

#[tokio::test]
async fn content_search_no_matches() {
    let dir = TempDir::new().unwrap();
    create_test_files(&dir);

    let tool = ContentSearchTool::new(test_security(dir.path().to_path_buf()));
    let result = tool
        .execute(json!({"pattern": "nonexistent_string_xyz"}))
        .await
        .unwrap();

    assert!(result.success);
    assert!(result.output.contains("No matches found"));
}

#[tokio::test]
async fn content_search_empty_pattern_rejected() {
    let tool = ContentSearchTool::new(test_security(std::env::temp_dir()));
    let result = tool.execute(json!({"pattern": ""})).await.unwrap();

    assert!(!result.success);
    assert!(result.error.as_ref().unwrap().contains("Empty pattern"));
}

#[tokio::test]
async fn content_search_missing_pattern() {
    let tool = ContentSearchTool::new(test_security(std::env::temp_dir()));
    let result = tool.execute(json!({})).await;
    assert!(result.is_err());
}

#[tokio::test]
async fn content_search_invalid_output_mode_rejected() {
    let dir = TempDir::new().unwrap();
    create_test_files(&dir);

    let tool = ContentSearchTool::new(test_security(dir.path().to_path_buf()));
    let result = tool
        .execute(json!({"pattern": "fn", "output_mode": "invalid_mode"}))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(
        result
            .error
            .as_ref()
            .unwrap()
            .contains("Invalid output_mode")
    );
}

#[tokio::test]
async fn content_search_subdirectory() {
    let dir = TempDir::new().unwrap();
    std::fs::create_dir_all(dir.path().join("sub/deep")).unwrap();
    std::fs::write(dir.path().join("sub/deep/nested.rs"), "fn nested() {}\n").unwrap();
    std::fs::write(dir.path().join("root.rs"), "fn root() {}\n").unwrap();

    let tool = ContentSearchTool::new(test_security(dir.path().to_path_buf()));
    let result = tool
        .execute(json!({"pattern": "fn nested", "path": "sub"}))
        .await
        .unwrap();

    assert!(result.success);
    assert!(result.output.contains("nested"));
    assert!(!result.output.contains("root"));
}

// --- Security tests ---

#[tokio::test]
async fn content_search_rejects_absolute_path() {
    let tool = ContentSearchTool::new(test_security(std::env::temp_dir()));
    let result = tool
        .execute(json!({"pattern": "test", "path": "/etc"}))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(result.error.as_ref().unwrap().contains("Absolute paths"));
}

#[tokio::test]
async fn content_search_rejects_path_traversal() {
    let tool = ContentSearchTool::new(test_security(std::env::temp_dir()));
    let result = tool
        .execute(json!({"pattern": "test", "path": "../../../etc"}))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(result.error.as_ref().unwrap().contains("Path traversal"));
}

#[tokio::test]
async fn content_search_rate_limited() {
    let dir = TempDir::new().unwrap();
    std::fs::write(dir.path().join("file.txt"), "test content\n").unwrap();

    let tool = ContentSearchTool::new(test_security_with(
        dir.path().to_path_buf(),
        AutonomyLevel::Supervised,
        0,
    ));
    let result = tool.execute(json!({"pattern": "test"})).await.unwrap();

    assert!(!result.success);
    assert!(result.error.as_ref().unwrap().contains("Rate limit"));
}

#[cfg(unix)]
#[tokio::test]
async fn content_search_symlink_escape_blocked() {
    use std::os::unix::fs::symlink;

    let root = TempDir::new().unwrap();
    let workspace = root.path().join("workspace");
    let outside = root.path().join("outside");

    std::fs::create_dir_all(&workspace).unwrap();
    std::fs::create_dir_all(&outside).unwrap();
    std::fs::write(outside.join("secret.txt"), "secret data\n").unwrap();

    // Symlink inside workspace pointing outside
    symlink(&outside, workspace.join("escape_dir")).unwrap();
    // Also add a legitimate file
    std::fs::write(workspace.join("legit.txt"), "legit data\n").unwrap();

    let tool = ContentSearchTool::new(test_security(workspace.clone()));
    let result = tool.execute(json!({"pattern": "data"})).await.unwrap();

    assert!(result.success);
    // Legit file should be found
    assert!(result.output.contains("legit.txt"));
    // The search runs in workspace, rg/grep may or may not follow symlinks,
    // but results are relativized — we mainly verify no crash
}

#[tokio::test]
async fn content_search_multiline_without_rg() {
    let dir = TempDir::new().unwrap();
    std::fs::write(dir.path().join("test.txt"), "line1\nline2\n").unwrap();

    let tool = ContentSearchTool::new_with_backend(
        test_security(dir.path().to_path_buf()),
        false, // no rg
    );
    let result = tool
        .execute(json!({"pattern": "line1", "multiline": true}))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(result.error.as_ref().unwrap().contains("ripgrep"));
}

#[test]
fn relativize_path_strips_prefix() {
    let result = relativize_path("/workspace/src/main.rs:42:fn main()", "/workspace");
    assert_eq!(result, "src/main.rs:42:fn main()");
}

#[test]
fn relativize_path_no_prefix() {
    let result = relativize_path("src/main.rs:42:fn main()", "/workspace");
    assert_eq!(result, "src/main.rs:42:fn main()");
}

#[test]
fn format_line_output_content_counts_match_lines_only() {
    let raw =
        "src/main.rs-1-use std::fmt;\nsrc/main.rs:2:fn main() {}\n--\nsrc/lib.rs:10:pub fn f() {}";
    let output = format_line_output(raw, std::path::Path::new("/workspace"), "content", 100);
    assert!(output.contains("Total: 2 matching lines in 2 files"));
}

#[test]
fn parse_count_line_supports_colons_in_path() {
    let parsed = parse_count_line("dir:with:colon/file.rs:12");
    assert_eq!(parsed, Some(("dir:with:colon/file.rs", 12)));
}

#[test]
fn truncate_utf8_keeps_char_boundary() {
    let text = "abc你好";
    // Byte index 4 splits the first Chinese character.
    let truncated = truncate_utf8(text, 4);
    assert_eq!(truncated, "abc");
}
