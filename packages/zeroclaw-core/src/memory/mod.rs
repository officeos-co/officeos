//! Memory factory — selects and builds the `Box<dyn Memory>` for an Agent.
//!
//! The main entry point is `create_memory_with_storage_and_routes`, which
//! reads `config.memory` and dispatches via
//! [`backend::classify_memory_backend`] to one of the three surviving
//! backends. The returned `Box<dyn Memory>` is what every
//! [`Agent`](crate::agent::Agent) reads from and writes to during a turn.
//!
//! ## Backends (post Phase 4)
//!
//! - [`sqlite::SqliteMemory`] — the default. Hybrid retrieval (FTS + vector)
//!   layered by `retrieval.rs`, which wraps the raw backend with cache, FTS,
//!   and vector stages. This is the only backend with the full retrieval
//!   pipeline today.
//! - [`obsidian::ObsidianMemory`] — opt-in. Writes notes to a CouchDB-backed
//!   vault via `obsctl` and is intended for human-readable, syncable memory.
//! - [`none::NoneMemory`] — no-op stub used for tests and stateless agents.
//!
//! Historical backends (lucid, qdrant, markdown) and the
//! audit/policy/hygiene/snapshot decorators were removed as orphan code.
//!
//! [`namespaced::NamespacedMemory`] is a decorator that wraps any `Memory`
//! to add namespace isolation. It is used by `src/tools/delegate.rs` to give
//! delegated sub-agents their own private memory namespace within the same
//! underlying backend.
//!
//! ## Key types
//! - [`traits::Memory`] — the trait every backend implements.
//! - [`backend::classify_memory_backend`] — config → backend kind.
//! - [`namespaced::NamespacedMemory`] — namespace-isolating decorator.
//!
//! ## Related
//! - `src/memory/retrieval.rs` — hybrid retrieval pipeline (SQLite only).
//! - `src/tools/delegate.rs` — uses `NamespacedMemory` for sub-agents.
//! - `docs/reference/memory-future.md` — planned move to a centralized
//!   `RemoteMemory` service replacing SQLite.

pub mod backend;
pub mod chunker;
pub mod cli;
pub mod conflict;
pub mod consolidation;
pub mod decay;
pub mod embeddings;
pub mod importance;
pub mod namespaced;
pub mod none;
pub mod obsidian;
pub mod response_cache;
pub mod retrieval;
pub mod sqlite;
pub mod traits;
pub mod vector;

#[allow(unused_imports)]
pub use backend::{
    MemoryBackendKind, MemoryBackendProfile, classify_memory_backend, default_memory_backend_key,
    memory_backend_profile, selectable_memory_backends,
};
pub use namespaced::NamespacedMemory;
pub use none::NoneMemory;
pub use obsidian::ObsidianMemory;
pub use response_cache::ResponseCache;
#[allow(unused_imports)]
pub use retrieval::{RetrievalConfig, RetrievalPipeline};
pub use sqlite::SqliteMemory;
pub use traits::Memory;
#[allow(unused_imports)]
pub use traits::{ExportFilter, MemoryCategory, MemoryEntry, ProceduralMessage};

use crate::config::{EmbeddingRouteConfig, MemoryConfig, StorageProviderConfig};
use std::path::Path;
use std::sync::Arc;

fn create_memory_with_builders<F>(
    backend_name: &str,
    _workspace_dir: &Path,
    mut sqlite_builder: F,
    unknown_context: &str,
) -> anyhow::Result<Box<dyn Memory>>
where
    F: FnMut() -> anyhow::Result<SqliteMemory>,
{
    match classify_memory_backend(backend_name) {
        MemoryBackendKind::Sqlite => Ok(Box::new(sqlite_builder()?)),
        MemoryBackendKind::Obsidian => {
            tracing::info!("📓 Obsidian vault memory backend enabled");
            Ok(Box::new(ObsidianMemory::new()))
        }
        MemoryBackendKind::None => Ok(Box::new(NoneMemory::new())),
        MemoryBackendKind::Unknown => {
            tracing::warn!(
                "Unknown memory backend '{backend_name}'{unknown_context}, disabling persistent memory"
            );
            Ok(Box::new(NoneMemory::new()))
        }
    }
}

