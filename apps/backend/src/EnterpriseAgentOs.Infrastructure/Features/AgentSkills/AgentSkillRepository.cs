namespace EnterpriseAgentOs.Infrastructure.Features.AgentSkills;

internal sealed class AgentSkillRepository : IAgentSkillRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentSkillRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<IReadOnlyList<AgentSkillRecord>> ListByAgentAsync(Guid agentId, CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.AgentSkills
            .AsNoTracking()
            .Where(a => a.AgentId == agentId)
            .OrderBy(a => a.SkillName)
            .ToListAsync(ct);
        return entities.Select(ToAgentSkillRecord).ToList();
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

        var entities = await _eaosDbContext.Skills
            .AsNoTracking()
            .Where(s => skillNames.Contains(s.Name))
            .ToListAsync(ct);
        return entities.Select(ToSkillRecord).ToList();
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

            _eaosDbContext.AgentSkills.Add(ToAgentSkillEntity(new AgentSkillRecord
            {
                AgentId = agentId,
                SkillName = normalized,
                EnabledAt = DateTimeOffset.UtcNow,
            }));
        }

        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> RemoveAsync(Guid agentId, string skillName, CancellationToken ct = default)
    {
        var normalized = skillName.Trim().ToLowerInvariant();
        var entity = await _eaosDbContext.AgentSkills
            .FirstOrDefaultAsync(a => a.AgentId == agentId && a.SkillName == normalized, ct);

        if (entity is null) return false;

        _eaosDbContext.AgentSkills.Remove(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<AgentToolPermissionRecord>> ListToolPermissionsAsync(Guid agentId, CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.AgentToolPermissions
            .AsNoTracking()
            .Where(p => p.AgentId == agentId)
            .ToListAsync(ct);
        return entities.Select(ToAgentToolPermissionRecord).ToList();
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
            var record = new AgentToolPermissionRecord
            {
                AgentId = agentId,
                SkillName = skill,
                ToolName = tool,
                Permission = permission,
            };
            _eaosDbContext.AgentToolPermissions.Add(ToAgentToolPermissionEntity(record));
            await _eaosDbContext.SaveChangesAsync(ct);
            return record;
        }
        else
        {
            existing.Permission = permission;
            existing.UpdatedAt = DateTime.UtcNow;
            await _eaosDbContext.SaveChangesAsync(ct);
            return ToAgentToolPermissionRecord(existing);
        }
    }

    internal static SkillRecord ToSkillRecord(SkillEntity e) => new()
    {
        Id = e.Id, Name = e.Name, Title = e.Title, Description = e.Description,
        Doc = e.Doc, Source = e.Source, Logo = e.Logo, License = e.License,
        Repository = e.Repository, RequiresApproval = e.RequiresApproval,
        Readme = e.Readme, Changelog = e.Changelog, Category = e.Category,
        AuthorName = e.AuthorName, AuthorUrl = e.AuthorUrl,
        Categories = e.Categories, Keywords = e.Keywords,
        ActionsJson = e.ActionsJson, CredentialFieldsJson = e.CredentialFieldsJson,
        ContributorsJson = e.ContributorsJson, BundleS3Key = e.BundleS3Key,
        Version = e.Version, Status = e.Status, BuildError = e.BuildError,
        GitHubRepoUrl = e.GitHubRepoUrl, GitHubBranch = e.GitHubBranch,
        IsSystem = e.IsSystem, OwnerId = e.OwnerId,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
    };

    private static AgentSkillRecord ToAgentSkillRecord(AgentSkillEntity e) => new()
    {
        Id = e.Id,
        AgentId = e.AgentId,
        SkillName = e.SkillName,
        EnabledAt = e.EnabledAt,
    };

    private static AgentSkillEntity ToAgentSkillEntity(AgentSkillRecord r) => new()
    {
        Id = r.Id,
        AgentId = r.AgentId,
        SkillName = r.SkillName,
        EnabledAt = r.EnabledAt,
    };

    private static AgentToolPermissionRecord ToAgentToolPermissionRecord(AgentToolPermissionEntity e) => new()
    {
        Id = e.Id,
        AgentId = e.AgentId,
        SkillName = e.SkillName,
        ToolName = e.ToolName,
        Permission = e.Permission,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        Agent = null,
    };

    private static AgentToolPermissionEntity ToAgentToolPermissionEntity(AgentToolPermissionRecord r) => new()
    {
        Id = r.Id,
        AgentId = r.AgentId,
        SkillName = r.SkillName,
        ToolName = r.ToolName,
        Permission = r.Permission,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
    };
}
