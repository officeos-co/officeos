using MediatR;

namespace EnterpriseAgentOs.Application.Features.Atlas;

internal sealed class AtlasService : IAtlasService
{
    internal static readonly string[] SupportedGitHubEntities = AtlasConnectorRegistry.GitHubEntities;

    private readonly IAtlasConnectionRepository _connections;
    private readonly IAtlasEntityStatusRepository _entityStatuses;
    private readonly IAtlasIndexJobRepository _jobs;
    private readonly IAtlasIndexedRecordRepository _records;
    private readonly IAtlasRequestHistoryRepository _history;
    private readonly AtlasGitHubClient _github;
    private readonly IPublisher _publisher;

    public AtlasService(
        IAtlasConnectionRepository connections,
        IAtlasEntityStatusRepository entityStatuses,
        IAtlasIndexJobRepository jobs,
        IAtlasIndexedRecordRepository records,
        IAtlasRequestHistoryRepository history,
        AtlasGitHubClient github,
        IPublisher publisher)
    {
        _connections = connections;
        _entityStatuses = entityStatuses;
        _jobs = jobs;
        _records = records;
        _history = history;
        _github = github;
        _publisher = publisher;
    }

    public Task<IReadOnlyList<AtlasConnectorTypeRecord>> ListConnectorTypesAsync(CancellationToken ct = default)
        => Task.FromResult(AtlasConnectorRegistry.BuiltinConnectors);

    public Task<IReadOnlyList<AtlasConnectorConnectionRecord>> ListConnectionsAsync(CancellationToken ct = default)
        => _connections.ListAsync(ct);

    public Task<AtlasConnectorConnectionRecord?> GetConnectionAsync(Guid id, CancellationToken ct = default)
        => _connections.GetByAsync(new AtlasConnectionFilter { Id = id }, ct);

    public Task<IReadOnlyList<AtlasRequestHistoryRecord>> ListHistoryAsync(Guid? connectionId, CancellationToken ct = default)
        => _history.ListAsync(new AtlasRequestHistoryFilter { ConnectionId = connectionId }, ct);

    public async Task<AtlasConnectorConnectionRecord> CreateGitHubConnectionAsync(CreateAtlasGitHubConnectionRequest request, CancellationToken ct = default)
    {
        var repositories = NormalizeRepositories(request.Repositories);
        var entities = NormalizeEntities(request.Entities);
        var hasToken = await _github.HasTokenAsync(ct);
        if (hasToken)
            await _github.ValidateRepositoriesAsync(repositories, ct);

        var connection = new AtlasConnectorConnectionRecord
        {
            Provider = AtlasConnectorProvider.GitHub,
            WorkspaceName = string.IsNullOrWhiteSpace(request.WorkspaceName) ? "default" : request.WorkspaceName.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? "GitHub" : request.DisplayName.Trim(),
            RepositoriesJson = JsonSerializer.Serialize(repositories),
            EntitiesJson = JsonSerializer.Serialize(entities),
            Status = hasToken ? AtlasConnectorStatus.Indexing : AtlasConnectorStatus.NeedsAuth,
            CreatedById = request.CreatedById,
        };

        var saved = await _connections.UpsertAsync(connection, ct);
        foreach (var entity in entities)
            await _entityStatuses.UpsertAsync(new AtlasEntityStatusRecord
            {
                ConnectionId = saved.Id,
                Entity = entity,
                Status = hasToken ? AtlasEntityStatus.Initializing : AtlasEntityStatus.Failed,
                Error = hasToken ? null : "GitHub OAuth is not connected.",
            }, ct);

        await _publisher.Publish(new AtlasConnectionCreatedEvent(saved.Id, "github", saved.WorkspaceName), ct);
        if (hasToken)
            await StartIndexAsync(saved.Id, ct);
        return await GetConnectionAsync(saved.Id, ct) ?? saved;
    }

    public async Task<AtlasConnectorConnectionRecord> UpdateGitHubConnectionAsync(UpdateAtlasGitHubConnectionRequest request, CancellationToken ct = default)
    {
        var existing = await GetConnectionAsync(request.Id, ct)
            ?? throw new InvalidOperationException("Atlas connection not found.");
        var repositories = NormalizeRepositories(request.Repositories);
        var entities = NormalizeEntities(request.Entities);
        var hasToken = await _github.HasTokenAsync(ct);
        if (hasToken)
            await _github.ValidateRepositoriesAsync(repositories, ct);

        var updated = new AtlasConnectorConnectionRecord
        {
            Id = existing.Id,
            Provider = existing.Provider,
            WorkspaceName = existing.WorkspaceName,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? existing.DisplayName : request.DisplayName.Trim(),
            RepositoriesJson = JsonSerializer.Serialize(repositories),
            EntitiesJson = JsonSerializer.Serialize(entities),
            Status = hasToken ? AtlasConnectorStatus.Indexing : AtlasConnectorStatus.NeedsAuth,
            Error = hasToken ? null : "GitHub OAuth is not connected.",
            CreatedById = existing.CreatedById,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
        };

        var saved = await _connections.UpsertAsync(updated, ct);
        await _records.DeleteForConnectionAsync(saved.Id, ct);
        foreach (var entity in entities)
            await _entityStatuses.UpsertAsync(new AtlasEntityStatusRecord
            {
                ConnectionId = saved.Id,
                Entity = entity,
                Status = hasToken ? AtlasEntityStatus.Initializing : AtlasEntityStatus.Failed,
                Error = hasToken ? null : "GitHub OAuth is not connected.",
            }, ct);

        await _publisher.Publish(new AtlasConnectionUpdatedEvent(saved.Id, "github"), ct);
        if (hasToken)
            await StartIndexAsync(saved.Id, ct);
        return await GetConnectionAsync(saved.Id, ct) ?? saved;
    }

    public async Task DeleteConnectionAsync(Guid id, CancellationToken ct = default)
        => await _connections.DeleteAsync(id, ct);

    public async Task<AtlasIndexJobRecord> StartIndexAsync(Guid connectionId, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(connectionId, ct)
            ?? throw new InvalidOperationException("Atlas connection not found.");
        var job = await _jobs.CreateAsync(new AtlasIndexJobRecord
        {
            ConnectionId = connection.Id,
            Status = AtlasIndexJobStatus.Queued,
        }, ct);
        await _connections.SetStatusAsync(connection.Id, AtlasConnectorStatus.Indexing, null, ct);
        await _publisher.Publish(new AtlasIndexRequestedEvent(connection.Id, job.Id), ct);
        return job;
    }

    private static IReadOnlyList<string> NormalizeRepositories(IReadOnlyList<string> repositories)
    {
        var normalized = repositories
            .Select(r => r.Trim())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (normalized.Count == 0)
            throw new InvalidOperationException("At least one GitHub repository is required.");
        if (normalized.Any(r => r.Contains('*') || r.Split('/').Length != 2))
            throw new InvalidOperationException("Atlas V1 supports only explicit GitHub repositories in owner/repo format.");
        return normalized;
    }

    private static IReadOnlyList<string> NormalizeEntities(IReadOnlyList<string> entities)
    {
        var allowed = SupportedGitHubEntities.ToHashSet(StringComparer.Ordinal);
        var normalized = entities.Where(allowed.Contains).Distinct(StringComparer.Ordinal).ToList();
        return normalized.Count == 0 ? SupportedGitHubEntities : normalized;
    }
}
