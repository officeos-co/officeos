use super::*;
use crate::channels::session_store::SessionStore;
use crate::providers::traits::ChatMessage;
use tempfile::TempDir;

fn test_security() -> Arc<SecurityPolicy> {
    Arc::new(SecurityPolicy::default())
}

fn test_backend() -> (TempDir, Arc<dyn SessionBackend>) {
    let tmp = TempDir::new().unwrap();
    let store = SessionStore::new(tmp.path()).unwrap();
    (tmp, Arc::new(store))
}

fn seeded_backend() -> (TempDir, Arc<dyn SessionBackend>) {
    let tmp = TempDir::new().unwrap();
    let store = SessionStore::new(tmp.path()).unwrap();
    store
        .append("telegram__alice", &ChatMessage::user("Hello from Alice"))
        .unwrap();
    store
        .append(
            "telegram__alice",
            &ChatMessage::assistant("Hi Alice, how can I help?"),
        )
        .unwrap();
    store
        .append("discord__bob", &ChatMessage::user("Hey from Bob"))
        .unwrap();
    (tmp, Arc::new(store))
}

// ── SessionsListTool tests ──────────────────────────────────────

#[tokio::test]
async fn list_empty_sessions() {
    let (_tmp, backend) = test_backend();
    let tool = SessionsListTool::new(backend);
    let result = tool.execute(json!({})).await.unwrap();
    assert!(result.success);
    assert!(result.output.contains("No active sessions"));
}

#[tokio::test]
async fn list_sessions_shows_all() {
    let (_tmp, backend) = seeded_backend();
    let tool = SessionsListTool::new(backend);
    let result = tool.execute(json!({})).await.unwrap();
    assert!(result.success);
    assert!(result.output.contains("2 session(s)"));
    assert!(result.output.contains("telegram__alice"));
    assert!(result.output.contains("discord__bob"));
}

#[tokio::test]
async fn list_sessions_respects_limit() {
    let (_tmp, backend) = seeded_backend();
    let tool = SessionsListTool::new(backend);
    let result = tool.execute(json!({"limit": 1})).await.unwrap();
    assert!(result.success);
    assert!(result.output.contains("1 session(s)"));
}

#[tokio::test]
async fn list_sessions_extracts_channel() {
    let (_tmp, backend) = seeded_backend();
    let tool = SessionsListTool::new(backend);
    let result = tool.execute(json!({})).await.unwrap();
    assert!(result.output.contains("channel=telegram"));
    assert!(result.output.contains("channel=discord"));
}

#[test]
fn list_tool_name_and_schema() {
    let (_tmp, backend) = test_backend();
    let tool = SessionsListTool::new(backend);
    assert_eq!(tool.name(), "sessions_list");
    assert!(tool.parameters_schema()["properties"]["limit"].is_object());
}

// ── SessionsHistoryTool tests ───────────────────────────────────

#[tokio::test]
async fn history_empty_session() {
    let (_tmp, backend) = test_backend();
    let tool = SessionsHistoryTool::new(backend, test_security());
    let result = tool
        .execute(json!({"session_id": "nonexistent"}))
        .await
        .unwrap();
    assert!(result.success);
    assert!(result.output.contains("No messages found"));
}

#[tokio::test]
async fn history_returns_messages() {
    let (_tmp, backend) = seeded_backend();
    let tool = SessionsHistoryTool::new(backend, test_security());
    let result = tool
        .execute(json!({"session_id": "telegram__alice"}))
        .await
        .unwrap();
    assert!(result.success);
    assert!(result.output.contains("showing 2/2 messages"));
    assert!(result.output.contains("[user] Hello from Alice"));
    assert!(result.output.contains("[assistant] Hi Alice"));
}

#[tokio::test]
async fn history_respects_limit() {
    let (_tmp, backend) = seeded_backend();
    let tool = SessionsHistoryTool::new(backend, test_security());
    let result = tool
        .execute(json!({"session_id": "telegram__alice", "limit": 1}))
        .await
        .unwrap();
    assert!(result.success);
    assert!(result.output.contains("showing 1/2 messages"));
    // Should show only the last message
    assert!(result.output.contains("[assistant]"));
    assert!(!result.output.contains("[user] Hello from Alice"));
}

