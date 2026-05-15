namespace OffceOs.Domain.Features.AgentRoutines;

public interface IAgentRoutineRepository
{
    Task<IReadOnlyList<AgentRoutineRecord>> ListAsync(AgentRoutineFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<AgentRoutineWithAgentRecord>> ListForOwnerAsync(Guid? ownerId, Guid? workspaceId = null, CancellationToken ct = default);
    Task<IReadOnlyList<AgentRoutineRecord>> ListAllEnabledAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AgentRoutineExecutionRecord>> ListAllEnabledForExecutionAsync(CancellationToken ct = default);
    Task<AgentRoutineRecord?> GetByAsync(AgentRoutineFilter filter, CancellationToken ct = default);
    Task<AgentRoutineWithAgentRecord?> GetForOwnerAsync(Guid id, Guid? ownerId, Guid? workspaceId = null, CancellationToken ct = default);
    Task<AgentRoutineTriggerRecord?> GetTriggerByAsync(Guid triggerId, CancellationToken ct = default);
    Task<AgentRoutineRecord> UpsertAsync(AgentRoutineRecord record, CancellationToken ct = default);
    Task<AgentRoutinePollCursorRecord?> GetPollCursorAsync(Guid triggerId, string @event, CancellationToken ct = default);
    Task<AgentRoutinePollCursorRecord> UpsertPollCursorAsync(AgentRoutinePollCursorRecord record, CancellationToken ct = default);
    Task SetEnabledAsync(Guid id, bool enabled, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed record AgentRoutineExecutionRecord(
    AgentRoutineRecord Routine,
    Guid OwnerId,
    Guid WorkspaceId);

public sealed class AgentRoutinePollCursorRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid TriggerId { get; init; }
    public string Event { get; init; } = string.Empty;
    public DateTime CursorAt { get; set; }
    public DateTime? LastPolledAt { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
