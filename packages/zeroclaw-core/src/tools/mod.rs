//! Tool factory — assembles the `Vec<Box<dyn Tool>>` every Agent runs with.
//!
//! This module is the central registration point for the ~30 core tools that
//! ship post-Phase-2.6. The main entry point is
//! [`all_tools_with_runtime`], which takes a `&Config` plus a runtime handle
//! and returns the boxed tool list passed into [`Agent`](crate::agent::Agent)
//! at construction time. Each tool is built from config so feature flags,
//! security policy, and runtime adapters are wired in once at boot.
//!
//! ## Categories
//!
//! - **Filesystem:** `file_read`, `file_write`, `file_edit`, `file_append`,
//!   `glob_search`, `content_search`.
//! - **Memory:** `memory_store`, `memory_recall`, `memory_export`,
//!   `memory_forget`, `memory_purge`.
//! - **Web:** `http_request`, `web_fetch`, `web_search_tool`.
//! - **MCP & skills:** `mcp_*`, `skill_*`, `read_skill`.
//! - **Agent control:** `canvas`, `sessions_*`, `poll`, `reaction`,
//!   `ask_user`, `escalate`, `delegate`, `tool_search`.
//! - **Shell:** the gated shell executor.
//!
//! MCP tools are **discovered dynamically** at boot — when an MCP server is
//! registered in config, its advertised tools are added to the returned
//! registry as additional `Box<dyn Tool>` entries. This happens once during
//! factory construction; the agent itself sees no difference between native
//! and MCP-sourced tools.
//!
//! ## Key types
//! - [`Tool`] — the trait every tool implements (`name`, `description`,
//!   `parameters_schema`, async `execute`). Defined in [`traits`].
//! - [`ToolResult`](traits::ToolResult) — structured tool output.
//! - [`ToolSpec`] — the LLM-facing description sent to providers.
//!
//! ## Related
//! - `src/tools/traits.rs` — the [`Tool`] trait itself.
//! - `src/agent/tool_execution.rs` — runtime dispatch + policy enforcement.
//! - `src/agent/dispatcher.rs` — formats tool specs for the active provider.
//! - `docs/contributing/change-playbooks.md` §7.3 — adding a new tool.

pub mod ask_user;
pub mod backend_skill_tool;
pub mod canvas;
pub mod content_search;
pub mod delegate;
pub mod escalate;
pub mod file_edit;
pub mod file_read;
pub mod file_write;
pub mod glob_search;
pub mod heartbeat_add;
pub mod heartbeat_list;
pub mod heartbeat_remove;
pub mod heartbeat_update;
pub mod http_request;
pub mod mcp_client;
pub mod mcp_deferred;
pub mod mcp_protocol;
pub mod mcp_tool;
pub mod mcp_transport;
pub mod memory_export;
pub mod memory_forget;
pub mod memory_purge;
pub mod memory_recall;
pub mod memory_store;
pub mod obsidian_find_by_category;
pub mod obsidian_query_by_property;
pub mod poll;
pub mod reaction;
pub mod read_skill;
pub mod schema;
pub mod sessions;
pub mod shell;
pub mod skill_exec;
pub mod skill_http;
pub mod skill_tool;
pub mod tool_search;
pub mod traits;
pub mod web_fetch;
mod web_search_provider_routing;
pub mod web_search_tool;
pub mod wrappers;

pub use ask_user::AskUserTool;
pub use canvas::{CanvasStore, CanvasTool};
pub use content_search::ContentSearchTool;
pub use delegate::DelegateTool;
#[allow(unused_imports)]
pub use delegate::{BackgroundDelegateResult, BackgroundTaskStatus};
pub use escalate::EscalateToHumanTool;
pub use file_edit::FileEditTool;
pub use file_read::FileReadTool;
pub use file_write::FileWriteTool;
pub use glob_search::GlobSearchTool;
pub use heartbeat_add::HeartbeatAddTool;
pub use heartbeat_list::HeartbeatListTool;
pub use heartbeat_remove::HeartbeatRemoveTool;
pub use heartbeat_update::HeartbeatUpdateTool;
pub use http_request::HttpRequestTool;
pub use mcp_client::McpRegistry;
pub use mcp_deferred::{ActivatedToolSet, DeferredMcpToolSet};
pub use mcp_tool::McpToolWrapper;
pub use memory_export::MemoryExportTool;
pub use memory_forget::MemoryForgetTool;
pub use memory_purge::MemoryPurgeTool;
pub use memory_recall::MemoryRecallTool;
pub use memory_store::MemoryStoreTool;
pub use obsidian_find_by_category::ObsidianFindByCategoryTool;
pub use obsidian_query_by_property::ObsidianQueryByPropertyTool;
pub use poll::{ChannelMapHandle, PollTool};
pub use reaction::ReactionTool;
pub use read_skill::ReadSkillTool;
#[allow(unused_imports)]
pub use schema::{CleaningStrategy, SchemaCleanr};
pub use sessions::{SessionsHistoryTool, SessionsListTool, SessionsSendTool};
pub use shell::ShellTool;
#[allow(unused_imports)]
pub use skill_http::SkillHttpTool;
#[allow(unused_imports)]
pub use skill_tool::SkillShellTool;
pub use tool_search::ToolSearchTool;
pub use traits::Tool;
#[allow(unused_imports)]
pub use traits::{ToolResult, ToolSpec};
pub use web_fetch::WebFetchTool;
pub use web_search_tool::WebSearchTool;
pub use wrappers::{PathGuardedTool, RateLimitedTool};

