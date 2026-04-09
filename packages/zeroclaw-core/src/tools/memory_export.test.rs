use super::*;
use crate::memory::SqliteMemory;
use tempfile::TempDir;

fn test_mem() -> (TempDir, Arc<dyn Memory>) {
    let tmp = TempDir::new().unwrap();
    let mem = SqliteMemory::new(tmp.path()).unwrap();
    (tmp, Arc::new(mem))
}

#[test]
fn name_and_schema() {
    let (_tmp, mem) = test_mem();
    let tool = MemoryExportTool::new(mem);
    assert_eq!(tool.name(), "memory_export");
    let schema = tool.parameters_schema();
    assert!(schema["properties"]["namespace"].is_object());
    assert!(schema["properties"]["session_id"].is_object());
    assert!(schema["properties"]["category"].is_object());
    assert!(schema["properties"]["since"].is_object());
    assert!(schema["properties"]["until"].is_object());
}

#[tokio::test]
async fn export_produces_valid_json_output() {
    let (_tmp, mem) = test_mem();
    mem.store("k1", "test data", MemoryCategory::Core, None)
        .await
        .unwrap();

    let tool = MemoryExportTool::new(mem);
    let result = tool.execute(json!({})).await.unwrap();
    assert!(result.success);
    let parsed: serde_json::Value = serde_json::from_str(&result.output).unwrap();
    assert!(parsed.is_array());
    assert_eq!(parsed.as_array().unwrap().len(), 1);
}

#[tokio::test]
async fn export_empty_database_returns_empty_array() {
    let (_tmp, mem) = test_mem();
    let tool = MemoryExportTool::new(mem);
    let result = tool.execute(json!({})).await.unwrap();
    assert!(result.success);
    let parsed: serde_json::Value = serde_json::from_str(&result.output).unwrap();
    assert!(parsed.is_array());
    assert!(parsed.as_array().unwrap().is_empty());
}

#[tokio::test]
async fn export_with_category_filter() {
    let (_tmp, mem) = test_mem();
    mem.store("k1", "core data", MemoryCategory::Core, None)
        .await
        .unwrap();
    mem.store("k2", "daily data", MemoryCategory::Daily, None)
        .await
        .unwrap();

    let tool = MemoryExportTool::new(mem);
    let result = tool.execute(json!({"category": "core"})).await.unwrap();
    assert!(result.success);
    let parsed: serde_json::Value = serde_json::from_str(&result.output).unwrap();
    let arr = parsed.as_array().unwrap();
    assert_eq!(arr.len(), 1);
    assert_eq!(arr[0]["category"], "core");
}

#[tokio::test]
async fn export_with_session_filter() {
    let (_tmp, mem) = test_mem();
    mem.store("k1", "sess-a data", MemoryCategory::Core, Some("sess-a"))
        .await
        .unwrap();
    mem.store("k2", "sess-b data", MemoryCategory::Core, Some("sess-b"))
        .await
        .unwrap();

    let tool = MemoryExportTool::new(mem);
    let result = tool.execute(json!({"session_id": "sess-a"})).await.unwrap();
    assert!(result.success);
    let parsed: serde_json::Value = serde_json::from_str(&result.output).unwrap();
    let arr = parsed.as_array().unwrap();
    assert_eq!(arr.len(), 1);
    assert_eq!(arr[0]["key"], "k1");
}
