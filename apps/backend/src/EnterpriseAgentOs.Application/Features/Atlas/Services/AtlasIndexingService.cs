using System.Text.Json;
using System.Text.Json.Nodes;
using MediatR;

namespace EnterpriseAgentOs.Application.Features.Agents.Integrations;

internal sealed class IntegrationIndexingService
{
    private readonly IIntegrationConnectionRepository _connections;
    private readonly IIntegrationIndexEntityStatusRepository _entityStatuses;
    private readonly IIntegrationIndexJobRepository _jobs;
    private readonly IIntegrationIndexedRecordRepository _records;
    private readonly IIntegrationActivityRepository _activity;
    private readonly GitHubIntegrationClient _github;
    private readonly IPublisher _publisher;
    private readonly ILogger<IntegrationIndexingService> _logger;

    public IntegrationIndexingService(
        IIntegrationConnectionRepository connections,
        IIntegrationIndexEntityStatusRepository entityStatuses,
        IIntegrationIndexJobRepository jobs,
        IIntegrationIndexedRecordRepository records,
        IIntegrationActivityRepository activity,
        GitHubIntegrationClient github,
        IPublisher publisher,
        ILogger<IntegrationIndexingService> logger)
    {
        _connections = connections;
        _entityStatuses = entityStatuses;
        _jobs = jobs;
        _records = records;
        _activity = activity;
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
            var connection = await _connections.GetByAsync(new IntegrationConnectionFilter { Id = job.ConnectionId }, ct)
                ?? throw new InvalidOperationException("Integration connection not found.");
            var repositories = ParseStringArray(connection.RepositoriesJson);
            var entities = ParseStringArray(connection.EntitiesJson);

            await _connections.SetStatusAsync(connection.Id, IntegrationConnectionStatus.Indexing, null, ct);
            await LogActivityAsync(connection.Id, "index_started", null, "Indexing started.", new { JobId = job.Id }, true, ct);
            await _records.DeleteForConnectionAsync(connection.Id, ct);
            await LogActivityAsync(connection.Id, "records_cleared", null, "Previous indexed records cleared.", new { JobId = job.Id }, true, ct);

            foreach (var entity in entities)
            {
                await _entityStatuses.UpsertAsync(new IntegrationIndexEntityStatusRecord
                {
                    ConnectionId = connection.Id,
                    Entity = entity,
                    Status = IntegrationIndexEntityStatus.Indexing,
                }, ct);
                await LogActivityAsync(connection.Id, "entity_index_started", entity, $"Indexing {DisplayEntity(entity)} started.", new
                {
                    JobId = job.Id,
                    Repositories = repositories,
                }, true, ct);

                var entityRecords = new List<IntegrationIndexedRecordRecord>();
                foreach (var repository in repositories)
                {
                    await LogActivityAsync(connection.Id, "repository_fetch_started", entity, $"Fetching {DisplayEntity(entity)} from {repository}.", new
                    {
                        JobId = job.Id,
                        Repository = repository,
                    }, true, ct);
                    var rows = await _github.FetchEntityAsync(entity, repository, perPage: 100, ct);
                    entityRecords.AddRange(rows.Select(row => ToIndexedRecord(connection.Id, entity, repository, row)));
                    await LogActivityAsync(connection.Id, "repository_fetch_completed", entity, $"Fetched {rows.Count} {DisplayEntity(entity)} from {repository}.", new
                    {
                        JobId = job.Id,
                        Repository = repository,
                        RecordCount = rows.Count,
                    }, true, ct);
                }

                if (entityRecords.Count > 0)
                    await _records.UpsertManyAsync(entityRecords, ct);
                recordsIndexed += entityRecords.Count;

                await _entityStatuses.UpsertAsync(new IntegrationIndexEntityStatusRecord
                {
                    ConnectionId = connection.Id,
                    Entity = entity,
                    Status = IntegrationIndexEntityStatus.Ready,
                    RecordCount = entityRecords.Count,
                    LastSyncedAt = DateTime.UtcNow,
                }, ct);
                await LogActivityAsync(connection.Id, "entity_index_completed", entity, $"Indexed {entityRecords.Count} {DisplayEntity(entity)}.", new
                {
                    JobId = job.Id,
                    RecordCount = entityRecords.Count,
                }, true, ct);
            }

            await _connections.SetStatusAsync(connection.Id, IntegrationConnectionStatus.Ready, null, ct);
            await _jobs.UpdateAsync(new IntegrationIndexJobRecord
            {
                Id = job.Id,
                ConnectionId = job.ConnectionId,
                Status = IntegrationIndexJobStatus.Succeeded,
                RecordsIndexed = recordsIndexed,
                CreatedAt = job.CreatedAt,
                StartedAt = job.StartedAt,
                CompletedAt = DateTime.UtcNow,
            }, ct);
            await LogActivityAsync(connection.Id, "index_completed", null, $"Indexing completed with {recordsIndexed} records.", new
            {
                JobId = job.Id,
                RecordsIndexed = recordsIndexed,
            }, true, ct);
            await _publisher.Publish(new IntegrationIndexCompletedEvent(connection.Id, job.Id, true, recordsIndexed, null), ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Integration index job {JobId} failed", job.Id);
            await _connections.SetStatusAsync(job.ConnectionId, IntegrationConnectionStatus.Failed, ex.Message, ct);
            await _jobs.UpdateAsync(new IntegrationIndexJobRecord
            {
                Id = job.Id,
                ConnectionId = job.ConnectionId,
                Status = IntegrationIndexJobStatus.Failed,
                Error = ex.Message,
                RecordsIndexed = recordsIndexed,
                CreatedAt = job.CreatedAt,
                StartedAt = job.StartedAt,
                CompletedAt = DateTime.UtcNow,
            }, ct);
            await LogActivityAsync(job.ConnectionId, "index_failed", null, $"Indexing failed: {ex.Message}", new
            {
                JobId = job.Id,
                RecordsIndexed = recordsIndexed,
                Error = ex.Message,
            }, false, ct);
            await _publisher.Publish(new IntegrationIndexCompletedEvent(job.ConnectionId, job.Id, false, recordsIndexed, ex.Message), ct);
            return true;
        }
    }

    private static IntegrationIndexedRecordRecord ToIndexedRecord(Guid connectionId, string entity, string repository, JsonObject row)
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
        return new IntegrationIndexedRecordRecord
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

    private static string DisplayEntity(string entity)
        => entity.Replace('_', ' ');
}

internal sealed class IntegrationIndexSchedulerService : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(10);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IntegrationIndexSchedulerService> _logger;

    public IntegrationIndexSchedulerService(IServiceScopeFactory scopeFactory, ILogger<IntegrationIndexSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("IntegrationIndexSchedulerService started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var indexing = scope.ServiceProvider.GetRequiredService<IntegrationIndexingService>();
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
                _logger.LogError(ex, "Integration index scheduler tick failed");
                await Task.Delay(IdleDelay, stoppingToken);
            }
        }
    }
}
