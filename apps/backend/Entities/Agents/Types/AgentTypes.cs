namespace EnterpriseAgentOs.Api.Entities.Agents.Types;

public sealed record CreateAgentInput(
    string Name,
    string Provider,
    string? Model,
    string? Prompt,
    List<string>? IntegrationSlugs,
    List<string>? ChannelSlugs,
    List<string>? ToolNames);

public sealed record UpdateAgentInput(
    string? Name,
    string? Provider,
    string? Model,
    string? Prompt);

/// <summary>
/// Bootstrap payload returned by <c>GET /api/agents/{id}</c> (agent-pod-facing,
/// <c>AgentTokenAuth</c>). Contains everything a pod needs to boot without a
/// local config.toml — provider endpoint hint, vault location, installed skills
/// summary, and per-tool permissions. NEVER contains credentials.
/// </summary>
public sealed record AgentBootstrapPayload(
    Guid AgentId,
    string Name,
    AgentProviderBootstrap Provider,
    AgentVaultBootstrap Vault,
    IReadOnlyList<AgentInstalledSkillSummary> Skills,
    IReadOnlyList<AgentBootstrapToolPermission> ToolPermissions);

public sealed record AgentProviderBootstrap(
    string Backend,
    string Model,
    string ProxyEndpoint);

public sealed record AgentVaultBootstrap(string BaseUrl);

public sealed record AgentInstalledSkillSummary(string Name);

public sealed record AgentBootstrapToolPermission(
    string Skill,
    string Tool,
    string Mode);

[GraphQLName("ToolPermission")]
public sealed record ToolPermissionPayload(
    string SkillName,
    string ToolName,
    EnterpriseAgentOs.Api.Database.Models.ToolPermission Mode);

public sealed record SetAgentToolPermissionInput(
    Guid AgentId,
    string Skill,
    string Tool,
    EnterpriseAgentOs.Api.Database.Models.ToolPermission Mode);
