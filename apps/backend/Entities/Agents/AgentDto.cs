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

public sealed record CreateAgentRequest(
    [Required, MinLength(1)] string Name,
    [Required, MinLength(1)] string Provider,
    string? Model);
