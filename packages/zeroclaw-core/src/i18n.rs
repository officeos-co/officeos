//! Internationalization support for tool descriptions.
//!
//! Loads tool descriptions from TOML locale files in `tool_descriptions/`.
//! Falls back to English when a locale file or specific key is missing,
//! and ultimately falls back to the hardcoded `tool.description()` value
//! if no file-based description exists.

use std::collections::HashMap;
use std::path::{Path, PathBuf};
use tracing::debug;

/// Container for locale-specific tool descriptions loaded from TOML files.
#[derive(Debug, Clone)]
pub struct ToolDescriptions {
    /// Descriptions from the requested locale (may be empty if file missing).
    locale_descriptions: HashMap<String, String>,
    /// English fallback descriptions (always loaded when locale != "en").
    english_fallback: HashMap<String, String>,
    /// The resolved locale tag (e.g. "en", "zh-CN").
    locale: String,
}

/// TOML structure: `[tools]` table mapping tool name -> description string.
#[derive(Debug, serde::Deserialize)]
struct DescriptionFile {
    #[serde(default)]
    tools: HashMap<String, String>,
}

impl ToolDescriptions {
    /// Load descriptions for the given locale.
    ///
    /// `search_dirs` lists directories to probe for `tool_descriptions/<locale>.toml`.
    /// The first directory containing a matching file wins.
    ///
    /// Resolution:
    /// 1. Look up tool name in the locale file.
    /// 2. If missing (or locale file absent), look up in `en.toml`.
    /// 3. If still missing, callers fall back to `tool.description()`.
    pub fn load(locale: &str, search_dirs: &[PathBuf]) -> Self {
        let locale_descriptions = load_locale_file(locale, search_dirs);

        let english_fallback = if locale == "en" {
            HashMap::new()
        } else {
            load_locale_file("en", search_dirs)
        };

        debug!(
            locale = locale,
            locale_keys = locale_descriptions.len(),
            english_keys = english_fallback.len(),
            "tool descriptions loaded"
        );

        Self {
            locale_descriptions,
            english_fallback,
            locale: locale.to_string(),
        }
    }

    /// Get the description for a tool by name.
    ///
    /// Returns `Some(description)` if found in the locale file or English fallback.
    /// Returns `None` if neither file contains the key (caller should use hardcoded).
    pub fn get(&self, tool_name: &str) -> Option<&str> {
        self.locale_descriptions
            .get(tool_name)
            .or_else(|| self.english_fallback.get(tool_name))
            .map(String::as_str)
    }

    /// The resolved locale tag.
    pub fn locale(&self) -> &str {
        &self.locale
    }

    /// Create an empty instance that always returns `None` (hardcoded fallback).
    pub fn empty() -> Self {
        Self {
            locale_descriptions: HashMap::new(),
            english_fallback: HashMap::new(),
            locale: "en".to_string(),
        }
    }
}

/// Detect the user's preferred locale from environment variables.
///
/// Checks `ZEROCLAW_LOCALE`, then `LANG`, then `LC_ALL`.
/// Returns "en" if none are set or parseable.
pub fn detect_locale() -> String {
    if let Ok(val) = std::env::var("ZEROCLAW_LOCALE") {
        let val = val.trim().to_string();
        if !val.is_empty() {
            return normalize_locale(&val);
        }
    }
    for var in &["LANG", "LC_ALL"] {
        if let Ok(val) = std::env::var(var) {
            let locale = normalize_locale(&val);
            if locale != "C" && locale != "POSIX" && !locale.is_empty() {
                return locale;
            }
        }
    }
    "en".to_string()
}

/// Normalize a raw locale string (e.g. "zh_CN.UTF-8") to a tag we use
/// for file lookup (e.g. "zh-CN").
fn normalize_locale(raw: &str) -> String {
    // Strip encoding suffix (.UTF-8, .utf8, etc.)
    let base = raw.split('.').next().unwrap_or(raw);
    // Replace underscores with hyphens for BCP-47-ish consistency
    base.replace('_', "-")
}

/// Build the default set of search directories for locale files.
///
/// 1. The workspace directory itself (for project-local overrides).
/// 2. The binary's parent directory (for installed distributions).
/// 3. The compile-time `CARGO_MANIFEST_DIR` as a final fallback during dev.
pub fn default_search_dirs(workspace_dir: &Path) -> Vec<PathBuf> {
    let mut dirs = vec![workspace_dir.to_path_buf()];

    if let Ok(exe) = std::env::current_exe() {
        if let Some(parent) = exe.parent() {
            dirs.push(parent.to_path_buf());
        }
    }

    // During development, also check the project root (where Cargo.toml lives).
    let manifest_dir = PathBuf::from(env!("CARGO_MANIFEST_DIR"));
    if !dirs.contains(&manifest_dir) {
        dirs.push(manifest_dir);
    }

    dirs
}

/// Try to load and parse a locale TOML file from the first matching search dir.
fn load_locale_file(locale: &str, search_dirs: &[PathBuf]) -> HashMap<String, String> {
    let filename = format!("tool_descriptions/{locale}.toml");

    for dir in search_dirs {
        let path = dir.join(&filename);
        match std::fs::read_to_string(&path) {
            Ok(contents) => match toml::from_str::<DescriptionFile>(&contents) {
                Ok(parsed) => {
                    debug!(path = %path.display(), keys = parsed.tools.len(), "loaded locale file");
                    return parsed.tools;
                }
                Err(e) => {
                    debug!(path = %path.display(), error = %e, "failed to parse locale file");
                }
            },
            Err(_) => {
                // File not found in this directory, try next.
            }
        }
    }

    debug!(
        locale = locale,
        "no locale file found in any search directory"
    );
    HashMap::new()
}


#[cfg(test)]
#[path = "i18n.test.rs"]
mod tests;
