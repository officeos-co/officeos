namespace OffceOs.Domain.Features.Quickstart;

public sealed record QuickstartWorkspaceConfig(
    string? Kind,
    QuickstartWorkspaceResourcesConfig? Resources,
    IReadOnlyList<QuickstartWorkspaceAgentConfig>? Agents,
    JsonElement? Metadata);

public sealed record QuickstartWorkspaceResourcesConfig(
    IReadOnlyList<QuickstartNamedResourceConfig>? Browsers,
    IReadOnlyList<QuickstartNamedResourceConfig>? MemoryStores);

public sealed record QuickstartNamedResourceConfig(
    string Key,
    string? DisplayName);

public sealed record QuickstartWorkspaceAgentConfig(
    string Key,
    string File);

public sealed record QuickstartAgentBlueprintConfig(
    string? Kind,
    string? Key,
    string Name,
    string? Description,
    string Model,
    string? System,
    IReadOnlyList<AgentMcpServerConfig>? McpServers,
    IReadOnlyList<AgentToolsetConfig>? Tools,
    IReadOnlyList<QuickstartAgentResourceConfig>? Resources,
    IReadOnlyList<AgentRoutineConfig>? Routines,
    JsonElement? Metadata);

public sealed record QuickstartAgentResourceConfig(
    string Type,
    string? Ref,
    Guid? ResourceId,
    string? AccessMode,
    string? Instructions);
