namespace OffceOs.Domain.Features.AgentRoutines;

public interface IAgentRoutineRepository
{
    Task<IReadOnlyList<AgentRoutineRecord>> ListAsync(AgentRoutineFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<AgentRoutineWithAgentRecord>> ListForOwnerAsync(Guid? ownerId, Guid? workspaceId = null, CancellationToken ct = default);
    Task<IReadOnlyList<AgentRoutineRecord>> ListAllEnabledAsync(CancellationToken ct = default);
    Task<AgentRoutineRecord?> GetByAsync(AgentRoutineFilter filter, CancellationToken ct = default);
    Task<AgentRoutineWithAgentRecord?> GetForOwnerAsync(Guid id, Guid? ownerId, Guid? workspaceId = null, CancellationToken ct = default);
    Task<AgentRoutineTriggerRecord?> GetTriggerByAsync(Guid triggerId, CancellationToken ct = default);
    Task<AgentRoutineRecord> UpsertAsync(AgentRoutineRecord record, CancellationToken ct = default);
    Task SetEnabledAsync(Guid id, bool enabled, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
