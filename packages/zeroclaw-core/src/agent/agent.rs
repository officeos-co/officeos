//! The `Agent` struct and its builder — the heart of the runtime.
//!
//! This file (~1300 lines) defines [`Agent`], the central object every
//! conversation turn runs against. An `Agent` owns the live state of a running
//! agent: provider, memory, tools, dispatcher, observer, memory_loader,
//! conversation `history`, loaded skills, and the merged [`Config`]. Every
//! channel, gateway, and CLI entry point eventually funnels user input through
//! a single `Agent` instance.
//!
//! ## Lifecycle
//!
//! - **Boot:** [`Agent::from_config`] (line 360) is the production constructor
//!   used by `src/daemon/mod.rs::run`. It calls
//!   `personality::load_personality_strict` as the Phase 3 boot gate — if
//!   required personality files are missing, the process fails fast here
//!   rather than degrading silently at turn time.
//! - **Per turn:** [`Agent::turn`] (line 738) runs one full LLM loop, looping
//!   up to `config.max_tool_iterations` times through the inner tool-call
//!   pass. [`Agent::turn_streamed`] is the streaming variant that emits
//!   [`TurnEvent`]s over an mpsc channel.
//! - **Per tool call:** `execute_tools(calls)` delegates to
//!   `src/agent/tool_execution.rs`, which enforces security policy and
//!   dispatches to the matching [`Tool`].
//!
//! When `temperature == 0.0`, the response cache is consulted before each
//! provider call so identical prompts short-circuit the network round-trip.
//! The `AgentBuilder` pattern lets tests assemble agents with mock providers,
//! tools, and memory without going through `from_config`.
//!
//! ## Key types
//! - [`Agent`] — the running agent; owns provider, memory, tools, history.
//! - [`AgentBuilder`] — builder used by tests to inject mocks.
//! - [`TurnEvent`] — streamed events from `turn_streamed`.
//!
//! ## Related
//! - `src/agent/dispatcher.rs` — provider message format translation.
//! - `src/agent/prompt.rs` — system prompt assembly (rebuilt every turn).
//! - `src/agent/tool_execution.rs` — tool dispatch + policy enforcement.
//! - `src/daemon/mod.rs` — the production boot path that calls `from_config`.
//! - `docs/architecture/overview.md` — the big picture.

use crate::agent::personality;
use crate::agent::dispatcher::{
    NativeToolDispatcher, ParsedToolCall, ToolDispatcher, ToolExecutionResult, XmlToolDispatcher,
};
use crate::agent::memory_loader::{DefaultMemoryLoader, MemoryLoader};
use crate::agent::prompt::{PromptContext, SystemPromptBuilder};
use crate::config::Config;
use crate::i18n::ToolDescriptions;
use crate::memory::{self, Memory, MemoryCategory};
use crate::observability::{self, Observer, ObserverEvent};
use crate::providers::{self, ChatMessage, ChatRequest, ConversationMessage, Provider};
use crate::runtime;
use crate::security::SecurityPolicy;
use crate::tools::{self, Tool, ToolSpec};
use anyhow::Result;
use chrono::{Datelike, Timelike};
use std::collections::HashMap;
use std::io::Write as IoWrite;
use std::sync::Arc;
use std::time::Instant;

/// Events emitted during a streamed agent turn.
///
/// Consumers receive these through a `tokio::sync::mpsc::Sender<TurnEvent>`
/// passed to [`Agent::turn_streamed`].
#[derive(Debug, Clone)]
pub enum TurnEvent {
    /// A text chunk from the LLM response (may arrive many times).
    Chunk { delta: String },
    /// A reasoning/thinking chunk from a thinking model (may arrive many times).
    Thinking { delta: String },
    /// The agent is invoking a tool.
    ToolCall {
        name: String,
        args: serde_json::Value,
    },
    /// A tool has returned a result.
    ToolResult { name: String, output: String },
}

pub struct Agent {
    provider: Box<dyn Provider>,
    tools: Vec<Box<dyn Tool>>,
    tool_specs: Vec<ToolSpec>,
    memory: Arc<dyn Memory>,
    observer: Arc<dyn Observer>,
    prompt_builder: SystemPromptBuilder,
    tool_dispatcher: Box<dyn ToolDispatcher>,
    memory_loader: Box<dyn MemoryLoader>,
    config: crate::config::AgentConfig,
    model_name: String,
    temperature: f64,
    workspace_dir: std::path::PathBuf,
    skills: Vec<crate::skills::Skill>,
    skills_prompt_mode: crate::config::SkillsPromptInjectionMode,
    auto_save: bool,
    memory_session_id: Option<String>,
    history: Vec<ConversationMessage>,
    classification_config: crate::config::QueryClassificationConfig,
    available_hints: Vec<String>,
    route_model_by_hint: HashMap<String, String>,
    allowed_tools: Option<Vec<String>>,
    response_cache: Option<Arc<crate::memory::response_cache::ResponseCache>>,
    tool_descriptions: Option<ToolDescriptions>,
    /// Pre-rendered security policy summary injected into the system prompt
    /// so the LLM knows the concrete constraints before making tool calls.
    security_summary: Option<String>,
    /// Autonomy level from config; controls safety prompt instructions.
    autonomy_level: crate::security::AutonomyLevel,
    /// Activated MCP tools for deferred loading mode.
    /// When MCP deferred loading is enabled, tools are activated via `tool_search`
    /// and stored here for lookup during tool execution.
    activated_tools: Option<Arc<std::sync::Mutex<crate::tools::ActivatedToolSet>>>,
    /// Backend-capability cache. When `config.skills.backend_url` is set
    /// the agent pulls its live tool list from
    /// `{backend_url}/api/capabilities` at the start of every turn
    /// and swaps `self.tools` / `self.tool_specs` on changes.
    capability_cache: Option<tokio::sync::Mutex<crate::skills::live::CapabilityCache>>,
}

