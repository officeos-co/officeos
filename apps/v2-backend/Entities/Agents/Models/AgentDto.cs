namespace EnterpriseAgentOs.Api.Entities.Agents.Models;

public sealed record AgentDto(
    Guid Id,
    string Name,
    string? Model,
    string Status,
    DateTime CreatedAt);
