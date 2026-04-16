namespace EnterpriseAgentOs.Api.Entities.Channels;

public sealed class ChannelRepository : IChannelRepository
{
    private readonly EnterpriseAgentOs.Api.Database.EaosDbContext _db;

    public ChannelRepository(EnterpriseAgentOs.Api.Database.EaosDbContext db)
    {
        _db = db;
    }

    // ---------- Channel Connections ----------

    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.Database.Models.ChannelConnectionRecord>> ListConnectionsAsync(CancellationToken ct = default)
    {
        return await _db.ChannelConnections
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<EnterpriseAgentOs.Api.Database.Models.ChannelConnectionRecord?> GetConnectionAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.ChannelConnections.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<EnterpriseAgentOs.Api.Database.Models.ChannelConnectionRecord> CreateConnectionAsync(EnterpriseAgentOs.Api.Database.Models.ChannelConnectionRecord record, CancellationToken ct = default)
    {
        _db.ChannelConnections.Add(record);
        await _db.SaveChangesAsync(ct);
        return record;
    }

    public async Task<EnterpriseAgentOs.Api.Database.Models.ChannelConnectionRecord?> UpdateConnectionAsync(Guid id, Action<EnterpriseAgentOs.Api.Database.Models.ChannelConnectionRecord> apply, CancellationToken ct = default)
    {
        var row = await _db.ChannelConnections.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (row is null) return null;
        apply(row);
        await _db.SaveChangesAsync(ct);
        return row;
    }

    public async Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default)
    {
        var row = await _db.ChannelConnections.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (row is null) return false;

        // Also remove all bindings referencing this connection
        var bindings = await _db.AgentChannelBindings
            .Where(b => b.ChannelConnectionId == id)
            .ToListAsync(ct);
        _db.AgentChannelBindings.RemoveRange(bindings);

        _db.ChannelConnections.Remove(row);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ---------- Agent Channel Bindings ----------

    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.Database.Models.AgentChannelBindingRecord>> ListBindingsAsync(Guid agentId, CancellationToken ct = default)
    {
        return await _db.AgentChannelBindings
            .AsNoTracking()
            .Include(b => b.ChannelConnection)
            .Where(b => b.AgentId == agentId)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<EnterpriseAgentOs.Api.Database.Models.AgentChannelBindingRecord?> GetBindingAsync(Guid bindingId, CancellationToken ct = default)
    {
        return await _db.AgentChannelBindings
            .Include(b => b.ChannelConnection)
            .FirstOrDefaultAsync(b => b.Id == bindingId, ct);
    }

    public async Task<EnterpriseAgentOs.Api.Database.Models.AgentChannelBindingRecord> CreateBindingAsync(EnterpriseAgentOs.Api.Database.Models.AgentChannelBindingRecord record, CancellationToken ct = default)
    {
        _db.AgentChannelBindings.Add(record);
        await _db.SaveChangesAsync(ct);

        // Reload with connection included
        return (await _db.AgentChannelBindings
            .Include(b => b.ChannelConnection)
            .FirstAsync(b => b.Id == record.Id, ct));
    }

    public async Task<EnterpriseAgentOs.Api.Database.Models.AgentChannelBindingRecord?> UpdateBindingAsync(Guid bindingId, Action<EnterpriseAgentOs.Api.Database.Models.AgentChannelBindingRecord> apply, CancellationToken ct = default)
    {
        var row = await _db.AgentChannelBindings
            .Include(b => b.ChannelConnection)
            .FirstOrDefaultAsync(b => b.Id == bindingId, ct);
        if (row is null) return null;
        apply(row);
        await _db.SaveChangesAsync(ct);
        return row;
    }

    public async Task<bool> DeleteBindingAsync(Guid bindingId, CancellationToken ct = default)
    {
        var row = await _db.AgentChannelBindings.FirstOrDefaultAsync(b => b.Id == bindingId, ct);
        if (row is null) return false;
        _db.AgentChannelBindings.Remove(row);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ---------- Routing queries ----------

    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.Database.Models.AgentChannelBindingRecord>> FindBindingsByConnectionAsync(Guid connectionId, CancellationToken ct = default)
    {
        return await _db.AgentChannelBindings
            .AsNoTracking()
            .Include(b => b.Agent)
            .Include(b => b.ChannelConnection)
            .Where(b => b.ChannelConnectionId == connectionId && b.Enabled)
            .ToListAsync(ct);
    }
}