pub fn effective_memory_backend_name(
    memory_backend: &str,
    storage_provider: Option<&StorageProviderConfig>,
) -> String {
    if let Some(override_provider) = storage_provider
        .map(|cfg| cfg.provider.trim())
        .filter(|provider| !provider.is_empty())
    {
        return override_provider.to_ascii_lowercase();
    }

    memory_backend.trim().to_ascii_lowercase()
}

/// Legacy auto-save key used for model-authored assistant summaries.
/// These entries are treated as untrusted context and should not be re-injected.
pub fn is_assistant_autosave_key(key: &str) -> bool {
    let normalized = key.trim().to_ascii_lowercase();
    normalized == "assistant_resp" || normalized.starts_with("assistant_resp_")
}

/// Filter known synthetic autosave noise patterns that should not be
/// persisted as user conversation memories.
pub fn should_skip_autosave_content(content: &str) -> bool {
    let normalized = content.trim();
    if normalized.is_empty() {
        return true;
    }

    let lowered = normalized.to_ascii_lowercase();
    lowered.starts_with("[cron:")
        || lowered.starts_with("[heartbeat task")
        || lowered.starts_with("[distilled_")
        || lowered.contains("distilled_index_sig:")
}

#[derive(Clone, PartialEq, Eq)]
struct ResolvedEmbeddingConfig {
    provider: String,
    model: String,
    dimensions: usize,
    api_key: Option<String>,
}

impl std::fmt::Debug for ResolvedEmbeddingConfig {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.debug_struct("ResolvedEmbeddingConfig")
            .field("provider", &self.provider)
            .field("model", &self.model)
            .field("dimensions", &self.dimensions)
            .finish_non_exhaustive()
    }
}

/// Look up the provider-specific environment variable for common embedding providers,
/// so that `OPENAI_API_KEY` (etc.) takes precedence over the default-provider key
/// that the caller passes in. Returns `None` for unknown providers.
fn embedding_provider_env_key(provider: &str) -> Option<String> {
    let env_var = match provider.trim() {
        "openai" => "OPENAI_API_KEY",
        "openrouter" => "OPENROUTER_API_KEY",
        "cohere" => "COHERE_API_KEY",
        _ => return None,
    };
    std::env::var(env_var)
        .ok()
        .map(|v| v.trim().to_string())
        .filter(|v| !v.is_empty())
}

fn resolve_embedding_config(
    config: &MemoryConfig,
    embedding_routes: &[EmbeddingRouteConfig],
    api_key: Option<&str>,
) -> ResolvedEmbeddingConfig {
    let caller_api_key = api_key
        .map(str::trim)
        .filter(|value| !value.is_empty())
        .map(str::to_string);
    // Prefer a provider-specific env var over the caller-supplied key, which
    // may come from the default (chat) provider and differ from the embedding
    // provider (issue #3083: gemini key leaking to openai embeddings endpoint).
    let fallback_api_key =
        embedding_provider_env_key(config.embedding_provider.trim()).or(caller_api_key);
    let fallback = ResolvedEmbeddingConfig {
        provider: config.embedding_provider.trim().to_string(),
        model: config.embedding_model.trim().to_string(),
        dimensions: config.embedding_dimensions,
        api_key: fallback_api_key.clone(),
    };

    let Some(hint) = config
        .embedding_model
        .strip_prefix("hint:")
        .map(str::trim)
        .filter(|value| !value.is_empty())
    else {
        return fallback;
    };

    let Some(route) = embedding_routes
        .iter()
        .find(|route| route.hint.trim() == hint)
    else {
        tracing::warn!(
            hint,
            "Unknown embedding route hint; falling back to [memory] embedding settings"
        );
        return fallback;
    };

    let provider = route.provider.trim();
    let model = route.model.trim();
    let dimensions = route.dimensions.unwrap_or(config.embedding_dimensions);
    if provider.is_empty() || model.is_empty() || dimensions == 0 {
        tracing::warn!(
            hint,
            "Invalid embedding route configuration; falling back to [memory] embedding settings"
        );
        return fallback;
    }

    let routed_api_key = route
        .api_key
        .as_deref()
        .map(str::trim)
        .filter(|value: &&str| !value.is_empty())
        .map(|value| value.to_string());

    ResolvedEmbeddingConfig {
        provider: provider.to_string(),
        model: model.to_string(),
        dimensions,
        api_key: routed_api_key.or(fallback_api_key),
    }
}

