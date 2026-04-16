namespace EnterpriseAgentOs.Api.Entities.Runners.GraphQL;

public sealed record RunnerDto(
    Guid Id,
    string Name,
    string Status,
    DateTime? LastHeartbeatAt,
    string? Version,
    DateTime CreatedAt);

public sealed record RunnerJobDto(
    Guid Id,
    Guid RunnerId,
    string Status,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt);

public sealed record CreateRunnerResult(RunnerDto Runner, string RegistrationToken);

public static class RunnerGraphQLMapper
{
    public static RunnerDto ToDto(this RunnerRecord r) =>
        new(r.Id, r.Name, r.Status, r.LastHeartbeatAt, r.Version, r.CreatedAt);

    public static RunnerJobDto ToDto(this RunnerJobRecord j) =>
        new(j.Id, j.RunnerId, j.Status, j.CreatedAt, j.StartedAt, j.CompletedAt);
}
