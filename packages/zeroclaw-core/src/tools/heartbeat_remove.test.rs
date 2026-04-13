use super::*;
use crate::tools::traits::Tool;

#[tokio::test]
async fn remove_task() {
    let tmp = tempfile::tempdir().unwrap();
    std::fs::write(
        tmp.path().join("HEARTBEAT.md"),
        "tasks:\n- name: test\n  prompt: \"Do stuff\"\n  priority: medium\n  status: active\n",
    )
    .unwrap();

    let tool = HeartbeatRemoveTool::new(tmp.path().to_path_buf());
    let result = tool.execute(json!({"name": "test"})).await.unwrap();
    assert!(result.success);

    let content = std::fs::read_to_string(tmp.path().join("HEARTBEAT.md")).unwrap();
    assert!(!content.contains("test"));
}

#[tokio::test]
async fn remove_nonexistent_fails() {
    let tmp = tempfile::tempdir().unwrap();
    let tool = HeartbeatRemoveTool::new(tmp.path().to_path_buf());
    let result = tool.execute(json!({"name": "nope"})).await.unwrap();
    assert!(!result.success);
}
