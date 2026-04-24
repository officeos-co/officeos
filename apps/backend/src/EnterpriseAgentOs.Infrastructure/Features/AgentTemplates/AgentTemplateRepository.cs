namespace EnterpriseAgentOs.Infrastructure.Features.AgentTemplates;

internal sealed class AgentTemplateRepository : IAgentTemplateRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentTemplateRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<IReadOnlyList<AgentTemplateRecord>> ListAsync(CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.AgentTemplates.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);
        return entities.Select(ToAgentTemplateRecord).ToList();
    }

    public async Task<AgentTemplateRecord?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
        return entity is null ? null : ToAgentTemplateRecord(entity);
    }

    public async Task<AgentTemplateRecord?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentTemplates.FirstOrDefaultAsync(t => t.Name == name, ct);
        return entity is null ? null : ToAgentTemplateRecord(entity);
    }

    public async Task<AgentTemplateRecord> UpsertAsync(AgentTemplateRecord record, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.AgentTemplates.FirstOrDefaultAsync(t => t.Name == record.Name, ct);
        if (existing is null)
        {
            _eaosDbContext.AgentTemplates.Add(ToAgentTemplateEntity(record));
            await _eaosDbContext.SaveChangesAsync(ct);
            return record;
        }
        existing.Description = record.Description;
        existing.Prompt = record.Prompt;
        existing.IntegrationsJson = record.IntegrationsJson;
        existing.ChannelsJson = record.ChannelsJson;
        existing.IsBuiltin = record.IsBuiltin;
        existing.OwnerId = record.OwnerId;
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToAgentTemplateRecord(existing);
    }

    private static AgentTemplateRecord ToAgentTemplateRecord(AgentTemplateEntity e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Description = e.Description,
        Prompt = e.Prompt,
        IntegrationsJson = e.IntegrationsJson,
        ChannelsJson = e.ChannelsJson,
        IsBuiltin = e.IsBuiltin,
        OwnerId = e.OwnerId,
        CreatedAt = e.CreatedAt,
        Owner = null,
    };

    private static AgentTemplateEntity ToAgentTemplateEntity(AgentTemplateRecord r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        Prompt = r.Prompt,
        IntegrationsJson = r.IntegrationsJson,
        ChannelsJson = r.ChannelsJson,
        IsBuiltin = r.IsBuiltin,
        OwnerId = r.OwnerId,
        CreatedAt = r.CreatedAt,
    };
}
