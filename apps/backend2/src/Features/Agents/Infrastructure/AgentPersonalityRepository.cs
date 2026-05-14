namespace OffceOs.Infrastructure.Features.Agents;

internal sealed class AgentPersonalityRepository : IAgentPersonalityRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentPersonalityRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<IReadOnlyList<AgentPersonalityRecord>> ListAsync(Guid agentId, CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.AgentPersonalities
            .Where(p => p.AgentId == agentId)
            .OrderBy(p => p.FileName)
            .ToListAsync(ct);
        return entities.Select(ToAgentPersonalityRecord).ToList();
    }

    public async Task UpsertAsync(Guid agentId, string fileName, string content, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.AgentPersonalities
            .FirstOrDefaultAsync(p => p.AgentId == agentId && p.FileName == fileName, ct);

        if (existing is not null)
        {
            existing.Content = content;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var record = AgentPersonalityRecord.Create(agentId, fileName, content);
            _eaosDbContext.AgentPersonalities.Add(ToAgentPersonalityEntity(record));
        }

        await _eaosDbContext.SaveChangesAsync(ct);
    }

    private static AgentPersonalityRecord ToAgentPersonalityRecord(AgentPersonalityEntity e) => new()
    {
        Id = e.Id,
        AgentId = e.AgentId,
        FileName = e.FileName,
        Content = e.Content,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        Agent = null!,
    };

    private static AgentPersonalityEntity ToAgentPersonalityEntity(AgentPersonalityRecord r) => new()
    {
        Id = r.Id,
        AgentId = r.AgentId,
        FileName = r.FileName,
        Content = r.Content,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
    };
}
