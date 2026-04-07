//! Vault sync: pull/push personality and memory files to/from Obsidian vault.
//!
//! On agent boot, downloads Tier 0 (identity) and Tier 1 (memory) files from
//! the agent's personal vault database to the local workspace. The existing
//! `personality.rs` loader then reads them from disk — no prompt pipeline changes.
//!
//! Tier 1 files are synced bidirectionally: modified local files are pushed
//! back to the vault periodically or on shutdown.
//!
//! All vault operations go through the `vault` CLI (obsctl), preserving the
//! abstraction: the agent never sees CouchDB URLs or connection details.

use anyhow::Context;
use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::path::{Path, PathBuf};
use std::time::SystemTime;

use super::personality::PERSONALITY_FILES;

/// Files that are read-only from the vault (agent identity).
const TIER0_FILES: &[&str] = &[
    "SOUL.md",
    "IDENTITY.md",
    "AGENTS.md",
    "TOOLS.md",
    "HEARTBEAT.md",
    "BOOTSTRAP.md",
];

/// Files that are synced bidirectionally (agent working memory).
const TIER1_FILES: &[&str] = &["USER.md", "MEMORY.md"];

/// Maximum time to wait for a single vault CLI operation.
const VAULT_CMD_TIMEOUT_SECS: u64 = 10;

/// Result of a sync operation.
#[derive(Debug, Default)]
pub struct SyncReport {
    pub synced: Vec<String>,
    pub failed: Vec<(String, String)>,
    pub skipped: Vec<String>,
}

impl SyncReport {
    pub fn summary(&self) -> String {
        format!(
            "vault sync: {} synced, {} failed, {} skipped",
            self.synced.len(),
            self.failed.len(),
            self.skipped.len(),
        )
    }
}

/// Tracks sync state per file to avoid unnecessary reads/writes.
#[derive(Debug, Clone, Serialize, Deserialize, Default)]
struct SyncState {
    files: HashMap<String, FileSyncState>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
struct FileSyncState {
    /// Unix timestamp of last successful sync.
    last_synced_epoch: u64,
    /// File size at last sync (cheap change detection).
    last_synced_size: u64,
}

/// Vault sync manager. Syncs personality/memory files between local workspace
/// and the agent's personal vault database via the `vault` CLI.
pub struct VaultSync {
    workspace_dir: PathBuf,
    /// The CouchDB database name for this agent's personal vault.
    vault_database: String,
    /// Path to the sync state tracking file.
    sync_state_path: PathBuf,
}

impl VaultSync {
    pub fn new(workspace_dir: &Path, vault_database: &str) -> Self {
        let sync_state_path = workspace_dir.join(".vault_sync_state.json");
        Self {
            workspace_dir: workspace_dir.to_path_buf(),
            vault_database: vault_database.to_string(),
            sync_state_path,
        }
    }

    /// Check if the vault CLI is available and the database is reachable.
    pub fn is_available(&self) -> bool {
        match self.run_vault_cmd(&["ping", "--json"]) {
            Ok(output) => output.contains("ok"),
            Err(_) => false,
        }
    }

    /// Pull all personality files (Tier 0 + Tier 1) from the vault to local workspace.
    ///
    /// Called once at agent boot. Never fatal — logs warnings and continues
    /// with whatever local files already exist.
    pub fn sync_from_vault(&self) -> SyncReport {
        let mut report = SyncReport::default();

        for &filename in PERSONALITY_FILES {
            match self.pull_file(filename) {
                Ok(true) => report.synced.push(filename.to_string()),
                Ok(false) => report.skipped.push(filename.to_string()),
                Err(e) => {
                    tracing::warn!("vault sync: failed to pull {filename}: {e}");
                    report
                        .failed
                        .push((filename.to_string(), e.to_string()));
                }
            }
        }

        tracing::info!("{}", report.summary());
        report
    }