pub struct AgentBuilder {
    provider: Option<Box<dyn Provider>>,
    tools: Option<Vec<Box<dyn Tool>>>,
    memory: Option<Arc<dyn Memory>>,
    observer: Option<Arc<dyn Observer>>,
    prompt_builder: Option<SystemPromptBuilder>,
    tool_dispatcher: Option<Box<dyn ToolDispatcher>>,
    memory_loader: Option<Box<dyn MemoryLoader>>,
    config: Option<crate::config::AgentConfig>,
    model_name: Option<String>,
    temperature: Option<f64>,
    workspace_dir: Option<std::path::PathBuf>,
    skills: Option<Vec<crate::skills::Skill>>,
    skills_prompt_mode: Option<crate::config::SkillsPromptInjectionMode>,
    auto_save: Option<bool>,
    memory_session_id: Option<String>,
    classification_config: Option<crate::config::QueryClassificationConfig>,
    available_hints: Option<Vec<String>>,
    route_model_by_hint: Option<HashMap<String, String>>,
    allowed_tools: Option<Vec<String>>,
    response_cache: Option<Arc<crate::memory::response_cache::ResponseCache>>,
    tool_descriptions: Option<ToolDescriptions>,
    security_summary: Option<String>,
    autonomy_level: Option<crate::security::AutonomyLevel>,
    activated_tools: Option<Arc<std::sync::Mutex<crate::tools::ActivatedToolSet>>>,
    capability_cache: Option<crate::skills::live::CapabilityCache>,
}

impl AgentBuilder {
    pub fn new() -> Self {
        Self {
            provider: None,
            tools: None,
            memory: None,
            observer: None,
            prompt_builder: None,
            tool_dispatcher: None,
            memory_loader: None,
            config: None,
            model_name: None,
            temperature: None,
            workspace_dir: None,
            skills: None,
            skills_prompt_mode: None,
            auto_save: None,
            memory_session_id: None,
            classification_config: None,
            available_hints: None,
            route_model_by_hint: None,
            allowed_tools: None,
            response_cache: None,
            tool_descriptions: None,
            security_summary: None,
            autonomy_level: None,
            activated_tools: None,
            capability_cache: None,
        }
    }

    pub fn capability_cache(
        mut self,
        cache: crate::skills::live::CapabilityCache,
    ) -> Self {
        self.capability_cache = Some(cache);
        self
    }

    pub fn provider(mut self, provider: Box<dyn Provider>) -> Self {
        self.provider = Some(provider);
        self
    }

    pub fn tools(mut self, tools: Vec<Box<dyn Tool>>) -> Self {
        self.tools = Some(tools);
        self
    }

    pub fn memory(mut self, memory: Arc<dyn Memory>) -> Self {
        self.memory = Some(memory);
        self
    }

    pub fn observer(mut self, observer: Arc<dyn Observer>) -> Self {
        self.observer = Some(observer);
        self
    }

    pub fn prompt_builder(mut self, prompt_builder: SystemPromptBuilder) -> Self {
        self.prompt_builder = Some(prompt_builder);
        self
    }

    pub fn tool_dispatcher(mut self, tool_dispatcher: Box<dyn ToolDispatcher>) -> Self {
        self.tool_dispatcher = Some(tool_dispatcher);
        self
    }

    pub fn memory_loader(mut self, memory_loader: Box<dyn MemoryLoader>) -> Self {
        self.memory_loader = Some(memory_loader);
        self
    }

    pub fn config(mut self, config: crate::config::AgentConfig) -> Self {
        self.config = Some(config);
        self
    }

    pub fn model_name(mut self, model_name: String) -> Self {
        self.model_name = Some(model_name);
        self
    }

    pub fn temperature(mut self, temperature: f64) -> Self {
        self.temperature = Some(temperature);
        self
    }

    pub fn workspace_dir(mut self, workspace_dir: std::path::PathBuf) -> Self {
        self.workspace_dir = Some(workspace_dir);
        self
    }

    pub fn skills(mut self, skills: Vec<crate::skills::Skill>) -> Self {
        self.skills = Some(skills);
        self
    }

    pub fn skills_prompt_mode(
        mut self,
        skills_prompt_mode: crate::config::SkillsPromptInjectionMode,
    ) -> Self {
        self.skills_prompt_mode = Some(skills_prompt_mode);
        self
    }

    pub fn auto_save(mut self, auto_save: bool) -> Self {
        self.auto_save = Some(auto_save);
        self
    }

    pub fn memory_session_id(mut self, memory_session_id: Option<String>) -> Self {
        self.memory_session_id = memory_session_id;
        self
    }

    pub fn classification_config(
        mut self,
        classification_config: crate::config::QueryClassificationConfig,
    ) -> Self {
        self.classification_config = Some(classification_config);
        self
    }

    pub fn available_hints(mut self, available_hints: Vec<String>) -> Self {
        self.available_hints = Some(available_hints);
        self
    }

    pub fn route_model_by_hint(mut self, route_model_by_hint: HashMap<String, String>) -> Self {
        self.route_model_by_hint = Some(route_model_by_hint);
        self
    }

    pub fn allowed_tools(mut self, allowed_tools: Option<Vec<String>>) -> Self {
        self.allowed_tools = allowed_tools;
        self
    }

    pub fn response_cache(
        mut self,
        cache: Option<Arc<crate::memory::response_cache::ResponseCache>>,
    ) -> Self {
        self.response_cache = cache;
        self
    }

