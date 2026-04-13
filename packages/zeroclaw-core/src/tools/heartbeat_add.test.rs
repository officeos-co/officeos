use super::*;
use crate::tools::traits::Tool;

#[tokio::test]
async fn add_task_creates_file() {
    let tmp = tempfile::tempdir().unwrap();
    let tool = HeartbeatAddTool::new(tmp.path().to_path_buf());
    let result = tool
        .execute(json!({
            "name": "inbox-triage",
            "prompt": "Check for urgent emails",
            "interval": "30m",
            "priority": "high"
        }))
        .await
        .unwrap();
    assert!(result.success);

    let content = std::fs::read_to_string(tmp.path().join("HEARTBEAT.md")).unwrap();
    assert!(content.contains("inbox-triage"));
}

#[tokio::test]
async fn add_duplicate_fails() {
    let tmp = tempfile::tempdir().unwrap();
    let tool = HeartbeatAddTool::new(tmp.path().to_path_buf());
    tool.execute(json!({"name": "test", "prompt": "do stuff"}))
        .await
        .unwrap();
    let result = tool
        .execute(json!({"name": "test", "prompt": "do other stuff"}))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.unwrap().contains("already exists"));
}
