namespace EnterpriseAgentOs.Api.CronJobs;

public sealed record CreateCronJobInput(
    Guid AgentId,
    string Name,
    string Expression,
    string Prompt);