    pub fn tool_descriptions(mut self, tool_descriptions: Option<ToolDescriptions>) -> Self {
        self.tool_descriptions = tool_descriptions;
        self
    }

    pub fn security_summary(mut self, summary: Option<String>) -> Self {
        self.security_summary = summary;
        self
    }

    pub fn autonomy_level(mut self, level: crate::security::AutonomyLevel) -> Self {
        self.autonomy_level = Some(level);
        self
    }

    pub fn activated_tools(
        mut self,
        activated: Option<Arc<std::sync::Mutex<tools::ActivatedToolSet>>>,
    ) -> Self {
        self.activated_tools = activated;
        self
    }

    pub fn build(self) -> Result<Agent> {
        let mut tools = self
            .tools
            .ok_or_else(|| anyhow::anyhow!("tools are required"))?;
        let allowed = self.allowed_tools.clone();
        if let Some(ref allow_list) = allowed {
            tools.retain(|t| allow_list.iter().any(|name| name == t.name()));
        }
        let tool_specs = tools.iter().map(|tool| tool.spec()).collect();

        // The capability cache, if any, is constructed in `Agent::from_config`
        // where the full `Config` (and thus `config.skills`) is available.
        let capability_cache = self.capability_cache.map(tokio::sync::Mutex::new);

        Ok(Agent {
            provider: self
                .provider
                .ok_or_else(|| anyhow::anyhow!("provider is required"))?,
            tools,
            tool_specs,
            memory: self
                .memory
                .ok_or_else(|| anyhow::anyhow!("memory is required"))?,
            observer: self
                .observer
                .ok_or_else(|| anyhow::anyhow!("observer is required"))?,
            prompt_builder: self
                .prompt_builder
                .unwrap_or_else(SystemPromptBuilder::with_defaults),
            tool_dispatcher: self
                .tool_dispatcher
                .ok_or_else(|| anyhow::anyhow!("tool_dispatcher is required"))?,
            memory_loader: self
                .memory_loader
                .unwrap_or_else(|| Box::new(DefaultMemoryLoader::default())),
            config: self.config.unwrap_or_default(),
            model_name: self
                .model_name
                .unwrap_or_else(|| "anthropic/claude-sonnet-4-20250514".into()),
            temperature: self.temperature.unwrap_or(0.7),
            workspace_dir: self
                .workspace_dir
                .unwrap_or_else(|| std::path::PathBuf::from(".")),
            skills: self.skills.unwrap_or_default(),
            skills_prompt_mode: self.skills_prompt_mode.unwrap_or_default(),
            auto_save: self.auto_save.unwrap_or(false),
            memory_session_id: self.memory_session_id,
            history: Vec::new(),
            classification_config: self.classification_config.unwrap_or_default(),
            available_hints: self.available_hints.unwrap_or_default(),
            route_model_by_hint: self.route_model_by_hint.unwrap_or_default(),
            allowed_tools: allowed,
            response_cache: self.response_cache,
            tool_descriptions: self.tool_descriptions,
            security_summary: self.security_summary,
            autonomy_level: self
                .autonomy_level
                .unwrap_or(crate::security::AutonomyLevel::Supervised),
            activated_tools: self.activated_tools,
            capability_cache,
        })
    }
}

impl Agent {
    pub fn builder() -> AgentBuilder {
        AgentBuilder::new()
    }

    pub fn history(&self) -> &[ConversationMessage] {
        &self.history
    }

    pub fn clear_history(&mut self) {
        self.history.clear();
    }

    pub fn set_memory_session_id(&mut self, session_id: Option<String>) {
        self.memory_session_id = session_id;
    }

    /// Hydrate the agent with prior chat messages (e.g. from a session backend).
    ///
    /// Ensures a system prompt is prepended if history is empty, then appends all
    /// non-system messages from the seed. System messages in the seed are skipped
    /// to avoid duplicating the system prompt.
    pub fn seed_history(&mut self, messages: &[ChatMessage]) {
        if self.history.is_empty() {
            if let Ok(sys) = self.build_system_prompt() {
                self.history
                    .push(ConversationMessage::Chat(ChatMessage::system(sys)));
            }
        }
        for msg in messages {
            if msg.role != "system" {
                self.history.push(ConversationMessage::Chat(msg.clone()));
            }
        }
    }