use crate::config::{Config, DelegateAgentConfig};
use crate::memory::Memory;
use crate::runtime::{NativeRuntime, RuntimeAdapter};
use crate::security::{SecurityPolicy, create_sandbox};
use async_trait::async_trait;
use parking_lot::RwLock;
use std::collections::HashMap;
use std::sync::Arc;

/// Shared handle to the delegate tool's parent-tools list.
/// Callers can push additional tools (e.g. MCP wrappers) after construction.
pub type DelegateParentToolsHandle = Arc<RwLock<Vec<Arc<dyn Tool>>>>;

/// Thin wrapper that makes an `Arc<dyn Tool>` usable as `Box<dyn Tool>`.
pub struct ArcToolRef(pub Arc<dyn Tool>);

#[async_trait]
impl Tool for ArcToolRef {
    fn name(&self) -> &str {
        self.0.name()
    }

    fn description(&self) -> &str {
        self.0.description()
    }

    fn parameters_schema(&self) -> serde_json::Value {
        self.0.parameters_schema()
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        self.0.execute(args).await
    }
}

#[derive(Clone)]
struct ArcDelegatingTool {
    inner: Arc<dyn Tool>,
}

impl ArcDelegatingTool {
    fn boxed(inner: Arc<dyn Tool>) -> Box<dyn Tool> {
        Box::new(Self { inner })
    }
}

#[async_trait]
impl Tool for ArcDelegatingTool {
    fn name(&self) -> &str {
        self.inner.name()
    }

    fn description(&self) -> &str {
        self.inner.description()
    }

    fn parameters_schema(&self) -> serde_json::Value {
        self.inner.parameters_schema()
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        self.inner.execute(args).await
    }
}

fn boxed_registry_from_arcs(tools: Vec<Arc<dyn Tool>>) -> Vec<Box<dyn Tool>> {
    tools.into_iter().map(ArcDelegatingTool::boxed).collect()
}

/// Create the default tool registry
pub fn default_tools(security: Arc<SecurityPolicy>) -> Vec<Box<dyn Tool>> {
    default_tools_with_runtime(security, Arc::new(NativeRuntime::new()))
}

/// Create the default tool registry with explicit runtime adapter.
pub fn default_tools_with_runtime(
    security: Arc<SecurityPolicy>,
    runtime: Arc<dyn RuntimeAdapter>,
) -> Vec<Box<dyn Tool>> {
    vec![
        Box::new(RateLimitedTool::new(
            PathGuardedTool::new(ShellTool::new(security.clone(), runtime), security.clone()),
            security.clone(),
        )),
        Box::new(FileReadTool::new(security.clone())),
        Box::new(FileWriteTool::new(security.clone())),
        Box::new(FileEditTool::new(security.clone())),
        Box::new(GlobSearchTool::new(security.clone())),
        Box::new(ContentSearchTool::new(security)),
    ]
}

/// Register skill-defined tools into an existing tool registry.
///
/// Converts each skill's `[[tools]]` entries into callable `Tool` implementations
/// and appends them to the registry. Skill tools that would shadow a built-in tool
/// name are skipped with a warning.
pub fn register_skill_tools(
    tools_registry: &mut Vec<Box<dyn Tool>>,
    skills: &[crate::skills::Skill],
    security: Arc<SecurityPolicy>,
) {
    let skill_tools = crate::skills::skills_to_tools(skills, security);
    let existing_names: std::collections::HashSet<String> = tools_registry
        .iter()
        .map(|t| t.name().to_string())
        .collect();
    for tool in skill_tools {
        if existing_names.contains(tool.name()) {
            tracing::warn!(
                "Skill tool '{}' shadows built-in tool, skipping",
                tool.name()
            );
        } else {
            tools_registry.push(tool);
        }
    }
}

