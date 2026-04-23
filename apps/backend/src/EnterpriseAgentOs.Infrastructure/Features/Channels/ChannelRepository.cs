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
        return await _eaosDbContext.ChannelConnections
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<ChannelConnectionRecord?> GetConnectionAsync(Guid id, CancellationToken ct = default)
    {
        return await _eaosDbContext.ChannelConnections.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<ChannelConnectionRecord> CreateConnectionAsync(ChannelConnectionRecord record, CancellationToken ct = default)
    {
        _eaosDbContext.ChannelConnections.Add(record);
        await _eaosDbContext.SaveChangesAsync(ct);
        return record;
    }

    public async Task<ChannelConnectionRecord?> UpdateConnectionAsync(Guid id, Action<ChannelConnectionRecord> apply, CancellationToken ct = default)
    {
        var row = await _eaosDbContext.ChannelConnections.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (row is null) return null;
        apply(row);
        await _eaosDbContext.SaveChangesAsync(ct);
        return row;
    }

    public async Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default)
    {
        var row = await _eaosDbContext.ChannelConnections.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (row is null) return false;

        // Also remove all bindings referencing this connection
        var bindings = await _eaosDbContext.AgentChannelBindings
            .Where(b => b.ChannelConnectionId == id)
            .ToListAsync(ct);
        _eaosDbContext.AgentChannelBindings.RemoveRange(bindings);

        _eaosDbContext.ChannelConnections.Remove(row);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    // ---------- Agent Channel Bindings ----------

    public async Task<IReadOnlyList<AgentChannelBindingRecord>> ListBindingsAsync(Guid agentId, CancellationToken ct = default)
    {
        return await _eaosDbContext.AgentChannelBindings
            .AsNoTracking()
            .Include(b => b.ChannelConnection)
            .Where(b => b.AgentId == agentId)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<AgentChannelBindingRecord?> GetBindingAsync(Guid bindingId, CancellationToken ct = default)
    {
        return await _eaosDbContext.AgentChannelBindings
            .Include(b => b.ChannelConnection)
            .FirstOrDefaultAsync(b => b.Id == bindingId, ct);
    }

    public async Task<AgentChannelBindingRecord> CreateBindingAsync(AgentChannelBindingRecord record, CancellationToken ct = default)
    {
        _eaosDbContext.AgentChannelBindings.Add(record);
        await _eaosDbContext.SaveChangesAsync(ct);

        // Reload with connection included
        return (await _eaosDbContext.AgentChannelBindings
            .Include(b => b.ChannelConnection)
            .FirstAsync(b => b.Id == record.Id, ct));
    }

    public async Task<AgentChannelBindingRecord?> UpdateBindingAsync(Guid bindingId, Action<AgentChannelBindingRecord> apply, CancellationToken ct = default)
    {
        var row = await _eaosDbContext.AgentChannelBindings
            .Include(b => b.ChannelConnection)
            .FirstOrDefaultAsync(b => b.Id == bindingId, ct);
        if (row is null) return null;
        apply(row);
        await _eaosDbContext.SaveChangesAsync(ct);
        return row;
    }

    public async Task<bool> DeleteBindingAsync(Guid bindingId, CancellationToken ct = default)
    {
        var row = await _eaosDbContext.AgentChannelBindings.FirstOrDefaultAsync(b => b.Id == bindingId, ct);
        if (row is null) return false;
        _eaosDbContext.AgentChannelBindings.Remove(row);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    // ---------- Routing queries ----------

    public async Task<IReadOnlyList<AgentChannelBindingRecord>> FindBindingsByConnectionAsync(Guid connectionId, CancellationToken ct = default)
    {
        return await _eaosDbContext.AgentChannelBindings
            .AsNoTracking()
            .Include(b => b.Agent)
            .Include(b => b.ChannelConnection)
            .Where(b => b.ChannelConnectionId == connectionId && b.Enabled)
            .ToListAsync(ct);
    }
}
