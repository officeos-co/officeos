namespace EnterpriseAgentOs.Api.Features.Agents;

public sealed record CreateCronJobInput(
    Guid AgentId,
    string Name,
    string Expression,
    string Prompt);
