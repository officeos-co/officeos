using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Features.Agents;
namespace OffceOs.Infrastructure.Features.Agents;

internal sealed class AgentSessionContextRepository : IAgentSessionContextRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentSessionContextRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<AgentSessionContextRecord?> GetByAsync(AgentSessionContextFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.AgentSessionContexts.AsNoTracking().AsQueryable();

        if (filter.AgentId.HasValue)
            query = query.Where(c => c.AgentId == filter.AgentId.Value);

        var entity = await query.FirstOrDefaultAsync(ct);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task UpsertAsync(AgentSessionContextRecord context, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentSessionContexts
            .FirstOrDefaultAsync(c => c.AgentId == context.AgentId, ct);

        if (entity is null)
        {
            _eaosDbContext.AgentSessionContexts.Add(ToEntity(context));
        }
        else
        {
            entity.Summary = context.Summary;
            entity.LastCompactedLogId = context.LastCompactedLogId;
            entity.LastCompactedAt = context.LastCompactedAt;
            entity.PreCompactTokens = context.PreCompactTokens;
            entity.PostCompactTokens = context.PostCompactTokens;
            entity.CompactionVersion = context.CompactionVersion;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await _eaosDbContext.SaveChangesAsync(ct);
    }

    private static AgentSessionContextRecord ToRecord(AgentSessionContextEntity e) => new()
    {
        Id = e.Id,
        AgentId = e.AgentId,
        Summary = e.Summary,
        LastCompactedLogId = e.LastCompactedLogId,
        LastCompactedAt = e.LastCompactedAt,
        PreCompactTokens = e.PreCompactTokens,
        PostCompactTokens = e.PostCompactTokens,
        CompactionVersion = e.CompactionVersion,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
    };

    private static AgentSessionContextEntity ToEntity(AgentSessionContextRecord r) => new()
    {
        Id = r.Id,
        AgentId = r.AgentId,
        Summary = r.Summary,
        LastCompactedLogId = r.LastCompactedLogId,
        LastCompactedAt = r.LastCompactedAt,
        PreCompactTokens = r.PreCompactTokens,
        PostCompactTokens = r.PostCompactTokens,
        CompactionVersion = r.CompactionVersion,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
    };
}
