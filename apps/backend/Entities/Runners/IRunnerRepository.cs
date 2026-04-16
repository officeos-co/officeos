namespace EnterpriseAgentOs.Api.Entities.Runners;

public interface IRunnerRepository
{
    Task<EnterpriseAgentOs.Api.Database.Models.RunnerRecord> CreateAsync(Guid ownerId, string name, string registrationTokenHash, CancellationToken ct = default);
    Task<List<EnterpriseAgentOs.Api.Database.Models.RunnerRecord>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.RunnerRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.RunnerRecord?> GetByRegistrationTokenHashAsync(string hash, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.RunnerRecord?> GetByAuthTokenHashAsync(string hash, CancellationToken ct = default);
    Task<List<EnterpriseAgentOs.Api.Database.Models.RunnerRecord>> GetOnlineRunnersAsync(CancellationToken ct = default);
    Task UpdateAsync(EnterpriseAgentOs.Api.Database.Models.RunnerRecord runner, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
