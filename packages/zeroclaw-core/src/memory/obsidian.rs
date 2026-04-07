//! Obsidian vault-backed memory backend.
//!
//! Implements the [`Memory`] trait using obsctl (vault CLI) for persistence
//! to CouchDB, with an in-memory cache for fast in-conversation recall.
//!
//! ## Note path convention
//!
//! Memories are stored as Obsidian-compatible markdown notes at:
//! `memory/{category}/{key}.md`
//!
//! Each note has YAML frontmatter with metadata (category, timestamp,
//! importance) and the memory content as the body.
//!
//! ## Two-layer architecture
//!
//! - **Local cache**: In-memory `HashMap` for fast reads during conversation
//! - **Vault (remote)**: Write-through to CouchDB via `vault` CLI
//!
//! If the vault is unreachable, writes are queued and retried on the next
//! `store()` call.

use super::traits::{Memory, MemoryCategory, MemoryEntry};
use async_trait::async_trait;
use parking_lot::Mutex;
use std::collections::HashMap;
use uuid::Uuid;

/// Maximum number of entries to keep in the local cache.
const DEFAULT_CACHE_SIZE: usize = 1000;

/// Pending write that failed to reach the vault.
struct PendingWrite {
    path: String,
    content: String,
}

/// Obsidian vault-backed memory backend.
pub struct ObsidianMemory {
    cache: Mutex<HashMap<String, MemoryEntry>>,
    pending_writes: Mutex<Vec<PendingWrite>>,
    cache_size: usize,
}

impl ObsidianMemory {
    /// Create a new Obsidian memory backend.
    pub fn new() -> Self {
        Self::with_cache_size(DEFAULT_CACHE_SIZE)
    }

    /// Create with a custom cache size.
    pub fn with_cache_size(cache_size: usize) -> Self {
        Self {
            cache: Mutex::new(HashMap::new()),
            pending_writes: Mutex::new(Vec::new()),
            cache_size,
        }
    }

    /// Generate the vault path for a memory entry.
    fn note_path(key: &str, category: &MemoryCategory) -> String {
        let cat_dir = match category {
            MemoryCategory::Core => "core",
            MemoryCategory::Daily => "daily",
            MemoryCategory::Conversation => "conversation",
            MemoryCategory::Custom(name) => name.as_str(),
        };
        // Sanitize key for filesystem safety
        let safe_key: String = key
            .chars()
            .map(|c| if c.is_alphanumeric() || c == '-' || c == '_' { c } else { '_' })
            .collect();
        format!("memory/{cat_dir}/{safe_key}.md")
    }

    /// Build note content with frontmatter.
    fn build_note(
        key: &str,
        content: &str,
        category: &MemoryCategory,
        session_id: Option<&str>,
    ) -> String {
        let timestamp = now_rfc3339();
        let mut note = String::from("---\n");
        note.push_str(&format!("key: \"{key}\"\n"));
        note.push_str(&format!("category: \"{category}\"\n"));
        note.push_str(&format!("created_at: \"{timestamp}\"\n"));
        if let Some(sid) = session_id {
            note.push_str(&format!("session_id: \"{sid}\"\n"));
        }
        note.push_str("---\n\n");
        note.push_str(content);
        note
    }

    /// Write a note to the vault via CLI. Returns Ok(()) on success.
    fn write_to_vault(path: &str, content: &str) -> anyhow::Result<()> {
        let output = std::process::Command::new("vault")
            .args(["write", "--path", path, "--content", content, "--force", "--yes"])
            .stdout(std::process::Stdio::piped())
            .stderr(std::process::Stdio::piped())
            .output()?;

        if !output.status.success() {
            let stderr = String::from_utf8_lossy(&output.stderr);
            anyhow::bail!("vault write failed: {stderr}");
        }
        Ok(())
    }

    /// Read a note from the vault via CLI.
    fn read_from_vault(path: &str) -> anyhow::Result<Option<String>> {
        let output = std::process::Command::new("vault")
            .args(["read", "--file", path, "--json"])
            .stdout(std::process::Stdio::piped())
            .stderr(std::process::Stdio::piped())
            .output()?;

        if !output.status.success() {
            return Ok(None);
        }

        let stdout = String::from_utf8_lossy(&output.stdout);
        let parsed: serde_json::Value = serde_json::from_str(&stdout)?;
        Ok(parsed.get("content").and_then(|v| v.as_str()).map(String::from))
    }

