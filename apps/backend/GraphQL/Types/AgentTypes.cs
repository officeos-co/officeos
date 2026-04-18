namespace EnterpriseAgentOs.Api.GraphQL.Types;

public sealed record CreateAgentInput(
    string Name,
    string Provider,
    string? Model,
    string? Prompt,
    List<string>? IntegrationSlugs,
    List<string>? ChannelSlugs,
    List<string>? ToolNames,
    List<ToolPermissionInput>? ToolPermissions);

/// <summary>
/// Per-tool allow/deny override submitted alongside agent creation.
/// `Tool` is the fully-qualified "skill:tool" key (matches the dashboard
/// permission map); persisted to <c>AgentToolPermissionRecord</c>.
/// </summary>
public sealed record ToolPermissionInput(
    string Tool,
    EnterpriseAgentOs.Domain.Models.ToolPermission Mode);

public sealed record UpdateAgentInput(
    string? Name,
    string? Provider,
    string? Model,
    string? Prompt);

/// <summary>
/// Bootstrap payload returned by <c>GET /api/agents/{id}</c> (agent-pod-facing,
/// <c>AgentTokenAuth</c>). Contains everything a pod needs to boot without a
/// local config.toml: display name, the user-supplied system prompt (merged
/// locally into the zeroclaw-core embedded BOOTSTRAP.md), provider/proxy/gateway
/// endpoints, installed skills summary, and per-tool permissions. NEVER
/// contains credentials — the LLM proxy injects provider keys per-request and
/// skill credentials live behind the skill gateway.
/// </summary>
public sealed record AgentBootstrapPayload(
    Guid AgentId,
    string DisplayName,
    string? SystemPrompt,
    AgentProviderBootstrap Provider,
    AgentProxyBootstrap Proxy,
    AgentGatewayBootstrap Gateway,
    IReadOnlyList<AgentInstalledSkillSummary> Skills,
    AgentToolPermissionsBootstrap ToolPermissions);

public sealed record AgentProviderBootstrap(
    string Name,
    string Model,
    string ApiUrl,
    string? TokenRef);

public sealed record AgentProxyBootstrap(
    string Url,
    string? Token);

public sealed record AgentGatewayBootstrap(
    string Host,
    int Port,
    string? TlsCertRef);

public sealed record AgentInstalledSkillSummary(string Name);

public sealed record AgentToolPermissionsBootstrap(
    IReadOnlyList<AgentBootstrapToolPermission> Entries);

public sealed record AgentBootstrapToolPermission(
    string Skill,
    string Tool,
    string Mode);

[GraphQLName("ToolPermission")]
public sealed record ToolPermissionPayload(
    string SkillName,
    string ToolName,
    EnterpriseAgentOs.Domain.Models.ToolPermission Mode);

public sealed record SetAgentToolPermissionInput(
    Guid AgentId,
    string Skill,
    string Tool,
    EnterpriseAgentOs.Domain.Models.ToolPermission Mode);
