//! Tool factory — assembles the `Vec<Box<dyn Tool>>` every Agent runs with.
//!
//! 1.0 strip-down: channel/mcp/obsidian/delegate/sessions/heartbeat tools
//! removed. Remaining categories:
//! - Filesystem: `file_read`, `file_write`, `file_edit`, `glob_search`,
//!   `content_search`.
//! - Memory: `memory_store`, `memory_recall`, `memory_export`,
//!   `memory_forget`, `memory_purge`.
//! - Web: `http_request`, `web_fetch`, `web_search_tool`.
//! - Agent control: `canvas`, `ask_user`, `escalate`, `tool_search`.
//! - Shell: the gated shell executor.
//! - Skills: `skill_exec` (GraphQL-backed single-dispatch tool).

pub mod ask_user;
pub mod canvas;
pub mod content_search;
pub mod escalate;
pub mod file_edit;
pub mod file_read;
pub mod file_write;
pub mod glob_search;
pub mod http_request;
pub mod memory_export;
pub mod memory_forget;
pub mod memory_purge;
pub mod memory_recall;
pub mod memory_store;
pub mod schema;
pub mod shell;
pub mod skill_exec;
pub mod tool_search;
pub mod traits;
pub mod web_fetch;
mod web_search_provider_routing;
pub mod web_search_tool;
pub mod wrappers;

pub use ask_user::AskUserTool;
pub use canvas::{CanvasStore, CanvasTool};
pub use content_search::ContentSearchTool;
pub use escalate::EscalateToHumanTool;
pub use file_edit::FileEditTool;
pub use file_read::FileReadTool;
pub use file_write::FileWriteTool;
pub use glob_search::GlobSearchTool;
pub use http_request::HttpRequestTool;
pub use memory_export::MemoryExportTool;
pub use memory_forget::MemoryForgetTool;
pub use memory_purge::MemoryPurgeTool;
pub use memory_recall::MemoryRecallTool;
pub use memory_store::MemoryStoreTool;
#[allow(unused_imports)]
pub use schema::{CleaningStrategy, SchemaCleanr};
pub use shell::ShellTool;
pub use tool_search::ToolSearchTool;
pub use traits::Tool;
#[allow(unused_imports)]
pub use traits::{ToolResult, ToolSpec};
pub use web_fetch::WebFetchTool;
pub use web_search_tool::WebSearchTool;
pub use wrappers::{PathGuardedTool, RateLimitedTool};

use crate::config::Config;
use crate::memory::Memory;
use crate::runtime::{NativeRuntime, RuntimeAdapter};
use crate::security::{SecurityPolicy, create_sandbox};
use async_trait::async_trait;
use parking_lot::RwLock;
use std::collections::HashMap;
use std::sync::Arc;

/// Shared channel-map handle type (kept as a stub for API compat with
/// the gateway/daemon wiring that still expects these handles). After the
/// channel subsystem deletion, these maps stay empty.
pub type ChannelMapHandle = Arc<RwLock<HashMap<String, ()>>>;

/// Shared handle used by the (now-removed) delegate tool. Kept as a type
/// alias so callers still compile while we phase the rest out.
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

/// Create full tool registry. 1.0 slim signature — drops composio, browser,
/// delegate-agents, channel session store.
#[allow(clippy::too_many_arguments, clippy::type_complexity)]
pub fn all_tools(
    config: Arc<Config>,
    security: &Arc<SecurityPolicy>,
    memory: Arc<dyn Memory>,
    http_config: &crate::config::HttpRequestConfig,
    web_fetch_config: &crate::config::WebFetchConfig,
    workspace_dir: &std::path::Path,
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
        http_config,
        web_fetch_config,
        workspace_dir,
        root_config,
        canvas_store,
    )
}

#[allow(clippy::too_many_arguments, clippy::type_complexity)]
pub fn all_tools_with_runtime(
    config: Arc<Config>,
    security: &Arc<SecurityPolicy>,
    runtime: Arc<dyn RuntimeAdapter>,
    memory: Arc<dyn Memory>,
    http_config: &crate::config::HttpRequestConfig,
    web_fetch_config: &crate::config::WebFetchConfig,
    workspace_dir: &std::path::Path,
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
    ];

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

    let channel_map_handle: ChannelMapHandle = Arc::new(RwLock::new(HashMap::new()));

    let ask_user_tool = AskUserTool::new(security.clone());
    let ask_user_handle = ask_user_tool.channel_map_handle();
    tool_arcs.push(Arc::new(ask_user_tool));

    let escalate_tool = EscalateToHumanTool::new(security.clone(), workspace_dir.to_path_buf());
    let escalate_handle = escalate_tool.channel_map_handle();
    tool_arcs.push(Arc::new(escalate_tool));

    let _ = config;

    (
        boxed_registry_from_arcs(tool_arcs),
        None,
        None,
        channel_map_handle,
        Some(ask_user_handle),
        Some(escalate_handle),
    )
}

#[cfg(test)]
#[path = "tests.rs"]
mod tests;
