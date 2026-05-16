namespace OffceOs.Domain.Features.AgentDefinitions;

public sealed record AgentDefinitionConfig(
    string Name,
    string? Description,
    string Model,
    string? System,
    IReadOnlyList<AgentMcpServerConfig> McpServers,
    IReadOnlyList<AgentToolsetConfig> Tools,
    IReadOnlyList<AgentResourceAttachmentConfig>? Resources,
    IReadOnlyList<AgentRoutineConfig>? Routines,
    JsonElement? Metadata);

public sealed record AgentMcpServerConfig(
    string Name,
    string Type,
    string? Url);

public sealed record AgentToolsetConfig(
    string Type,
    string? McpServerName,
    AgentToolsetDefaultConfig? DefaultConfig);

public sealed record AgentToolsetDefaultConfig(
    AgentToolPermissionConfig? PermissionPolicy);

public sealed record AgentToolPermissionConfig(
    string Type,
    IReadOnlyList<string>? Tools);

public sealed record AgentResourceAttachmentConfig(
    string Type,
    Guid ResourceId,
    string? AccessMode,
    string? Instructions);

public sealed record AgentRoutineConfig(
    string Name,
    string Prompt,
    IReadOnlyList<AgentRoutineScheduleTriggerConfig>? ScheduleTriggers,
    IReadOnlyList<AgentRoutineApiTriggerConfig>? ApiTriggers,
    IReadOnlyList<AgentRoutineGitHubTriggerConfig>? GitHubTriggers);

public sealed record AgentRoutineScheduleTriggerConfig(
    string Name,
    string Expression);

public sealed record AgentRoutineApiTriggerConfig(
    string Name);

public sealed record AgentRoutineGitHubTriggerConfig(
    string Name,
    string Repo,
    IReadOnlyList<string> Events,
    string? AuthRef,
    string? Secret,
    string? Mode,
    int? PollIntervalSeconds);

public static class AgentToolsetKinds
{
    public const string Builtin = "agent_toolset_20260401";
    public const string Mcp = "mcp_toolset";
    public const string Browser = "browser_toolset";
}

public static class AgentToolPermissionKinds
{
    public const string AlwaysAllow = "always_allow";
    public const string AlwaysDeny = "always_deny";
    public const string AllowList = "allow_list";
    public const string DenyList = "deny_list";
}
