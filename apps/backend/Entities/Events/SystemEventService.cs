namespace EnterpriseAgentOs.Api.Entities.Events;

public interface ISystemEventService
{
    Task<EnterpriseAgentOs.Api.Database.Models.SystemEventRecord> RecordAsync(EnterpriseAgentOs.Api.Database.Models.SystemEventRecord ev, CancellationToken ct = default);
    Task<IReadOnlyList<EnterpriseAgentOs.Api.Database.Models.SystemEventRecord>> ListRecentAsync(
        int limit = 50, string? severity = null, string? category = null, CancellationToken ct = default);
    Task AcknowledgeAsync(Guid id, CancellationToken ct = default);
}

public sealed class SystemEventService : ISystemEventService
{
    private readonly EnterpriseAgentOs.Api.Database.EaosDbContext _db;
    private readonly SystemEventBroadcaster _broadcaster;
    private readonly ILogger<SystemEventService> _logger;

    public SystemEventService(EnterpriseAgentOs.Api.Database.EaosDbContext db, SystemEventBroadcaster broadcaster, ILogger<SystemEventService> logger)
    {
        _db = db;
        _broadcaster = broadcaster;
        _logger = logger;
    }

    public async Task<EnterpriseAgentOs.Api.Database.Models.SystemEventRecord> RecordAsync(EnterpriseAgentOs.Api.Database.Models.SystemEventRecord ev, CancellationToken ct = default)
    {
        _db.SystemEvents.Add(ev);
        await _db.SaveChangesAsync(ct);
        _broadcaster.Publish(ev);
        _logger.LogInformation("System event recorded: [{Severity}] {Category} — {Message}",
            ev.Severity, ev.Category, ev.Message);
        return ev;
    }

    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.Database.Models.SystemEventRecord>> ListRecentAsync(
        int limit = 50, string? severity = null, string? category = null, CancellationToken ct = default)
    {
        var query = _db.SystemEvents.AsQueryable();

        if (severity is not null)
            query = query.Where(e => e.Severity == severity);
        if (category is not null)
            query = query.Where(e => e.Category == category);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task AcknowledgeAsync(Guid id, CancellationToken ct = default)
    {
        var ev = await _db.SystemEvents.FindAsync([id], ct);
        if (ev is not null)
        {
            ev.Acknowledged = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
