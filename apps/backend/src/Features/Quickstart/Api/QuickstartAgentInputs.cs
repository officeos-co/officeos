namespace OffceOs.Api.Features.Quickstart;

public sealed record QuickstartAgentChatInput(
    string Message,
    string? CurrentYaml,
    List<QuickstartAgentMessageInput>? Messages,
    string? Provider,
    string? Model);

public sealed record QuickstartAgentMessageInput(
    string Role,
    string Content);
