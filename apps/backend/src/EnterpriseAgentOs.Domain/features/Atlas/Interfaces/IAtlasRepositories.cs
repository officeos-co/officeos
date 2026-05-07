namespace EnterpriseAgentOs.Domain.Features.Atlas;

public sealed record AtlasConnectionFilter
{
    public Guid? Id { get; init; }
    public AtlasConnectorProvider? Provider { get; init; }
}

public sealed record AtlasRequestHistoryFilter
{
    public Guid? ConnectionId { get; init; }
    public int Limit { get; init; } = 100;
}

public sealed record AtlasActivityFilter
{
    public Guid? ConnectionId { get; init; }
    public int Limit { get; init; } = 100;
}

public sealed record AtlasIndexedRecordFilter
{
    public Guid ConnectionId { get; init; }
    public string Entity { get; init; } = string.Empty;
    public string? Query { get; init; }
    public string? Cursor { get; init; }
    public int Limit { get; init; } = 20;
}

public sealed record AtlasIndexedRecordPage(
    IReadOnlyList<AtlasIndexedRecordRecord> Records,
    bool HasMore,
    string? Cursor);

public interface IAtlasConnectionRepository
{
    Task<IReadOnlyList<AtlasConnectorConnectionRecord>> ListAsync(CancellationToken ct = default);
    Task<AtlasConnectorConnectionRecord?> GetByAsync(AtlasConnectionFilter filter, CancellationToken ct = default);
    Task<AtlasConnectorConnectionRecord> UpsertAsync(AtlasConnectorConnectionRecord connection, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task SetStatusAsync(Guid id, AtlasConnectorStatus status, string? error, CancellationToken ct = default);
}

public interface IAtlasEntityStatusRepository
{
    Task<IReadOnlyList<AtlasEntityStatusRecord>> ListForConnectionAsync(Guid connectionId, CancellationToken ct = default);
    Task UpsertAsync(AtlasEntityStatusRecord status, CancellationToken ct = default);
}

public interface IAtlasIndexJobRepository
{
    Task<AtlasIndexJobRecord> CreateAsync(AtlasIndexJobRecord job, CancellationToken ct = default);
    Task<AtlasIndexJobRecord?> DequeueAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AtlasIndexJobRecord>> ListAsync(Guid connectionId, int limit = 20, CancellationToken ct = default);
    Task UpdateAsync(AtlasIndexJobRecord job, CancellationToken ct = default);
}

public interface IAtlasIndexedRecordRepository
{
    Task UpsertManyAsync(IReadOnlyList<AtlasIndexedRecordRecord> records, CancellationToken ct = default);
    Task<AtlasIndexedRecordRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AtlasIndexedRecordPage> SearchAsync(AtlasIndexedRecordFilter filter, CancellationToken ct = default);
    Task<int> CountAsync(Guid connectionId, string entity, CancellationToken ct = default);
    Task DeleteForConnectionAsync(Guid connectionId, CancellationToken ct = default);
}

public interface IAtlasRequestHistoryRepository
{
    Task AddAsync(AtlasRequestHistoryRecord history, CancellationToken ct = default);
    Task<IReadOnlyList<AtlasRequestHistoryRecord>> ListAsync(AtlasRequestHistoryFilter filter, CancellationToken ct = default);
}

public interface IAtlasActivityRepository
{
    Task AddAsync(AtlasActivityRecord activity, CancellationToken ct = default);
    Task<IReadOnlyList<AtlasActivityRecord>> ListAsync(AtlasActivityFilter filter, CancellationToken ct = default);
}
