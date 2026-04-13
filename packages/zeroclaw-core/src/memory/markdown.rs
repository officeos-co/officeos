//! Markdown-file-backed memory backend.
//!
//! Implements the [`Memory`] trait using local `.md` files on the agent's PVC.
//! Each memory entry is a markdown file with YAML frontmatter for metadata.
//!
//! ## Directory layout
//!
//! ```text
//! {base_dir}/
//! ├── MEMORY.md              # Auto-generated index
//! ├── core/
//! │   ├── user_preferences.md
//! │   └── project_context.md
//! ├── daily/
//! │   ├── 2026-04-13.md
//! │   └── 2026-04-12.md
//! └── learned/
//!     ├── tool_patterns.md
//!     └── workflow_insights.md
//! ```
//!
//! ## File format
//!
//! ```markdown
//! ---
//! key: user_preferences
//! category: core
//! created: 2026-04-13T10:30:00Z
//! updated: 2026-04-13T14:20:00Z
//! importance: 0.8
//! ---
//!
//! Content here...
//! ```

use super::importance::compute_importance;
use super::decay::{apply_time_decay, DEFAULT_HALF_LIFE_DAYS};
use super::traits::{ExportFilter, Memory, MemoryCategory, MemoryEntry};
use async_trait::async_trait;
use chrono::Utc;
use std::collections::HashMap;
use std::fmt::Write as FmtWrite;
use std::path::{Path, PathBuf};
use uuid::Uuid;

/// Markdown-file-backed persistent memory.
pub struct MarkdownMemory {
    base_dir: PathBuf,
}

impl MarkdownMemory {
    /// Create a new markdown memory backend rooted at `base_dir`.
    ///
    /// Creates the base directory and standard subdirectories if they don't exist.
    pub fn new(base_dir: &Path) -> anyhow::Result<Self> {
        let base = base_dir.join("memory");
        std::fs::create_dir_all(base.join("core"))?;
        std::fs::create_dir_all(base.join("daily"))?;
        std::fs::create_dir_all(base.join("learned"))?;
        Ok(Self { base_dir: base })
    }

    /// Sanitize a key for safe filesystem usage.
    fn sanitize_key(key: &str) -> String {
        key.chars()
            .map(|c| {
                if c.is_alphanumeric() || c == '-' || c == '_' {
                    c
                } else {
                    '_'
                }
            })
            .collect()
    }

    /// Map a category to its subdirectory name.
    fn category_dir(category: &MemoryCategory) -> &str {
        match category {
            MemoryCategory::Core => "core",
            MemoryCategory::Daily => "daily",
            MemoryCategory::Conversation => "conversation",
            MemoryCategory::Custom(name) => name.as_str(),
        }
    }

    /// Get the file path for a given key and category.
    fn file_path(&self, key: &str, category: &MemoryCategory) -> PathBuf {
        let dir = Self::category_dir(category);
        let safe_key = Self::sanitize_key(key);
        self.base_dir.join(dir).join(format!("{safe_key}.md"))
    }

    /// Build a markdown file with YAML frontmatter.
    fn build_file(
        key: &str,
        content: &str,
        category: &MemoryCategory,
        session_id: Option<&str>,
        namespace: &str,
        importance: f64,
        created: &str,
        updated: &str,
    ) -> String {
        let mut out = String::from("---\n");
        let _ = writeln!(out, "key: {key}");
        let _ = writeln!(out, "category: {category}");
        let _ = writeln!(out, "created: {created}");
        let _ = writeln!(out, "updated: {updated}");
        let _ = writeln!(out, "importance: {importance}");
        let _ = writeln!(out, "namespace: {namespace}");
        if let Some(sid) = session_id {
            let _ = writeln!(out, "session_id: {sid}");
        }
        out.push_str("---\n\n");
        out.push_str(content);
        out.push('\n');
        out
    }