    pub async fn from_config(config: &Config) -> Result<Self> {
        let observer: Arc<dyn Observer> =
            Arc::from(observability::create_observer(&config.observability));
        let runtime: Arc<dyn runtime::RuntimeAdapter> =
            Arc::from(runtime::create_runtime(&config.runtime)?);
        let security = Arc::new(SecurityPolicy::from_config(
            &config.autonomy,
            &config.workspace_dir,
        ));

        // Phase 3: the per-agent Obsidian vault is materialized into
        // workspace_dir by the dashboard backend (as a K8s ConfigMap
        // mounted at /vault-workspace) before this container starts.
        // Verify that the required personality files are present and
        // non-empty; if anything is missing, fail loudly so the pod
        // enters CrashLoopBackOff and the error surfaces via the
        // dashboard's existing K8sManager.get_status() chain.
        personality::load_personality_strict(&config.workspace_dir).map_err(|e| {
            anyhow::anyhow!(
                "Agent boot failed: required personality files missing or empty \
                 in workspace {:?}. The dashboard backend must seed the per-agent \
                 vault before the pod starts. Details: {e}",
                config.workspace_dir
            )
        })?;

        let memory: Arc<dyn Memory> = Arc::from(memory::create_memory_with_storage_and_routes(
            &config.memory,
            &config.embedding_routes,
            Some(&config.storage.provider.config),
            &config.workspace_dir,
            config.api_key.as_deref(),
        )?);

        let composio_key = if config.composio.enabled {
            config.composio.api_key.as_deref()
        } else {
            None
        };
        let composio_entity_id = if config.composio.enabled {
            Some(config.composio.entity_id.as_str())
        } else {
            None
        };

        let (
            mut tools,
            delegate_handle,
            _reaction_handle,
            _channel_map_handle,
            _ask_user_handle,
            _escalate_handle,
        ) = tools::all_tools_with_runtime(
            Arc::new(config.clone()),
            &security,
            runtime,
            memory.clone(),
            composio_key,
            composio_entity_id,
            &config.browser,
            &config.http_request,
            &config.web_fetch,
            &config.workspace_dir,
            &config.agents,
            config.api_key.as_deref(),
            config,
            None,
        );

        // ── Wire MCP tools (non-fatal) ─────────────────────────────
        // Replicates the same MCP initialization logic used in the CLI
        // and webhook paths (loop_.rs) so that the WebSocket/daemon UI
        // path also has access to MCP tools.
        let mut activated_tools: Option<Arc<std::sync::Mutex<tools::ActivatedToolSet>>> = None;
        if config.mcp.enabled && !config.mcp.servers.is_empty() {
            tracing::info!(
                "Initializing MCP client — {} server(s) configured",
                config.mcp.servers.len()
            );
            match tools::McpRegistry::connect_all(&config.mcp.servers).await {
                Ok(registry) => {
                    let registry = std::sync::Arc::new(registry);
                    if config.mcp.deferred_loading {
                        let deferred_set = tools::DeferredMcpToolSet::from_registry(
                            std::sync::Arc::clone(&registry),
                        )
                        .await;
                        tracing::info!(
                            "MCP deferred: {} tool stub(s) from {} server(s)",
                            deferred_set.len(),
                            registry.server_count()
                        );
                        let activated =
                            Arc::new(std::sync::Mutex::new(tools::ActivatedToolSet::new()));
                        activated_tools = Some(Arc::clone(&activated));
                        tools.push(Box::new(tools::ToolSearchTool::new(
                            deferred_set,
                            activated,
                        )));
                    } else {
                        let names = registry.tool_names();
                        let mut registered = 0usize;
                        for name in names {
                            if let Some(def) = registry.get_tool_def(&name).await {
                                let wrapper: std::sync::Arc<dyn tools::Tool> =
                                    std::sync::Arc::new(tools::McpToolWrapper::new(
                                        name,
                                        def,
                                        std::sync::Arc::clone(&registry),
                                    ));
                                if let Some(ref handle) = delegate_handle {
                                    handle.write().push(std::sync::Arc::clone(&wrapper));
                                }
                                tools.push(Box::new(tools::ArcToolRef(wrapper)));
                                registered += 1;
                            }
                        }
                        tracing::info!(
                            "MCP: {} tool(s) registered from {} server(s)",
                            registered,
                            registry.server_count()
                        );
                    }
                }
                Err(e) => {
                    tracing::error!("MCP registry failed to initialize: {e:#}");
                }
            }
        }

        let provider_name = config.default_provider.as_deref().unwrap_or("openrouter");

        let model_name = config
            .default_model
            .as_deref()
            .unwrap_or("anthropic/claude-sonnet-4-20250514")
            .to_string();

        let provider_runtime_options = providers::provider_runtime_options_from_config(config);

        let provider: Box<dyn Provider> = providers::create_routed_provider_with_options(
            provider_name,
            config.api_key.as_deref(),
            config.api_url.as_deref(),
            &config.reliability,
            &config.model_routes,
            &model_name,
            &provider_runtime_options,
        )?;

        let dispatcher_choice = config.agent.tool_dispatcher.as_str();
        let tool_dispatcher: Box<dyn ToolDispatcher> = match dispatcher_choice {
            "native" => Box::new(NativeToolDispatcher),
            "xml" => Box::new(XmlToolDispatcher),
            _ if provider.supports_native_tools() => Box::new(NativeToolDispatcher),
            _ => Box::new(XmlToolDispatcher),
        };

        let route_model_by_hint: HashMap<String, String> = config
            .model_routes
            .iter()
            .map(|route| (route.hint.clone(), route.model.clone()))
            .collect();
        let available_hints: Vec<String> = route_model_by_hint.keys().cloned().collect();

        let response_cache = if config.memory.response_cache_enabled {
            crate::memory::response_cache::ResponseCache::with_hot_cache(
                &config.workspace_dir,
                config.memory.response_cache_ttl_minutes,
                config.memory.response_cache_max_entries,
                config.memory.response_cache_hot_entries,
            )
            .ok()
            .map(Arc::new)
        } else {
            None
        };

        let builder = Agent::builder()
            .provider(provider)
            .tools(tools)
            .memory(memory)
            .observer(observer)
            .response_cache(response_cache)
            .tool_dispatcher(tool_dispatcher)
            .memory_loader(Box::new(DefaultMemoryLoader::new(
                5,
                config.memory.min_relevance_score,
            )))
            .prompt_builder(SystemPromptBuilder::with_defaults())
            .config(config.agent.clone())
            .model_name(model_name)
            .temperature(config.default_temperature)
            .workspace_dir(config.workspace_dir.clone())
            .classification_config(config.query_classification.clone())
            .available_hints(available_hints)
            .route_model_by_hint(route_model_by_hint)
            .skills(crate::skills::load_skills_with_config(
                &config.workspace_dir,
                config,
            ))
            .skills_prompt_mode(config.skills.prompt_injection_mode)
            .auto_save(config.memory.auto_save)
            .security_summary(Some(security.prompt_summary()))
            .autonomy_level(config.autonomy.level)
            .activated_tools(activated_tools);

        // If `[skills] backend_url` is set, build a live-refresh cache.
        // The cache is consulted at the start of every `turn()` and swaps
        // the agent's tool set when the backend reports a changed list.
        let builder = if let Some(backend_url) = config
            .skills
            .backend_url
            .as_ref()
            .map(|s| s.trim().to_string())
            .filter(|s| !s.is_empty())
        {
            builder.capability_cache(crate::skills::live::CapabilityCache::new(
                backend_url,
                config.skills.backend_token.clone(),
                config.skills.backend_refresh_seconds,
            ))
        } else {
            builder
        };

        builder.build()
    }

