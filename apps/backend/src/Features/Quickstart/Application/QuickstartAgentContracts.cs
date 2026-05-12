namespace OffceOs.Application.Features.Quickstart;

public interface IQuickstartAgentService
{
    Task<QuickstartAgentChatResult> ChatAsync(
        QuickstartAgentChatRequest request,
        Guid userId,
        Guid workspaceId,
        CancellationToken ct = default);
}

public sealed record QuickstartAgentChatRequest(
    string Message,
    string? CurrentYaml,
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
    string Model);
