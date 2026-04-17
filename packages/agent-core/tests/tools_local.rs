//! Local tool integration tests. See API.md §10.

use tempfile::TempDir;
use zeroclaw_agent::tools::content_search::ContentSearchTool;
use zeroclaw_agent::tools::file_read::FileReadTool;
use zeroclaw_agent::tools::file_write::FileWriteTool;
use zeroclaw_agent::tools::memory_recall::MemoryRecallTool;
use zeroclaw_agent::tools::memory_store::MemoryStoreTool;
use zeroclaw_agent::tools::shell::ShellTool;
use zeroclaw_agent::tools::Tool;

/// Dispatch shell with `echo hi`. Assert success and output contains "hi".
#[tokio::test]
async fn test_shell_runs_command() {
    let tmp = TempDir::new().unwrap();
    let tool = ShellTool::new(tmp.path().to_path_buf());

    let result = tool
        .execute(serde_json::json!({"command": "echo hi"}))
        .await
        .expect("execute should not return anyhow error");

    assert!(result.success, "shell should succeed");
    assert!(result.output.contains("hi"), "output should contain 'hi'");
}

/// Write a file then read it back. Content should match.
#[tokio::test]
async fn test_file_write_then_read() {
    let tmp = TempDir::new().unwrap();
    let write_tool = FileWriteTool::new(tmp.path().to_path_buf());
    let read_tool = FileReadTool::new(tmp.path().to_path_buf());

    let content = "Hello, world!\nSecond line.";
    let write_result = write_tool
        .execute(serde_json::json!({
            "path": "test.txt",
            "content": content
        }))
        .await
        .expect("write should not error");

    assert!(write_result.success, "write should succeed");

    let read_result = read_tool
        .execute(serde_json::json!({"path": "test.txt"}))
        .await
        .expect("read should not error");

    assert!(read_result.success, "read should succeed");
    assert!(
        read_result.output.contains("Hello, world!"),
        "read output should contain written content"
    );
}

/// Store a memory then recall it. Should match.
#[tokio::test]
async fn test_memory_store_then_recall() {
    let tmp = TempDir::new().unwrap();
    let store = MemoryStoreTool::new(tmp.path().to_path_buf());
    let recall = MemoryRecallTool::new(tmp.path().to_path_buf());

    let store_result = store
        .execute(serde_json::json!({
            "key": "favorite_color",
            "content": "The user's favorite color is blue.",
            "category": "core"
        }))
        .await
        .expect("store should not error");

    assert!(store_result.success, "store should succeed");

    let recall_result = recall
        .execute(serde_json::json!({"query": "favorite color", "limit": 5}))
        .await
        .expect("recall should not error");

    assert!(recall_result.success, "recall should succeed");
    assert!(
        recall_result.output.contains("blue"),
        "recall should find the stored memory"
    );
}

/// Seed a tmpdir with files, then search for a pattern.
#[tokio::test]
async fn test_content_search_finds_pattern() {
    let tmp = TempDir::new().unwrap();

    // Seed some files.
    std::fs::write(
        tmp.path().join("notes.txt"),
        "The quick brown fox jumps over the lazy dog.",
    )
    .unwrap();
    std::fs::write(tmp.path().join("data.txt"), "No matching content here.").unwrap();

    let tool = ContentSearchTool::new(tmp.path().to_path_buf());

    let result = tool
        .execute(serde_json::json!({
            "pattern": "brown fox",
            "path": "."
        }))
        .await
        .expect("search should not error");

    assert!(result.success, "search should succeed");
    assert!(
        result.output.contains("brown fox"),
        "search output should contain the matching pattern"
    );
}
