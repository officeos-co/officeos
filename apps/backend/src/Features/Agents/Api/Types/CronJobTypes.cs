namespace EnterpriseAgentOs.Api.Features.Agents;

public sealed record CreateCronJobInput(
    Guid AgentId,
    string Name,
    string Expression,
    string Prompt);

public sealed record CronJobPayload(
    Guid Id,
    Guid AgentId,
    string AgentName,
    string Name,
    CronExpression Expression,
    string Prompt,
    bool Enabled,
    DateTime? LastRunAt,
    DateTime? NextRunAt,
    DateTime CreatedAt);
