using System.Text.Json.Nodes;
using MediatR;

namespace EnterpriseAgentOs.Application.Features.Atlas;

internal sealed class AtlasIndexingService
{
    private readonly IAtlasConnectionRepository _connections;
    private readonly IAtlasEntityStatusRepository _entityStatuses;
    private readonly IAtlasIndexJobRepository _jobs;
    private readonly IAtlasIndexedRecordRepository _records;
    private readonly AtlasGitHubClient _github;
    private readonly IPublisher _publisher;
    private readonly ILogger<AtlasIndexingService> _logger;

    public AtlasIndexingService(
        IAtlasConnectionRepository connections,
        IAtlasEntityStatusRepository entityStatuses,
        IAtlasIndexJobRepository jobs,
        IAtlasIndexedRecordRepository records,
        AtlasGitHubClient github,
        IPublisher publisher,
        ILogger<AtlasIndexingService> logger)
    {
        _connections = connections;
        _entityStatuses = entityStatuses;
        _jobs = jobs;
        _records = records;
        _github = github;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<bool> ProcessOneAsync(CancellationToken ct)
    {
        var job = await _jobs.DequeueAsync(ct);
        if (job is null) return false;

        var recordsIndexed = 0;
        try
        {
            var connection = await _connections.GetByAsync(new AtlasConnectionFilter { Id = job.ConnectionId }, ct)
                ?? throw new InvalidOperationException("Atlas connection not found.");
            var repositories = ParseStringArray(connection.RepositoriesJson);
            var entities = ParseStringArray(connection.EntitiesJson);

            await _connections.SetStatusAsync(connection.Id, AtlasConnectorStatus.Indexing, null, ct);
            await _records.DeleteForConnectionAsync(connection.Id, ct);

            foreach (var entity in entities)
            {
                await _entityStatuses.UpsertAsync(new AtlasEntityStatusRecord
                {
                    ConnectionId = connection.Id,
                    Entity = entity,
                    Status = AtlasEntityStatus.Indexing,
                }, ct);

                var entityRecords = new List<AtlasIndexedRecordRecord>();
                foreach (var repository in repositories)
                {
                    var rows = await _github.FetchEntityAsync(entity, repository, perPage: 100, ct);
                    entityRecords.AddRange(rows.Select(row => ToIndexedRecord(connection.Id, entity, repository, row)));
                }

                if (entityRecords.Count > 0)
                    await _records.UpsertManyAsync(entityRecords, ct);
                recordsIndexed += entityRecords.Count;

                await _entityStatuses.UpsertAsync(new AtlasEntityStatusRecord
                {
                    ConnectionId = connection.Id,
                    Entity = entity,
                    Status = AtlasEntityStatus.Ready,
                    RecordCount = entityRecords.Count,
                    LastSyncedAt = DateTime.UtcNow,
                }, ct);
            }

            await _connections.SetStatusAsync(connection.Id, AtlasConnectorStatus.Ready, null, ct);
            await _jobs.UpdateAsync(new AtlasIndexJobRecord
            {
                Id = job.Id,
                ConnectionId = job.ConnectionId,
                Status = AtlasIndexJobStatus.Succeeded,
                RecordsIndexed = recordsIndexed,
                CreatedAt = job.CreatedAt,
                StartedAt = job.StartedAt,
                CompletedAt = DateTime.UtcNow,
            }, ct);
            await _publisher.Publish(new AtlasIndexCompletedEvent(connection.Id, job.Id, true, recordsIndexed, null), ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Atlas index job {JobId} failed", job.Id);
            await _connections.SetStatusAsync(job.ConnectionId, AtlasConnectorStatus.Failed, ex.Message, ct);
            await _jobs.UpdateAsync(new AtlasIndexJobRecord
            {
                Id = job.Id,
                ConnectionId = job.ConnectionId,
                Status = AtlasIndexJobStatus.Failed,
                Error = ex.Message,
                RecordsIndexed = recordsIndexed,
                CreatedAt = job.CreatedAt,
                StartedAt = job.StartedAt,
                CompletedAt = DateTime.UtcNow,
            }, ct);
            await _publisher.Publish(new AtlasIndexCompletedEvent(job.ConnectionId, job.Id, false, recordsIndexed, ex.Message), ct);
            return true;
        }
    }

    private static AtlasIndexedRecordRecord ToIndexedRecord(Guid connectionId, string entity, string repository, JsonObject row)
    {
        var raw = row.ToJsonString();
        var title = entity switch
        {
            "repositories" => ReadString(row, "full_name") ?? ReadString(row, "name") ?? repository,
            "issues" => ReadString(row, "title") ?? $"Issue {ReadString(row, "number")}",
            "pull_requests" => ReadString(row, "title") ?? $"Pull request {ReadString(row, "number")}",
            "commits" => ReadNestedString(row, "commit", "message")?.Split('\n')[0] ?? ReadString(row, "sha") ?? "Commit",
            _ => ReadString(row, "id") ?? entity,
        };
        var externalId = ReadString(row, "id")
            ?? ReadString(row, "node_id")
            ?? ReadString(row, "sha")
            ?? $"{repository}:{entity}:{title}";
        return new AtlasIndexedRecordRecord
        {
            ConnectionId = connectionId,
            Entity = entity,
            ExternalId = externalId,
            Title = title,
            SearchText = $"{repository} {title} {raw}",
            RawJson = raw,
            ExternalUpdatedAt = ReadDate(row, "updated_at") ?? ReadNestedDate(row, "commit", "committer", "date"),
        };
    }

    private static IReadOnlyList<string> ParseStringArray(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string? ReadString(JsonObject obj, string key)
        => obj.TryGetPropertyValue(key, out var node) ? node?.GetValue<object>()?.ToString() : null;

    private static string? ReadNestedString(JsonObject obj, params string[] path)
    {
        JsonNode? current = obj;
        foreach (var segment in path)
        {
            if (current is not JsonObject currentObj || !currentObj.TryGetPropertyValue(segment, out current))
                return null;
        }
        return current?.GetValue<object>()?.ToString();
    }

    private static DateTime? ReadDate(JsonObject obj, string key)
        => DateTime.TryParse(ReadString(obj, key), out var parsed) ? parsed.ToUniversalTime() : null;

    private static DateTime? ReadNestedDate(JsonObject obj, params string[] path)
        => DateTime.TryParse(ReadNestedString(obj, path), out var parsed) ? parsed.ToUniversalTime() : null;
}

internal sealed class AtlasIndexSchedulerService : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(10);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AtlasIndexSchedulerService> _logger;

    public AtlasIndexSchedulerService(IServiceScopeFactory scopeFactory, ILogger<AtlasIndexSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AtlasIndexSchedulerService started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var indexing = scope.ServiceProvider.GetRequiredService<AtlasIndexingService>();
                var processed = await indexing.ProcessOneAsync(stoppingToken);
                if (!processed)
                    await Task.Delay(IdleDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Atlas index scheduler tick failed");
                await Task.Delay(IdleDelay, stoppingToken);
            }
        }
    }
}
