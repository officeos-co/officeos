namespace OffceOs.Api.Features.Quickstart;

public sealed record QuickstartAgentChatInput(
    string Message,
    string? CurrentYaml,
    List<QuickstartFileInput>? CurrentFiles,
    List<QuickstartAgentMessageInput>? Messages,
    string? Provider,
    string? Model);

public sealed record QuickstartAgentMessageInput(
    string Role,
    string Content);

public sealed record QuickstartFileInput(
    string Path,
    string Content);

public sealed record QuickstartBlueprintApplyInput(
    List<QuickstartFileInput> Files,
    string? Provider,
    string? Model);
