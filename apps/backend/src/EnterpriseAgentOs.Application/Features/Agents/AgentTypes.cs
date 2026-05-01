namespace EnterpriseAgentOs.Application.Features.Agents;

public sealed record CreateAgentInput(
    string Name,
    string Provider,
    string? Model,
    string? Prompt,
    List<string>? IntegrationSlugs,
    List<string>? ChannelSlugs,
    List<string>? ToolNames,
    List<ToolPermissionInput>? ToolPermissions);

public sealed record ToolPermissionInput(
    string Tool,
    ToolPermission Mode);

public sealed record UpdateAgentInput(
    string? Name,
    string? Provider,
    string? Model,
    string? Prompt);

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

public sealed record ToolPermissionPayload(
    string SkillName,
    string ToolName,
    ToolPermission Mode);

public sealed record SetAgentToolPermissionInput(
    Guid AgentId,
    string Skill,
    string Tool,
    ToolPermission Mode);

public sealed record SetAgentToolPermissionsInput(
    Guid AgentId,
    IReadOnlyList<SetAgentToolPermissionEntryInput> Entries);

public sealed record SetAgentToolPermissionEntryInput(
    string Skill,
    string Tool,
    ToolPermission Mode);

public sealed record AgentToolCatalogEntry(
    string Group,
    string RuntimeName,
    string PermissionSkill,
    string PermissionTool,
    string Description,
    bool Deferred);
