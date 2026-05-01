namespace EnterpriseAgentOs.Infrastructure.Features.Agents;

internal sealed class AgentToolPermissionRepository : IAgentToolPermissionRepository
{
    private readonly EaosDbContext _db;

    public AgentToolPermissionRepository(EaosDbContext db) => _db = db;

    public async Task<IReadOnlyList<AgentToolPermissionRecord>> ListForAgentAsync(Guid agentId, CancellationToken ct = default)
    {
        var entities = await _db.AgentToolPermissions.AsNoTracking()
            .Where(p => p.AgentId == agentId)
            .OrderBy(p => p.SkillName).ThenBy(p => p.ToolName)
            .ToListAsync(ct);

        return entities.Select(ToRecord).ToList();
    }

    public async Task UpsertAsync(Guid agentId, string skillName, string toolName, ToolPermission permission, CancellationToken ct = default)
    {
        skillName = Normalize(skillName);
        toolName = Normalize(toolName);
        var existing = await _db.AgentToolPermissions.FirstOrDefaultAsync(
            p => p.AgentId == agentId && p.SkillName == skillName && p.ToolName == toolName, ct);

        if (existing is null)
        {
            _db.AgentToolPermissions.Add(new AgentToolPermissionEntity
            {
                Id = Guid.NewGuid(),
                AgentId = agentId,
                SkillName = skillName,
                ToolName = toolName,
                Permission = permission,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.Permission = permission;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task SetManyAsync(Guid agentId, IReadOnlyList<AgentToolPermissionRecord> entries, CancellationToken ct = default)
    {
        var existing = await _db.AgentToolPermissions
            .Where(p => p.AgentId == agentId)
            .ToListAsync(ct);
        var byKey = existing.ToDictionary(p => Key(p.SkillName, p.ToolName), StringComparer.OrdinalIgnoreCase);
        var incomingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var skill = Normalize(entry.SkillName);
            var tool = Normalize(entry.ToolName);
            var key = Key(skill, tool);
            incomingKeys.Add(key);

            if (byKey.TryGetValue(key, out var entity))
            {
                entity.Permission = entry.Permission;
                entity.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.AgentToolPermissions.Add(new AgentToolPermissionEntity
                {
                    Id = Guid.NewGuid(),
                    AgentId = agentId,
                    SkillName = skill,
                    ToolName = tool,
                    Permission = entry.Permission,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
            }
        }

        foreach (var stale in existing.Where(p => !incomingKeys.Contains(Key(p.SkillName, p.ToolName))))
            _db.AgentToolPermissions.Remove(stale);

        await _db.SaveChangesAsync(ct);
    }

    private static AgentToolPermissionRecord ToRecord(AgentToolPermissionEntity e) => new()
    {
        Id = e.Id,
        AgentId = e.AgentId,
        SkillName = e.SkillName,
        ToolName = e.ToolName,
        Permission = e.Permission,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
    };

    private static string Normalize(string value) => value.Trim();
    private static string Key(string skill, string tool) => $"{skill}:{tool}";
}