#[tokio::test]
async fn history_missing_session_id() {
    let (_tmp, backend) = test_backend();
    let tool = SessionsHistoryTool::new(backend, test_security());
    let result = tool.execute(json!({})).await;
    assert!(result.is_err());
    assert!(result.unwrap_err().to_string().contains("session_id"));
}

#[tokio::test]
async fn history_rejects_empty_session_id() {
    let (_tmp, backend) = test_backend();
    let tool = SessionsHistoryTool::new(backend, test_security());
    let result = tool.execute(json!({"session_id": "   "})).await.unwrap();
    assert!(!result.success);
    assert!(result.error.unwrap().contains("Invalid"));
}

#[test]
fn history_tool_name_and_schema() {
    let (_tmp, backend) = test_backend();
    let tool = SessionsHistoryTool::new(backend, test_security());
    assert_eq!(tool.name(), "sessions_history");
    let schema = tool.parameters_schema();
    assert!(schema["properties"]["session_id"].is_object());
    assert!(
        schema["required"]
            .as_array()
            .unwrap()
            .contains(&json!("session_id"))
    );
}

// ── SessionsSendTool tests ──────────────────────────────────────

#[tokio::test]
async fn send_appends_message() {
    let (_tmp, backend) = test_backend();
    let tool = SessionsSendTool::new(backend.clone(), test_security());
    let result = tool
        .execute(json!({
            "session_id": "telegram__alice",
            "message": "Hello from another agent"
        }))
        .await
        .unwrap();
    assert!(result.success);
    assert!(result.output.contains("Message sent"));

    // Verify message was appended
    let messages = backend.load("telegram__alice");
    assert_eq!(messages.len(), 1);
    assert_eq!(messages[0].role, "user");
    assert_eq!(messages[0].content, "Hello from another agent");
}

#[tokio::test]
async fn send_to_existing_session() {
    let (_tmp, backend) = seeded_backend();
    let tool = SessionsSendTool::new(backend.clone(), test_security());
    let result = tool
        .execute(json!({
            "session_id": "telegram__alice",
            "message": "Inter-agent message"
        }))
        .await
        .unwrap();
    assert!(result.success);

    let messages = backend.load("telegram__alice");
    assert_eq!(messages.len(), 3);
    assert_eq!(messages[2].content, "Inter-agent message");
}

#[tokio::test]
async fn send_rejects_empty_message() {
    let (_tmp, backend) = test_backend();
    let tool = SessionsSendTool::new(backend, test_security());
    let result = tool
        .execute(json!({
            "session_id": "telegram__alice",
            "message": "   "
        }))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.unwrap().contains("empty"));
}

#[tokio::test]
async fn send_rejects_empty_session_id() {
    let (_tmp, backend) = test_backend();
    let tool = SessionsSendTool::new(backend, test_security());
    let result = tool
        .execute(json!({
            "session_id": "",
            "message": "hello"
        }))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.unwrap().contains("Invalid"));
}

#[tokio::test]
async fn send_rejects_non_alphanumeric_session_id() {
    let (_tmp, backend) = test_backend();
    let tool = SessionsSendTool::new(backend, test_security());
    let result = tool
        .execute(json!({
            "session_id": "///",
            "message": "hello"
        }))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.unwrap().contains("Invalid"));
}

#[tokio::test]
async fn send_missing_session_id() {
    let (_tmp, backend) = test_backend();
    let tool = SessionsSendTool::new(backend, test_security());
    let result = tool.execute(json!({"message": "hi"})).await;
    assert!(result.is_err());
    assert!(result.unwrap_err().to_string().contains("session_id"));
}

#[tokio::test]
async fn send_missing_message() {
    let (_tmp, backend) = test_backend();
    let tool = SessionsSendTool::new(backend, test_security());
    let result = tool.execute(json!({"session_id": "telegram__alice"})).await;
    assert!(result.is_err());
    assert!(result.unwrap_err().to_string().contains("message"));
}

#[test]
fn send_tool_name_and_schema() {
    let (_tmp, backend) = test_backend();
    let tool = SessionsSendTool::new(backend, test_security());
    assert_eq!(tool.name(), "sessions_send");
    let schema = tool.parameters_schema();
    assert!(
        schema["required"]
            .as_array()
            .unwrap()
            .contains(&json!("session_id"))
    );
    assert!(
        schema["required"]
            .as_array()
            .unwrap()
            .contains(&json!("message"))
    );
}
