namespace OffceOs.Api.Features.AgentRoutines;

public sealed record CreateAgentRoutineInput(
    Guid AgentId,
    string Name,
    string Prompt,
    IReadOnlyList<CreateScheduleRoutineTriggerInput>? ScheduleTriggers,
    IReadOnlyList<CreateApiRoutineTriggerInput>? ApiTriggers,
    IReadOnlyList<CreateGitHubRoutineTriggerInput>? GitHubTriggers);

public sealed record CreateScheduleRoutineTriggerInput(
    string Name,
    string Expression);

public sealed record CreateApiRoutineTriggerInput(
    string Name);

public sealed record CreateGitHubRoutineTriggerInput(
    string Name,
    string Owner,
    string Repo,
    IReadOnlyList<string> Events,
    string Secret);

public sealed record AgentRoutinePayload(
    Guid Id,
    Guid AgentId,
    string AgentName,
    string Name,
    string Prompt,
    bool Enabled,
    DateTime? LastTriggeredAt,
    DateTime CreatedAt,
    IReadOnlyList<AgentRoutineTriggerPayload> Triggers);

public sealed record AgentRoutineTriggerPayload(
    Guid Id,
    string Kind,
    string Name,
    bool Enabled,
    string ConfigJson,
    DateTime? LastTriggeredAt,
    DateTime? NextRunAt,
    DateTime CreatedAt);

public sealed record CreateAgentRoutinePayload(
    AgentRoutinePayload Routine,
    IReadOnlyList<AgentRoutineGeneratedSecretPayload> GeneratedSecrets);

public sealed record AgentRoutineGeneratedSecretPayload(
    Guid TriggerId,
    string Kind,
    string Name,
    string Secret);

internal static class AgentRoutineMapper
{
    public static AgentRoutinePayload ToPayload(AgentRoutineWithAgentRecord row) =>
        ToPayload(row.Routine, row.AgentName);

    public static AgentRoutinePayload ToPayload(AgentRoutineRecord routine, string agentName) =>
        new(
            routine.Id,
            routine.AgentId,
            agentName,
            routine.Name,
            routine.Prompt,
            routine.Enabled,
            routine.LastTriggeredAt,
            routine.CreatedAt,
            routine.Triggers.Select(ToPayload).ToList());

    public static AgentRoutineTriggerPayload ToPayload(AgentRoutineTriggerRecord trigger) =>
        new(
            trigger.Id,
            trigger.Kind,
            trigger.Name,
            trigger.Enabled,
            trigger.ConfigJson,
            trigger.LastTriggeredAt,
            trigger.NextRunAt,
            trigger.CreatedAt);

    public static CreateAgentRoutinePayload ToPayload(AgentRoutineCreateResult result, string agentName) =>
        new(
            ToPayload(result.Routine, agentName),
            result.GeneratedSecrets
                .Select(secret => new AgentRoutineGeneratedSecretPayload(secret.TriggerId, secret.Kind, secret.Name, secret.Secret))
                .ToList());
}
