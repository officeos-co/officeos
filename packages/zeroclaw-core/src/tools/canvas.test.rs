use super::*;

#[test]
fn canvas_store_render_and_snapshot() {
    let store = CanvasStore::new();
    let frame = store.render("test", "html", "<h1>Hello</h1>").unwrap();
    assert_eq!(frame.content_type, "html");
    assert_eq!(frame.content, "<h1>Hello</h1>");

    let snapshot = store.snapshot("test").unwrap();
    assert_eq!(snapshot.frame_id, frame.frame_id);
    assert_eq!(snapshot.content, "<h1>Hello</h1>");
}

#[test]
fn canvas_store_snapshot_empty_returns_none() {
    let store = CanvasStore::new();
    assert!(store.snapshot("nonexistent").is_none());
}

#[test]
fn canvas_store_clear_removes_content() {
    let store = CanvasStore::new();
    store.render("test", "html", "<p>content</p>");
    assert!(store.snapshot("test").is_some());

    let cleared = store.clear("test");
    assert!(cleared);
    assert!(store.snapshot("test").is_none());
}

#[test]
fn canvas_store_clear_nonexistent_returns_false() {
    let store = CanvasStore::new();
    assert!(!store.clear("nonexistent"));
}

#[test]
fn canvas_store_history_tracks_frames() {
    let store = CanvasStore::new();
    store.render("test", "html", "frame1");
    store.render("test", "html", "frame2");
    store.render("test", "html", "frame3");

    let history = store.history("test");
    assert_eq!(history.len(), 3);
    assert_eq!(history[0].content, "frame1");
    assert_eq!(history[2].content, "frame3");
}

#[test]
fn canvas_store_history_limit_enforced() {
    let store = CanvasStore::new();
    for i in 0..60 {
        store.render("test", "html", &format!("frame{i}"));
    }

    let history = store.history("test");
    assert_eq!(history.len(), MAX_HISTORY_FRAMES);
    // Oldest frames should have been dropped
    assert_eq!(history[0].content, "frame10");
}

#[test]
fn canvas_store_list_returns_canvas_ids() {
    let store = CanvasStore::new();
    store.render("alpha", "html", "a");
    store.render("beta", "svg", "b");

    let mut ids = store.list();
    ids.sort();
    assert_eq!(ids, vec!["alpha", "beta"]);
}

#[test]
fn canvas_store_subscribe_receives_updates() {
    let store = CanvasStore::new();
    let mut rx = store.subscribe("test").unwrap();
    store.render("test", "html", "<p>live</p>");

    let frame = rx.try_recv().unwrap();
    assert_eq!(frame.content, "<p>live</p>");
}

#[tokio::test]
async fn canvas_tool_render_action() {
    let store = CanvasStore::new();
    let tool = CanvasTool::new(store.clone());
    let result = tool
        .execute(json!({
            "action": "render",
            "canvas_id": "test",
            "content_type": "html",
            "content": "<h1>Hello World</h1>"
        }))
        .await
        .unwrap();
    assert!(result.success);
    assert!(result.output.contains("Rendered html content"));

    let snapshot = store.snapshot("test").unwrap();
    assert_eq!(snapshot.content, "<h1>Hello World</h1>");
}

#[tokio::test]
async fn canvas_tool_snapshot_action() {
    let store = CanvasStore::new();
    store.render("test", "html", "<p>snap</p>");
    let tool = CanvasTool::new(store);
    let result = tool
        .execute(json!({"action": "snapshot", "canvas_id": "test"}))
        .await
        .unwrap();
    assert!(result.success);
    assert!(result.output.contains("<p>snap</p>"));
}

#[tokio::test]
async fn canvas_tool_snapshot_empty() {
    let store = CanvasStore::new();
    let tool = CanvasTool::new(store);
    let result = tool
        .execute(json!({"action": "snapshot", "canvas_id": "empty"}))
        .await
        .unwrap();
    assert!(result.success);
    assert!(result.output.contains("empty"));
}

#[tokio::test]
async fn canvas_tool_clear_action() {
    let store = CanvasStore::new();
    store.render("test", "html", "<p>clear me</p>");
    let tool = CanvasTool::new(store.clone());
    let result = tool
        .execute(json!({"action": "clear", "canvas_id": "test"}))
        .await
        .unwrap();
    assert!(result.success);
    assert!(result.output.contains("cleared"));
    assert!(store.snapshot("test").is_none());
}

#[tokio::test]
async fn canvas_tool_eval_action() {
    let store = CanvasStore::new();
    let tool = CanvasTool::new(store.clone());
    let result = tool
        .execute(json!({
            "action": "eval",
            "canvas_id": "test",
            "expression": "document.title"
        }))
        .await
        .unwrap();
    assert!(result.success);
    assert!(result.output.contains("Eval request sent"));

    let snapshot = store.snapshot("test").unwrap();
    assert_eq!(snapshot.content_type, "eval");
    assert_eq!(snapshot.content, "document.title");
}

#[tokio::test]
async fn canvas_tool_unknown_action() {
    let store = CanvasStore::new();
    let tool = CanvasTool::new(store);
    let result = tool.execute(json!({"action": "invalid"})).await.unwrap();
    assert!(!result.success);
    assert!(result.error.as_ref().unwrap().contains("Unknown action"));
}

#[tokio::test]
async fn canvas_tool_missing_action() {
    let store = CanvasStore::new();
    let tool = CanvasTool::new(store);
    let result = tool.execute(json!({})).await.unwrap();
    assert!(!result.success);
    assert!(result.error.as_ref().unwrap().contains("action"));
}

#[tokio::test]
async fn canvas_tool_render_missing_content() {
    let store = CanvasStore::new();
    let tool = CanvasTool::new(store);
    let result = tool
        .execute(json!({"action": "render", "canvas_id": "test"}))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.as_ref().unwrap().contains("content"));
}

#[tokio::test]
async fn canvas_tool_render_content_too_large() {
    let store = CanvasStore::new();
    let tool = CanvasTool::new(store);
    let big_content = "x".repeat(MAX_CONTENT_SIZE + 1);
    let result = tool
        .execute(json!({
            "action": "render",
            "canvas_id": "test",
            "content": big_content
        }))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.as_ref().unwrap().contains("maximum size"));
}

#[tokio::test]
async fn canvas_tool_default_canvas_id() {
    let store = CanvasStore::new();
    let tool = CanvasTool::new(store.clone());
    let result = tool
        .execute(json!({
            "action": "render",
            "content_type": "html",
            "content": "<p>default</p>"
        }))
        .await
        .unwrap();
    assert!(result.success);
    assert!(store.snapshot("default").is_some());
}

#[test]
fn canvas_store_enforces_max_canvas_count() {
    let store = CanvasStore::new();
    // Create MAX_CANVAS_COUNT canvases
    for i in 0..MAX_CANVAS_COUNT {
        assert!(
            store
                .render(&format!("canvas_{i}"), "html", "content")
                .is_some()
        );
    }
    // The next new canvas should be rejected
    assert!(store.render("one_too_many", "html", "content").is_none());
    // But rendering to an existing canvas should still work
    assert!(store.render("canvas_0", "html", "updated").is_some());
}

#[tokio::test]
async fn canvas_tool_eval_missing_expression() {
    let store = CanvasStore::new();
    let tool = CanvasTool::new(store);
    let result = tool
        .execute(json!({"action": "eval", "canvas_id": "test"}))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.as_ref().unwrap().contains("expression"));
}