    /// Pull the live capability list from the backend and swap tool set
    /// + tool specs if anything changed. Fails open: any error is logged
    /// and the previously cached tools are reused, so a flaky backend
    /// cannot take a running agent offline.
    async fn refresh_backend_capabilities(&mut self) {
        let Some(cache) = self.capability_cache.as_ref() else {
            return;
        };
        let refresh_result = {
            let mut guard = cache.lock().await;
            guard.refresh().await
        };
        match refresh_result {
            Ok(Some(refreshed)) => {
                self.tools = refreshed.tools;
                self.tool_specs = refreshed.specs;
                // Drop the existing system prompt so the next turn
                // rebuilds it with the new tool list.
                if let Some(ConversationMessage::Chat(chat)) = self.history.first() {
                    if chat.role == "system" {
                        self.history.remove(0);
                    }
                }
                tracing::info!(
                    tool_count = self.tools.len(),
                    "backend capabilities refreshed"
                );
            }
            Ok(None) => {}
            Err(err) => {
                tracing::warn!(
                    error = %err,
                    "backend capability refresh failed; reusing cached tools"
                );
            }
        }
    }

    fn trim_history(&mut self) {
        let max = self.config.max_history_messages;
        if self.history.len() <= max {
            return;
        }

        let mut system_messages = Vec::new();
        let mut other_messages = Vec::new();

        for msg in self.history.drain(..) {
            match &msg {
                ConversationMessage::Chat(chat) if chat.role == "system" => {
                    system_messages.push(msg);
                }
                _ => other_messages.push(msg),
            }
        }

        if other_messages.len() > max {
            let drop_count = other_messages.len() - max;
            other_messages.drain(0..drop_count);
        }

        self.history = system_messages;
        self.history.extend(other_messages);
    }

    fn build_system_prompt(&self) -> Result<String> {
        let instructions = self.tool_dispatcher.prompt_instructions(&self.tools);
        let ctx = PromptContext {
            workspace_dir: &self.workspace_dir,
            model_name: &self.model_name,
            tools: &self.tools,
            skills: &self.skills,
            skills_prompt_mode: self.skills_prompt_mode,
            dispatcher_instructions: &instructions,
            tool_descriptions: self.tool_descriptions.as_ref(),
            security_summary: self.security_summary.clone(),
            autonomy_level: self.autonomy_level,
        };
        self.prompt_builder.build(&ctx)
    }

    async fn execute_tool_call(&self, call: &ParsedToolCall) -> ToolExecutionResult {
        let start = Instant::now();

        // First try to find tool in static registry, then in activated MCP tools.
        let result = if let Some(tool) = self.tools.iter().find(|t| t.name() == call.name) {
            match tool.execute(call.arguments.clone()).await {
                Ok(r) => {
                    self.observer.record_event(&ObserverEvent::ToolCall {
                        tool: call.name.clone(),
                        duration: start.elapsed(),
                        success: r.success,
                    });
                    if r.success {
                        r.output
                    } else {
                        format!("Error: {}", r.error.unwrap_or(r.output))
                    }
                }
                Err(e) => {
                    self.observer.record_event(&ObserverEvent::ToolCall {
                        tool: call.name.clone(),
                        duration: start.elapsed(),
                        success: false,
                    });
                    format!("Error executing {}: {e}", call.name)
                }
            }
        } else if let Some(activated_arc) = self.activated_tools.as_ref() {
            // Try to find in activated MCP tools.
            let activated_opt = activated_arc.lock().unwrap().get_resolved(&call.name);
            if let Some(tool) = activated_opt {
                match tool.execute(call.arguments.clone()).await {
                    Ok(r) => {
                        self.observer.record_event(&ObserverEvent::ToolCall {
                            tool: call.name.clone(),
                            duration: start.elapsed(),
                            success: r.success,
                        });
                        if r.success {
                            r.output
                        } else {
                            format!("Error: {}", r.error.unwrap_or(r.output))
                        }
                    }
                    Err(e) => {
                        self.observer.record_event(&ObserverEvent::ToolCall {
                            tool: call.name.clone(),
                            duration: start.elapsed(),
                            success: false,
                        });
                        format!("Error executing {}: {e}", call.name)
                    }
                }
            } else {
                format!("Unknown tool: {}", call.name)
            }
        } else {
            format!("Unknown tool: {}", call.name)
        };

        ToolExecutionResult {
            name: call.name.clone(),
            output: result,
            success: true,
            tool_call_id: call.tool_call_id.clone(),
        }
    }

    async fn execute_tools(&self, calls: &[ParsedToolCall]) -> Vec<ToolExecutionResult> {
        if !self.config.parallel_tools {
            let mut results = Vec::with_capacity(calls.len());
            for call in calls {
                results.push(self.execute_tool_call(call).await);
            }
            return results;
        }

        let futs: Vec<_> = calls
            .iter()
            .map(|call| self.execute_tool_call(call))
            .collect();
        futures_util::future::join_all(futs).await
    }

