namespace EnterpriseAgentOs.Api.GraphQL;

public sealed record CreateCronJobInput(
    Guid AgentId,
    string Name,
    string Expression,
    string Prompt);

public sealed record CronJobPayload(
    Guid Id,
    Guid AgentId,
    string Name,
    string Expression,
    string Prompt,
    bool Enabled,
    DateTime? LastRunAt,
    DateTime? NextRunAt,
    DateTime CreatedAt);
