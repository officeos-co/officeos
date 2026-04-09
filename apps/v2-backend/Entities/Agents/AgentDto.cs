namespace EnterpriseAgentOs.Api.Entities.Agents;

public sealed record AgentDto(
    Guid Id,
    string Name,
    string? Model,
    string Status,
    DateTime CreatedAt);
