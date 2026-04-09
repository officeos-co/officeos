//! Personality system — loads workspace identity files (SOUL.md, IDENTITY.md,
//! USER.md, …) and injects them into the system prompt pipeline.
//!
//! As of Phase 3 (Office OS architecture) the agent does NOT provision its
//! own personality files. The dashboard backend renders Jinja2 templates
//! into the per-agent vault and into a Kubernetes ConfigMap that is mounted
//! at the agent's workspace directory before the container starts. The
//! agent only **reads** the files; the strict loader fails loudly if any
//! required file is missing so the dashboard can surface the error.
//!
//! See `docs/reference/identity-vault.md` for the file roster and per-file
//! conventions, and the Phase 3 entry in `STRIP_DOWN.md` for the
//! infrastructure flow.

use std::fmt::Write;
use std::path::{Path, PathBuf};

/// Maximum characters per personality file before truncation.
const MAX_FILE_CHARS: usize = 20_000;

/// Well-known personality files loaded from the workspace root.
///
/// This is the *full* roster the loader will look at. Some files are
/// REQUIRED for the agent to boot (see [`REQUIRED_PERSONALITY_FILES`]);
/// the rest are optional and may be absent without aborting boot.
pub const PERSONALITY_FILES: &[&str] = &[
    "SOUL.md",
    "IDENTITY.md",
    "USER.md",
    "AGENTS.md",
    "TOOLS.md",
    "HEARTBEAT.md",
    "BOOTSTRAP.md",
    "MEMORY.md",
];

/// Personality files that MUST be present and non-empty for the agent to
/// boot. If any of these is missing or empty, [`load_personality_strict`]
/// returns an error and the agent process should exit non-zero so the
/// dashboard can surface the failure.
///
/// The remaining entries in [`PERSONALITY_FILES`] (`USER.md`, `MEMORY.md`,
/// etc.) are optional — they describe state that may or may not exist for
/// a given agent and their absence is not a boot failure.
pub const REQUIRED_PERSONALITY_FILES: &[&str] = &["SOUL.md", "IDENTITY.md", "AGENTS.md"];

/// Errors returned by [`load_personality_strict`] when the workspace
/// directory does not contain a valid personality file set.
///
/// These errors are intentionally narrow: they describe failure modes the
/// dashboard backend should be able to recognize and display. They do NOT
/// trigger any auto-recovery — the agent's contract is "fail loudly so
/// infrastructure can fix it."
#[derive(Debug, thiserror::Error)]
pub enum PersonalityError {
    /// A required personality file is not present in the workspace.
    #[error("required personality file missing: {0}")]
    Missing(String),

    /// A required personality file exists but is empty (whitespace only).
    #[error("required personality file is empty: {0}")]
    Empty(String),

    /// I/O error reading a personality file (permissions, etc.).
    #[error("io error reading personality file {path}: {source}")]
    Io {
        path: String,
        #[source]
        source: std::io::Error,
    },
}

/// A single personality file loaded from the workspace.
#[derive(Debug, Clone)]
pub struct PersonalityFile {
    /// Filename (e.g. `SOUL.md`).
    pub name: String,
    /// Raw content (possibly truncated).
    pub content: String,
    /// Whether the content was truncated due to size limits.
    pub truncated: bool,
    /// Full path on disk.
    pub path: PathBuf,
}

/// Aggregated personality profile loaded from a workspace.
#[derive(Debug, Clone, Default)]
pub struct PersonalityProfile {
    /// Successfully loaded personality files.
    pub files: Vec<PersonalityFile>,
    /// Files that were expected but not found.
    pub missing: Vec<String>,
}

impl PersonalityProfile {
    /// Returns the content of a specific file by name, if loaded.
    pub fn get(&self, name: &str) -> Option<&str> {
        self.files
            .iter()
            .find(|f| f.name == name)
            .map(|f| f.content.as_str())
    }

    /// Returns `true` if no personality files were loaded.
    pub fn is_empty(&self) -> bool {
        self.files.is_empty()
    }

