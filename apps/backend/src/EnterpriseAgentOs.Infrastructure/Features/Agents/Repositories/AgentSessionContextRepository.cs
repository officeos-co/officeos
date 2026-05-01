namespace EnterpriseAgentOs.Infrastructure.Features.Agents;

internal sealed class AgentSessionContextRepository : IAgentSessionContextRepository
{
    private readonly EaosDbContext _db;

    public AgentSessionContextRepository(EaosDbContext db) => _db = db;

    public async Task<AgentSessionContextRecord?> GetAsync(Guid agentId, CancellationToken ct = default)
    {
        var entity = await _db.AgentSessionContexts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.AgentId == agentId, ct);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task UpsertAsync(AgentSessionContextRecord context, CancellationToken ct = default)
    {
        var entity = await _db.AgentSessionContexts
            .FirstOrDefaultAsync(c => c.AgentId == context.AgentId, ct);

        if (entity is null)
        {
            _db.AgentSessionContexts.Add(ToEntity(context));
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

        await _db.SaveChangesAsync(ct);
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
