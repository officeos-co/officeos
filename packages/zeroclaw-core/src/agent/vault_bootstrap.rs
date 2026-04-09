//! Vault bootstrap — hydrates the agent workspace from a CouchDB-backed
//! vault before the personality loader runs.
//!
//! Replaces the previous "init container writes files, agent reads files"
//! split with a single boot path: the agent itself fetches SOUL.md /
//! IDENTITY.md / AGENTS.md (and optional peers) from CouchDB on startup,
//! caches them on the workspace PVC, and then hands off to
//! `personality::load_personality_strict`.
//!
//! ## Configuration
//!
//! Four environment variables, all required if any is set:
//! - `ZEROCLAW_VAULT_URL` — e.g. `http://eaos-couchdb:5984`
//! - `ZEROCLAW_VAULT_DB`  — e.g. `eaos-agent-<uuid>`
//! - `ZEROCLAW_VAULT_USER`
//! - `ZEROCLAW_VAULT_PASSWORD`
//!
//! If none are set, [`hydrate`] is a no-op (preserves local-dev behavior
//! where a workspace directory is managed manually).
//!
//! ## Caching
//!
//! If every file in [`REQUIRED_PERSONALITY_FILES`] already exists
//! non-empty in `workspace_dir`, no network call is made. This makes
//! subsequent pod boots fast and lets the agent survive brief CouchDB
//! outages once it has successfully hydrated at least once.

use std::path::Path;
use std::time::Duration;

use base64::Engine;
use serde_json::Value;

use crate::agent::personality::{PERSONALITY_FILES, REQUIRED_PERSONALITY_FILES};

const ENV_URL: &str = "ZEROCLAW_VAULT_URL";
const ENV_DB: &str = "ZEROCLAW_VAULT_DB";
const ENV_USER: &str = "ZEROCLAW_VAULT_USER";
const ENV_PASSWORD: &str = "ZEROCLAW_VAULT_PASSWORD";

const MAX_ATTEMPTS: u32 = 5;
const INITIAL_BACKOFF_MS: u64 = 500;

#[derive(Debug, thiserror::Error)]
pub enum VaultBootstrapError {
    #[error(
        "partial vault configuration: {0} is set but one or more of \
         ZEROCLAW_VAULT_URL/DB/USER/PASSWORD is missing"
    )]
    PartialConfig(&'static str),

    #[error("failed to create workspace directory {path:?}: {source}")]
    WorkspaceCreate {
        path: std::path::PathBuf,
        #[source]
        source: std::io::Error,
    },

    #[error(
        "vault unreachable at {url} after {attempts} attempts: {source}"
    )]
    Unreachable {
        url: String,
        attempts: u32,
        #[source]
        source: reqwest::Error,
    },

    #[error("vault auth failed ({status}) — check ZEROCLAW_VAULT_USER/PASSWORD")]
    AuthFailed { status: u16 },

    #[error("vault database {db} not found on server {url}")]
    DbNotFound { url: String, db: String },

    #[error("required vault document missing: {0}")]
    DocMissing(String),

    #[error("vault document {name} is malformed: {reason}")]
    DocMalformed { name: String, reason: String },

    #[error("vault server error {status} fetching {name}")]
    ServerError { status: u16, name: String },

    #[error("failed to write {path:?}: {source}")]
    Io {
        path: std::path::PathBuf,
        #[source]
        source: std::io::Error,
    },
}

struct VaultConfig {
    url: String,
    db: String,
    basic_auth: String,
}

fn load_config() -> Result<Option<VaultConfig>, VaultBootstrapError> {
    let url = std::env::var(ENV_URL).ok();
    let db = std::env::var(ENV_DB).ok();
    let user = std::env::var(ENV_USER).ok();
    let password = std::env::var(ENV_PASSWORD).ok();

    match (url, db, user, password) {
        (None, None, None, None) => Ok(None),
        (Some(url), Some(db), Some(user), Some(password)) => {
            let token = base64::engine::general_purpose::STANDARD
                .encode(format!("{user}:{password}"));
            Ok(Some(VaultConfig {
                url: url.trim_end_matches('/').to_string(),
                db,
                basic_auth: format!("Basic {token}"),
            }))
        }
        (None, _, _, _) => Err(VaultBootstrapError::PartialConfig(ENV_URL)),
        (_, None, _, _) => Err(VaultBootstrapError::PartialConfig(ENV_DB)),
        (_, _, None, _) => Err(VaultBootstrapError::PartialConfig(ENV_USER)),
        (_, _, _, None) => Err(VaultBootstrapError::PartialConfig(ENV_PASSWORD)),
    }
}

fn cache_is_valid(workspace_dir: &Path) -> bool {
    REQUIRED_PERSONALITY_FILES.iter().all(|&name| {
        let path = workspace_dir.join(name);
        match std::fs::read_to_string(&path) {
            Ok(content) => !content.trim().is_empty(),
            Err(_) => false,
        }
    })
}