    /// Render all loaded personality files into a prompt fragment.
    pub fn render(&self) -> String {
        let mut out = String::new();
        for file in &self.files {
            let _ = writeln!(out, "### {}\n", file.name);
            out.push_str(&file.content);
            if file.truncated {
                let _ = writeln!(
                    out,
                    "\n\n[... truncated at {MAX_FILE_CHARS} chars — use `read` for full file]\n"
                );
            } else {
                out.push_str("\n\n");
            }
        }
        out
    }
}

/// Loads personality files from a workspace directory.
///
/// Each well-known file is read and validated.  Missing files are recorded
/// in `PersonalityProfile::missing` rather than treated as errors.
pub fn load_personality(workspace_dir: &Path) -> PersonalityProfile {
    load_personality_files(workspace_dir, PERSONALITY_FILES)
}

/// Load a specific set of personality files from a workspace directory.
pub fn load_personality_files(workspace_dir: &Path, filenames: &[&str]) -> PersonalityProfile {
    let mut profile = PersonalityProfile::default();

    for &filename in filenames {
        let path = workspace_dir.join(filename);
        match std::fs::read_to_string(&path) {
            Ok(raw) => {
                let trimmed = raw.trim();
                if trimmed.is_empty() {
                    profile.missing.push(filename.to_string());
                    continue;
                }
                let (content, truncated) = truncate_content(trimmed);
                profile.files.push(PersonalityFile {
                    name: filename.to_string(),
                    content,
                    truncated,
                    path,
                });
            }
            Err(_) => {
                profile.missing.push(filename.to_string());
            }
        }
    }

    profile
}

/// Strict loader for the agent boot path.
///
/// Loads the same set of files as [`load_personality`] but treats every
/// entry in [`REQUIRED_PERSONALITY_FILES`] as mandatory:
///
/// - If a required file is missing, returns [`PersonalityError::Missing`].
/// - If a required file exists but is empty (whitespace only), returns
///   [`PersonalityError::Empty`].
/// - If a required file cannot be read for I/O reasons (permission, etc.),
///   returns [`PersonalityError::Io`].
///
/// Optional files (everything in [`PERSONALITY_FILES`] that is not in
/// [`REQUIRED_PERSONALITY_FILES`]) follow the same lenient behaviour as
/// [`load_personality`]: missing-or-empty entries are recorded in
/// [`PersonalityProfile::missing`] and do not abort the boot.
///
/// On success, returns a fully populated [`PersonalityProfile`] ready to
/// be rendered into the system prompt.
///
/// This is the function the agent's boot path should call. The lenient
/// [`load_personality`] is retained for tooling that wants to inspect a
/// workspace without aborting (doctor, dashboard preview, etc.).
pub fn load_personality_strict(
    workspace_dir: &Path,
) -> Result<PersonalityProfile, PersonalityError> {
    let mut profile = PersonalityProfile::default();

    for &filename in PERSONALITY_FILES {
        let path = workspace_dir.join(filename);
        let is_required = REQUIRED_PERSONALITY_FILES.contains(&filename);

        match std::fs::read_to_string(&path) {
            Ok(raw) => {
                let trimmed = raw.trim();
                if trimmed.is_empty() {
                    if is_required {
                        return Err(PersonalityError::Empty(filename.to_string()));
                    }
                    profile.missing.push(filename.to_string());
                    continue;
                }
                let (content, truncated) = truncate_content(trimmed);
                profile.files.push(PersonalityFile {
                    name: filename.to_string(),
                    content,
                    truncated,
                    path,
                });
            }
            Err(err) => {
                if is_required {
                    if err.kind() == std::io::ErrorKind::NotFound {
                        return Err(PersonalityError::Missing(filename.to_string()));
                    }
                    return Err(PersonalityError::Io {
                        path: filename.to_string(),
                        source: err,
                    });
                }
                profile.missing.push(filename.to_string());
            }
        }
    }

    Ok(profile)
}

/// Truncate content to `MAX_FILE_CHARS` if necessary.
fn truncate_content(content: &str) -> (String, bool) {
    if content.chars().count() <= MAX_FILE_CHARS {
        return (content.to_string(), false);
    }
    let truncated = content
        .char_indices()
        .nth(MAX_FILE_CHARS)
        .map(|(idx, _)| &content[..idx])
        .unwrap_or(content);
    (truncated.to_string(), true)
}

#[cfg(test)]
#[path = "personality.test.rs"]
mod tests;
