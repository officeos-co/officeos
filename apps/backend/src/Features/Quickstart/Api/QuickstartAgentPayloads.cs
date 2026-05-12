namespace OffceOs.Api.Features.Quickstart;

public sealed record QuickstartAgentChatPayload(
    string Message,
    string ConfigYaml,
    string ConfigJson,
    string Provider,
    string Model);
