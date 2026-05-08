using MediatR;

namespace EnterpriseAgentOs.Application.Features.Agents.Integrations;

internal sealed class IntegrationConnectionService : IIntegrationConnectionService
{
    internal static readonly string[] SupportedGitHubEntities = IntegrationDefinitionCatalog.GitHubEntities;

    private readonly IIntegrationConnectionRepository _connections;
    private readonly IIntegrationIndexEntityStatusRepository _entityStatuses;
    private readonly IIntegrationIndexJobRepository _jobs;
    private readonly IIntegrationIndexedRecordRepository _records;
    private readonly IIntegrationActivityRepository _activity;
    private readonly IIntegrationRequestHistoryRepository _history;
    private readonly GitHubIntegrationClient _github;
    private readonly IPublisher _publisher;

    public IntegrationConnectionService(
        IIntegrationConnectionRepository connections,
        IIntegrationIndexEntityStatusRepository entityStatuses,
        IIntegrationIndexJobRepository jobs,
        IIntegrationIndexedRecordRepository records,
        IIntegrationActivityRepository activity,
        IIntegrationRequestHistoryRepository history,
        GitHubIntegrationClient github,
        IPublisher publisher)
    {
        _connections = connections;
        _entityStatuses = entityStatuses;
        _jobs = jobs;
        _records = records;
        _activity = activity;
        _history = history;
        _github = github;
        _publisher = publisher;
    }

    public async Task<IReadOnlyList<IntegrationDefinitionRecord>> ListIntegrationDefinitionsAsync(CancellationToken ct = default)
    {
        var githubConfigured = await _github.HasTokenAsync(ct);
        return IntegrationDefinitionCatalog.BuiltinDefinitions
            .Select(connector => connector.OauthProvider == "github"
                ? connector with { OauthConfigured = githubConfigured }
                : connector)
            .ToList();
    }

    public Task<IReadOnlyList<IntegrationConnectionRecord>> ListAsync(IntegrationConnectionFilter filter, CancellationToken ct = default)
        => _connections.ListAsync(filter, ct);

    public Task<IntegrationConnectionRecord?> GetByAsync(IntegrationConnectionFilter filter, CancellationToken ct = default)
        => _connections.GetByAsync(filter, ct);

    public Task<IReadOnlyList<IntegrationActivityRecord>> ListAsync(IntegrationActivityFilter filter, CancellationToken ct = default)
        => _activity.ListAsync(filter with { Limit = NormalizeLimit(filter.Limit, 100, 500) }, ct);

    public Task<IReadOnlyList<IntegrationRequestHistoryRecord>> ListAsync(IntegrationRequestHistoryFilter filter, CancellationToken ct = default)
        => _history.ListAsync(filter with { Limit = NormalizeLimit(filter.Limit, 100, 500) }, ct);

    public Task<IReadOnlyList<IntegrationIndexJobRecord>> ListAsync(IntegrationIndexJobFilter filter, CancellationToken ct = default)
        => _jobs.ListAsync(filter with { Limit = NormalizeLimit(filter.Limit, 20, 100) }, ct);

    public Task<IntegrationIndexedRecordRecord?> GetByAsync(IntegrationIndexedRecordFilter filter, CancellationToken ct = default)
        => _records.GetByAsync(filter, ct);

    public Task<IntegrationIndexedRecordPage> SearchAsync(IntegrationIndexedRecordFilter filter, CancellationToken ct = default)
        => _records.SearchAsync(filter with
        {
            Entity = filter.Entity?.Trim(),
            Query = filter.Query?.Trim(),
            Limit = NormalizeLimit(filter.Limit, 20, 100),
        }, ct);

    public async Task<IntegrationConnectionRecord> CreateGitHubConnectionAsync(CreateGitHubIntegrationConnectionRequest request, CancellationToken ct = default)
    {
        var repositories = NormalizeRepositories(request.Repositories);
        var entities = NormalizeEntities(request.Entities);
        var hasToken = await _github.HasTokenAsync(ct);
        if (hasToken)
            await _github.ValidateRepositoriesAsync(repositories, ct);

        var connection = new IntegrationConnectionRecord
        {
            Provider = IntegrationProviderType.GitHub,
            WorkspaceName = string.IsNullOrWhiteSpace(request.WorkspaceName) ? "default" : request.WorkspaceName.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? "GitHub" : request.DisplayName.Trim(),
            RepositoriesJson = JsonSerializer.Serialize(repositories),
            EntitiesJson = JsonSerializer.Serialize(entities),
            Status = hasToken ? IntegrationConnectionStatus.Indexing : IntegrationConnectionStatus.NeedsAuth,
            CreatedById = request.CreatedById,
        };

        var saved = await _connections.UpsertAsync(connection, ct);
        await LogActivityAsync(saved.Id, "connection_created", null, "GitHub connector created.", new
        {
            saved.WorkspaceName,
            Repositories = repositories,
            Entities = entities,
            OAuthConfigured = hasToken,
        }, true, ct);
        if (hasToken)
        {
            await LogActivityAsync(saved.Id, "auth_connected", null, "GitHub OAuth token found.", null, true, ct);
            await LogActivityAsync(saved.Id, "repositories_validated", null, "GitHub repositories validated.", new
            {
                Repositories = repositories,
            }, true, ct);
        }

        foreach (var entity in entities)
            await _entityStatuses.UpsertAsync(new IntegrationIndexEntityStatusRecord
            {
                ConnectionId = saved.Id,
                Entity = entity,
                Status = hasToken ? IntegrationIndexEntityStatus.Initializing : IntegrationIndexEntityStatus.Failed,
                Error = hasToken ? null : "GitHub OAuth is not connected.",
            }, ct);

        if (!hasToken)
            await LogActivityAsync(saved.Id, "auth_required", null, "GitHub OAuth is not connected.", null, false, ct);

        await _publisher.Publish(new IntegrationConnectionCreatedEvent(saved.Id, "github", saved.WorkspaceName), ct);
        if (hasToken)
            await StartIndexAsync(saved.Id, ct);
        return await GetByAsync(new IntegrationConnectionFilter { Id = saved.Id }, ct) ?? saved;
    }

