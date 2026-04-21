namespace EnterpriseAgentOs.Domain.Models;

public sealed record AgentTemplateDto(
    Guid Id,
    string Name,
    string Description,
    string Prompt,
    IReadOnlyList<string> Integrations,
    IReadOnlyList<string> Channels,
    bool IsBuiltin);
