//! Conflict resolution for memory entries.
//!
//! Before storing Core memories, performs a semantic similarity check against
//! existing entries. If cosine similarity exceeds a threshold but content
//! differs, the old entry is marked as superseded.

use super::traits::{Memory, MemoryCategory, MemoryEntry};

/// Check for conflicting memories and mark old ones as superseded.
///
/// Returns the list of entry IDs that were superseded.
pub async fn check_and_resolve_conflicts(
    memory: &dyn Memory,
    key: &str,
    content: &str,
    category: &MemoryCategory,
    threshold: f64,
) -> anyhow::Result<Vec<String>> {
    // Only check conflicts for Core memories
    if !matches!(category, MemoryCategory::Core) {
        return Ok(Vec::new());
    }

    // Search for similar existing entries
    let candidates = memory.recall(content, 10, None, None, None).await?;

    let mut superseded = Vec::new();
    for candidate in &candidates {
        if candidate.key == key {
            continue; // Same key = update, not conflict
        }
        if !matches!(candidate.category, MemoryCategory::Core) {
            continue;
        }
        if let Some(score) = candidate.score {
            if score > threshold && candidate.content != content {
                superseded.push(candidate.id.clone());
            }
        }
    }

    Ok(superseded)
}

/// Mark entries as superseded in SQLite by setting their `superseded_by` column.
pub fn mark_superseded(
    conn: &rusqlite::Connection,
    superseded_ids: &[String],
    new_id: &str,
) -> anyhow::Result<()> {
    if superseded_ids.is_empty() {
        return Ok(());
    }

    for id in superseded_ids {
        conn.execute(
            "UPDATE memories SET superseded_by = ?1 WHERE id = ?2",
            rusqlite::params![new_id, id],
        )?;
    }

    Ok(())
}

/// Simple text-based conflict detection without embeddings.
///
/// Uses token overlap (Jaccard similarity) as a fast approximation
/// when vector embeddings are unavailable.
pub fn jaccard_similarity(a: &str, b: &str) -> f64 {
    let words_a: std::collections::HashSet<&str> = a.split_whitespace().collect();
    let words_b: std::collections::HashSet<&str> = b.split_whitespace().collect();

    if words_a.is_empty() && words_b.is_empty() {
        return 1.0;
    }
    if words_a.is_empty() || words_b.is_empty() {
        return 0.0;
    }

    let intersection = words_a.intersection(&words_b).count();
    let union = words_a.union(&words_b).count();

    if union == 0 {
        0.0
    } else {
        intersection as f64 / union as f64
    }
}

/// Find potentially conflicting entries using text similarity when embeddings
/// are not available. Returns entries above the threshold.
pub fn find_text_conflicts(
    entries: &[MemoryEntry],
    new_content: &str,
    threshold: f64,
) -> Vec<String> {
    entries
        .iter()
        .filter(|e| {
            matches!(e.category, MemoryCategory::Core)
                && e.superseded_by.is_none()
                && jaccard_similarity(&e.content, new_content) > threshold
                && e.content != new_content
        })
        .map(|e| e.id.clone())
        .collect()
}


#[cfg(test)]
#[path = "conflict.test.rs"]
mod tests;