    /// Search the vault via CLI.
    fn search_vault(query: &str) -> anyhow::Result<Vec<String>> {
        let output = std::process::Command::new("vault")
            .args(["search", "--query", query, "--json"])
            .stdout(std::process::Stdio::piped())
            .stderr(std::process::Stdio::piped())
            .output()?;

        if !output.status.success() {
            return Ok(Vec::new());
        }

        let stdout = String::from_utf8_lossy(&output.stdout);
        // Parse search results — vault search returns an array of matches
        let parsed: serde_json::Value = serde_json::from_str(&stdout).unwrap_or_default();
        let results = parsed
            .as_array()
            .map(|arr| {
                arr.iter()
                    .filter_map(|v| v.get("path").and_then(|p| p.as_str()).map(String::from))
                    .filter(|p| p.starts_with("memory/"))
                    .collect()
            })
            .unwrap_or_default();

        Ok(results)
    }

    /// Delete a note from the vault via CLI.
    fn delete_from_vault(path: &str) -> anyhow::Result<()> {
        let output = std::process::Command::new("vault")
            .args(["delete", "--file", path, "--yes"])
            .stdout(std::process::Stdio::piped())
            .stderr(std::process::Stdio::piped())
            .output()?;

        if !output.status.success() {
            let stderr = String::from_utf8_lossy(&output.stderr);
            anyhow::bail!("vault delete failed: {stderr}");
        }
        Ok(())
    }

    /// Drain pending writes (retry failed vault writes).
    fn drain_pending(&self) {
        let pending: Vec<PendingWrite> = {
            let mut queue = self.pending_writes.lock();
            std::mem::take(&mut *queue)
        };

        for write in pending {
            if let Err(e) = Self::write_to_vault(&write.path, &write.content) {
                tracing::debug!("vault retry failed for {}: {e}", write.path);
                self.pending_writes.lock().push(write);
            }
        }
    }

    /// Evict oldest entries from cache if over limit.
    fn evict_cache(&self) {
        let mut cache = self.cache.lock();
        if cache.len() <= self.cache_size {
            return;
        }

        // Simple eviction: remove entries with oldest timestamps
        let mut entries: Vec<(String, String)> = cache
            .iter()
            .map(|(k, v)| (k.clone(), v.timestamp.clone()))
            .collect();
        entries.sort_by(|a, b| a.1.cmp(&b.1));

        let to_remove = cache.len() - self.cache_size;
        for (key, _) in entries.into_iter().take(to_remove) {
            cache.remove(&key);
        }
    }
}

#[async_trait]
impl Memory for ObsidianMemory {
    fn name(&self) -> &str {
        "obsidian"
    }

    async fn store(
        &self,
        key: &str,
        content: &str,
        category: MemoryCategory,
        session_id: Option<&str>,
    ) -> anyhow::Result<()> {
        // Drain any pending writes first
        self.drain_pending();

        let timestamp = now_rfc3339();
        let path = Self::note_path(key, &category);
        let note = Self::build_note(key, content, &category, session_id);

        // Write to local cache
        {
            let entry = MemoryEntry {
                id: Uuid::new_v4().to_string(),
                key: key.to_string(),
                content: content.to_string(),
                category: category.clone(),
                timestamp: timestamp.clone(),
                session_id: session_id.map(String::from),
                score: None,
                namespace: "default".to_string(),
                importance: None,
                superseded_by: None,
            };
            self.cache.lock().insert(key.to_string(), entry);
        }
        self.evict_cache();

        // Write-through to vault (non-blocking queue on failure)
        if let Err(e) = Self::write_to_vault(&path, &note) {
            tracing::debug!("vault write failed for {path}, queuing for retry: {e}");
            self.pending_writes.lock().push(PendingWrite {
                path,
                content: note,
            });
        }

        Ok(())
    }

    async fn recall(
        &self,
        query: &str,
        limit: usize,
        session_id: Option<&str>,
        _since: Option<&str>,
        _until: Option<&str>,
    ) -> anyhow::Result<Vec<MemoryEntry>> {
        let query_lower = query.to_ascii_lowercase();

        // First: search local cache
        let mut results: Vec<MemoryEntry> = {
            let cache = self.cache.lock();
            cache
                .values()
                .filter(|e| {
                    let matches_query = query.is_empty()
                        || e.content.to_ascii_lowercase().contains(&query_lower)
                        || e.key.to_ascii_lowercase().contains(&query_lower);
                    let matches_session = session_id
                        .map(|sid| e.session_id.as_deref() == Some(sid))
                        .unwrap_or(true);
                    matches_query && matches_session
                })
                .cloned()
                .collect()
        };

        // If we have enough from cache, return
        if results.len() >= limit {
            results.truncate(limit);
            return Ok(results);
        }

        // Fall back to vault search if not enough results
        if !query.is_empty() {
            if let Ok(paths) = Self::search_vault(query) {
                let cache = self.cache.lock();
                for path in paths.into_iter().take(limit * 2) {
                    // Skip if already in results (from cache)
                    let key_from_path = path
                        .rsplit('/')
                        .next()
                        .unwrap_or("")
                        .trim_end_matches(".md");
                    if cache.contains_key(key_from_path) {
                        continue;
                    }

                    // Read the note from vault
                    if let Ok(Some(content)) = Self::read_from_vault(&path) {
                        // Parse frontmatter to extract metadata
                        let (body, _key, category) = parse_note_content(&content);
                        results.push(MemoryEntry {
                            id: Uuid::new_v4().to_string(),
                            key: key_from_path.to_string(),
                            content: body,
                            category,
                            timestamp: now_rfc3339(),
                            session_id: None,
                            score: Some(50.0),
                            namespace: "default".to_string(),
                            importance: None,
                            superseded_by: None,
                        });

                        if results.len() >= limit {
                            break;
                        }
                    }
                }
            }
        }

        results.truncate(limit);
        Ok(results)
    }

