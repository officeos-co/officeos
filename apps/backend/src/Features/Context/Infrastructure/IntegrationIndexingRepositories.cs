namespace OffceOs.Infrastructure.Features.Context;

internal sealed class IntegrationConnectionRepository : IIntegrationConnectionRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public IntegrationConnectionRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<IReadOnlyList<IntegrationConnectionRecord>> ListAsync(IntegrationConnectionFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.IntegrationConnections.AsNoTracking().AsQueryable();
        if (filter.Id.HasValue) query = query.Where(c => c.Id == filter.Id.Value);
        if (filter.Provider.HasValue) query = query.Where(c => c.Provider == filter.Provider.Value.ToString());
        if (filter.CreatedById.HasValue) query = query.Where(c => c.CreatedById == filter.CreatedById.Value);
        if (filter.WorkspaceId.HasValue) query = query.Where(c => c.WorkspaceId == filter.WorkspaceId.Value);

        var rows = await query
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);
        var ids = rows.Select(r => r.Id).ToList();
        var statuses = await _eaosDbContext.IntegrationIndexEntityStatuses.AsNoTracking()
            .Where(s => ids.Contains(s.ConnectionId))
            .ToListAsync(ct);
        return rows.Select(r => ToRecord(r, statuses.Where(s => s.ConnectionId == r.Id))).ToList();
    }

    public async Task<IntegrationConnectionRecord?> GetByAsync(IntegrationConnectionFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.IntegrationConnections.AsNoTracking().AsQueryable();
        if (filter.Id.HasValue) query = query.Where(c => c.Id == filter.Id.Value);
        if (filter.Provider.HasValue) query = query.Where(c => c.Provider == filter.Provider.Value.ToString());
        if (filter.CreatedById.HasValue) query = query.Where(c => c.CreatedById == filter.CreatedById.Value);
        if (filter.WorkspaceId.HasValue) query = query.Where(c => c.WorkspaceId == filter.WorkspaceId.Value);

        var row = await query.FirstOrDefaultAsync(ct);
        if (row is null) return null;

        var statuses = await _eaosDbContext.IntegrationIndexEntityStatuses.AsNoTracking()
            .Where(s => s.ConnectionId == row.Id)
            .ToListAsync(ct);
        return ToRecord(row, statuses);
    }

    public async Task<IntegrationConnectionRecord> UpsertAsync(IntegrationConnectionRecord connection, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.IntegrationConnections.FirstOrDefaultAsync(c => c.Id == connection.Id, ct);
        if (existing is null)
        {
            existing = ToEntity(connection);
            _eaosDbContext.IntegrationConnections.Add(existing);
        }
        else
        {
            existing.DisplayName = connection.DisplayName;
            existing.RepositoriesJson = connection.RepositoriesJson;
            existing.EntitiesJson = connection.EntitiesJson;
            existing.Status = connection.Status.ToString();
            existing.Error = connection.Error;
            existing.WorkspaceId = connection.WorkspaceId;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _eaosDbContext.SaveChangesAsync(ct);
        return ToRecord(existing, []);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await _eaosDbContext.IntegrationConnections.Where(c => c.Id == id).ExecuteDeleteAsync(ct);

    public async Task SetStatusAsync(Guid id, IntegrationConnectionStatus status, string? error, CancellationToken ct = default)
    {
        var row = await _eaosDbContext.IntegrationConnections.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (row is null) return;
        row.Status = status.ToString();
        row.Error = error;
        row.UpdatedAt = DateTime.UtcNow;
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    private static IntegrationConnectionRecord ToRecord(
        IntegrationConnectionEntity e,
        IEnumerable<IntegrationIndexEntityStatusEntity> statuses) => new()
    {
        Id = e.Id,
        Provider = Enum.Parse<IntegrationProviderType>(e.Provider),
        WorkspaceName = e.WorkspaceName,
        DisplayName = e.DisplayName,
        RepositoriesJson = e.RepositoriesJson,
        EntitiesJson = e.EntitiesJson,
        Status = Enum.Parse<IntegrationConnectionStatus>(e.Status),
        Error = e.Error,
        CreatedById = e.CreatedById,
        WorkspaceId = e.WorkspaceId,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        EntityStatuses = statuses.Select(IntegrationIndexEntityStatusRepository.ToRecord).ToList(),
    };

    private static IntegrationConnectionEntity ToEntity(IntegrationConnectionRecord r) => new()
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
        WorkspaceId = r.WorkspaceId,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
    };
}

internal sealed class IntegrationIndexEntityStatusRepository : IIntegrationIndexEntityStatusRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public IntegrationIndexEntityStatusRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<IReadOnlyList<IntegrationIndexEntityStatusRecord>> ListForConnectionAsync(Guid connectionId, CancellationToken ct = default)
        => (await _eaosDbContext.IntegrationIndexEntityStatuses.AsNoTracking()
            .Where(s => s.ConnectionId == connectionId)
            .OrderBy(s => s.Entity)
            .ToListAsync(ct))
            .Select(ToRecord)
            .ToList();

    public async Task UpsertAsync(IntegrationIndexEntityStatusRecord status, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.IntegrationIndexEntityStatuses
            .FirstOrDefaultAsync(s => s.ConnectionId == status.ConnectionId && s.Entity == status.Entity, ct);
        if (existing is null)
        {
            _eaosDbContext.IntegrationIndexEntityStatuses.Add(new IntegrationIndexEntityStatusEntity
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
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    internal static IntegrationIndexEntityStatusRecord ToRecord(IntegrationIndexEntityStatusEntity e) => new()
    {
        Id = e.Id,
        ConnectionId = e.ConnectionId,
        Entity = e.Entity,
        Status = Enum.Parse<IntegrationIndexEntityStatus>(e.Status),
        RecordCount = e.RecordCount,
        Error = e.Error,
        LastSyncedAt = e.LastSyncedAt,
        UpdatedAt = e.UpdatedAt,
    };
}

internal sealed class IntegrationIndexJobRepository : IIntegrationIndexJobRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public IntegrationIndexJobRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<IntegrationIndexJobRecord> CreateAsync(IntegrationIndexJobRecord job, CancellationToken ct = default)
    {
        var entity = ToEntity(job);
        _eaosDbContext.IntegrationIndexJobs.Add(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    public async Task<IntegrationIndexJobRecord?> DequeueAsync(CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.IntegrationIndexJobs
            .Where(j => j.Status == IntegrationIndexJobStatus.Queued.ToString())
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (entity is null) return null;

        entity.Status = IntegrationIndexJobStatus.Running.ToString();
        entity.StartedAt = DateTime.UtcNow;
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    public async Task<IReadOnlyList<IntegrationIndexJobRecord>> ListAsync(IntegrationIndexJobFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.IntegrationIndexJobs.AsNoTracking().AsQueryable();
        if (filter.ConnectionId.HasValue)
            query = query.Where(j => j.ConnectionId == filter.ConnectionId.Value);

        return (await query
            .OrderByDescending(j => j.CreatedAt)
            .Take(filter.Limit)
            .ToListAsync(ct))
            .Select(ToRecord)
            .ToList();
    }

    public async Task UpdateAsync(IntegrationIndexJobRecord job, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.IntegrationIndexJobs.FirstOrDefaultAsync(j => j.Id == job.Id, ct);
        if (entity is null) return;
        entity.Status = job.Status.ToString();
        entity.Error = job.Error;
        entity.RecordsIndexed = job.RecordsIndexed;
        entity.StartedAt = job.StartedAt;
        entity.CompletedAt = job.CompletedAt;
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    private static IntegrationIndexJobRecord ToRecord(IntegrationIndexJobEntity e) => new()
    {
        Id = e.Id,
        ConnectionId = e.ConnectionId,
        Status = Enum.Parse<IntegrationIndexJobStatus>(e.Status),
        Error = e.Error,
        RecordsIndexed = e.RecordsIndexed,
        CreatedAt = e.CreatedAt,
        StartedAt = e.StartedAt,
        CompletedAt = e.CompletedAt,
    };

    private static IntegrationIndexJobEntity ToEntity(IntegrationIndexJobRecord r) => new()
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

internal sealed class IntegrationIndexedRecordRepository : IIntegrationIndexedRecordRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public IntegrationIndexedRecordRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task UpsertManyAsync(IReadOnlyList<IntegrationIndexedRecordRecord> records, CancellationToken ct = default)
    {
        foreach (var record in records)
        {
            var existing = await _eaosDbContext.IntegrationIndexedRecords.FirstOrDefaultAsync(
                r => r.ConnectionId == record.ConnectionId && r.Entity == record.Entity && r.ExternalId == record.ExternalId,
                ct);
            if (existing is null)
            {
                _eaosDbContext.IntegrationIndexedRecords.Add(ToEntity(record));
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
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task<IntegrationIndexedRecordRecord?> GetByAsync(IntegrationIndexedRecordFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.IntegrationIndexedRecords.AsNoTracking().AsQueryable();
        if (filter.Id.HasValue) query = query.Where(r => r.Id == filter.Id.Value);
        if (filter.ConnectionId.HasValue) query = query.Where(r => r.ConnectionId == filter.ConnectionId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Entity)) query = query.Where(r => r.Entity == filter.Entity);

        var entity = await query.FirstOrDefaultAsync(ct);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<IntegrationIndexedRecordPage> SearchAsync(IntegrationIndexedRecordFilter filter, CancellationToken ct = default)
    {
        if (!filter.ConnectionId.HasValue)
            throw new ArgumentException("SearchAsync requires IntegrationIndexedRecordFilter.ConnectionId.", nameof(filter));
        if (string.IsNullOrWhiteSpace(filter.Entity))
            throw new ArgumentException("SearchAsync requires IntegrationIndexedRecordFilter.Entity.", nameof(filter));

        var query = _eaosDbContext.IntegrationIndexedRecords.AsNoTracking()
            .Where(r => r.ConnectionId == filter.ConnectionId.Value && r.Entity == filter.Entity);

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var needle = filter.Query.Trim().ToLowerInvariant();
            query = query.Where(r => r.SearchText.ToLower().Contains(needle) || r.Title.ToLower().Contains(needle));
        }

        var offset = int.TryParse(filter.Cursor, out var parsedOffset) && parsedOffset > 0 ? parsedOffset : 0;
        var rows = await query.OrderBy(r => r.Id).Skip(offset).Take(filter.Limit + 1).ToListAsync(ct);
        var hasMore = rows.Count > filter.Limit;
        var page = rows.Take(filter.Limit).Select(ToRecord).ToList();
        return new IntegrationIndexedRecordPage(page, hasMore, hasMore ? (offset + filter.Limit).ToString() : null);
    }

    public async Task<int> CountAsync(Guid connectionId, string entity, CancellationToken ct = default)
        => await _eaosDbContext.IntegrationIndexedRecords.CountAsync(r => r.ConnectionId == connectionId && r.Entity == entity, ct);

    public async Task DeleteForConnectionAsync(Guid connectionId, CancellationToken ct = default)
        => await _eaosDbContext.IntegrationIndexedRecords.Where(r => r.ConnectionId == connectionId).ExecuteDeleteAsync(ct);

    private static IntegrationIndexedRecordRecord ToRecord(IntegrationIndexedRecordEntity e) => new()
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

    private static IntegrationIndexedRecordEntity ToEntity(IntegrationIndexedRecordRecord r) => new()
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

internal sealed class IntegrationRequestHistoryRepository : IIntegrationRequestHistoryRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public IntegrationRequestHistoryRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task AddAsync(IntegrationRequestHistoryRecord history, CancellationToken ct = default)
    {
        _eaosDbContext.IntegrationRequestHistory.Add(new IntegrationRequestHistoryEntity
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
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<IntegrationRequestHistoryRecord>> ListAsync(IntegrationRequestHistoryFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.IntegrationRequestHistory.AsNoTracking().AsQueryable();
        if (filter.ConnectionId.HasValue) query = query.Where(h => h.ConnectionId == filter.ConnectionId.Value);
        return (await query.OrderByDescending(h => h.CreatedAt).Take(filter.Limit).ToListAsync(ct))
            .Select(h => new IntegrationRequestHistoryRecord
            {
                Id = h.Id,
                ConnectionId = h.ConnectionId,
                Type = Enum.Parse<IntegrationRequestType>(h.Type),
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

internal sealed class IntegrationActivityRepository : IIntegrationActivityRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public IntegrationActivityRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task AddAsync(IntegrationActivityRecord activity, CancellationToken ct = default)
    {
        _eaosDbContext.IntegrationActivity.Add(new IntegrationActivityEntity
        {
            Id = activity.Id,
            ConnectionId = activity.ConnectionId,
            Type = activity.Type,
            Entity = activity.Entity,
            Message = activity.Message,
            DetailsJson = activity.DetailsJson,
            Success = activity.Success,
            CreatedAt = activity.CreatedAt,
        });
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<IntegrationActivityRecord>> ListAsync(IntegrationActivityFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.IntegrationActivity.AsNoTracking().AsQueryable();
        if (filter.ConnectionId.HasValue)
            query = query.Where(a => a.ConnectionId == filter.ConnectionId.Value);

        return (await query
                .OrderByDescending(a => a.CreatedAt)
                .Take(filter.Limit)
                .ToListAsync(ct))
            .Select(a => new IntegrationActivityRecord
            {
                Id = a.Id,
                ConnectionId = a.ConnectionId,
                Type = a.Type,
                Entity = a.Entity,
                Message = a.Message,
                DetailsJson = a.DetailsJson,
                Success = a.Success,
                CreatedAt = a.CreatedAt,
            })
            .ToList();
    }
}
