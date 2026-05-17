using OffceOs.Features.Agents.Domain;

namespace OffceOs.Features.Agents.Application;

public interface IAgentSessionService
{
    Task<IReadOnlyList<AgentSessionRecord>> ListByAgentAsync(Guid agentId, Guid ownerId, int limit = 20, CancellationToken ct = default);
    Task<AgentSessionRecord?> GetForOwnerAsync(Guid sessionId, Guid ownerId, CancellationToken ct = default);
    Task<AgentSessionRecord> CreateRunAsync(CreateAgentSessionRequest request, Guid ownerId, CancellationToken ct = default);
    Task MarkRunningAsync(Guid sessionId, string sandboxId, string serviceUrl, CancellationToken ct = default);
    Task MarkCompletedAsync(Guid sessionId, CancellationToken ct = default);
    Task MarkFailedAsync(Guid sessionId, string error, CancellationToken ct = default);
}

public sealed record CreateAgentSessionRequest(
    Guid AgentId,
    string Input,
    string Purpose,
    string Source,
    string CorrelationId,
    Guid? RoutineId = null,
    Guid? TriggerId = null,
    Guid? DefinitionId = null,
    string? TriggerPayloadJson = null,
    AgentSessionRepositoryConfig? Repository = null);