    /// Parse a markdown file's frontmatter and body into a `MemoryEntry`.
    fn parse_file(path: &Path) -> anyhow::Result<MemoryEntry> {
        let raw = std::fs::read_to_string(path)?;
        let trimmed = raw.trim();
        if !trimmed.starts_with("---") {
            anyhow::bail!("missing frontmatter in {}", path.display());
        }

        let rest = &trimmed[3..];
        let end = rest
            .find("---")
            .ok_or_else(|| anyhow::anyhow!("unclosed frontmatter in {}", path.display()))?;
        let frontmatter = &rest[..end];
        let body = rest[end + 3..].trim().to_string();

        let mut key = String::new();
        let mut category = MemoryCategory::Core;
        let mut created = String::new();
        let mut updated = String::new();
        let mut importance: Option<f64> = None;
        let mut namespace = "default".to_string();
        let mut session_id: Option<String> = None;

        for line in frontmatter.lines() {
            let line = line.trim();
            if let Some(val) = line.strip_prefix("key:") {
                key = val.trim().trim_matches('"').to_string();
            } else if let Some(val) = line.strip_prefix("category:") {
                let cat_str = val.trim().trim_matches('"');
                category = match cat_str {
                    "core" => MemoryCategory::Core,
                    "daily" => MemoryCategory::Daily,
                    "conversation" => MemoryCategory::Conversation,
                    other => MemoryCategory::Custom(other.to_string()),
                };
            } else if let Some(val) = line.strip_prefix("created:") {
                created = val.trim().trim_matches('"').to_string();
            } else if let Some(val) = line.strip_prefix("updated:") {
                updated = val.trim().trim_matches('"').to_string();
            } else if let Some(val) = line.strip_prefix("importance:") {
                importance = val.trim().parse().ok();
            } else if let Some(val) = line.strip_prefix("namespace:") {
                namespace = val.trim().trim_matches('"').to_string();
            } else if let Some(val) = line.strip_prefix("session_id:") {
                session_id = Some(val.trim().trim_matches('"').to_string());
            }
        }

        // Use updated timestamp as the entry's timestamp (most recent mutation).
        let timestamp = if updated.is_empty() {
            if created.is_empty() {
                Utc::now().to_rfc3339()
            } else {
                created.clone()
            }
        } else {
            updated.clone()
        };

        Ok(MemoryEntry {
            id: Uuid::new_v4().to_string(),
            key,
            content: body,
            category,
            timestamp,
            session_id,
            score: None,
            namespace,
            importance,
            superseded_by: None,
        })
    }

    /// Scan all markdown files in the base directory, returning parsed entries.
    fn scan_all(&self) -> anyhow::Result<Vec<MemoryEntry>> {
        let mut entries = Vec::new();
        self.scan_dir(&self.base_dir, &mut entries)?;
        Ok(entries)
    }

    /// Recursively scan a directory for `.md` files (skipping MEMORY.md).
    fn scan_dir(&self, dir: &Path, entries: &mut Vec<MemoryEntry>) -> anyhow::Result<()> {
        let read = match std::fs::read_dir(dir) {
            Ok(rd) => rd,
            Err(_) => return Ok(()),
        };
        for entry in read {
            let entry = entry?;
            let path = entry.path();
            if path.is_dir() {
                self.scan_dir(&path, entries)?;
            } else if path.extension().is_some_and(|e| e == "md") {
                // Skip the index file
                if path
                    .file_name()
                    .is_some_and(|n| n == "MEMORY.md")
                {
                    continue;
                }
                match Self::parse_file(&path) {
                    Ok(e) => entries.push(e),
                    Err(err) => {
                        tracing::debug!("skipping unparseable memory file {}: {err}", path.display());
                    }
                }
            }
        }
        Ok(())
    }

    /// Regenerate the `MEMORY.md` index file.
    fn update_index(&self) -> anyhow::Result<()> {
        let entries = self.scan_all()?;

        // Group by category directory
        let mut groups: HashMap<String, Vec<&MemoryEntry>> = HashMap::new();
        for e in &entries {
            groups
                .entry(Self::category_dir(&e.category).to_string())
                .or_default()
                .push(e);
        }

        let mut idx = String::from("# Memory Index\n\n");
        let _ = writeln!(
            idx,
            "_Auto-generated at {}. Do not edit manually._\n",
            Utc::now().to_rfc3339()
        );

        let mut cats: Vec<_> = groups.keys().cloned().collect();
        cats.sort();
        for cat in &cats {
            let _ = writeln!(idx, "## {cat}\n");
            let items = groups.get(cat).unwrap();
            let mut sorted: Vec<_> = items.iter().collect();
            sorted.sort_by(|a, b| a.key.cmp(&b.key));
            for item in sorted {
                let imp = item.importance.unwrap_or(0.0);
                let _ = writeln!(
                    idx,
                    "- **{}** (importance: {:.1}) — _{}_",
                    item.key, imp, item.timestamp
                );
            }
            idx.push('\n');
        }

        std::fs::write(self.base_dir.join("MEMORY.md"), idx)?;
        Ok(())
    }