    fn classify_model(&self, user_message: &str) -> String {
        if let Some(decision) =
            super::classifier::classify_with_decision(&self.classification_config, user_message)
        {
            if self.available_hints.contains(&decision.hint) {
                let resolved_model = self
                    .route_model_by_hint
                    .get(&decision.hint)
                    .map(String::as_str)
                    .unwrap_or("unknown");
                tracing::info!(
                    target: "query_classification",
                    hint = decision.hint.as_str(),
                    model = resolved_model,
                    rule_priority = decision.priority,
                    message_length = user_message.len(),
                    "Classified message route"
                );
                return format!("hint:{}", decision.hint);
            }
        }

        // Fallback: auto-classify by complexity when no rule matched.
        if let Some(ref ac) = self.config.auto_classify {
            let tier = super::eval::estimate_complexity(user_message);
            if let Some(hint) = ac.hint_for(tier) {
                if self.available_hints.contains(&hint.to_string()) {
                    tracing::info!(
                        target: "query_classification",
                        hint = hint,
                        complexity = ?tier,
                        message_length = user_message.len(),
                        "Auto-classified by complexity"
                    );
                    return format!("hint:{hint}");
                }
            }
        }

        self.model_name.clone()
    }

    pub async fn turn(&mut self, user_message: &str) -> Result<String> {
        // Pull the live capability list from the backend before building
        // the system prompt. When the backend returns a changed set we
        // swap `tools`/`tool_specs` and reset `history[0]` so the next
        // prompt rebuild picks up the new tool list.
        self.refresh_backend_capabilities().await;

        if self.history.is_empty() {
            let system_prompt = self.build_system_prompt()?;
            self.history
                .push(ConversationMessage::Chat(ChatMessage::system(
                    system_prompt,
                )));
        }

        let context = self
            .memory_loader
            .load_context(
                self.memory.as_ref(),
                user_message,
                self.memory_session_id.as_deref(),
            )
            .await
            .unwrap_or_default();

        if self.auto_save {
            let _ = self
                .memory
                .store(
                    "user_msg",
                    user_message,
                    MemoryCategory::Conversation,
                    self.memory_session_id.as_deref(),
                )
                .await;
        }

        let now = chrono::Local::now();
        let (year, month, day) = (now.year(), now.month(), now.day());
        let (hour, minute, second) = (now.hour(), now.minute(), now.second());
        let tz = now.format("%Z");
        let date_str =
            format!("{year:04}-{month:02}-{day:02} {hour:02}:{minute:02}:{second:02} {tz}");

        let enriched = if context.is_empty() {
            format!("[CURRENT DATE & TIME: {date_str}]\n\n{user_message}")
        } else {
            format!("[CURRENT DATE & TIME: {date_str}]\n\n{context}\n\n{user_message}")
        };

        self.history
            .push(ConversationMessage::Chat(ChatMessage::user(enriched)));

        let effective_model = self.classify_model(user_message);

        for _ in 0..self.config.max_tool_iterations {
            let messages = self.tool_dispatcher.to_provider_messages(&self.history);

            // Response cache: check before LLM call (only for deterministic, text-only prompts)
            let cache_key = if self.temperature == 0.0 {
                self.response_cache.as_ref().map(|_| {
                    let last_user = messages
                        .iter()
                        .rfind(|m| m.role == "user")
                        .map(|m| m.content.as_str())
                        .unwrap_or("");
                    let system = messages
                        .iter()
                        .find(|m| m.role == "system")
                        .map(|m| m.content.as_str());
                    crate::memory::response_cache::ResponseCache::cache_key(
                        &effective_model,
                        system,
                        last_user,
                    )
                })
            } else {
                None
            };

            if let (Some(cache), Some(key)) = (&self.response_cache, &cache_key) {
                if let Ok(Some(cached)) = cache.get(key) {
                    self.observer.record_event(&ObserverEvent::CacheHit {
                        cache_type: "response".into(),
                        tokens_saved: 0,
                    });
                    self.history
                        .push(ConversationMessage::Chat(ChatMessage::assistant(
                            cached.clone(),
                        )));
                    self.trim_history();
                    return Ok(cached);
                }
                self.observer.record_event(&ObserverEvent::CacheMiss {
                    cache_type: "response".into(),
                });
            }

            let response = match self
                .provider
                .chat(
                    ChatRequest {
                        messages: &messages,
                        tools: if self.tool_dispatcher.should_send_tool_specs() {
                            Some(&self.tool_specs)
                        } else {
                            None
                        },
                    },
                    &effective_model,
                    self.temperature,
                )
                .await
            {
                Ok(resp) => resp,
                Err(err) => return Err(err),
            };

            let (text, calls) = self.tool_dispatcher.parse_response(&response);
            if calls.is_empty() {
                let final_text = if text.is_empty() {
                    response.text.unwrap_or_default()
                } else {
                    text
                };

                // Store in response cache (text-only, no tool calls)
                if let (Some(cache), Some(key)) = (&self.response_cache, &cache_key) {
                    let token_count = response
                        .usage
                        .as_ref()
                        .and_then(|u| u.output_tokens)
                        .unwrap_or(0);
                    #[allow(clippy::cast_possible_truncation)]
                    let _ = cache.put(key, &effective_model, &final_text, token_count as u32);
                }

                self.history
                    .push(ConversationMessage::Chat(ChatMessage::assistant(
                        final_text.clone(),
                    )));
                self.trim_history();

                return Ok(final_text);
            }

            if !text.is_empty() {
                self.history
                    .push(ConversationMessage::Chat(ChatMessage::assistant(
                        text.clone(),
                    )));
                print!("{text}");
                let _ = std::io::stdout().flush();
            }

            self.history.push(ConversationMessage::AssistantToolCalls {
                text: response.text.clone(),
                tool_calls: response.tool_calls.clone(),
                reasoning_content: response.reasoning_content.clone(),
            });

            let results = self.execute_tools(&calls).await;
            let formatted = self.tool_dispatcher.format_results(&results);
            self.history.push(formatted);
            self.trim_history();
        }

        anyhow::bail!(
            "Agent exceeded maximum tool iterations ({})",
            self.config.max_tool_iterations
        )
    }

