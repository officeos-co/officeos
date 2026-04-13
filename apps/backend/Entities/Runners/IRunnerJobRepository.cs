namespace EnterpriseAgentOs.Api.Entities.Runners;

public interface IRunnerJobRepository
{
    Task<RunnerJobRecord> CreateAsync(Guid runnerId, string payload, TimeSpan claimTimeout, CancellationToken ct = default);
    Task<RunnerJobRecord?> ClaimNextAsync(Guid runnerId, CancellationToken ct = default);
    Task<RunnerJobRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(RunnerJobRecord job, CancellationToken ct = default);
    Task<List<RunnerJobRecord>> GetRecentByRunnerAsync(Guid runnerId, int limit, CancellationToken ct = default);
    Task FailStaleJobsAsync(CancellationToken ct = default);
}
