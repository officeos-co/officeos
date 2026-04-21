namespace EnterpriseAgentOs.Infrastructure.Agents;

internal sealed class AgentSkillRepository : IAgentSkillRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentSkillRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<IReadOnlyList<AgentSkillRecord>> ListByAgentAsync(Guid agentId, CancellationToken ct = default)
    {
        return await _eaosDbContext.AgentSkills
            .AsNoTracking()
            .Where(a => a.AgentId == agentId)
            .OrderBy(a => a.SkillName)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<string>> ListSkillNamesByAgentAsync(Guid agentId, CancellationToken ct = default)
    {
        return await _eaosDbContext.AgentSkills
            .AsNoTracking()
            .Where(a => a.AgentId == agentId)
            .Select(a => a.SkillName)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SkillRecord>> ListSkillDetailsForAgentAsync(Guid agentId, CancellationToken ct = default)
    {
        var skillNames = await _eaosDbContext.AgentSkills
            .AsNoTracking()
            .Where(a => a.AgentId == agentId)
            .Select(a => a.SkillName)
            .ToListAsync(ct);

        return await _eaosDbContext.Skills
            .AsNoTracking()
            .Where(s => skillNames.Contains(s.Name))
            .ToListAsync(ct);
    }

    public async Task AssignAsync(Guid agentId, IEnumerable<string> skillNames, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.AgentSkills
            .Where(a => a.AgentId == agentId)
            .Select(a => a.SkillName)
            .ToListAsync(ct);

        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        foreach (var name in skillNames)
        {
            var normalized = name.Trim().ToLowerInvariant();
            if (existingSet.Contains(normalized)) continue;

            _eaosDbContext.AgentSkills.Add(new AgentSkillRecord
            {
                AgentId = agentId,
                SkillName = normalized,
                EnabledAt = DateTimeOffset.UtcNow,
            });
        }

        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> RemoveAsync(Guid agentId, string skillName, CancellationToken ct = default)
    {
        var normalized = skillName.Trim().ToLowerInvariant();
        var row = await _eaosDbContext.AgentSkills
            .FirstOrDefaultAsync(a => a.AgentId == agentId && a.SkillName == normalized, ct);

        if (row is null) return false;

        _eaosDbContext.AgentSkills.Remove(row);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<AgentToolPermissionRecord>> ListToolPermissionsAsync(Guid agentId, CancellationToken ct = default)
    {
        return await _eaosDbContext.AgentToolPermissions
            .AsNoTracking()
            .Where(p => p.AgentId == agentId)
            .ToListAsync(ct);
    }

    public async Task<AgentToolPermissionRecord> UpsertToolPermissionAsync(
        Guid agentId, string skillName, string toolName,
        ToolPermission permission, CancellationToken ct = default)
    {
        var skill = skillName.Trim().ToLowerInvariant();
        var tool = toolName.Trim();

        var existing = await _eaosDbContext.AgentToolPermissions
            .FirstOrDefaultAsync(p => p.AgentId == agentId && p.SkillName == skill && p.ToolName == tool, ct);

        if (existing is null)
        {
            existing = new AgentToolPermissionRecord
            {
                AgentId = agentId,
                SkillName = skill,
                ToolName = tool,
                Permission = permission,
            };
            _eaosDbContext.AgentToolPermissions.Add(existing);
        }
        else
        {
            existing.Permission = permission;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await _eaosDbContext.SaveChangesAsync(ct);
        return existing;
    }
}
