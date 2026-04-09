namespace EnterpriseAgentOs.Api.Entities.Agents;

public sealed record AgentDto(
    Guid Id,
    string Name,
    string Provider,
    string? Model,
    string Status,
    string? PodName,
    string? ServiceUrl,
    DateTime CreatedAt);
