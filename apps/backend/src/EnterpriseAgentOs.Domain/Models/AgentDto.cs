namespace EnterpriseAgentOs.Domain.Models;

public sealed record AgentDto(
    Guid Id,
    string Name,
    string Provider,
    string? Model,
    string? Prompt,
    string Status,
    string? PodName,
    string? ServiceUrl,
    DateTime CreatedAt);

public sealed record CreateAgentRequest(
    [Required, MinLength(1)] string Name,
    [Required, MinLength(1)] string Provider,
    string? Model,
    string? Prompt = null);