    /// BM25-inspired keyword scoring.
    ///
    /// Computes a simple TF-based score for `query` against `text`.
    /// Each query term's contribution is `tf / (tf + 1.2)` (BM25-like saturation).
    fn bm25_score(query: &str, text: &str) -> f64 {
        let text_lower = text.to_ascii_lowercase();
        let terms: Vec<&str> = query
            .split_whitespace()
            .filter(|t| t.len() > 1)
            .collect();
        if terms.is_empty() {
            return 0.0;
        }

        let mut total = 0.0;
        for term in &terms {
            let term_lower = term.to_ascii_lowercase();
            let tf = text_lower.matches(&term_lower).count() as f64;
            // BM25 saturation: tf / (tf + k1), k1 = 1.2
            total += tf / (tf + 1.2);
        }
        // Normalize by number of query terms
        total / terms.len() as f64
    }

    /// Find the existing file for a key by scanning category subdirectories.
    ///
    /// Returns `(path, existing_entry)` if found.
    fn find_existing(&self, key: &str) -> Option<(PathBuf, MemoryEntry)> {
        let safe_key = Self::sanitize_key(key);
        let filename = format!("{safe_key}.md");

        let read = match std::fs::read_dir(&self.base_dir) {
            Ok(rd) => rd,
            Err(_) => return None,
        };
        for entry in read.flatten() {
            let path = entry.path();
            if path.is_dir() {
                let candidate = path.join(&filename);
                if candidate.exists() {
                    if let Ok(parsed) = Self::parse_file(&candidate) {
                        return Some((candidate, parsed));
                    }
                }
            }
        }
        None
    }
}

#[async_trait]
impl Memory for MarkdownMemory {
    fn name(&self) -> &str {
        "markdown"
    }

    async fn store(
        &self,
        key: &str,
        content: &str,
        category: MemoryCategory,
        session_id: Option<&str>,
    ) -> anyhow::Result<()> {
        self.store_with_metadata(key, content, category, session_id, None, None)
            .await
    }

    async fn store_with_metadata(
        &self,
        key: &str,
        content: &str,
        category: MemoryCategory,
        session_id: Option<&str>,
        namespace: Option<&str>,
        importance: Option<f64>,
    ) -> anyhow::Result<()> {
        let now = Utc::now().to_rfc3339();
        let ns = namespace.unwrap_or("default");
        let imp = importance.unwrap_or_else(|| compute_importance(content, &category));

        // If an existing file exists for this key (possibly in a different category dir),
        // preserve the original created timestamp and remove the old file if category changed.
        let (created, old_path) = match self.find_existing(key) {
            Some((old_path, old_entry)) => {
                let created = if old_entry.timestamp.is_empty() {
                    now.clone()
                } else {
                    // The stored 'timestamp' was set from the updated field; try to
                    // recover the original created from the file. For simplicity, just
                    // re-read created from the old entry's file. We already parsed it,
                    // but parse_file maps created → timestamp only when updated is empty,
                    // so we re-parse minimally here.
                    let raw = std::fs::read_to_string(&old_path).unwrap_or_default();
                    Self::extract_frontmatter_field(&raw, "created")
                        .unwrap_or_else(|| now.clone())
                };
                (created, Some(old_path))
            }
            None => (now.clone(), None),
        };

        let target = self.file_path(key, &category);
        if let Some(parent) = target.parent() {
            std::fs::create_dir_all(parent)?;
        }

        let file_content =
            Self::build_file(key, content, &category, session_id, ns, imp, &created, &now);
        std::fs::write(&target, file_content)?;

        // Remove old file if it moved to a new category directory.
        if let Some(old) = old_path {
            if old != target {
                let _ = std::fs::remove_file(old);
            }
        }

        // Best-effort index update
        if let Err(e) = self.update_index() {
            tracing::debug!("failed to update MEMORY.md index: {e}");
        }

        Ok(())
    }

    async fn recall(
        &self,
        query: &str,
        limit: usize,
        session_id: Option<&str>,
        since: Option<&str>,
        until: Option<&str>,
    ) -> anyhow::Result<Vec<MemoryEntry>> {
        let mut entries = self.scan_all()?;

        // Apply session filter
        if let Some(sid) = session_id {
            entries.retain(|e| e.session_id.as_deref() == Some(sid));
        }

        // Apply time range filters
        if let Some(since) = since {
            entries.retain(|e| e.timestamp.as_str() >= since);
        }
        if let Some(until) = until {
            entries.retain(|e| e.timestamp.as_str() <= until);
        }

        if query.is_empty() {
            entries.truncate(limit);
            return Ok(entries);
        }

        // BM25 keyword scoring
        for entry in &mut entries {
            let text = format!("{} {}", entry.key, entry.content);
            let bm25 = Self::bm25_score(query, &text);
            entry.score = Some(bm25);
        }

        // Filter out zero-score entries
        entries.retain(|e| e.score.unwrap_or(0.0) > 0.0);

        // Apply importance weighting and time decay
        let imp_weight = 0.2;
        let bm25_weight = 0.7;
        let recency_weight = 0.1;

        // Apply time decay (modifies scores in-place for non-Core entries)
        apply_time_decay(&mut entries, DEFAULT_HALF_LIFE_DAYS);

        // Compute final weighted score
        for entry in &mut entries {
            let bm25 = entry.score.unwrap_or(0.0);
            let imp = entry.importance.unwrap_or(0.0);
            // After decay, bm25 already has recency factored in for non-Core.
            // We still add a small recency component from importance weighting.
            entry.score = Some(bm25 * bm25_weight + imp * imp_weight + bm25 * recency_weight);
        }

        // Sort by score descending
        entries.sort_by(|a, b| {
            b.score
                .unwrap_or(0.0)
                .partial_cmp(&a.score.unwrap_or(0.0))
                .unwrap_or(std::cmp::Ordering::Equal)
        });

        entries.truncate(limit);
        Ok(entries)
    }

