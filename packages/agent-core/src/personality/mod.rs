// Rust guideline compliant 2026-02-21

//! Embedded personality templates and idempotent first-boot seeding.
//!
//! See API.md §5.

use std::path::Path;

use crate::error::{Error, Result};

/// The three personality templates embedded via `include_str!`.
///
/// Order is the canonical load order (also used by the
/// `IdentitySection` prompt builder). `AGENTS.md` is explicitly NOT
/// in this list per API.md §19 (resolution 7).
pub const TEMPLATES: &[(&str, &str)] = &[
    ("SOUL.md", include_str!("templates/SOUL.md")),
    ("IDENTITY.md", include_str!("templates/IDENTITY.md")),
    ("BOOTSTRAP.md", include_str!("templates/BOOTSTRAP.md")),
];

/// Literal substitution token inside `BOOTSTRAP.md`.
///
/// Replaced with the bootstrap payload's `systemPrompt` on first
/// write only.
pub const PROMPT_TOKEN: &str = "{{prompt}}";

/// Write missing templates into `memory_dir`.
///
/// For each template: if the file exists (any content), skip it;
/// otherwise, substitute `{{prompt}}` (only meaningful for
/// `BOOTSTRAP.md`) and write it. After writing, performs a strict
/// check: all three files must exist and be non-empty.
///
/// # Errors
///
/// Returns `Error::Personality` if any file is missing or empty after
/// seeding, and `Error::MemoryIo` on filesystem failure.
pub async fn seed(memory_dir: &Path, system_prompt: &str) -> Result<()> {
    tracing::info!(
        name: "personality.seed.start",
        file_directory = %memory_dir.display(),
        "seeding personality files in {{file_directory}}",
    );
    // Create memory_dir if it doesn't exist.
    tokio::fs::create_dir_all(memory_dir).await?;

    for &(name, template) in TEMPLATES {
        let dst = memory_dir.join(name);
        if dst.exists() {
            continue;
        }

        let content = if name == "BOOTSTRAP.md" {
            template.replace(PROMPT_TOKEN, system_prompt)
        } else {
            template.to_string()
        };

        tokio::fs::write(&dst, &content).await?;
        tracing::info!(
            name: "personality.file.written",
            file_path = %dst.display(),
            "wrote personality file: {{file_path}}",
        );
    }

    // Strict post-check: all three must exist and be non-empty.
    for &(name, _) in TEMPLATES {
        let dst = memory_dir.join(name);
        let meta = tokio::fs::metadata(&dst)
            .await
            .map_err(|e| Error::Personality(format!("{name} missing after seed: {e}")))?;
        if meta.len() == 0 {
            return Err(Error::Personality(format!("{name} is empty after seed")));
        }
    }

    Ok(())
}
