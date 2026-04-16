use super::*;
use tempfile::TempDir;

#[test]
fn round_trip_append_and_load() {
    let tmp = TempDir::new().unwrap();
    let store = SessionStore::new(tmp.path()).unwrap();

    store
        .append("telegram_user123", &ChatMessage::user("hello"))
        .unwrap();
    store
        .append("telegram_user123", &ChatMessage::assistant("hi there"))
        .unwrap();

    let messages = store.load("telegram_user123");
    assert_eq!(messages.len(), 2);
    assert_eq!(messages[0].role, "user");
    assert_eq!(messages[0].content, "hello");
    assert_eq!(messages[1].role, "assistant");
    assert_eq!(messages[1].content, "hi there");
}

#[test]
fn load_nonexistent_session_returns_empty() {
    let tmp = TempDir::new().unwrap();
    let store = SessionStore::new(tmp.path()).unwrap();

    let messages = store.load("nonexistent");
    assert!(messages.is_empty());
}

#[test]
fn key_sanitization() {
    let tmp = TempDir::new().unwrap();
    let store = SessionStore::new(tmp.path()).unwrap();

    // Keys with special chars should be sanitized
    store
        .append("slack/thread:123/user", &ChatMessage::user("test"))
        .unwrap();

    let messages = store.load("slack/thread:123/user");
    assert_eq!(messages.len(), 1);
}

#[test]
fn list_sessions_returns_keys() {
    let tmp = TempDir::new().unwrap();
    let store = SessionStore::new(tmp.path()).unwrap();

    store
        .append("telegram_alice", &ChatMessage::user("hi"))
        .unwrap();
    store
        .append("discord_bob", &ChatMessage::user("hey"))
        .unwrap();

    let mut sessions = store.list_sessions();
    sessions.sort();
    assert_eq!(sessions.len(), 2);
    assert!(sessions.contains(&"discord_bob".to_string()));
    assert!(sessions.contains(&"telegram_alice".to_string()));
}

#[test]
fn append_is_truly_append_only() {
    let tmp = TempDir::new().unwrap();
    let store = SessionStore::new(tmp.path()).unwrap();
    let key = "test_session";

    store.append(key, &ChatMessage::user("msg1")).unwrap();
    store.append(key, &ChatMessage::user("msg2")).unwrap();

    // Read raw file to verify append-only format
    let path = store.session_path(key);
    let content = std::fs::read_to_string(&path).unwrap();
    let lines: Vec<&str> = content.trim().lines().collect();
    assert_eq!(lines.len(), 2);
}

#[test]
fn remove_last_drops_final_message() {
    let tmp = TempDir::new().unwrap();
    let store = SessionStore::new(tmp.path()).unwrap();

    store
        .append("rm_test", &ChatMessage::user("first"))
        .unwrap();
    store
        .append("rm_test", &ChatMessage::user("second"))
        .unwrap();

    assert!(store.remove_last("rm_test").unwrap());
    let messages = store.load("rm_test");
    assert_eq!(messages.len(), 1);
    assert_eq!(messages[0].content, "first");
}

#[test]
fn remove_last_empty_returns_false() {
    let tmp = TempDir::new().unwrap();
    let store = SessionStore::new(tmp.path()).unwrap();
    assert!(!store.remove_last("nonexistent").unwrap());
}

#[test]
fn compact_removes_corrupt_lines() {
    let tmp = TempDir::new().unwrap();
    let store = SessionStore::new(tmp.path()).unwrap();
    let key = "compact_test";

    let path = store.session_path(key);
    std::fs::create_dir_all(path.parent().unwrap()).unwrap();
    let mut file = std::fs::File::create(&path).unwrap();
    writeln!(file, r#"{{"role":"user","content":"ok"}}"#).unwrap();
    writeln!(file, "corrupt line").unwrap();
    writeln!(file, r#"{{"role":"assistant","content":"hi"}}"#).unwrap();

    store.compact(key).unwrap();

    let raw = std::fs::read_to_string(&path).unwrap();
    assert_eq!(raw.trim().lines().count(), 2);
}

#[test]
fn session_backend_trait_works_via_dyn() {
    let tmp = TempDir::new().unwrap();
    let store = SessionStore::new(tmp.path()).unwrap();
    let backend: &dyn SessionBackend = &store;

    backend
        .append("trait_test", &ChatMessage::user("hello"))
        .unwrap();
    let msgs = backend.load("trait_test");
    assert_eq!(msgs.len(), 1);
}

#[test]
fn handles_corrupt_lines_gracefully() {
    let tmp = TempDir::new().unwrap();
    let store = SessionStore::new(tmp.path()).unwrap();
    let key = "corrupt_test";

    // Write valid message + corrupt line + valid message
    let path = store.session_path(key);
    std::fs::create_dir_all(path.parent().unwrap()).unwrap();
    let mut file = std::fs::File::create(&path).unwrap();
    writeln!(file, r#"{{"role":"user","content":"hello"}}"#).unwrap();
    writeln!(file, "this is not valid json").unwrap();
    writeln!(file, r#"{{"role":"assistant","content":"world"}}"#).unwrap();

    let messages = store.load(key);
    assert_eq!(messages.len(), 2);
    assert_eq!(messages[0].content, "hello");
    assert_eq!(messages[1].content, "world");
}

#[test]
fn delete_session_removes_jsonl_file() {
    let tmp = TempDir::new().unwrap();
    let store = SessionStore::new(tmp.path()).unwrap();
    let key = "delete_test";

    store.append(key, &ChatMessage::user("hello")).unwrap();
    assert_eq!(store.load(key).len(), 1);

    let deleted = store.delete_session(key).unwrap();
    assert!(deleted);
    assert!(store.load(key).is_empty());
    assert!(!store.session_path(key).exists());
}

#[test]
fn delete_session_nonexistent_returns_false() {
    let tmp = TempDir::new().unwrap();
    let store = SessionStore::new(tmp.path()).unwrap();

    let deleted = store.delete_session("nonexistent").unwrap();
    assert!(!deleted);
}

#[test]
fn delete_session_via_trait() {
    let tmp = TempDir::new().unwrap();
    let store = SessionStore::new(tmp.path()).unwrap();
    let backend: &dyn SessionBackend = &store;

    backend
        .append("trait_delete", &ChatMessage::user("hello"))
        .unwrap();
    assert_eq!(backend.load("trait_delete").len(), 1);

    let deleted = backend.delete_session("trait_delete").unwrap();
    assert!(deleted);
    assert!(backend.load("trait_delete").is_empty());
}
