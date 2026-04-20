namespace EnterpriseAgentOs.Infrastructure.Persistence.Repositories;

public sealed class AgentPersonalityRepository : IAgentPersonalityRepository
{
    private readonly EaosDbContext _db;

    public AgentPersonalityRepository(EaosDbContext db) => _db = db;

    public async Task<IReadOnlyList<AgentPersonalityRecord>> ListAsync(Guid agentId, CancellationToken ct = default)
        => await _db.AgentPersonalities
            .Where(p => p.AgentId == agentId)
            .OrderBy(p => p.FileName)
            .ToListAsync(ct);

    public async Task UpsertAsync(Guid agentId, string fileName, string content, CancellationToken ct = default)
    {
        var existing = await _db.AgentPersonalities
            .FirstOrDefaultAsync(p => p.AgentId == agentId && p.FileName == fileName, ct);

        if (existing is not null)
        {
            existing.UpdateContent(content);
        }
        else
        {
            _db.AgentPersonalities.Add(AgentPersonalityRecord.Create(agentId, fileName, content));
        }

        await _db.SaveChangesAsync(ct);
    }
}