/// Factory: create the right memory backend from config
pub fn create_memory(
    config: &MemoryConfig,
    workspace_dir: &Path,
    api_key: Option<&str>,
) -> anyhow::Result<Box<dyn Memory>> {
    create_memory_with_storage_and_routes(config, &[], None, workspace_dir, api_key)
}

/// Factory: create memory with optional storage-provider override.
pub fn create_memory_with_storage(
    config: &MemoryConfig,
    storage_provider: Option<&StorageProviderConfig>,
    workspace_dir: &Path,
    api_key: Option<&str>,
) -> anyhow::Result<Box<dyn Memory>> {
    create_memory_with_storage_and_routes(config, &[], storage_provider, workspace_dir, api_key)
}

/// Factory: create memory with optional storage-provider override and embedding routes.
pub fn create_memory_with_storage_and_routes(
    config: &MemoryConfig,
    embedding_routes: &[EmbeddingRouteConfig],
    storage_provider: Option<&StorageProviderConfig>,
    workspace_dir: &Path,
    api_key: Option<&str>,
) -> anyhow::Result<Box<dyn Memory>> {
    let backend_name = effective_memory_backend_name(&config.backend, storage_provider);
    let resolved_embedding = resolve_embedding_config(config, embedding_routes, api_key);

    fn build_sqlite_memory(
        config: &MemoryConfig,
        workspace_dir: &Path,
        resolved_embedding: &ResolvedEmbeddingConfig,
    ) -> anyhow::Result<SqliteMemory> {
        let embedder: Arc<dyn embeddings::EmbeddingProvider> =
            Arc::from(embeddings::create_embedding_provider(
                &resolved_embedding.provider,
                resolved_embedding.api_key.as_deref(),
                &resolved_embedding.model,
                resolved_embedding.dimensions,
            ));

        #[allow(clippy::cast_possible_truncation)]
        let mem = SqliteMemory::with_embedder(
            workspace_dir,
            embedder,
            config.vector_weight as f32,
            config.keyword_weight as f32,
            config.embedding_cache_size,
            config.sqlite_open_timeout_secs,
            config.search_mode.clone(),
        )?;
        Ok(mem)
    }

    create_memory_with_builders(
        &backend_name,
        workspace_dir,
        || build_sqlite_memory(config, workspace_dir, &resolved_embedding),
        "",
    )
}

pub fn create_memory_for_migration(
    backend: &str,
    workspace_dir: &Path,
) -> anyhow::Result<Box<dyn Memory>> {
    if matches!(classify_memory_backend(backend), MemoryBackendKind::None) {
        anyhow::bail!(
            "memory backend 'none' disables persistence; choose sqlite or obsidian before migration"
        );
    }

    create_memory_with_builders(
        backend,
        workspace_dir,
        || SqliteMemory::new(workspace_dir),
        " during migration",
    )
}

/// Factory: create an optional response cache from config.
pub fn create_response_cache(config: &MemoryConfig, workspace_dir: &Path) -> Option<ResponseCache> {
    if !config.response_cache_enabled {
        return None;
    }

    match ResponseCache::new(
        workspace_dir,
        config.response_cache_ttl_minutes,
        config.response_cache_max_entries,
    ) {
        Ok(cache) => {
            tracing::info!(
                "💾 Response cache enabled (TTL: {}min, max: {} entries)",
                config.response_cache_ttl_minutes,
                config.response_cache_max_entries
            );
            Some(cache)
        }
        Err(e) => {
            tracing::warn!("Response cache disabled due to error: {e}");
            None
        }
    }
}

#[cfg(test)]
#[path = "tests.rs"]
mod tests;