    async fn get(&self, key: &str) -> anyhow::Result<Option<MemoryEntry>> {
        // Check cache first
        if let Some(entry) = self.cache.lock().get(key) {
            return Ok(Some(entry.clone()));
        }

        // Try vault for each possible category
        for cat in &["core", "daily", "conversation"] {
            let path = format!("memory/{cat}/{key}.md");
            if let Ok(Some(content)) = Self::read_from_vault(&path) {
                let (body, _, category) = parse_note_content(&content);
                let entry = MemoryEntry {
                    id: Uuid::new_v4().to_string(),
                    key: key.to_string(),
                    content: body,
                    category,
                    timestamp: now_rfc3339(),
                    session_id: None,
                    score: None,
                    namespace: "default".to_string(),
                    importance: None,
                    superseded_by: None,
                };
                // Cache for future reads
                self.cache.lock().insert(key.to_string(), entry.clone());
                return Ok(Some(entry));
            }
        }

        Ok(None)
    }

    async fn list(
        &self,
        category: Option<&MemoryCategory>,
        session_id: Option<&str>,
    ) -> anyhow::Result<Vec<MemoryEntry>> {
        let cache = self.cache.lock();
        let results: Vec<MemoryEntry> = cache
            .values()
            .filter(|e| {
                let matches_cat = category.map(|c| &e.category == c).unwrap_or(true);
                let matches_session = session_id
                    .map(|sid| e.session_id.as_deref() == Some(sid))
                    .unwrap_or(true);
                matches_cat && matches_session
            })
            .cloned()
            .collect();
        Ok(results)
    }

    async fn forget(&self, key: &str) -> anyhow::Result<bool> {
        let removed = self.cache.lock().remove(key).is_some();

        // Try deleting from vault for each category
        for cat in &["core", "daily", "conversation"] {
            let path = format!("memory/{cat}/{key}.md");
            let _ = Self::delete_from_vault(&path);
        }

        Ok(removed)
    }

    async fn count(&self) -> anyhow::Result<usize> {
        Ok(self.cache.lock().len())
    }

    async fn health_check(&self) -> bool {
        std::process::Command::new("vault")
            .args(["ping", "--json"])
            .stdout(std::process::Stdio::piped())
            .stderr(std::process::Stdio::piped())
            .output()
            .map(|o| o.status.success())
            .unwrap_or(false)
    }
}

/// Parse note content, extracting the body and metadata from frontmatter.
fn parse_note_content(content: &str) -> (String, Option<String>, MemoryCategory) {
    let trimmed = content.trim();
    if !trimmed.starts_with("---") {
        return (content.to_string(), None, MemoryCategory::Core);
    }

    // Find the closing --- of frontmatter
    if let Some(end) = trimmed[3..].find("---") {
        let frontmatter = &trimmed[3..3 + end];
        let body = trimmed[3 + end + 3..].trim().to_string();

        let mut key = None;
        let mut category = MemoryCategory::Core;

        for line in frontmatter.lines() {
            let line = line.trim();
            if let Some(val) = line.strip_prefix("key:") {
                key = Some(val.trim().trim_matches('"').to_string());
            } else if let Some(val) = line.strip_prefix("category:") {
                let cat_str = val.trim().trim_matches('"');
                category = match cat_str {
                    "core" => MemoryCategory::Core,
                    "daily" => MemoryCategory::Daily,
                    "conversation" => MemoryCategory::Conversation,
                    other => MemoryCategory::Custom(other.to_string()),
                };
            }
        }

        (body, key, category)
    } else {
        (content.to_string(), None, MemoryCategory::Core)
    }
}

fn now_rfc3339() -> String {
    chrono::Utc::now().to_rfc3339()
}


#[cfg(test)]
#[path = "obsidian.test.rs"]
mod tests;
