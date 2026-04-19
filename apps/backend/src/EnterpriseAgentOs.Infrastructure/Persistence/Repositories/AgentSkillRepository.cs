namespace EnterpriseAgentOs.Infrastructure.Persistence.Repositories;

public sealed class AgentSkillRepository : IAgentSkillRepository
{
    private readonly EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext _db;

    public AgentSkillRepository(EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.AgentSkillRecord>> ListByAgentAsync(Guid agentId, CancellationToken ct = default)
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

            _db.AgentSkills.Add(new EnterpriseAgentOs.Domain.Models.AgentSkillRecord
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

    public async Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.AgentToolPermissionRecord>> ListToolPermissionsAsync(Guid agentId, CancellationToken ct = default)
    {
        return await _db.AgentToolPermissions
            .AsNoTracking()
            .Where(p => p.AgentId == agentId)
            .ToListAsync(ct);
    }

    public async Task<EnterpriseAgentOs.Domain.Models.AgentToolPermissionRecord> UpsertToolPermissionAsync(
        Guid agentId, string skillName, string toolName,
        EnterpriseAgentOs.Domain.Models.ToolPermission permission, CancellationToken ct = default)
    {
        var skill = skillName.Trim().ToLowerInvariant();
        var tool = toolName.Trim();

        var existing = await _db.AgentToolPermissions
            .FirstOrDefaultAsync(p => p.AgentId == agentId && p.SkillName == skill && p.ToolName == tool, ct);

        if (existing is null)
        {
            existing = new EnterpriseAgentOs.Domain.Models.AgentToolPermissionRecord
            {
                AgentId = agentId,
                SkillName = skill,
                ToolName = tool,
                Permission = permission,
            };
            _db.AgentToolPermissions.Add(existing);
        }
        else
        {
            existing.Permission = permission;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
        return existing;
    }
}