    /// Execute a single agent turn while streaming intermediate events.
    ///
    /// Behaves identically to [`turn`](Self::turn) but forwards [`TurnEvent`]s
    /// through the provided channel so callers (e.g. the WebSocket gateway)
    /// can relay incremental updates to clients.
    ///
    /// The returned `String` is the final, complete assistant response — the
    /// same value that `turn` would return.
    pub async fn turn_streamed(
        &mut self,
        user_message: &str,
        event_tx: tokio::sync::mpsc::Sender<TurnEvent>,
    ) -> Result<String> {
        // ── Preamble (identical to turn) ───────────────────────────────
        if self.history.is_empty() {
            let system_prompt = self.build_system_prompt()?;
            self.history
                .push(ConversationMessage::Chat(ChatMessage::system(
                    system_prompt,
                )));
        }

        let context = self
            .memory_loader
            .load_context(
                self.memory.as_ref(),
                user_message,
                self.memory_session_id.as_deref(),
            )
            .await
            .unwrap_or_default();

        if self.auto_save {
            let _ = self
                .memory
                .store(
                    "user_msg",
                    user_message,
                    MemoryCategory::Conversation,
                    self.memory_session_id.as_deref(),
                )
                .await;
        }

        let now = chrono::Local::now().format("%Y-%m-%d %H:%M:%S %Z");
        let enriched = if context.is_empty() {
            format!("[{now}] {user_message}")
        } else {
            format!("{context}[{now}] {user_message}")
        };

        self.history
            .push(ConversationMessage::Chat(ChatMessage::user(enriched)));

        let effective_model = self.classify_model(user_message);

        // ── Turn loop ──────────────────────────────────────────────────
        for _ in 0..self.config.max_tool_iterations {
            let messages = self.tool_dispatcher.to_provider_messages(&self.history);

            // Response cache check (same as turn)
            let cache_key = if self.temperature == 0.0 {
                self.response_cache.as_ref().map(|_| {
                    let last_user = messages
                        .iter()
                        .rfind(|m| m.role == "user")
                        .map(|m| m.content.as_str())
                        .unwrap_or("");
                    let system = messages
                        .iter()
                        .find(|m| m.role == "system")
                        .map(|m| m.content.as_str());
                    crate::memory::response_cache::ResponseCache::cache_key(
                        &effective_model,
                        system,
                        last_user,
                    )
                })
            } else {
                None
            };

            if let (Some(cache), Some(key)) = (&self.response_cache, &cache_key) {
                if let Ok(Some(cached)) = cache.get(key) {
                    self.observer.record_event(&ObserverEvent::CacheHit {
                        cache_type: "response".into(),
                        tokens_saved: 0,
                    });
                    self.history
                        .push(ConversationMessage::Chat(ChatMessage::assistant(
                            cached.clone(),
                        )));
                    self.trim_history();
                    return Ok(cached);
                }
                self.observer.record_event(&ObserverEvent::CacheMiss {
                    cache_type: "response".into(),
                });
            }

            // ── Streaming LLM call ────────────────────────────────────
            // Try streaming first; if the provider returns content we
            // forward deltas.  Otherwise fall back to non-streaming chat.
            use futures_util::StreamExt;

            let stream_opts = crate::providers::traits::StreamOptions::new(true);
            let mut stream = self.provider.stream_chat(
                crate::providers::ChatRequest {
                    messages: &messages,
                    tools: if self.tool_dispatcher.should_send_tool_specs() {
                        Some(&self.tool_specs)
                    } else {
                        None
                    },
                },
                &effective_model,
                self.temperature,
                stream_opts,
            );

            let mut streamed_text = String::new();
            let mut streamed_tool_calls: Vec<crate::providers::traits::ToolCall> = Vec::new();
            let mut got_stream = false;

            while let Some(item) = stream.next().await {
                match item {
                    Ok(event) => match event {
                        crate::providers::traits::StreamEvent::TextDelta(chunk) => {
                            if let Some(reasoning) = chunk.reasoning {
                                if !reasoning.is_empty() {
                                    let _ = event_tx
                                        .send(TurnEvent::Thinking { delta: reasoning })
                                        .await;
                                }
                            }
                            if !chunk.delta.is_empty() {
                                got_stream = true;
                                streamed_text.push_str(&chunk.delta);
                                let _ =
                                    event_tx.send(TurnEvent::Chunk { delta: chunk.delta }).await;
                            }
                        }
                        crate::providers::traits::StreamEvent::ToolCall(tc) => {
                            got_stream = true;
                            let _ = event_tx
                                .send(TurnEvent::ToolCall {
                                    name: tc.name.clone(),
                                    args: serde_json::from_str(&tc.arguments).unwrap_or_default(),
                                })
                                .await;
                            streamed_tool_calls.push(tc);
                        }
                        crate::providers::traits::StreamEvent::PreExecutedToolCall {
                            name,
                            args,
                        } => {
                            let _ = event_tx
                                .send(TurnEvent::ToolCall {
                                    name,
                                    args: serde_json::from_str(&args).unwrap_or_default(),
                                })
                                .await;
                            // NOT pushed to streamed_tool_calls — already executed by proxy
                        }
                        crate::providers::traits::StreamEvent::PreExecutedToolResult {
                            name,
                            output,
                        } => {
                            let _ = event_tx.send(TurnEvent::ToolResult { name, output }).await;
                        }
                        crate::providers::traits::StreamEvent::Final => break,
                    },
                    Err(_) => break,
                }
            }
            // Drop the stream so we release the borrow on provider.
            drop(stream);

            // If streaming produced text, use it as the response and
            // check for tool calls via the dispatcher.
            let response = if got_stream {
                // Build a synthetic ChatResponse from streamed text
                crate::providers::ChatResponse {
                    text: Some(streamed_text),
                    tool_calls: streamed_tool_calls,
                    usage: None,
                    reasoning_content: None,
                }
            } else {
                // Fall back to non-streaming chat
                match self
                    .provider
                    .chat(
                        ChatRequest {
                            messages: &messages,
                            tools: if self.tool_dispatcher.should_send_tool_specs() {
                                Some(&self.tool_specs)
                            } else {
                                None
                            },
                        },
                        &effective_model,
                        self.temperature,
                    )
                    .await
                {
                    Ok(resp) => resp,
                    Err(err) => return Err(err),
                }
            };

            let (text, calls) = self.tool_dispatcher.parse_response(&response);
            if calls.is_empty() {
                let final_text = if text.is_empty() {
                    response.text.unwrap_or_default()
                } else {
                    text
                };

                // Store in response cache
                if let (Some(cache), Some(key)) = (&self.response_cache, &cache_key) {
                    let token_count = response
                        .usage
                        .as_ref()
                        .and_then(|u| u.output_tokens)
                        .unwrap_or(0);
                    #[allow(clippy::cast_possible_truncation)]
                    let _ = cache.put(key, &effective_model, &final_text, token_count as u32);
                }

                // If we didn't stream, send the full response as a single chunk
                if !got_stream && !final_text.is_empty() {
                    let _ = event_tx
                        .send(TurnEvent::Chunk {
                            delta: final_text.clone(),
                        })
                        .await;
                }

                self.history
                    .push(ConversationMessage::Chat(ChatMessage::assistant(
                        final_text.clone(),
                    )));
                self.trim_history();

                return Ok(final_text);
            }

            // ── Tool calls ─────────────────────────────────────────────
            if !text.is_empty() {
                self.history
                    .push(ConversationMessage::Chat(ChatMessage::assistant(
                        text.clone(),
                    )));
            }

            self.history.push(ConversationMessage::AssistantToolCalls {
                text: response.text.clone(),
                tool_calls: response.tool_calls.clone(),
                reasoning_content: response.reasoning_content.clone(),
            });

            // Notify about each tool call
            for call in &calls {
                let _ = event_tx
                    .send(TurnEvent::ToolCall {
                        name: call.name.clone(),
                        args: call.arguments.clone(),
                    })
                    .await;
            }

            let results = self.execute_tools(&calls).await;

            // Notify about each tool result
            for result in &results {
                let _ = event_tx
                    .send(TurnEvent::ToolResult {
                        name: result.name.clone(),
                        output: result.output.clone(),
                    })
                    .await;
            }

            let formatted = self.tool_dispatcher.format_results(&results);
            self.history.push(formatted);
            self.trim_history();
        }

        anyhow::bail!(
            "Agent exceeded maximum tool iterations ({})",
            self.config.max_tool_iterations
        )
    }

