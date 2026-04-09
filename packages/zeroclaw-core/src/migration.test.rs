use super::*;
use crate::config::{Config, MemoryConfig};
use crate::memory::SqliteMemory;
use rusqlite::params;
use tempfile::TempDir;

fn test_config(workspace: &Path) -> Config {
    Config {
        workspace_dir: workspace.to_path_buf(),
        config_path: workspace.join("config.toml"),
        memory: MemoryConfig {
            backend: "sqlite".to_string(),
            ..MemoryConfig::default()
        },
        ..Config::default()
    }
}

#[test]
fn parse_structured_markdown_line() {
    let line = "**user_pref**: likes Rust";
    let parsed = parse_structured_memory_line(line).unwrap();
    assert_eq!(parsed.0, "user_pref");
    assert_eq!(parsed.1, "likes Rust");
}

#[test]
fn parse_unstructured_markdown_generates_key() {
    let entries = parse_markdown_file(
        Path::new("/tmp/MEMORY.md"),
        "- plain note",
        MemoryCategory::Core,
        "core",
    );
    assert_eq!(entries.len(), 1);
    assert!(entries[0].key.starts_with("openclaw_core_"));
    assert_eq!(entries[0].content, "plain note");
}

#[test]
fn sqlite_reader_supports_legacy_value_column() {
    let dir = TempDir::new().unwrap();
    let db_path = dir.path().join("brain.db");
    let conn = Connection::open(&db_path).unwrap();

    conn.execute_batch("CREATE TABLE memories (key TEXT, value TEXT, type TEXT);")
        .unwrap();
    conn.execute(
        "INSERT INTO memories (key, value, type) VALUES (?1, ?2, ?3)",
        params!["legacy_key", "legacy_value", "daily"],
    )
    .unwrap();

    let rows = read_openclaw_sqlite_entries(&db_path).unwrap();
    assert_eq!(rows.len(), 1);
    assert_eq!(rows[0].key, "legacy_key");
    assert_eq!(rows[0].content, "legacy_value");
    assert_eq!(rows[0].category, MemoryCategory::Daily);
}

#[tokio::test]
async fn migration_renames_conflicting_key() {
    let source = TempDir::new().unwrap();
    let target = TempDir::new().unwrap();

    // Existing target memory
    let target_mem = SqliteMemory::new(target.path()).unwrap();
    target_mem
        .store("k", "new value", MemoryCategory::Core, None)
        .await
        .unwrap();

    // Source sqlite with conflicting key + different content
    let source_db_dir = source.path().join("memory");
    fs::create_dir_all(&source_db_dir).unwrap();
    let source_db = source_db_dir.join("brain.db");
    let conn = Connection::open(&source_db).unwrap();
    conn.execute_batch("CREATE TABLE memories (key TEXT, content TEXT, category TEXT);")
        .unwrap();
    conn.execute(
        "INSERT INTO memories (key, content, category) VALUES (?1, ?2, ?3)",
        params!["k", "old value", "core"],
    )
    .unwrap();

    let config = test_config(target.path());
    migrate_openclaw_memory(&config, Some(source.path().to_path_buf()), false)
        .await
        .unwrap();

    let all = target_mem.list(None, None).await.unwrap();
    assert!(all.iter().any(|e| e.key == "k" && e.content == "new value"));
    assert!(
        all.iter()
            .any(|e| e.key.starts_with("k__openclaw_") && e.content == "old value")
    );
}

#[tokio::test]
async fn dry_run_does_not_write() {
    let source = TempDir::new().unwrap();
    let target = TempDir::new().unwrap();
    let source_db_dir = source.path().join("memory");
    fs::create_dir_all(&source_db_dir).unwrap();

    let source_db = source_db_dir.join("brain.db");
    let conn = Connection::open(&source_db).unwrap();
    conn.execute_batch("CREATE TABLE memories (key TEXT, content TEXT, category TEXT);")
        .unwrap();
    conn.execute(
        "INSERT INTO memories (key, content, category) VALUES (?1, ?2, ?3)",
        params!["dry", "run", "core"],
    )
    .unwrap();

    let config = test_config(target.path());
    migrate_openclaw_memory(&config, Some(source.path().to_path_buf()), true)
        .await
        .unwrap();

    let target_mem = SqliteMemory::new(target.path()).unwrap();
    assert_eq!(target_mem.count().await.unwrap(), 0);
}

