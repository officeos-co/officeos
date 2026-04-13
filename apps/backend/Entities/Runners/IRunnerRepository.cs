namespace EnterpriseAgentOs.Api.Entities.Runners;

public interface IRunnerRepository
{
    Task<RunnerRecord> CreateAsync(Guid ownerId, string name, string registrationTokenHash, CancellationToken ct = default);
    Task<List<RunnerRecord>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<RunnerRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<RunnerRecord?> GetByRegistrationTokenHashAsync(string hash, CancellationToken ct = default);
    Task<RunnerRecord?> GetByAuthTokenHashAsync(string hash, CancellationToken ct = default);
    Task<List<RunnerRecord>> GetOnlineRunnersAsync(CancellationToken ct = default);
    Task UpdateAsync(RunnerRecord runner, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
