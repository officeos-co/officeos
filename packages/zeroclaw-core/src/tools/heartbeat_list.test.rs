use super::*;
use crate::tools::traits::Tool;

#[tokio::test]
async fn list_empty_workspace() {
    let tmp = tempfile::tempdir().unwrap();
    let tool = HeartbeatListTool::new(tmp.path().to_path_buf());
    let result = tool.execute(json!({})).await.unwrap();
    assert!(result.success);
    assert!(result.output.contains("No heartbeat tasks"));
}

#[tokio::test]
async fn list_tasks_from_yaml() {
    let tmp = tempfile::tempdir().unwrap();
    std::fs::write(
        tmp.path().join("HEARTBEAT.md"),
        "tasks:\n- name: inbox\n  interval: 30m\n  prompt: \"Check email\"\n  priority: high\n",
    )
    .unwrap();

    let tool = HeartbeatListTool::new(tmp.path().to_path_buf());
    let result = tool.execute(json!({})).await.unwrap();
    assert!(result.success);
    assert!(result.output.contains("inbox"));
    assert!(result.output.contains("high"));
}
