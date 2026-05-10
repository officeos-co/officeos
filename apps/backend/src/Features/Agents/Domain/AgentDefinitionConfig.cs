namespace OffceOs.Domain.Features.Agents;

public sealed record AgentDefinitionConfig(
    string Name,
    string? Description,
    string Model,
    string? System,
    IReadOnlyList<AgentMcpServerConfig> McpServers,
    IReadOnlyList<AgentToolsetConfig> Tools,
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
