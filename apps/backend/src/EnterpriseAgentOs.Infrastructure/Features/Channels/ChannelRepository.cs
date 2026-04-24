namespace EnterpriseAgentOs.Infrastructure.Features.Channels;

internal sealed class ChannelRepository : IChannelRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public ChannelRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    // ---------- Channel Connections ----------

    public async Task<IReadOnlyList<ChannelConnectionRecord>> ListConnectionsAsync(CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.ChannelConnections
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
        return entities.Select(ToChannelConnectionRecord).ToList();
    }

    public async Task<ChannelConnectionRecord?> GetConnectionAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.ChannelConnections.FirstOrDefaultAsync(c => c.Id == id, ct);
        return entity is null ? null : ToChannelConnectionRecord(entity);
    }

    public async Task<ChannelConnectionRecord> CreateConnectionAsync(ChannelConnectionRecord record, CancellationToken ct = default)
    {
        var entity = ToChannelConnectionEntity(record);
        _eaosDbContext.ChannelConnections.Add(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToChannelConnectionRecord(entity);
    }

    public async Task<ChannelConnectionRecord?> UpdateConnectionAsync(Guid id, Action<ChannelConnectionRecord> apply, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.ChannelConnections.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null) return null;
        var record = ToChannelConnectionRecord(entity);
        apply(record);
        MapToChannelConnectionEntity(record, entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return record;
    }

    public async Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.ChannelConnections.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null) return false;

        // Also remove all bindings referencing this connection
        var bindings = await _eaosDbContext.AgentChannelBindings
            .Where(b => b.ChannelConnectionId == id)
            .ToListAsync(ct);
        _eaosDbContext.AgentChannelBindings.RemoveRange(bindings);

        _eaosDbContext.ChannelConnections.Remove(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    // ---------- Agent Channel Bindings ----------

    public async Task<IReadOnlyList<AgentChannelBindingRecord>> ListBindingsAsync(Guid agentId, CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.AgentChannelBindings
            .AsNoTracking()
            .Include(b => b.ChannelConnection)
            .Where(b => b.AgentId == agentId)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync(ct);
        return entities.Select(ToAgentChannelBindingRecord).ToList();
    }

    public async Task<AgentChannelBindingRecord?> GetBindingAsync(Guid bindingId, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentChannelBindings
            .Include(b => b.ChannelConnection)
            .FirstOrDefaultAsync(b => b.Id == bindingId, ct);
        return entity is null ? null : ToAgentChannelBindingRecord(entity);
    }

    public async Task<AgentChannelBindingRecord> CreateBindingAsync(AgentChannelBindingRecord record, CancellationToken ct = default)
    {
        var entity = ToAgentChannelBindingEntity(record);
        _eaosDbContext.AgentChannelBindings.Add(entity);
        await _eaosDbContext.SaveChangesAsync(ct);

        // Reload with connection included
        var reloaded = await _eaosDbContext.AgentChannelBindings
            .Include(b => b.ChannelConnection)
            .FirstAsync(b => b.Id == entity.Id, ct);
        return ToAgentChannelBindingRecord(reloaded);
    }

    public async Task<AgentChannelBindingRecord?> UpdateBindingAsync(Guid bindingId, Action<AgentChannelBindingRecord> apply, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentChannelBindings
            .Include(b => b.ChannelConnection)
            .FirstOrDefaultAsync(b => b.Id == bindingId, ct);
        if (entity is null) return null;
        var record = ToAgentChannelBindingRecord(entity);
        apply(record);
        MapToAgentChannelBindingEntity(record, entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return record;
    }

    public async Task<bool> DeleteBindingAsync(Guid bindingId, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentChannelBindings.FirstOrDefaultAsync(b => b.Id == bindingId, ct);
        if (entity is null) return false;
        _eaosDbContext.AgentChannelBindings.Remove(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    // ---------- Routing queries ----------

    public async Task<IReadOnlyList<AgentChannelBindingRecord>> FindBindingsByConnectionAsync(Guid connectionId, CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.AgentChannelBindings
            .AsNoTracking()
            .Include(b => b.Agent)
            .Include(b => b.ChannelConnection)
            .Where(b => b.ChannelConnectionId == connectionId && b.Enabled)
            .ToListAsync(ct);
        return entities.Select(ToAgentChannelBindingRecord).ToList();
    }

    // ── Mapping ──────────────────────────────────────────────────────

    private static ChannelConnectionRecord ToChannelConnectionRecord(ChannelConnectionEntity e) => new()
    {
        Id = e.Id,
        ChannelType = e.ChannelType,
        DisplayName = e.DisplayName,
        Enabled = e.Enabled,
        CreatedAt = e.CreatedAt,
        CreatedById = e.CreatedById,
    };

    private static ChannelConnectionEntity ToChannelConnectionEntity(ChannelConnectionRecord r) => new()
    {
        Id = r.Id,
        ChannelType = r.ChannelType,
        DisplayName = r.DisplayName,
        Enabled = r.Enabled,
        CreatedAt = r.CreatedAt,
        CreatedById = r.CreatedById,
    };

    private static void MapToChannelConnectionEntity(ChannelConnectionRecord r, ChannelConnectionEntity e)
    {
        e.ChannelType = r.ChannelType;
        e.DisplayName = r.DisplayName;
        e.Enabled = r.Enabled;
        e.CreatedById = r.CreatedById;
    }

    private static AgentChannelBindingRecord ToAgentChannelBindingRecord(AgentChannelBindingEntity e) => new()
    {
        Id = e.Id,
        AgentId = e.AgentId,
        ChannelConnectionId = e.ChannelConnectionId,
        Enabled = e.Enabled,
        Config = e.Config,
        CreatedAt = e.CreatedAt,
        Agent = e.Agent is not null ? AgentRepository.ToAgentRecord(e.Agent) : null,
        ChannelConnection = e.ChannelConnection is not null ? ToChannelConnectionRecord(e.ChannelConnection) : null,
    };

    private static AgentChannelBindingEntity ToAgentChannelBindingEntity(AgentChannelBindingRecord r) => new()
    {
        Id = r.Id,
        AgentId = r.AgentId,
        ChannelConnectionId = r.ChannelConnectionId,
        Enabled = r.Enabled,
        Config = r.Config,
        CreatedAt = r.CreatedAt,
    };

    private static void MapToAgentChannelBindingEntity(AgentChannelBindingRecord r, AgentChannelBindingEntity e)
    {
        e.AgentId = r.AgentId;
        e.ChannelConnectionId = r.ChannelConnectionId;
        e.Enabled = r.Enabled;
        e.Config = r.Config;
    }
}
