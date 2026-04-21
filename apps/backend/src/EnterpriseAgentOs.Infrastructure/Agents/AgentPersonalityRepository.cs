namespace EnterpriseAgentOs.Infrastructure.Agents;

internal sealed class AgentPersonalityRepository : IAgentPersonalityRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentPersonalityRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<IReadOnlyList<AgentPersonalityRecord>> ListAsync(Guid agentId, CancellationToken ct = default)
        => await _eaosDbContext.AgentPersonalities
            .Where(p => p.AgentId == agentId)
            .OrderBy(p => p.FileName)
            .ToListAsync(ct);

    public async Task UpsertAsync(Guid agentId, string fileName, string content, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.AgentPersonalities
            .FirstOrDefaultAsync(p => p.AgentId == agentId && p.FileName == fileName, ct);

        if (existing is not null)
        {
            existing.UpdateContent(content);
        }
        else
        {
            _eaosDbContext.AgentPersonalities.Add(AgentPersonalityRecord.Create(agentId, fileName, content));
        }

        await _eaosDbContext.SaveChangesAsync(ct);
    }
}