#[test]
fn migration_target_rejects_none_backend() {
    let target = TempDir::new().unwrap();
    let mut config = test_config(target.path());
    config.memory.backend = "none".to_string();

    let err = target_memory_backend(&config)
        .err()
        .expect("backend=none should be rejected for migration target");
    assert!(err.to_string().contains("disables persistence"));
}

// ── §7.1 / §7.2 Config backward compatibility & migration tests ──

#[test]
fn parse_category_handles_all_variants() {
    assert_eq!(parse_category("core"), MemoryCategory::Core);
    assert_eq!(parse_category("daily"), MemoryCategory::Daily);
    assert_eq!(parse_category("conversation"), MemoryCategory::Conversation);
    assert_eq!(parse_category(""), MemoryCategory::Core);
    assert_eq!(
        parse_category("custom_type"),
        MemoryCategory::Custom("custom_type".to_string())
    );
}

#[test]
fn parse_category_case_insensitive() {
    assert_eq!(parse_category("CORE"), MemoryCategory::Core);
    assert_eq!(parse_category("Daily"), MemoryCategory::Daily);
    assert_eq!(parse_category("CONVERSATION"), MemoryCategory::Conversation);
}

#[test]
fn normalize_key_handles_empty_string() {
    let key = normalize_key("", 42);
    assert_eq!(key, "openclaw_42");
}

#[test]
fn normalize_key_trims_whitespace() {
    let key = normalize_key("  my_key  ", 0);
    assert_eq!(key, "my_key");
}

#[test]
fn parse_structured_markdown_rejects_empty_key() {
    assert!(parse_structured_memory_line("****:value").is_none());
}

#[test]
fn parse_structured_markdown_rejects_empty_value() {
    assert!(parse_structured_memory_line("**key**:").is_none());
}

#[test]
fn parse_structured_markdown_rejects_no_stars() {
    assert!(parse_structured_memory_line("key: value").is_none());
}

#[tokio::test]
async fn migration_skips_empty_content() {
    let dir = TempDir::new().unwrap();
    let db_path = dir.path().join("brain.db");
    let conn = Connection::open(&db_path).unwrap();

    conn.execute_batch("CREATE TABLE memories (key TEXT, content TEXT, category TEXT);")
        .unwrap();
    conn.execute(
        "INSERT INTO memories (key, content, category) VALUES (?1, ?2, ?3)",
        params!["empty_key", "   ", "core"],
    )
    .unwrap();

    let rows = read_openclaw_sqlite_entries(&db_path).unwrap();
    assert_eq!(
        rows.len(),
        0,
        "entries with empty/whitespace content must be skipped"
    );
}

#[test]
fn backup_creates_timestamped_directory() {
    let tmp = TempDir::new().unwrap();
    let mem_dir = tmp.path().join("memory");
    std::fs::create_dir_all(&mem_dir).unwrap();

    // Create a brain.db to back up
    let db_path = mem_dir.join("brain.db");
    std::fs::write(&db_path, "fake db content").unwrap();

    let result = backup_target_memory(tmp.path()).unwrap();
    assert!(
        result.is_some(),
        "backup should be created when files exist"
    );

    let backup_dir = result.unwrap();
    assert!(backup_dir.exists());
    assert!(
        backup_dir.to_string_lossy().contains("openclaw-"),
        "backup dir must contain openclaw- prefix"
    );
}

#[test]
fn backup_returns_none_when_no_files() {
    let tmp = TempDir::new().unwrap();
    let result = backup_target_memory(tmp.path()).unwrap();
    assert!(
        result.is_none(),
        "backup should return None when no files to backup"
    );
}
