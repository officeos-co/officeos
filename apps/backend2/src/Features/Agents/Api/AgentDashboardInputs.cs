namespace OffceOs.Api.Features.Agents;

public sealed record CreateAgentInput(
    string Name,
    string Provider,
    string? Model,
    string? Prompt,
    string? ConfigJson,
    List<string>? IntegrationSlugs,
    List<Guid>? ChannelConnectionIds,
    List<string>? ToolNames,
    List<AgentResourceAttachmentInput>? Resources = null,
    string? BootstrapMessage = null);

public sealed record AgentResourceAttachmentInput(
    string ResourceType,
    Guid ResourceId,
    string? AccessMode,
    string? Instructions);

public sealed record UpdateAgentInput(
    string? Name,
    string? Provider,
    string? Model,
    string? Prompt,
    string? ConfigJson);

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
    IReadOnlyList<AgentInstalledSkillSummary> Skills);

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