/// Create full tool registry including memory tools and optional Composio
#[allow(
    clippy::implicit_hasher,
    clippy::too_many_arguments,
    clippy::type_complexity
)]
pub fn all_tools(
    config: Arc<Config>,
    security: &Arc<SecurityPolicy>,
    memory: Arc<dyn Memory>,
    composio_key: Option<&str>,
    composio_entity_id: Option<&str>,
    browser_config: &crate::config::BrowserConfig,
    http_config: &crate::config::HttpRequestConfig,
    web_fetch_config: &crate::config::WebFetchConfig,
    workspace_dir: &std::path::Path,
    agents: &HashMap<String, DelegateAgentConfig>,
    fallback_api_key: Option<&str>,
    root_config: &crate::config::Config,
    canvas_store: Option<CanvasStore>,
) -> (
    Vec<Box<dyn Tool>>,
    Option<DelegateParentToolsHandle>,
    Option<ChannelMapHandle>,
    ChannelMapHandle,
    Option<ChannelMapHandle>,
    Option<ChannelMapHandle>,
) {
    all_tools_with_runtime(
        config,
        security,
        Arc::new(NativeRuntime::new()),
        memory,
        composio_key,
        composio_entity_id,
        browser_config,
        http_config,
        web_fetch_config,
        workspace_dir,
        agents,
        fallback_api_key,
        root_config,
        canvas_store,
    )
}

