//! Workspace profile management for multi-client isolation.
//!
//! Each workspace represents an isolated client engagement with its own
//! memory namespace, audit trail, secrets scope, and tool restrictions.
//! Profiles are stored under `~/.zeroclaw/workspaces/<client_name>/`.

use anyhow::{Context, Result, bail};
use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::path::{Path, PathBuf};

/// A single client workspace profile.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct WorkspaceProfile {
    /// Human-readable workspace name (also used as directory name).
    pub name: String,
    /// Allowed domains for network access within this workspace.
    #[serde(default)]
    pub allowed_domains: Vec<String>,
    /// Credential profile name scoped to this workspace.
    #[serde(default)]
    pub credential_profile: Option<String>,
    /// Memory namespace prefix for isolation.
    #[serde(default)]
    pub memory_namespace: Option<String>,
    /// Audit namespace prefix for isolation.
    #[serde(default)]
    pub audit_namespace: Option<String>,
    /// Tool names denied in this workspace (e.g. `["shell"]` to block shell access).
    #[serde(default)]
    pub tool_restrictions: Vec<String>,
}

impl WorkspaceProfile {
    /// Effective memory namespace (falls back to workspace name).
    pub fn effective_memory_namespace(&self) -> &str {
        self.memory_namespace
            .as_deref()
            .unwrap_or(self.name.as_str())
    }

    /// Effective audit namespace (falls back to workspace name).
    pub fn effective_audit_namespace(&self) -> &str {
        self.audit_namespace
            .as_deref()
            .unwrap_or(self.name.as_str())
    }

    /// Returns true if the given tool name is restricted in this workspace.
    pub fn is_tool_restricted(&self, tool_name: &str) -> bool {
        self.tool_restrictions
            .iter()
            .any(|r| r.eq_ignore_ascii_case(tool_name))
    }

    /// Returns true if the given domain is allowed for this workspace.
    /// An empty allowlist means all domains are allowed.
    pub fn is_domain_allowed(&self, domain: &str) -> bool {
        if self.allowed_domains.is_empty() {
            return true;
        }
        let domain_lower = domain.to_ascii_lowercase();
        self.allowed_domains
            .iter()
            .any(|d| domain_lower == d.to_ascii_lowercase())
    }
}

/// Manages loading and switching between client workspace profiles.
#[derive(Debug, Clone)]
pub struct WorkspaceManager {
    /// Base directory containing all workspace subdirectories.
    workspaces_dir: PathBuf,
    /// Loaded workspace profiles keyed by name.
    profiles: HashMap<String, WorkspaceProfile>,
    /// Currently active workspace name.
    active: Option<String>,
}

impl WorkspaceManager {
    /// Create a new workspace manager rooted at the given directory.
    pub fn new(workspaces_dir: PathBuf) -> Self {
        Self {
            workspaces_dir,
            profiles: HashMap::new(),
            active: None,
        }
    }

    /// Load all workspace profiles from disk.
    ///
    /// Each subdirectory of `workspaces_dir` that contains a `profile.toml`
    /// is treated as a workspace.
    pub async fn load_profiles(&mut self) -> Result<()> {
        self.profiles.clear();

        let dir = &self.workspaces_dir;
        if !dir.exists() {
            return Ok(());
        }

        let mut entries = tokio::fs::read_dir(dir)
            .await
            .with_context(|| format!("reading workspaces directory: {}", dir.display()))?;

        while let Some(entry) = entries.next_entry().await? {
            let path = entry.path();
            if !path.is_dir() {
                continue;
            }
            let profile_path = path.join("profile.toml");
            if !profile_path.exists() {
                continue;
            }
            match tokio::fs::read_to_string(&profile_path).await {
                Ok(contents) => match toml::from_str::<WorkspaceProfile>(&contents) {
                    Ok(profile) => {
                        self.profiles.insert(profile.name.clone(), profile);
                    }
                    Err(e) => {
                        tracing::warn!(
                            "skipping malformed workspace profile {}: {e}",
                            profile_path.display()
                        );
                    }
                },
                Err(e) => {
                    tracing::warn!(
                        "skipping unreadable workspace profile {}: {e}",
                        profile_path.display()
                    );
                }
            }
        }

        Ok(())
    }

    /// Switch to the named workspace. Returns an error if it does not exist.
    pub fn switch(&mut self, name: &str) -> Result<&WorkspaceProfile> {
        if !self.profiles.contains_key(name) {
            bail!("workspace '{}' not found", name);
        }
        self.active = Some(name.to_string());
        Ok(&self.profiles[name])
    }

    /// Get the currently active workspace profile, if any.
    pub fn active_profile(&self) -> Option<&WorkspaceProfile> {
        self.active
            .as_deref()
            .and_then(|name| self.profiles.get(name))
    }

    /// Get the active workspace name.
    pub fn active_name(&self) -> Option<&str> {
        self.active.as_deref()
    }

    /// List all loaded workspace names.
    pub fn list(&self) -> Vec<&str> {
        let mut names: Vec<&str> = self.profiles.keys().map(String::as_str).collect();
        names.sort_unstable();
        names
    }

    /// Get a workspace profile by name.
    pub fn get(&self, name: &str) -> Option<&WorkspaceProfile> {
        self.profiles.get(name)
    }

    /// Create a new workspace on disk and register it.
    pub async fn create(&mut self, name: &str) -> Result<&WorkspaceProfile> {
        if name.is_empty() {
            bail!("workspace name must not be empty");
        }
        // Validate name: alphanumeric, hyphens, underscores only
        if !name
            .chars()
            .all(|c| c.is_ascii_alphanumeric() || c == '-' || c == '_')
        {
            bail!(
                "workspace name must contain only alphanumeric characters, hyphens, or underscores"
            );
        }
        if self.profiles.contains_key(name) {
            bail!("workspace '{}' already exists", name);
        }

        let ws_dir = self.workspaces_dir.join(name);
        tokio::fs::create_dir_all(&ws_dir)
            .await
            .with_context(|| format!("creating workspace directory: {}", ws_dir.display()))?;

        let profile = WorkspaceProfile {
            name: name.to_string(),
            allowed_domains: Vec::new(),
            credential_profile: None,
            memory_namespace: Some(name.to_string()),
            audit_namespace: Some(name.to_string()),
            tool_restrictions: Vec::new(),
        };

        let toml_str = toml::to_string_pretty(&profile).context("serializing workspace profile")?;
        let profile_path = ws_dir.join("profile.toml");
        tokio::fs::write(&profile_path, toml_str)
            .await
            .with_context(|| format!("writing workspace profile: {}", profile_path.display()))?;

        self.profiles.insert(name.to_string(), profile);
        Ok(&self.profiles[name])
    }

    /// Export a workspace profile as a sanitized TOML string (no secrets).
    pub fn export(&self, name: &str) -> Result<String> {
        let profile = self
            .profiles
            .get(name)
            .with_context(|| format!("workspace '{}' not found", name))?;

        // Create an export-safe copy with credential_profile redacted
        let export = WorkspaceProfile {
            credential_profile: profile
                .credential_profile
                .as_ref()
                .map(|_| "***".to_string()),
            ..profile.clone()
        };

        toml::to_string_pretty(&export).context("serializing workspace profile for export")
    }

    /// Directory for a specific workspace.
    pub fn workspace_dir(&self, name: &str) -> PathBuf {
        self.workspaces_dir.join(name)
    }

    /// Base workspaces directory.
    pub fn workspaces_dir(&self) -> &Path {
        &self.workspaces_dir
    }
}

#[cfg(test)]
#[path = "workspace.test.rs"]
mod tests;