    async fn get(&self, key: &str) -> anyhow::Result<Option<MemoryEntry>> {
        match self.find_existing(key) {
            Some((_, entry)) => Ok(Some(entry)),
            None => Ok(None),
        }
    }

    async fn list(
        &self,
        category: Option<&MemoryCategory>,
        session_id: Option<&str>,
    ) -> anyhow::Result<Vec<MemoryEntry>> {
        let mut entries = self.scan_all()?;

        if let Some(cat) = category {
            entries.retain(|e| &e.category == cat);
        }
        if let Some(sid) = session_id {
            entries.retain(|e| e.session_id.as_deref() == Some(sid));
        }

        Ok(entries)
    }

    async fn forget(&self, key: &str) -> anyhow::Result<bool> {
        match self.find_existing(key) {
            Some((path, _)) => {
                std::fs::remove_file(path)?;
                if let Err(e) = self.update_index() {
                    tracing::debug!("failed to update MEMORY.md index after forget: {e}");
                }
                Ok(true)
            }
            None => Ok(false),
        }
    }

    async fn purge_namespace(&self, namespace: &str) -> anyhow::Result<usize> {
        let entries = self.scan_all()?;
        let mut count = 0;
        for entry in &entries {
            if entry.namespace == namespace {
                if let Some((path, _)) = self.find_existing(&entry.key) {
                    std::fs::remove_file(path)?;
                    count += 1;
                }
            }
        }
        if count > 0 {
            let _ = self.update_index();
        }
        Ok(count)
    }

    async fn purge_session(&self, session_id: &str) -> anyhow::Result<usize> {
        let entries = self.scan_all()?;
        let mut count = 0;
        for entry in &entries {
            if entry.session_id.as_deref() == Some(session_id) {
                if let Some((path, _)) = self.find_existing(&entry.key) {
                    std::fs::remove_file(path)?;
                    count += 1;
                }
            }
        }
        if count > 0 {
            let _ = self.update_index();
        }
        Ok(count)
    }

    async fn count(&self) -> anyhow::Result<usize> {
        let entries = self.scan_all()?;
        Ok(entries.len())
    }

    async fn health_check(&self) -> bool {
        self.base_dir.exists() && self.base_dir.is_dir()
    }

    async fn export(&self, filter: &ExportFilter) -> anyhow::Result<Vec<MemoryEntry>> {
        let mut entries = self
            .list(filter.category.as_ref(), filter.session_id.as_deref())
            .await?;

        entries.retain(|e| {
            if let Some(ref ns) = filter.namespace {
                if e.namespace != *ns {
                    return false;
                }
            }
            if let Some(ref since) = filter.since {
                if e.timestamp.as_str() < since.as_str() {
                    return false;
                }
            }
            if let Some(ref until) = filter.until {
                if e.timestamp.as_str() > until.as_str() {
                    return false;
                }
            }
            true
        });

        // Sort by timestamp ascending for export (data portability order)
        entries.sort_by(|a, b| a.timestamp.cmp(&b.timestamp));
        Ok(entries)
    }
}

impl MarkdownMemory {
    /// Extract a single frontmatter field value from raw file content.
    fn extract_frontmatter_field(raw: &str, field: &str) -> Option<String> {
        let trimmed = raw.trim();
        if !trimmed.starts_with("---") {
            return None;
        }
        let rest = &trimmed[3..];
        let end = rest.find("---")?;
        let frontmatter = &rest[..end];
        let prefix = format!("{field}:");
        for line in frontmatter.lines() {
            let line = line.trim();
            if let Some(val) = line.strip_prefix(&prefix) {
                return Some(val.trim().trim_matches('"').to_string());
            }
        }
        None
    }
}

#[cfg(test)]
#[path = "markdown.test.rs"]
mod tests;