    public async Task<IntegrationConnectionRecord> UpdateGitHubConnectionAsync(UpdateGitHubIntegrationConnectionRequest request, CancellationToken ct = default)
    {
        var existing = await GetByAsync(new IntegrationConnectionFilter { Id = request.Id }, ct)
            ?? throw new InvalidOperationException("Integration connection not found.");
        var repositories = NormalizeRepositories(request.Repositories);
        var entities = NormalizeEntities(request.Entities);
        var hasToken = await _github.HasTokenAsync(ct);
        if (hasToken)
            await _github.ValidateRepositoriesAsync(repositories, ct);

        var updated = new IntegrationConnectionRecord
        {
            Id = existing.Id,
            Provider = existing.Provider,
            WorkspaceName = existing.WorkspaceName,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? existing.DisplayName : request.DisplayName.Trim(),
            RepositoriesJson = JsonSerializer.Serialize(repositories),
            EntitiesJson = JsonSerializer.Serialize(entities),
            Status = hasToken ? IntegrationConnectionStatus.Indexing : IntegrationConnectionStatus.NeedsAuth,
            Error = hasToken ? null : "GitHub OAuth is not connected.",
            CreatedById = existing.CreatedById,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
        };

        var saved = await _connections.UpsertAsync(updated, ct);
        await LogActivityAsync(saved.Id, "connection_updated", null, "GitHub connector settings updated.", new
        {
            Repositories = repositories,
            Entities = entities,
            OAuthConfigured = hasToken,
        }, true, ct);
        if (hasToken)
        {
            await LogActivityAsync(saved.Id, "auth_connected", null, "GitHub OAuth token found.", null, true, ct);
            await LogActivityAsync(saved.Id, "repositories_validated", null, "GitHub repositories validated.", new
            {
                Repositories = repositories,
            }, true, ct);
        }

        await _records.DeleteForConnectionAsync(saved.Id, ct);
        foreach (var entity in entities)
            await _entityStatuses.UpsertAsync(new IntegrationIndexEntityStatusRecord
            {
                ConnectionId = saved.Id,
                Entity = entity,
                Status = hasToken ? IntegrationIndexEntityStatus.Initializing : IntegrationIndexEntityStatus.Failed,
                Error = hasToken ? null : "GitHub OAuth is not connected.",
            }, ct);

        if (!hasToken)
            await LogActivityAsync(saved.Id, "auth_required", null, "GitHub OAuth is not connected.", null, false, ct);

        await _publisher.Publish(new IntegrationConnectionUpdatedEvent(saved.Id, "github"), ct);
        if (hasToken)
            await StartIndexAsync(saved.Id, ct);
        return await GetByAsync(new IntegrationConnectionFilter { Id = saved.Id }, ct) ?? saved;
    }

    public async Task DeleteConnectionAsync(Guid id, CancellationToken ct = default)
        => await _connections.DeleteAsync(id, ct);

    public async Task<IntegrationIndexJobRecord> StartIndexAsync(Guid connectionId, CancellationToken ct = default)
    {
        var connection = await GetByAsync(new IntegrationConnectionFilter { Id = connectionId }, ct)
            ?? throw new InvalidOperationException("Integration connection not found.");
        var job = await _jobs.CreateAsync(new IntegrationIndexJobRecord
        {
            ConnectionId = connection.Id,
            Status = IntegrationIndexJobStatus.Queued,
        }, ct);
        await _connections.SetStatusAsync(connection.Id, IntegrationConnectionStatus.Indexing, null, ct);
        await LogActivityAsync(connection.Id, "index_queued", null, "Index job queued.", new { JobId = job.Id }, true, ct);
        await _publisher.Publish(new IntegrationIndexRequestedEvent(connection.Id, job.Id), ct);
        return job;
    }

    private Task LogActivityAsync(
        Guid connectionId,
        string type,
        string? entity,
        string message,
        object? details,
        bool success,
        CancellationToken ct)
        => _activity.AddAsync(new IntegrationActivityRecord
        {
            ConnectionId = connectionId,
            Type = type,
            Entity = entity,
            Message = message,
            DetailsJson = details is null ? "{}" : JsonSerializer.Serialize(details),
            Success = success,
        }, ct);

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
            throw new InvalidOperationException("Integration V1 supports only explicit GitHub repositories in owner/repo format.");
        return normalized;
    }

    private static IReadOnlyList<string> NormalizeEntities(IReadOnlyList<string> entities)
    {
        var allowed = SupportedGitHubEntities.ToHashSet(StringComparer.Ordinal);
        var normalized = entities.Where(allowed.Contains).Distinct(StringComparer.Ordinal).ToList();
        return normalized.Count == 0 ? SupportedGitHubEntities : normalized;
    }

    private static int NormalizeLimit(int limit, int defaultLimit, int maxLimit)
        => Math.Clamp(limit <= 0 ? defaultLimit : limit, 1, maxLimit);
}