    /// Push modified Tier 1 files back to the vault.
    ///
    /// Only syncs files that have been modified since the last sync.
    pub fn sync_to_vault(&self) -> SyncReport {
        let mut report = SyncReport::default();
        let state = self.load_sync_state();

        for &filename in TIER1_FILES {
            let local_path = self.workspace_dir.join(filename);
            if !local_path.exists() {
                report.skipped.push(filename.to_string());
                continue;
            }

            // Check if file was modified since last sync
            let current_size = std::fs::metadata(&local_path)
                .map(|m| m.len())
                .unwrap_or(0);

            if let Some(file_state) = state.files.get(filename) {
                if file_state.last_synced_size == current_size {
                    report.skipped.push(filename.to_string());
                    continue;
                }
            }

            match self.push_file(filename) {
                Ok(()) => report.synced.push(filename.to_string()),
                Err(e) => {
                    tracing::warn!("vault sync: failed to push {filename}: {e}");
                    report
                        .failed
                        .push((filename.to_string(), e.to_string()));
                }
            }
        }

        if !report.synced.is_empty() {
            tracing::info!("vault push: {}", report.summary());
        }
        report
    }

    /// Pull a single file from the vault to local workspace.
    /// Returns Ok(true) if file was synced, Ok(false) if not found in vault.
    fn pull_file(&self, filename: &str) -> anyhow::Result<bool> {
        let output = self.run_vault_cmd(&["read", "--file", filename, "--json"])?;

        // Parse JSON output from vault CLI
        let parsed: serde_json::Value = serde_json::from_str(&output)
            .context("failed to parse vault read output as JSON")?;

        let content = match parsed.get("content").and_then(|v| v.as_str()) {
            Some(c) => c,
            None => return Ok(false), // Note not found in vault
        };

        // Write to local workspace
        let local_path = self.workspace_dir.join(filename);
        std::fs::write(&local_path, content)
            .with_context(|| format!("failed to write {filename} to workspace"))?;

        // Update sync state
        self.update_sync_state(filename, content.len() as u64);

        Ok(true)
    }

    /// Push a single file from local workspace to the vault.
    fn push_file(&self, filename: &str) -> anyhow::Result<()> {
        let local_path = self.workspace_dir.join(filename);
        let content = std::fs::read_to_string(&local_path)
            .with_context(|| format!("failed to read local {filename}"))?;

        self.run_vault_cmd(&[
            "write",
            "--path",
            filename,
            "--content",
            &content,
            "--force",
            "--yes",
        ])?;

        self.update_sync_state(filename, content.len() as u64);
        Ok(())
    }

    /// Execute a vault CLI command with the agent's database.
    fn run_vault_cmd(&self, args: &[&str]) -> anyhow::Result<String> {
        let output = std::process::Command::new("vault")
            .args(args)
            .env("VAULT_DATABASE", &self.vault_database)
            .stdout(std::process::Stdio::piped())
            .stderr(std::process::Stdio::piped())
            .spawn()
            .context("failed to spawn vault CLI — is obsctl installed?")?
            .wait_with_output()
            .context("vault CLI process failed")?;

        if !output.status.success() {
            let stderr = String::from_utf8_lossy(&output.stderr);
            anyhow::bail!("vault CLI error: {stderr}");
        }

        Ok(String::from_utf8_lossy(&output.stdout).to_string())
    }

    fn load_sync_state(&self) -> SyncState {
        std::fs::read_to_string(&self.sync_state_path)
            .ok()
            .and_then(|s| serde_json::from_str(&s).ok())
            .unwrap_or_default()
    }

    fn update_sync_state(&self, filename: &str, size: u64) {
        let mut state = self.load_sync_state();
        let now = SystemTime::now()
            .duration_since(SystemTime::UNIX_EPOCH)
            .map(|d| d.as_secs())
            .unwrap_or(0);

        state.files.insert(
            filename.to_string(),
            FileSyncState {
                last_synced_epoch: now,
                last_synced_size: size,
            },
        );

        if let Ok(json) = serde_json::to_string_pretty(&state) {
            let _ = std::fs::write(&self.sync_state_path, json);
        }
    }
}


#[cfg(test)]
#[path = "vault_sync.test.rs"]
mod tests;
