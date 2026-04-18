namespace EnterpriseAgentOs.Infrastructure.Persistence.Repositories;

public sealed class AgentTemplateRepository : IAgentTemplateRepository
{
    private readonly EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext _db;

    public AgentTemplateRepository(EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.AgentTemplateRecord>> ListAsync(CancellationToken ct = default) =>
        await _db.AgentTemplates.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);

    public async Task<EnterpriseAgentOs.Domain.Models.AgentTemplateRecord?> GetAsync(Guid id, CancellationToken ct = default) =>
        await _db.AgentTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<EnterpriseAgentOs.Domain.Models.AgentTemplateRecord?> GetByNameAsync(string name, CancellationToken ct = default) =>
        await _db.AgentTemplates.FirstOrDefaultAsync(t => t.Name == name, ct);

    public async Task<EnterpriseAgentOs.Domain.Models.AgentTemplateRecord> UpsertAsync(EnterpriseAgentOs.Domain.Models.AgentTemplateRecord record, CancellationToken ct = default)
    {
        var existing = await _db.AgentTemplates.FirstOrDefaultAsync(t => t.Name == record.Name, ct);
        if (existing is null)
        {
            _db.AgentTemplates.Add(record);
            await _db.SaveChangesAsync(ct);
            return record;
        }
        existing.Description = record.Description;
        existing.Prompt = record.Prompt;
        existing.IntegrationsJson = record.IntegrationsJson;
        existing.ChannelsJson = record.ChannelsJson;
        existing.IsBuiltin = record.IsBuiltin;
        existing.OwnerId = record.OwnerId;
        await _db.SaveChangesAsync(ct);
        return existing;
    }
}
