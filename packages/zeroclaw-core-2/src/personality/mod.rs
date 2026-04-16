//! Embedded personality templates + idempotent write on first boot.
//! See API.md §5.

use std::path::Path;

use crate::error::Result;

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
/// Phase 3: implement atomic-write-if-absent + strict post-check for the
/// three required files (SOUL, IDENTITY, BOOTSTRAP must exist and be
/// non-empty afterwards).
pub async fn seed(memory_dir: &Path, system_prompt: &str) -> Result<()> {
    let _ = (memory_dir, system_prompt);
    todo!("Phase 3: write missing templates, substitute {{prompt}}, strict check")
}
