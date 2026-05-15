namespace OffceOs.Domain.Features.AgentHarness;

public sealed record AgentRuntimeCleanupResult(int Pods, int Services, int Volumes);

public interface IAgentRuntimeCleaner
{
    Task<AgentRuntimeCleanupResult> CleanupUnusedAsync(
        IReadOnlySet<Guid> activeAgentIds,
        CancellationToken ct = default);
}
