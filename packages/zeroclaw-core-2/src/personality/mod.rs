//! Embedded personality templates + idempotent write on first boot.
//! See API.md §5.

use std::path::Path;

use crate::error::{Error, Result};

/// The three personality templates embedded in the binary via
/// `include_str!`. Order is the canonical load order (also used by the
/// `IdentitySection` prompt builder).
///
/// Real constant — defines what gets embedded. `AGENTS.md` is explicitly
/// NOT in this list per API.md §19 (resolution 7).
pub const TEMPLATES: &[(&str, &str)] = &[
    ("SOUL.md", include_str!("templates/SOUL.md")),
    ("IDENTITY.md", include_str!("templates/IDENTITY.md")),
    ("BOOTSTRAP.md", include_str!("templates/BOOTSTRAP.md")),
];

/// Literal substitution token inside `BOOTSTRAP.md`. Replaced with the
/// bootstrap payload's `systemPrompt` on first write only.
pub const PROMPT_TOKEN: &str = "{{prompt}}";

/// Write missing templates into `memory_dir`. For each template:
/// - if the file exists (any content), skip it;
/// - otherwise, substitute `{{prompt}}` (only meaningful for BOOTSTRAP.md)
///   and atomically write it.
///
/// After writing, performs a strict check: all three files must exist and
/// be non-empty.
pub async fn seed(memory_dir: &Path, system_prompt: &str) -> Result<()> {
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
    }

    // Strict post-check: all three must exist and be non-empty.
    for &(name, _) in TEMPLATES {
        let dst = memory_dir.join(name);
        let meta = tokio::fs::metadata(&dst).await.map_err(|_| {
            Error::Personality(format!("{name} missing after seed"))
        })?;
        if meta.len() == 0 {
            return Err(Error::Personality(format!("{name} is empty after seed")));
        }
    }

    Ok(())
}
