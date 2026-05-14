namespace OffceOs.Api.Features.Quickstart;

public sealed record QuickstartAgentChatPayload(
    string Message,
    string ConfigYaml,
    string ConfigJson,
    string Provider,
    string Model,
    IReadOnlyList<QuickstartFilePayload> Files);

public sealed record QuickstartFilePayload(
    string Path,
    string Content);

public sealed record QuickstartBlueprintApplyPayload(
    IReadOnlyList<QuickstartCreatedAgentPayload> Agents);

public sealed record QuickstartCreatedAgentPayload(
    Guid Id,
    string Name,
    string FilePath);
