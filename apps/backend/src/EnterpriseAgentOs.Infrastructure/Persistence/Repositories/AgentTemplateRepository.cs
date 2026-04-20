namespace EnterpriseAgentOs.Infrastructure.Persistence.Repositories;

public sealed class AgentTemplateRepository : IAgentTemplateRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentTemplateRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<IReadOnlyList<AgentTemplateRecord>> ListAsync(CancellationToken ct = default) =>
        await _eaosDbContext.AgentTemplates.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);

    public async Task<AgentTemplateRecord?> GetAsync(Guid id, CancellationToken ct = default) =>
        await _eaosDbContext.AgentTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<AgentTemplateRecord?> GetByNameAsync(string name, CancellationToken ct = default) =>
        await _eaosDbContext.AgentTemplates.FirstOrDefaultAsync(t => t.Name == name, ct);

    public async Task<AgentTemplateRecord> UpsertAsync(AgentTemplateRecord record, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.AgentTemplates.FirstOrDefaultAsync(t => t.Name == record.Name, ct);
        if (existing is null)
        {
            _eaosDbContext.AgentTemplates.Add(record);
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
        return existing;
    }
}