    pub async fn run_single(&mut self, message: &str) -> Result<String> {
        self.turn(message).await
    }

    pub async fn run_interactive(&mut self) -> Result<()> {
        println!("🦀 ZeroClaw Interactive Mode");
        println!("Type /quit to exit.\n");

        let (tx, mut rx) = tokio::sync::mpsc::channel(32);
        let cli = crate::channels::CliChannel::new();

        let listen_handle = tokio::spawn(async move {
            let _ = crate::channels::Channel::listen(&cli, tx).await;
        });

        while let Some(msg) = rx.recv().await {
            let response = match self.turn(&msg.content).await {
                Ok(resp) => resp,
                Err(e) => {
                    eprintln!("\nError: {e}\n");
                    continue;
                }
            };
            println!("\n{response}\n");
        }

        listen_handle.abort();
        Ok(())
    }
}

pub async fn run(
    config: Config,
    message: Option<String>,
    provider_override: Option<String>,
    model_override: Option<String>,
    temperature: f64,
) -> Result<()> {
    let start = Instant::now();

    let mut effective_config = config;
    if let Some(p) = provider_override {
        effective_config.default_provider = Some(p);
    }
    if let Some(m) = model_override {
        effective_config.default_model = Some(m);
    }
    effective_config.default_temperature = temperature;

    let mut agent = Agent::from_config(&effective_config).await?;

    let provider_name = effective_config
        .default_provider
        .as_deref()
        .unwrap_or("openrouter")
        .to_string();
    let model_name = effective_config
        .default_model
        .as_deref()
        .unwrap_or("anthropic/claude-sonnet-4-20250514")
        .to_string();

    agent.observer.record_event(&ObserverEvent::AgentStart {
        provider: provider_name.clone(),
        model: model_name.clone(),
    });

    if let Some(msg) = message {
        let response = agent.run_single(&msg).await?;
        println!("{response}");
    } else {
        agent.run_interactive().await?;
    }

    agent.observer.record_event(&ObserverEvent::AgentEnd {
        provider: provider_name,
        model: model_name,
        duration: start.elapsed(),
        tokens_used: None,
        cost_usd: None,
    });

    Ok(())
}


#[cfg(test)]
#[path = "agent.test.rs"]
mod tests;
