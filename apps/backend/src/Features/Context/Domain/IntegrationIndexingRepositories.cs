namespace EnterpriseAgentOs.Domain.Features.Context;

public sealed record IntegrationConnectionFilter
{
    public Guid? Id { get; init; }
    public IntegrationProviderType? Provider { get; init; }
}

public sealed record IntegrationRequestHistoryFilter
{
    public Guid? ConnectionId { get; init; }
    public int Limit { get; init; } = 100;
}

public sealed record IntegrationActivityFilter
{
    public Guid? ConnectionId { get; init; }
    public int Limit { get; init; } = 100;
}

public sealed record IntegrationIndexJobFilter
{
    public Guid? ConnectionId { get; init; }
    public int Limit { get; init; } = 20;
}

public sealed record IntegrationIndexedRecordFilter
{
    public Guid? Id { get; init; }
    public Guid? ConnectionId { get; init; }
    public string? Entity { get; init; }
    public string? Query { get; init; }
    public string? Cursor { get; init; }
    public int Limit { get; init; } = 20;
}

public sealed record IntegrationIndexedRecordPage(
    IReadOnlyList<IntegrationIndexedRecordRecord> Records,
    bool HasMore,
    string? Cursor);

public interface IIntegrationConnectionRepository
{
    Task<IReadOnlyList<IntegrationConnectionRecord>> ListAsync(IntegrationConnectionFilter filter, CancellationToken ct = default);
    Task<IntegrationConnectionRecord?> GetByAsync(IntegrationConnectionFilter filter, CancellationToken ct = default);
    Task<IntegrationConnectionRecord> UpsertAsync(IntegrationConnectionRecord connection, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task SetStatusAsync(Guid id, IntegrationConnectionStatus status, string? error, CancellationToken ct = default);
}

public interface IIntegrationIndexEntityStatusRepository
{
    Task<IReadOnlyList<IntegrationIndexEntityStatusRecord>> ListForConnectionAsync(Guid connectionId, CancellationToken ct = default);
    Task UpsertAsync(IntegrationIndexEntityStatusRecord status, CancellationToken ct = default);
}

public interface IIntegrationIndexJobRepository
{
    Task<IntegrationIndexJobRecord> CreateAsync(IntegrationIndexJobRecord job, CancellationToken ct = default);
    Task<IntegrationIndexJobRecord?> DequeueAsync(CancellationToken ct = default);
    Task<IReadOnlyList<IntegrationIndexJobRecord>> ListAsync(IntegrationIndexJobFilter filter, CancellationToken ct = default);
    Task UpdateAsync(IntegrationIndexJobRecord job, CancellationToken ct = default);
}

public interface IIntegrationIndexedRecordRepository
{
    Task UpsertManyAsync(IReadOnlyList<IntegrationIndexedRecordRecord> records, CancellationToken ct = default);
    Task<IntegrationIndexedRecordRecord?> GetByAsync(IntegrationIndexedRecordFilter filter, CancellationToken ct = default);
    Task<IntegrationIndexedRecordPage> SearchAsync(IntegrationIndexedRecordFilter filter, CancellationToken ct = default);
    Task<int> CountAsync(Guid connectionId, string entity, CancellationToken ct = default);
    Task DeleteForConnectionAsync(Guid connectionId, CancellationToken ct = default);
}

public interface IIntegrationRequestHistoryRepository
{
    Task AddAsync(IntegrationRequestHistoryRecord history, CancellationToken ct = default);
    Task<IReadOnlyList<IntegrationRequestHistoryRecord>> ListAsync(IntegrationRequestHistoryFilter filter, CancellationToken ct = default);
}

public interface IIntegrationActivityRepository
{
    Task AddAsync(IntegrationActivityRecord activity, CancellationToken ct = default);
    Task<IReadOnlyList<IntegrationActivityRecord>> ListAsync(IntegrationActivityFilter filter, CancellationToken ct = default);
}
