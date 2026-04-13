use super::*;
use crate::tools::traits::Tool;

#[tokio::test]
async fn update_task_priority() {
    let tmp = tempfile::tempdir().unwrap();
    std::fs::write(
        tmp.path().join("HEARTBEAT.md"),
        "tasks:\n- name: test\n  prompt: \"Do stuff\"\n  priority: medium\n  status: active\n",
    )
    .unwrap();

    let tool = HeartbeatUpdateTool::new(tmp.path().to_path_buf());
    let result = tool
        .execute(json!({"name": "test", "priority": "high"}))
        .await
        .unwrap();
    assert!(result.success);

    let content = std::fs::read_to_string(tmp.path().join("HEARTBEAT.md")).unwrap();
    assert!(content.contains("high"));
}

#[tokio::test]
async fn update_nonexistent_fails() {
    let tmp = tempfile::tempdir().unwrap();
    let tool = HeartbeatUpdateTool::new(tmp.path().to_path_buf());
    let result = tool
        .execute(json!({"name": "nope"}))
        .await
        .unwrap();
    assert!(!result.success);
}
