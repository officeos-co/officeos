namespace OffceOs.Application.Features.Quickstart;

public interface IQuickstartAgentService
{
    Task<QuickstartAgentChatResult> ChatAsync(
        QuickstartAgentChatRequest request,
        Guid userId,
        Guid workspaceId,
        CancellationToken ct = default);

    Task<QuickstartBlueprintApplyResult> ApplyAsync(
        QuickstartBlueprintApplyRequest request,
        Guid userId,
        Guid workspaceId,
        CancellationToken ct = default);
}

public sealed record QuickstartAgentChatRequest(
    string Message,
    string? CurrentYaml,
    IReadOnlyList<QuickstartFileRequest>? CurrentFiles,
    IReadOnlyList<QuickstartAgentMessageRequest>? Messages,
    string? Provider,
    string? Model);

public sealed record QuickstartAgentMessageRequest(
    string Role,
    string Content);

public sealed record QuickstartAgentChatResult(
    string Message,
    string ConfigYaml,
    string ConfigJson,
    string Provider,
    string Model,
    IReadOnlyList<QuickstartFileResult> Files);

public sealed record QuickstartFileRequest(
    string Path,
    string Content);

public sealed record QuickstartFileResult(
    string Path,
    string Content);

public sealed record QuickstartBlueprintApplyRequest(
    IReadOnlyList<QuickstartFileRequest> Files,
    string? Provider,
    string? Model);

public sealed record QuickstartBlueprintApplyResult(
    IReadOnlyList<QuickstartCreatedAgentResult> Agents);

public sealed record QuickstartCreatedAgentResult(
    Guid Id,
    string Name,
    string FilePath);
