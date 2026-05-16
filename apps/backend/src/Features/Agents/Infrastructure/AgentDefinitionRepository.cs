using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Features.Agents.Domain;
namespace OffceOs.Features.Agents.Infrastructure;

internal sealed class AgentDefinitionRepository : IAgentDefinitionRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentDefinitionRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<AgentDefinitionRecord?> GetByAsync(AgentDefinitionFilter filter, CancellationToken ct = default)
    {
        var query = Query(filter);
        var entity = await query.OrderByDescending(definition => definition.Version).FirstOrDefaultAsync(ct);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<IReadOnlyList<AgentDefinitionRecord>> ListAsync(AgentDefinitionFilter filter, CancellationToken ct = default)
    {
        var entities = await Query(filter)
            .OrderByDescending(definition => definition.Version)
            .ToListAsync(ct);
        return entities.Select(ToRecord).ToList();
    }

    public async Task AddAsync(AgentDefinitionRecord definition, CancellationToken ct = default)
    {
        _eaosDbContext.AgentDefinitions.Add(ToEntity(definition));
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task<int> GetNextVersionAsync(Guid agentId, CancellationToken ct = default)
    {
        var max = await _eaosDbContext.AgentDefinitions.AsNoTracking()
            .Where(definition => definition.AgentId == agentId)
            .Select(definition => (int?)definition.Version)
            .MaxAsync(ct);
        return (max ?? 0) + 1;
    }

    private IQueryable<AgentDefinitionEntity> Query(AgentDefinitionFilter filter)
    {
        var query = _eaosDbContext.AgentDefinitions.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(definition => definition.Id == filter.Id.Value);

        if (filter.AgentId.HasValue)
            query = query.Where(definition => definition.AgentId == filter.AgentId.Value);

        if (filter.ActiveOnly)
        {
            query = query.Where(definition => _eaosDbContext.Agents
                .Any(agent => agent.Id == definition.AgentId && agent.ActiveDefinitionId == definition.Id));
        }

        return query;
    }

    private static AgentDefinitionRecord ToRecord(AgentDefinitionEntity entity) => new()
    {
        Id = entity.Id,
        AgentId = entity.AgentId,
        Version = entity.Version,
        Name = entity.Name,
        Description = entity.Description,
        Provider = entity.Provider,
        Model = entity.Model,
        SystemPrompt = entity.SystemPrompt,
        ConfigJson = entity.ConfigJson,
        ConfigHash = entity.ConfigHash,
        CreatedBy = entity.CreatedBy,
        CreatedAt = entity.CreatedAt,
    };

    private static AgentDefinitionEntity ToEntity(AgentDefinitionRecord record) => new()
    {
        Id = record.Id,
        AgentId = record.AgentId,
        Version = record.Version,
        Name = record.Name,
        Description = record.Description,
        Provider = record.Provider,
        Model = record.Model,
        SystemPrompt = record.SystemPrompt,
        ConfigJson = record.ConfigJson,
        ConfigHash = record.ConfigHash,
        CreatedBy = record.CreatedBy,
        CreatedAt = record.CreatedAt,
    };
}