/// Hydrate the agent workspace from CouchDB if configured.
///
/// On success, every file in [`REQUIRED_PERSONALITY_FILES`] is guaranteed
/// to exist and be non-empty under `workspace_dir` (either cached from a
/// previous boot or freshly fetched).
pub async fn hydrate(workspace_dir: &Path) -> Result<(), VaultBootstrapError> {
    let Some(config) = load_config()? else {
        tracing::info!(
            "vault bootstrap: no ZEROCLAW_VAULT_* env vars set, skipping"
        );
        return Ok(());
    };

    std::fs::create_dir_all(workspace_dir).map_err(|source| {
        VaultBootstrapError::WorkspaceCreate {
            path: workspace_dir.to_path_buf(),
            source,
        }
    })?;

    if cache_is_valid(workspace_dir) {
        tracing::info!(
            "vault bootstrap: required files already cached on disk at {:?}, skipping fetch",
            workspace_dir
        );
        return Ok(());
    }

    tracing::info!(
        "vault bootstrap: fetching vault {}/{} into {:?}",
        config.url,
        config.db,
        workspace_dir
    );

    let client = reqwest::Client::builder()
        .timeout(Duration::from_secs(10))
        .build()
        .expect("reqwest client builds with default config");

    for &name in PERSONALITY_FILES {
        let required = REQUIRED_PERSONALITY_FILES.contains(&name);
        match fetch_doc(&client, &config, name).await {
            Ok(Some(content)) => {
                let path = workspace_dir.join(name);
                std::fs::write(&path, content).map_err(|source| {
                    VaultBootstrapError::Io { path, source }
                })?;
            }
            Ok(None) => {
                if required {
                    return Err(VaultBootstrapError::DocMissing(name.to_string()));
                }
                tracing::debug!("vault bootstrap: optional file {} not present, skipping", name);
            }
            Err(err) => {
                if required {
                    return Err(err);
                }
                tracing::warn!(
                    "vault bootstrap: optional file {} failed to fetch, skipping: {}",
                    name,
                    err
                );
            }
        }
    }

    tracing::info!("vault bootstrap: hydration complete");
    Ok(())
}

async fn fetch_doc(
    client: &reqwest::Client,
    config: &VaultConfig,
    name: &str,
) -> Result<Option<String>, VaultBootstrapError> {
    let url = format!("{}/{}/{}", config.url, config.db, name);

    let mut last_transport_err: Option<reqwest::Error> = None;
    let mut backoff_ms = INITIAL_BACKOFF_MS;

    for attempt in 1..=MAX_ATTEMPTS {
        let response = client
            .get(&url)
            .header(reqwest::header::AUTHORIZATION, &config.basic_auth)
            .send()
            .await;

        match response {
            Ok(resp) => {
                let status = resp.status();
                if status.is_success() {
                    let body = resp.text().await.map_err(|source| {
                        VaultBootstrapError::Unreachable {
                            url: url.clone(),
                            attempts: attempt,
                            source,
                        }
                    })?;
                    let value: Value = serde_json::from_str(&body).map_err(|e| {
                        VaultBootstrapError::DocMalformed {
                            name: name.to_string(),
                            reason: format!("invalid JSON: {e}"),
                        }
                    })?;
                    let content = value
                        .get("content")
                        .and_then(|v| v.as_str())
                        .ok_or_else(|| VaultBootstrapError::DocMalformed {
                            name: name.to_string(),
                            reason: "missing string field `content`".to_string(),
                        })?;
                    return Ok(Some(content.to_string()));
                }

                if status == reqwest::StatusCode::NOT_FOUND {
                    // Could be db-missing or doc-missing. Distinguish by
                    // hitting the DB root on a clean 404.
                    if is_db_missing(client, config).await {
                        return Err(VaultBootstrapError::DbNotFound {
                            url: config.url.clone(),
                            db: config.db.clone(),
                        });
                    }
                    return Ok(None);
                }

                if status == reqwest::StatusCode::UNAUTHORIZED
                    || status == reqwest::StatusCode::FORBIDDEN
                {
                    return Err(VaultBootstrapError::AuthFailed {
                        status: status.as_u16(),
                    });
                }

                if !status.is_server_error() {
                    return Err(VaultBootstrapError::ServerError {
                        status: status.as_u16(),
                        name: name.to_string(),
                    });
                }
                tracing::warn!(
                    "vault bootstrap: {} returned {}, retrying (attempt {}/{})",
                    url,
                    status,
                    attempt,
                    MAX_ATTEMPTS
                );
            }
            Err(err) => {
                tracing::warn!(
                    "vault bootstrap: transport error on {}: {} (attempt {}/{})",
                    url,
                    err,
                    attempt,
                    MAX_ATTEMPTS
                );
                last_transport_err = Some(err);
            }
        }

        if attempt < MAX_ATTEMPTS {
            tokio::time::sleep(Duration::from_millis(backoff_ms)).await;
            backoff_ms = backoff_ms.saturating_mul(2);
        }
    }

    Err(VaultBootstrapError::Unreachable {
        url,
        attempts: MAX_ATTEMPTS,
        source: last_transport_err.expect("loop exits via success or transport error"),
    })
}

async fn is_db_missing(client: &reqwest::Client, config: &VaultConfig) -> bool {
    let url = format!("{}/{}", config.url, config.db);
    match client
        .get(&url)
        .header(reqwest::header::AUTHORIZATION, &config.basic_auth)
        .send()
        .await
    {
        Ok(resp) => resp.status() == reqwest::StatusCode::NOT_FOUND,
        Err(_) => false,
    }
}
