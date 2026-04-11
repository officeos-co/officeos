namespace EnterpriseAgentOs.Api.Entities.Runners;

public sealed class RunnerRepository : IRunnerRepository
{
    private readonly EaosDbContext _db;

    public RunnerRepository(EaosDbContext db) => _db = db;

    public async Task<RunnerRecord> CreateAsync(Guid ownerId, string name, string registrationTokenHash, CancellationToken ct)
    {
        var runner = new RunnerRecord
        {
            OwnerId = ownerId,
            Name = name,
            RegistrationTokenHash = registrationTokenHash,
        };
        _db.Runners.Add(runner);
        await _db.SaveChangesAsync(ct);
        return runner;
    }

    public async Task<List<RunnerRecord>> ListByOwnerAsync(Guid ownerId, CancellationToken ct)
        => await _db.Runners.Where(r => r.OwnerId == ownerId).OrderByDescending(r => r.CreatedAt).ToListAsync(ct);

    public async Task<RunnerRecord?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _db.Runners.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<RunnerRecord?> GetByRegistrationTokenHashAsync(string hash, CancellationToken ct)
        => await _db.Runners.FirstOrDefaultAsync(r => r.RegistrationTokenHash == hash, ct);

    public async Task<RunnerRecord?> GetByAuthTokenHashAsync(string hash, CancellationToken ct)
        => await _db.Runners.FirstOrDefaultAsync(r => r.AuthTokenHash == hash, ct);

    public async Task<List<RunnerRecord>> GetOnlineRunnersAsync(CancellationToken ct)
        => await _db.Runners.Where(r => r.Status == "online").ToListAsync(ct);

    public async Task UpdateAsync(RunnerRecord runner, CancellationToken ct)
    {
        _db.Runners.Update(runner);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var runner = await _db.Runners.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (runner is not null)
        {
            _db.Runners.Remove(runner);
            await _db.SaveChangesAsync(ct);
        }
    }
}
