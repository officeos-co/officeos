use super::*;
use crate::memory::traits::{ExportFilter, Memory, MemoryCategory};
use tempfile::TempDir;

fn make_memory() -> (TempDir, MarkdownMemory) {
    let tmp = TempDir::new().unwrap();
    let mem = MarkdownMemory::new(tmp.path()).unwrap();
    (tmp, mem)
}

#[tokio::test]
async fn test_store_and_get() {
    let (_tmp, mem) = make_memory();

    mem.store("prefs", "dark mode enabled", MemoryCategory::Core, None)
        .await
        .unwrap();

    let entry = mem.get("prefs").await.unwrap().expect("entry should exist");
    assert_eq!(entry.key, "prefs");
    assert_eq!(entry.content, "dark mode enabled");
    assert_eq!(entry.category, MemoryCategory::Core);
}

#[tokio::test]
async fn test_store_updates_existing() {
    let (_tmp, mem) = make_memory();

    mem.store("prefs", "v1", MemoryCategory::Core, None)
        .await
        .unwrap();
    mem.store("prefs", "v2", MemoryCategory::Core, None)
        .await
        .unwrap();

    let entry = mem.get("prefs").await.unwrap().unwrap();
    assert_eq!(entry.content, "v2");
    assert_eq!(mem.count().await.unwrap(), 1);
}

#[tokio::test]
async fn test_forget() {
    let (_tmp, mem) = make_memory();

    mem.store("temp", "temporary data", MemoryCategory::Daily, None)
        .await
        .unwrap();
    assert!(mem.forget("temp").await.unwrap());
    assert!(mem.get("temp").await.unwrap().is_none());
    assert!(!mem.forget("nonexistent").await.unwrap());
}

#[tokio::test]
async fn test_list_by_category() {
    let (_tmp, mem) = make_memory();

    mem.store("a", "core stuff", MemoryCategory::Core, None)
        .await
        .unwrap();
    mem.store("b", "daily stuff", MemoryCategory::Daily, None)
        .await
        .unwrap();
    mem.store("c", "more core", MemoryCategory::Core, None)
        .await
        .unwrap();

    let core = mem.list(Some(&MemoryCategory::Core), None).await.unwrap();
    assert_eq!(core.len(), 2);

    let daily = mem.list(Some(&MemoryCategory::Daily), None).await.unwrap();
    assert_eq!(daily.len(), 1);

    let all = mem.list(None, None).await.unwrap();
    assert_eq!(all.len(), 3);
}

#[tokio::test]
async fn test_recall_keyword_search() {
    let (_tmp, mem) = make_memory();

    mem.store("rust", "Rust is a systems programming language", MemoryCategory::Core, None)
        .await
        .unwrap();
    mem.store("python", "Python is great for scripting", MemoryCategory::Core, None)
        .await
        .unwrap();
    mem.store("js", "JavaScript runs in the browser", MemoryCategory::Daily, None)
        .await
        .unwrap();

    let results = mem.recall("Rust programming", 10, None, None, None).await.unwrap();
    assert!(!results.is_empty());
    assert_eq!(results[0].key, "rust");
}

#[tokio::test]
async fn test_recall_empty_query_returns_all() {
    let (_tmp, mem) = make_memory();

    mem.store("a", "alpha", MemoryCategory::Core, None).await.unwrap();
    mem.store("b", "beta", MemoryCategory::Core, None).await.unwrap();

    let results = mem.recall("", 10, None, None, None).await.unwrap();
    assert_eq!(results.len(), 2);
}

#[tokio::test]
async fn test_recall_with_limit() {
    let (_tmp, mem) = make_memory();

    for i in 0..5 {
        mem.store(
            &format!("item_{i}"),
            &format!("content about testing item {i}"),
            MemoryCategory::Core,
            None,
        )
        .await
        .unwrap();
    }

    let results = mem.recall("testing", 2, None, None, None).await.unwrap();
    assert_eq!(results.len(), 2);
}

#[tokio::test]
async fn test_session_filtering() {
    let (_tmp, mem) = make_memory();

    mem.store("a", "session one data", MemoryCategory::Daily, Some("s1"))
        .await
        .unwrap();
    mem.store("b", "session two data", MemoryCategory::Daily, Some("s2"))
        .await
        .unwrap();

    let s1 = mem.list(None, Some("s1")).await.unwrap();
    assert_eq!(s1.len(), 1);
    assert_eq!(s1[0].key, "a");
}

#[tokio::test]
async fn test_count() {
    let (_tmp, mem) = make_memory();

    assert_eq!(mem.count().await.unwrap(), 0);
    mem.store("x", "data", MemoryCategory::Core, None).await.unwrap();
    assert_eq!(mem.count().await.unwrap(), 1);
    mem.store("y", "data", MemoryCategory::Daily, None).await.unwrap();
    assert_eq!(mem.count().await.unwrap(), 2);
    mem.forget("x").await.unwrap();
    assert_eq!(mem.count().await.unwrap(), 1);
}

