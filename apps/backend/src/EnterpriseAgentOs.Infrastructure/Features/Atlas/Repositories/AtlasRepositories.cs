namespace EnterpriseAgentOs.Infrastructure.Features.Atlas;

internal sealed class AtlasConnectionRepository : IAtlasConnectionRepository
{
    private readonly EaosDbContext _db;

    public AtlasConnectionRepository(EaosDbContext db) => _db = db;

    public async Task<IReadOnlyList<AtlasConnectorConnectionRecord>> ListAsync(CancellationToken ct = default)
    {
        var rows = await _db.AtlasConnectorConnections.AsNoTracking()
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);
        var ids = rows.Select(r => r.Id).ToList();
        var statuses = await _db.AtlasEntityStatuses.AsNoTracking()
            .Where(s => ids.Contains(s.ConnectionId))
            .ToListAsync(ct);
        return rows.Select(r => ToRecord(r, statuses.Where(s => s.ConnectionId == r.Id))).ToList();
    }

    public async Task<AtlasConnectorConnectionRecord?> GetByAsync(AtlasConnectionFilter filter, CancellationToken ct = default)
    {
        var query = _db.AtlasConnectorConnections.AsNoTracking().AsQueryable();
        if (filter.Id.HasValue) query = query.Where(c => c.Id == filter.Id.Value);
        if (filter.Provider.HasValue) query = query.Where(c => c.Provider == filter.Provider.Value.ToString());

        var row = await query.FirstOrDefaultAsync(ct);
        if (row is null) return null;

        var statuses = await _db.AtlasEntityStatuses.AsNoTracking()
            .Where(s => s.ConnectionId == row.Id)
            .ToListAsync(ct);
        return ToRecord(row, statuses);
    }

    public async Task<AtlasConnectorConnectionRecord> UpsertAsync(AtlasConnectorConnectionRecord connection, CancellationToken ct = default)
    {
        var existing = await _db.AtlasConnectorConnections.FirstOrDefaultAsync(c => c.Id == connection.Id, ct);
        if (existing is null)
        {
            existing = ToEntity(connection);
            _db.AtlasConnectorConnections.Add(existing);
        }
        else
        {
            existing.DisplayName = connection.DisplayName;
            existing.RepositoriesJson = connection.RepositoriesJson;
            existing.EntitiesJson = connection.EntitiesJson;
            existing.Status = connection.Status.ToString();
            existing.Error = connection.Error;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return ToRecord(existing, []);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await _db.AtlasConnectorConnections.Where(c => c.Id == id).ExecuteDeleteAsync(ct);

    public async Task SetStatusAsync(Guid id, AtlasConnectorStatus status, string? error, CancellationToken ct = default)
    {
        var row = await _db.AtlasConnectorConnections.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (row is null) return;
        row.Status = status.ToString();
        row.Error = error;
        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static AtlasConnectorConnectionRecord ToRecord(
        AtlasConnectorConnectionEntity e,
        IEnumerable<AtlasEntityStatusEntity> statuses) => new()
    {
        Id = e.Id,
        Provider = Enum.Parse<AtlasConnectorProvider>(e.Provider),
        WorkspaceName = e.WorkspaceName,
        DisplayName = e.DisplayName,
        RepositoriesJson = e.RepositoriesJson,
        EntitiesJson = e.EntitiesJson,
        Status = Enum.Parse<AtlasConnectorStatus>(e.Status),
        Error = e.Error,
        CreatedById = e.CreatedById,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        EntityStatuses = statuses.Select(AtlasEntityStatusRepository.ToRecord).ToList(),
    };

    private static AtlasConnectorConnectionEntity ToEntity(AtlasConnectorConnectionRecord r) => new()
    {
        Id = r.Id,
        Provider = r.Provider.ToString(),
        WorkspaceName = r.WorkspaceName,
        DisplayName = r.DisplayName,
        RepositoriesJson = r.RepositoriesJson,
        EntitiesJson = r.EntitiesJson,
        Status = r.Status.ToString(),
        Error = r.Error,
        CreatedById = r.CreatedById,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
    };
}

internal sealed class AtlasEntityStatusRepository : IAtlasEntityStatusRepository
{
    private readonly EaosDbContext _db;

    public AtlasEntityStatusRepository(EaosDbContext db) => _db = db;

    public async Task<IReadOnlyList<AtlasEntityStatusRecord>> ListForConnectionAsync(Guid connectionId, CancellationToken ct = default)
        => (await _db.AtlasEntityStatuses.AsNoTracking()
            .Where(s => s.ConnectionId == connectionId)
            .OrderBy(s => s.Entity)
            .ToListAsync(ct))
            .Select(ToRecord)
            .ToList();

    public async Task UpsertAsync(AtlasEntityStatusRecord status, CancellationToken ct = default)
    {
        var existing = await _db.AtlasEntityStatuses
            .FirstOrDefaultAsync(s => s.ConnectionId == status.ConnectionId && s.Entity == status.Entity, ct);
        if (existing is null)
        {
            _db.AtlasEntityStatuses.Add(new AtlasEntityStatusEntity
            {
                Id = status.Id,
                ConnectionId = status.ConnectionId,
                Entity = status.Entity,
                Status = status.Status.ToString(),
                RecordCount = status.RecordCount,
                Error = status.Error,
                LastSyncedAt = status.LastSyncedAt,
                UpdatedAt = status.UpdatedAt,
            });
        }
        else
        {
            existing.Status = status.Status.ToString();
            existing.RecordCount = status.RecordCount;
            existing.Error = status.Error;
            existing.LastSyncedAt = status.LastSyncedAt;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    internal static AtlasEntityStatusRecord ToRecord(AtlasEntityStatusEntity e) => new()
    {
        Id = e.Id,
        ConnectionId = e.ConnectionId,
        Entity = e.Entity,
        Status = Enum.Parse<AtlasEntityStatus>(e.Status),
        RecordCount = e.RecordCount,
        Error = e.Error,
        LastSyncedAt = e.LastSyncedAt,
        UpdatedAt = e.UpdatedAt,
    };
}

internal sealed class AtlasIndexJobRepository : IAtlasIndexJobRepository
{
    private readonly EaosDbContext _db;

    public AtlasIndexJobRepository(EaosDbContext db) => _db = db;

    public async Task<AtlasIndexJobRecord> CreateAsync(AtlasIndexJobRecord job, CancellationToken ct = default)
    {
        var entity = ToEntity(job);
        _db.AtlasIndexJobs.Add(entity);
        await _db.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    public async Task<AtlasIndexJobRecord?> DequeueAsync(CancellationToken ct = default)
    {
        var entity = await _db.AtlasIndexJobs
            .Where(j => j.Status == AtlasIndexJobStatus.Queued.ToString())
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (entity is null) return null;

        entity.Status = AtlasIndexJobStatus.Running.ToString();
        entity.StartedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    public async Task UpdateAsync(AtlasIndexJobRecord job, CancellationToken ct = default)
    {
        var entity = await _db.AtlasIndexJobs.FirstOrDefaultAsync(j => j.Id == job.Id, ct);
        if (entity is null) return;
        entity.Status = job.Status.ToString();
        entity.Error = job.Error;
        entity.RecordsIndexed = job.RecordsIndexed;
        entity.StartedAt = job.StartedAt;
        entity.CompletedAt = job.CompletedAt;
        await _db.SaveChangesAsync(ct);
    }

    private static AtlasIndexJobRecord ToRecord(AtlasIndexJobEntity e) => new()
    {
        Id = e.Id,
        ConnectionId = e.ConnectionId,
        Status = Enum.Parse<AtlasIndexJobStatus>(e.Status),
        Error = e.Error,
        RecordsIndexed = e.RecordsIndexed,
        CreatedAt = e.CreatedAt,
        StartedAt = e.StartedAt,
        CompletedAt = e.CompletedAt,
    };

    private static AtlasIndexJobEntity ToEntity(AtlasIndexJobRecord r) => new()
    {
        Id = r.Id,
        ConnectionId = r.ConnectionId,
        Status = r.Status.ToString(),
        Error = r.Error,
        RecordsIndexed = r.RecordsIndexed,
        CreatedAt = r.CreatedAt,
        StartedAt = r.StartedAt,
        CompletedAt = r.CompletedAt,
    };
}

internal sealed class AtlasIndexedRecordRepository : IAtlasIndexedRecordRepository
{
    private readonly EaosDbContext _db;

    public AtlasIndexedRecordRepository(EaosDbContext db) => _db = db;

    public async Task UpsertManyAsync(IReadOnlyList<AtlasIndexedRecordRecord> records, CancellationToken ct = default)
    {
        foreach (var record in records)
        {
            var existing = await _db.AtlasIndexedRecords.FirstOrDefaultAsync(
                r => r.ConnectionId == record.ConnectionId && r.Entity == record.Entity && r.ExternalId == record.ExternalId,
                ct);
            if (existing is null)
            {
                _db.AtlasIndexedRecords.Add(ToEntity(record));
            }
            else
            {
                existing.Title = record.Title;
                existing.SearchText = record.SearchText;
                existing.RawJson = record.RawJson;
                existing.ExternalUpdatedAt = record.ExternalUpdatedAt;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AtlasIndexedRecordPage> SearchAsync(AtlasIndexedRecordFilter filter, CancellationToken ct = default)
    {
        var query = _db.AtlasIndexedRecords.AsNoTracking()
            .Where(r => r.ConnectionId == filter.ConnectionId && r.Entity == filter.Entity);

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var needle = filter.Query.Trim().ToLowerInvariant();
            query = query.Where(r => r.SearchText.ToLower().Contains(needle) || r.Title.ToLower().Contains(needle));
        }

        var offset = int.TryParse(filter.Cursor, out var parsedOffset) && parsedOffset > 0 ? parsedOffset : 0;
        var rows = await query.OrderBy(r => r.Id).Skip(offset).Take(filter.Limit + 1).ToListAsync(ct);
        var hasMore = rows.Count > filter.Limit;
        var page = rows.Take(filter.Limit).Select(ToRecord).ToList();
        return new AtlasIndexedRecordPage(page, hasMore, hasMore ? (offset + filter.Limit).ToString() : null);
    }

    public async Task<int> CountAsync(Guid connectionId, string entity, CancellationToken ct = default)
        => await _db.AtlasIndexedRecords.CountAsync(r => r.ConnectionId == connectionId && r.Entity == entity, ct);

    public async Task DeleteForConnectionAsync(Guid connectionId, CancellationToken ct = default)
        => await _db.AtlasIndexedRecords.Where(r => r.ConnectionId == connectionId).ExecuteDeleteAsync(ct);

    private static AtlasIndexedRecordRecord ToRecord(AtlasIndexedRecordEntity e) => new()
    {
        Id = e.Id,
        ConnectionId = e.ConnectionId,
        Entity = e.Entity,
        ExternalId = e.ExternalId,
        Title = e.Title,
        SearchText = e.SearchText,
        RawJson = e.RawJson,
        ExternalUpdatedAt = e.ExternalUpdatedAt,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
    };

    private static AtlasIndexedRecordEntity ToEntity(AtlasIndexedRecordRecord r) => new()
    {
        Id = r.Id,
        ConnectionId = r.ConnectionId,
        Entity = r.Entity,
        ExternalId = r.ExternalId,
        Title = r.Title,
        SearchText = r.SearchText,
        RawJson = r.RawJson,
        ExternalUpdatedAt = r.ExternalUpdatedAt,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
    };
}

internal sealed class AtlasRequestHistoryRepository : IAtlasRequestHistoryRepository
{
    private readonly EaosDbContext _db;

    public AtlasRequestHistoryRepository(EaosDbContext db) => _db = db;

    public async Task AddAsync(AtlasRequestHistoryRecord history, CancellationToken ct = default)
    {
        _db.AtlasRequestHistory.Add(new AtlasRequestHistoryEntity
        {
            Id = history.Id,
            ConnectionId = history.ConnectionId,
            Type = history.Type.ToString(),
            Entity = history.Entity,
            Action = history.Action,
            ParamsJson = history.ParamsJson,
            Success = history.Success,
            DurationMs = history.DurationMs,
            Error = history.Error,
            CreatedAt = history.CreatedAt,
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AtlasRequestHistoryRecord>> ListAsync(AtlasRequestHistoryFilter filter, CancellationToken ct = default)
    {
        var query = _db.AtlasRequestHistory.AsNoTracking().AsQueryable();
        if (filter.ConnectionId.HasValue) query = query.Where(h => h.ConnectionId == filter.ConnectionId.Value);
        return (await query.OrderByDescending(h => h.CreatedAt).Take(filter.Limit).ToListAsync(ct))
            .Select(h => new AtlasRequestHistoryRecord
            {
                Id = h.Id,
                ConnectionId = h.ConnectionId,
                Type = Enum.Parse<AtlasRequestType>(h.Type),
                Entity = h.Entity,
                Action = h.Action,
                ParamsJson = h.ParamsJson,
                Success = h.Success,
                DurationMs = h.DurationMs,
                Error = h.Error,
                CreatedAt = h.CreatedAt,
            })
            .ToList();
    }
}
