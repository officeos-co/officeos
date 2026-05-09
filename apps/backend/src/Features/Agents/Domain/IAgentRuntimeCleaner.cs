namespace OffceOs.Domain.Features.Agents;

public sealed record AgentRuntimeCleanupResult(int Pods, int Services, int Volumes);

public interface IAgentRuntimeCleaner
{
    Task<AgentRuntimeCleanupResult> CleanupUnusedAsync(
        IReadOnlySet<Guid> activeAgentIds,
        CancellationToken ct = default);
}