#[tokio::test]
async fn test_health_check() {
    let (_tmp, mem) = make_memory();
    assert!(mem.health_check().await);
}

#[tokio::test]
async fn test_name() {
    let (_tmp, mem) = make_memory();
    assert_eq!(mem.name(), "markdown");
}

#[tokio::test]
async fn test_purge_namespace() {
    let (_tmp, mem) = make_memory();

    mem.store_with_metadata("a", "ns1 data", MemoryCategory::Core, None, Some("ns1"), None)
        .await
        .unwrap();
    mem.store_with_metadata("b", "ns2 data", MemoryCategory::Core, None, Some("ns2"), None)
        .await
        .unwrap();
    mem.store_with_metadata("c", "ns1 again", MemoryCategory::Daily, None, Some("ns1"), None)
        .await
        .unwrap();

    let purged = mem.purge_namespace("ns1").await.unwrap();
    assert_eq!(purged, 2);
    assert_eq!(mem.count().await.unwrap(), 1);
}

#[tokio::test]
async fn test_purge_session() {
    let (_tmp, mem) = make_memory();

    mem.store("a", "s1 data", MemoryCategory::Core, Some("session-1"))
        .await
        .unwrap();
    mem.store("b", "s2 data", MemoryCategory::Core, Some("session-2"))
        .await
        .unwrap();

    let purged = mem.purge_session("session-1").await.unwrap();
    assert_eq!(purged, 1);
    assert_eq!(mem.count().await.unwrap(), 1);
}

#[tokio::test]
async fn test_export_with_filter() {
    let (_tmp, mem) = make_memory();

    mem.store("a", "core item", MemoryCategory::Core, None).await.unwrap();
    mem.store("b", "daily item", MemoryCategory::Daily, None).await.unwrap();

    let filter = ExportFilter {
        category: Some(MemoryCategory::Core),
        ..Default::default()
    };
    let exported = mem.export(&filter).await.unwrap();
    assert_eq!(exported.len(), 1);
    assert_eq!(exported[0].key, "a");
}

#[tokio::test]
async fn test_custom_category_creates_dir() {
    let (tmp, mem) = make_memory();

    let custom = MemoryCategory::Custom("learned".to_string());
    mem.store("patterns", "workflow patterns", custom, None)
        .await
        .unwrap();

    // Verify the file was created in the custom directory
    assert!(tmp.path().join("memory/learned/patterns.md").exists());

    let entry = mem.get("patterns").await.unwrap().unwrap();
    assert_eq!(entry.content, "workflow patterns");
}

#[tokio::test]
async fn test_index_file_generated() {
    let (tmp, mem) = make_memory();

    mem.store("hello", "world", MemoryCategory::Core, None)
        .await
        .unwrap();

    let index_path = tmp.path().join("memory/MEMORY.md");
    assert!(index_path.exists());
    let content = std::fs::read_to_string(index_path).unwrap();
    assert!(content.contains("# Memory Index"));
    assert!(content.contains("hello"));
}

#[tokio::test]
async fn test_importance_scoring() {
    let (_tmp, mem) = make_memory();

    // "important" keyword should boost importance
    mem.store("critical", "This is an important decision that must be followed", MemoryCategory::Core, None)
        .await
        .unwrap();

    let entry = mem.get("critical").await.unwrap().unwrap();
    // Core base (0.7) + keyword boost for "important", "decision", "must"
    assert!(entry.importance.unwrap_or(0.0) > 0.7);
}

#[tokio::test]
async fn test_store_with_explicit_importance() {
    let (_tmp, mem) = make_memory();

    mem.store_with_metadata("key1", "content", MemoryCategory::Core, None, None, Some(0.95))
        .await
        .unwrap();

    let entry = mem.get("key1").await.unwrap().unwrap();
    assert!((entry.importance.unwrap() - 0.95).abs() < 0.01);
}

#[tokio::test]
async fn test_bm25_scoring() {
    // Direct unit test of the BM25 scorer
    let score = MarkdownMemory::bm25_score("rust systems", "Rust is a systems programming language");
    assert!(score > 0.0);

    let no_match = MarkdownMemory::bm25_score("python", "Rust is a systems programming language");
    assert_eq!(no_match, 0.0);

    let empty = MarkdownMemory::bm25_score("", "any content");
    assert_eq!(empty, 0.0);
}

#[tokio::test]
async fn test_key_sanitization() {
    let (_tmp, mem) = make_memory();

    mem.store("my key/with:special chars!", "data", MemoryCategory::Core, None)
        .await
        .unwrap();

    let entry = mem.get("my key/with:special chars!").await.unwrap().unwrap();
    assert_eq!(entry.content, "data");
}
