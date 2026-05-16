using OffceOs.Features.AgentRoutines.Domain;

namespace OffceOs.Features.AgentRoutines.Application;

public interface IAgentRoutineService
{
    Task<IReadOnlyList<AgentRoutineWithAgentRecord>> ListForOwnerAsync(Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<AgentRoutineWithAgentRecord?> GetForOwnerAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<IReadOnlyList<AgentRoutineRecord>> ListForAgentAsync(Guid agentId, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<AgentRoutineCreateResult> CreateAsync(CreateAgentRoutineRequest request, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<bool> SetEnabledAsync(Guid id, Guid ownerId, Guid workspaceId, bool enabled, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
}

public interface IAgentRoutineExecutionService
{
    Task<AgentRoutineExecutionResult> RunDueSchedulesAsync(DateTime now, CancellationToken ct = default);
    Task<AgentRoutineExecutionResult> ExecuteApiTriggerAsync(Guid triggerId, string secret, string? payloadJson, CancellationToken ct = default);
    Task<AgentRoutineExecutionResult> ExecuteGitHubWebhookAsync(GitHubRoutineWebhookRequest request, CancellationToken ct = default);
    Task<AgentRoutineExecutionResult> ExecuteGitHubPollTriggerAsync(Guid triggerId, string payloadJson, CancellationToken ct = default);
}

public sealed record CreateAgentRoutineRequest(
    Guid AgentId,
    string Name,
    string Prompt,
    IReadOnlyList<CreateScheduleRoutineTriggerRequest> ScheduleTriggers,
    IReadOnlyList<CreateApiRoutineTriggerRequest> ApiTriggers,
    IReadOnlyList<CreateGitHubRoutineTriggerRequest> GitHubTriggers);

public sealed record CreateScheduleRoutineTriggerRequest(
    string Name,
    string Expression);

public sealed record CreateApiRoutineTriggerRequest(
    string Name);

public sealed record CreateGitHubRoutineTriggerRequest(
    string Name,
    string Repo,
    IReadOnlyList<string> Events,
    string? AuthRef,
    string? Secret,
    string? Mode = null,
    int? PollIntervalSeconds = null);

public sealed record AgentRoutineCreateResult(
    AgentRoutineRecord Routine,
    IReadOnlyList<AgentRoutineGeneratedSecretResult> GeneratedSecrets);

public sealed record AgentRoutineGeneratedSecretResult(
    Guid TriggerId,
    string Kind,
    string Name,
    string Secret);

public sealed record AgentRoutineExecutionResult(
    int TriggeredCount,
    IReadOnlyList<Guid> RoutineIds);

public sealed record GitHubRoutineWebhookRequest(
    string Event,
    string Payload);
