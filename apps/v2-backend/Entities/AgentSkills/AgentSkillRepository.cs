namespace EnterpriseAgentOs.Api.Entities.AgentSkills;

public sealed class AgentSkillRepository : IAgentSkillRepository
{
    private readonly EaosDbContext _db;

    public AgentSkillRepository(EaosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AgentSkillRecord>> ListByAgentAsync(Guid agentId, CancellationToken ct = default)
    {
        return await _db.AgentSkills
            .AsNoTracking()
            .Where(a => a.AgentId == agentId)
            .OrderBy(a => a.SkillName)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<string>> ListSkillNamesByAgentAsync(Guid agentId, CancellationToken ct = default)
    {
        return await _db.AgentSkills
            .AsNoTracking()
            .Where(a => a.AgentId == agentId)
            .Select(a => a.SkillName)
            .ToListAsync(ct);
    }

    public async Task AssignAsync(Guid agentId, IEnumerable<string> skillNames, CancellationToken ct = default)
    {
        var existing = await _db.AgentSkills
            .Where(a => a.AgentId == agentId)
            .Select(a => a.SkillName)
            .ToListAsync(ct);

        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        foreach (var name in skillNames)
        {
            var normalized = name.Trim().ToLowerInvariant();
            if (existingSet.Contains(normalized)) continue;

            _db.AgentSkills.Add(new AgentSkillRecord
            {
                AgentId = agentId,
                SkillName = normalized,
                EnabledAt = DateTimeOffset.UtcNow,
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> RemoveAsync(Guid agentId, string skillName, CancellationToken ct = default)
    {
        var normalized = skillName.Trim().ToLowerInvariant();
        var row = await _db.AgentSkills
            .FirstOrDefaultAsync(a => a.AgentId == agentId && a.SkillName == normalized, ct);

        if (row is null) return false;

        _db.AgentSkills.Remove(row);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