/// Create full tool registry including memory tools and optional Composio.
#[allow(
    clippy::implicit_hasher,
    clippy::too_many_arguments,
    clippy::type_complexity
)]
pub fn all_tools_with_runtime(
    config: Arc<Config>,
    security: &Arc<SecurityPolicy>,
    runtime: Arc<dyn RuntimeAdapter>,
    memory: Arc<dyn Memory>,
    composio_key: Option<&str>,
    composio_entity_id: Option<&str>,
    browser_config: &crate::config::BrowserConfig,
    http_config: &crate::config::HttpRequestConfig,
    web_fetch_config: &crate::config::WebFetchConfig,
    workspace_dir: &std::path::Path,
    agents: &HashMap<String, DelegateAgentConfig>,
    fallback_api_key: Option<&str>,
    root_config: &crate::config::Config,
    canvas_store: Option<CanvasStore>,
) -> (
    Vec<Box<dyn Tool>>,
    Option<DelegateParentToolsHandle>,
    Option<ChannelMapHandle>,
    ChannelMapHandle,
    Option<ChannelMapHandle>,
    Option<ChannelMapHandle>,
) {
    let _ = (
        composio_key,
        composio_entity_id,
        browser_config,
        fallback_api_key,
    );
    let sandbox = create_sandbox(&root_config.security);
    let mut tool_arcs: Vec<Arc<dyn Tool>> = vec![
        Arc::new(RateLimitedTool::new(
            PathGuardedTool::new(
                ShellTool::new_with_sandbox(security.clone(), runtime, sandbox)
                    .with_timeout_secs(root_config.shell_tool.timeout_secs),
                security.clone(),
            ),
            security.clone(),
        )),
        Arc::new(FileReadTool::new(security.clone())),
        Arc::new(FileWriteTool::new(security.clone())),
        Arc::new(FileEditTool::new(security.clone())),
        Arc::new(GlobSearchTool::new(security.clone())),
        Arc::new(ContentSearchTool::new(security.clone())),
        Arc::new(MemoryStoreTool::new(memory.clone(), security.clone())),
        Arc::new(MemoryRecallTool::new(memory.clone())),
        Arc::new(MemoryForgetTool::new(memory.clone(), security.clone())),
        Arc::new(MemoryExportTool::new(memory.clone())),
        Arc::new(MemoryPurgeTool::new(memory.clone(), security.clone())),
        Arc::new(CanvasTool::new(canvas_store.unwrap_or_default())),
        Arc::new(HeartbeatListTool::new(workspace_dir.to_path_buf())),
        Arc::new(HeartbeatAddTool::new(workspace_dir.to_path_buf())),
        Arc::new(HeartbeatUpdateTool::new(workspace_dir.to_path_buf())),
        Arc::new(HeartbeatRemoveTool::new(workspace_dir.to_path_buf())),
    ];

    if let Some(runtime_url) = root_config
        .skills
        .skill_runtime_url
        .as_ref()
        .map(|s| s.trim().to_string())
        .filter(|s| !s.is_empty())
    {
        tool_arcs.push(Arc::new(ObsidianFindByCategoryTool::new(
            runtime_url.clone(),
        )));
        tool_arcs.push(Arc::new(ObsidianQueryByPropertyTool::new(runtime_url)));
        tracing::info!("obsidian native tools registered (skill-runtime)");
    }

    tool_arcs.push(Arc::new(ReadSkillTool::new(
        workspace_dir.to_path_buf(),
        root_config.skills.open_skills_enabled,
        root_config.skills.open_skills_dir.clone(),
    )));

    if http_config.enabled {
        tool_arcs.push(Arc::new(HttpRequestTool::new(
            security.clone(),
            http_config.allowed_domains.clone(),
            http_config.max_response_size,
            http_config.timeout_secs,
            http_config.allow_private_hosts,
        )));
    }

    if web_fetch_config.enabled {
        tool_arcs.push(Arc::new(WebFetchTool::new(
            security.clone(),
            web_fetch_config.allowed_domains.clone(),
            web_fetch_config.blocked_domains.clone(),
            web_fetch_config.max_response_size,
            web_fetch_config.timeout_secs,
            web_fetch_config.firecrawl.clone(),
            web_fetch_config.allowed_private_hosts.clone(),
        )));
    }

    if root_config.web_search.enabled {
        tool_arcs.push(Arc::new(WebSearchTool::new_with_config(
            root_config.web_search.provider.clone(),
            root_config.web_search.brave_api_key.clone(),
            root_config.web_search.searxng_instance_url.clone(),
            root_config.web_search.max_results,
            root_config.web_search.timeout_secs,
            root_config.config_path.clone(),
            root_config.secrets.encrypt,
        )));
    }

    if let Ok(session_store) = crate::channels::session_store::SessionStore::new(workspace_dir) {
        let backend: Arc<dyn crate::channels::session_backend::SessionBackend> =
            Arc::new(session_store);
        tool_arcs.push(Arc::new(SessionsListTool::new(backend.clone())));
        tool_arcs.push(Arc::new(SessionsHistoryTool::new(
            backend.clone(),
            security.clone(),
        )));
        tool_arcs.push(Arc::new(SessionsSendTool::new(backend, security.clone())));
    }

    let channel_map_handle: ChannelMapHandle = Arc::new(RwLock::new(HashMap::new()));
    tool_arcs.push(Arc::new(PollTool::new(
        security.clone(),
        Arc::clone(&channel_map_handle),
    )));

    let reaction_tool = ReactionTool::new(security.clone());
    let reaction_handle = reaction_tool.channel_map_handle();
    tool_arcs.push(Arc::new(reaction_tool));

    let ask_user_tool = AskUserTool::new(security.clone());
    let ask_user_handle = ask_user_tool.channel_map_handle();
    tool_arcs.push(Arc::new(ask_user_tool));

    let escalate_tool = EscalateToHumanTool::new(security.clone(), workspace_dir.to_path_buf());
    let escalate_handle = escalate_tool.channel_map_handle();
    tool_arcs.push(Arc::new(escalate_tool));

    let delegate_handle: Option<DelegateParentToolsHandle> = if agents.is_empty() {
        None
    } else {
        let delegate_agents: HashMap<String, DelegateAgentConfig> = agents
            .iter()
            .map(|(name, cfg)| (name.clone(), cfg.clone()))
            .collect();
        let parent_tools = Arc::new(RwLock::new(tool_arcs.clone()));
        let provider_runtime_options = crate::providers::ProviderRuntimeOptions {
            auth_profile_override: None,
            provider_api_url: root_config.api_url.clone(),
            zeroclaw_dir: root_config
                .config_path
                .parent()
                .map(std::path::PathBuf::from),
            secrets_encrypt: root_config.secrets.encrypt,
            reasoning_enabled: root_config.runtime.reasoning_enabled,
            reasoning_effort: root_config.runtime.reasoning_effort.clone(),
            provider_timeout_secs: Some(root_config.provider_timeout_secs),
            provider_max_tokens: root_config.provider_max_tokens,
            extra_headers: root_config.extra_headers.clone(),
            api_path: root_config.api_path.clone(),
        };
        let delegate_tool = DelegateTool::new_with_options(
            delegate_agents,
            None,
            security.clone(),
            provider_runtime_options,
        )
        .with_parent_tools(Arc::clone(&parent_tools))
        .with_multimodal_config(root_config.multimodal.clone())
        .with_delegate_config(root_config.delegate.clone())
        .with_workspace_dir(workspace_dir.to_path_buf())
        .with_memory(memory.clone());
        tool_arcs.push(Arc::new(delegate_tool));
        Some(parent_tools)
    };

    let _ = config;

    (
        boxed_registry_from_arcs(tool_arcs),
        delegate_handle,
        Some(reaction_handle),
        channel_map_handle,
        Some(ask_user_handle),
        Some(escalate_handle),
    )
}

#[cfg(test)]
#[path = "tests.rs"]
mod tests;
