pub mod agent_runtime;
pub mod schema;
pub mod traits;
pub mod workspace;

#[allow(unused_imports)]
pub use agent_runtime::AgentRuntimeConfig;

#[allow(unused_imports)]
pub use schema::{
    AgentConfig, AssemblyAiSttConfig, AuditConfig, AutonomyConfig, BackupConfig,
    BrowserComputerUseConfig, BrowserConfig, BuiltinHooksConfig, ChannelsConfig,
    ClassificationRule, ClaudeCodeConfig, ClaudeCodeRunnerConfig, CloudOpsConfig, CodexCliConfig,
    ComposioConfig, Config, ConversationalAiConfig, CostConfig, CronConfig, CronJobDecl,
    CronScheduleDecl, DEFAULT_GWS_SERVICES, DataRetentionConfig, DeepgramSttConfig,
    DelegateAgentConfig, DelegateToolConfig, DiscordConfig, DockerRuntimeConfig, EdgeTtsConfig,
    ElevenLabsTtsConfig, EmbeddingRouteConfig, EstopConfig, FeishuConfig, GatewayConfig,
    GeminiCliConfig, GoogleSttConfig, GoogleTtsConfig, GoogleWorkspaceAllowedOperation,
    GoogleWorkspaceConfig, HeartbeatConfig, HooksConfig, HttpRequestConfig, IMessageConfig,
    ImageGenConfig, ImageProviderDalleConfig, ImageProviderFluxConfig, ImageProviderImagenConfig,
    ImageProviderStabilityConfig, JiraConfig, LarkConfig, LinkEnricherConfig, LinkedInConfig,
    LinkedInContentConfig, LinkedInImageConfig, LocalWhisperConfig, MatrixConfig, McpConfig,
    McpServerConfig, McpTransport, MediaPipelineConfig, MemoryConfig, Microsoft365Config,
    ModelRouteConfig, MqttConfig, MultimodalConfig, NextcloudTalkConfig, NodeTransportConfig,
    NodesConfig, NotionConfig, ObservabilityConfig, OpenAiSttConfig, OpenAiTtsConfig,
    OpenCodeCliConfig, OtpConfig, OtpMethod, PacingConfig, PipelineConfig, PiperTtsConfig,
    PluginsConfig, ProjectIntelConfig, ProxyConfig, ProxyScope, QueryClassificationConfig,
    ReliabilityConfig, ResourceLimitsConfig, RuntimeConfig, SandboxBackend, SandboxConfig,
    SchedulerConfig, SearchMode, SecretsConfig, SecurityConfig, SecurityOpsConfig, ShellToolConfig,
    SkillCreationConfig, SkillImprovementConfig, SkillsConfig,
    SlackConfig, StorageConfig, StorageProviderConfig, StorageProviderSection, StreamMode,
    SwarmConfig, SwarmStrategy, TelegramConfig, TextBrowserConfig, ToolFilterGroup,
    ToolFilterGroupMode, TranscriptionConfig, TtsConfig, WebFetchConfig, WebSearchConfig,
    WebhookConfig, WhatsAppChatPolicy, WhatsAppWebMode, WorkspaceConfig,
    apply_channel_proxy_to_builder, apply_runtime_proxy_to_builder, build_channel_proxy_client,
    build_channel_proxy_client_with_timeouts, build_runtime_proxy_client,
    build_runtime_proxy_client_with_timeouts, runtime_proxy_config, set_runtime_proxy_config,
    ws_connect_with_proxy,
};

pub fn name_and_presence<T: traits::ChannelConfig>(channel: Option<&T>) -> (&'static str, bool) {
    (T::name(), channel.is_some())
}

#[cfg(test)]
#[path = "tests.rs"]
mod tests;
