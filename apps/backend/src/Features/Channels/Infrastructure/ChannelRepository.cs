namespace OffceOs.Infrastructure.Features.Channels;

internal sealed class ChannelRepository : IChannelRepository
{
    private static readonly string[] SupportedChannelTypes =
    [
        ChannelType.Internal.ToStorageString(),
        ChannelType.Slack.ToStorageString(),
        ChannelType.Telegram.ToStorageString(),
    ];

    private readonly EaosDbContext _eaosDbContext;

    public ChannelRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    // ---------- Channel Connections ----------

    public async Task<IReadOnlyList<ChannelConnectionRecord>> ListConnectionsAsync(ChannelConnectionFilter? filter = null, CancellationToken ct = default)
    {
        var query = _eaosDbContext.ChannelConnections.AsNoTracking()
            .Where(c => SupportedChannelTypes.Contains(c.ChannelType));

        if (filter?.Id is { } id)
            query = query.Where(c => c.Id == id);

        if (!string.IsNullOrEmpty(filter?.ChannelType))
            query = query.Where(c => c.ChannelType == filter.ChannelType);

        if (filter?.CreatedById is { } createdById)
            query = query.Where(c => c.CreatedById == createdById);

        if (filter?.WorkspaceId is { } workspaceId)
            query = query.Where(c => c.WorkspaceId == workspaceId);

        var entities = await query.OrderBy(c => c.CreatedAt).ToListAsync(ct);
        return entities.Select(ToChannelConnectionRecord).ToList();
    }

    public async Task<ChannelConnectionRecord?> GetConnectionByAsync(ChannelConnectionFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.ChannelConnections
            .Where(c => SupportedChannelTypes.Contains(c.ChannelType))
            .AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(c => c.Id == filter.Id.Value);

        if (!string.IsNullOrEmpty(filter.ChannelType))
            query = query.Where(c => c.ChannelType == filter.ChannelType);

        if (filter.CreatedById.HasValue)
            query = query.Where(c => c.CreatedById == filter.CreatedById.Value);

        if (filter.WorkspaceId.HasValue)
            query = query.Where(c => c.WorkspaceId == filter.WorkspaceId.Value);

        var entity = await query.FirstOrDefaultAsync(ct);
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
            .Where(b => b.ChannelConnection != null && SupportedChannelTypes.Contains(b.ChannelConnection.ChannelType))
            .OrderBy(b => b.CreatedAt)
            .ToListAsync(ct);
        return entities.Select(ToAgentChannelBindingRecord).ToList();
    }

    public async Task<AgentChannelBindingRecord?> GetBindingByAsync(AgentChannelBindingFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.AgentChannelBindings
            .Include(b => b.ChannelConnection)
            .Where(b => b.ChannelConnection != null && SupportedChannelTypes.Contains(b.ChannelConnection.ChannelType))
            .AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(b => b.Id == filter.Id.Value);

        if (filter.AgentId.HasValue)
            query = query.Where(b => b.AgentId == filter.AgentId.Value);

        if (filter.ChannelConnectionId.HasValue)
            query = query.Where(b => b.ChannelConnectionId == filter.ChannelConnectionId.Value);

        var entity = await query.FirstOrDefaultAsync(ct);
        return entity is null ? null : ToAgentChannelBindingRecord(entity);
    }

    public async Task<AgentChannelBindingRecord> CreateBindingAsync(AgentChannelBindingRecord record, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.AgentChannelBindings
            .AsNoTracking()
            .Include(b => b.ChannelConnection)
            .Where(b => b.ChannelConnection != null && SupportedChannelTypes.Contains(b.ChannelConnection.ChannelType))
            .FirstOrDefaultAsync(
                b => b.AgentId == record.AgentId
                    && b.ChannelConnectionId == record.ChannelConnectionId,
                ct);
        if (existing is not null)
            return ToAgentChannelBindingRecord(existing);

        var entity = ToAgentChannelBindingEntity(record);
        _eaosDbContext.AgentChannelBindings.Add(entity);
        try
        {
            await _eaosDbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            _eaosDbContext.Entry(entity).State = EntityState.Detached;

            existing = await _eaosDbContext.AgentChannelBindings
                .AsNoTracking()
                .Include(b => b.ChannelConnection)
                .Where(b => b.ChannelConnection != null && SupportedChannelTypes.Contains(b.ChannelConnection.ChannelType))
                .FirstOrDefaultAsync(
                    b => b.AgentId == record.AgentId
                        && b.ChannelConnectionId == record.ChannelConnectionId,
                    ct);
            if (existing is null)
                throw;

            return ToAgentChannelBindingRecord(existing);
        }

        // Reload with connection included
        var reloaded = await _eaosDbContext.AgentChannelBindings
            .AsNoTracking()
            .Include(b => b.ChannelConnection)
            .Where(b => b.ChannelConnection != null && SupportedChannelTypes.Contains(b.ChannelConnection.ChannelType))
            .FirstAsync(b => b.Id == entity.Id, ct);
        return ToAgentChannelBindingRecord(reloaded);
    }

    public async Task<AgentChannelBindingRecord?> UpdateBindingAsync(Guid bindingId, Action<AgentChannelBindingRecord> apply, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentChannelBindings
            .Include(b => b.ChannelConnection)
            .Where(b => b.ChannelConnection != null && SupportedChannelTypes.Contains(b.ChannelConnection.ChannelType))
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
            .Where(b => b.ChannelConnectionId == connectionId)
            .Where(b => b.ChannelConnection != null && SupportedChannelTypes.Contains(b.ChannelConnection.ChannelType))
            .OrderBy(b => b.CreatedAt)
            .ToListAsync(ct);
        return entities.Select(ToAgentChannelBindingRecord).ToList();
    }

    // ── Mapping ──────────────────────────────────────────────────────

    private static ChannelConnectionRecord ToChannelConnectionRecord(ChannelConnectionEntity e) => new()
    {
        Id = e.Id,
        ChannelType = e.ChannelType.ToChannelType(),
        DisplayName = e.DisplayName,
        Enabled = e.Enabled,
        CreatedAt = e.CreatedAt,
        CreatedById = e.CreatedById,
        WorkspaceId = e.WorkspaceId,
        EncryptedCreds = e.EncryptedCreds,
    };

    private static ChannelConnectionEntity ToChannelConnectionEntity(ChannelConnectionRecord r) => new()
    {
        Id = r.Id,
        ChannelType = r.ChannelType.ToStorageString(),
        DisplayName = r.DisplayName,
        Enabled = r.Enabled,
        CreatedAt = r.CreatedAt,
        CreatedById = r.CreatedById,
        WorkspaceId = r.WorkspaceId,
        EncryptedCreds = r.EncryptedCreds,
    };

    private static void MapToChannelConnectionEntity(ChannelConnectionRecord r, ChannelConnectionEntity e)
    {
        e.ChannelType = r.ChannelType.ToStorageString();
        e.DisplayName = r.DisplayName;
        e.Enabled = r.Enabled;
        e.CreatedById = r.CreatedById;
        e.WorkspaceId = r.WorkspaceId;
        e.EncryptedCreds = r.EncryptedCreds;
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
