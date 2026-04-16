namespace EnterpriseAgentOs.Api.Entities.Runners;

public interface IRunnerJobRepository
{
    Task<EnterpriseAgentOs.Api.Database.Models.RunnerJobRecord> CreateAsync(Guid runnerId, string payload, TimeSpan claimTimeout, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.RunnerJobRecord?> ClaimNextAsync(Guid runnerId, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.RunnerJobRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(EnterpriseAgentOs.Api.Database.Models.RunnerJobRecord job, CancellationToken ct = default);
    Task<List<EnterpriseAgentOs.Api.Database.Models.RunnerJobRecord>> GetRecentByRunnerAsync(Guid runnerId, int limit, CancellationToken ct = default);
    Task FailStaleJobsAsync(CancellationToken ct = default);
}
