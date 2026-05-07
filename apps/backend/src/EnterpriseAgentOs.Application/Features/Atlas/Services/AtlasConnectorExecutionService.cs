using System.Diagnostics;
using System.Text.Json.Nodes;
using MediatR;

namespace EnterpriseAgentOs.Application.Features.Atlas;

internal sealed class AtlasConnectorExecutionService : IAtlasConnectorExecutionService
{
    private readonly IAtlasConnectionRepository _connections;
    private readonly IAtlasIndexedRecordRepository _records;
    private readonly IAtlasRequestHistoryRepository _history;
    private readonly AtlasGitHubClient _github;
    private readonly IPublisher _publisher;

    public AtlasConnectorExecutionService(
        IAtlasConnectionRepository connections,
        IAtlasIndexedRecordRepository records,
        IAtlasRequestHistoryRepository history,
        AtlasGitHubClient github,
        IPublisher publisher)
    {
        _connections = connections;
        _records = records;
        _history = history;
        _github = github;
        _publisher = publisher;
    }

    public async Task<JsonElement> ExecuteAsync(AtlasConnectorExecuteRequest request, CancellationToken ct = default)
    {
        var started = Stopwatch.GetTimestamp();
        var type = request.Action == "context_store_search" ? AtlasRequestType.Search : AtlasRequestType.Direct;
        var success = false;
        string? error = null;
        try
        {
            var connection = await _connections.GetByAsync(new AtlasConnectionFilter { Id = request.SourceId }, ct)
                ?? throw new InvalidOperationException("Atlas connector source_id was not found.");
            if (connection.Provider != AtlasConnectorProvider.GitHub)
                throw new InvalidOperationException($"Unsupported Atlas provider '{connection.Provider}'.");

            JsonElement response = type == AtlasRequestType.Search
                ? await ExecuteSearchAsync(connection, request, ElapsedMs(started), ct)
                : await ExecuteDirectAsync(connection, request, ElapsedMs(started), ct);
            success = true;
            return response;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return JsonSerializer.SerializeToElement(new
            {
                status = "error",
                result = Array.Empty<object>(),
                connector_metadata = (object?)null,
                execution_metadata = new
                {
                    connector_instance_id = $"source_id:{request.SourceId}",
                    execution_time_ms = ElapsedMs(started),
                },
                error = new { code = "atlas_connector_error", message = ex.Message, detail = ex.ToString() },
            });
        }
        finally
        {
            var durationMs = ElapsedMs(started);
            await _history.AddAsync(new AtlasRequestHistoryRecord
            {
                ConnectionId = request.SourceId,
                Type = type,
                Entity = request.Entity,
                Action = request.Action,
                ParamsJson = request.Params.GetRawText(),
                Success = success,
                DurationMs = durationMs,
                Error = error,
            }, ct);
            await _publisher.Publish(new AtlasConnectorExecutedEvent(request.SourceId, request.Entity, request.Action, success, durationMs), ct);
        }
    }

    private async Task<JsonElement> ExecuteSearchAsync(
        AtlasConnectorConnectionRecord connection,
        AtlasConnectorExecuteRequest request,
        int durationMs,
        CancellationToken ct)
    {
        var query = ExtractSearchQuery(request.Params);
        var cursor = ReadString(request.Params, "cursor");
        var limit = ReadInt(request.Params, "limit") ?? ReadInt(request.Params, "per_page") ?? 20;
        var page = await _records.SearchAsync(new AtlasIndexedRecordFilter
        {
            ConnectionId = connection.Id,
            Entity = request.Entity,
            Query = query,
            Cursor = cursor,
            Limit = Math.Clamp(limit, 1, 100),
        }, ct);
        var data = page.Records
            .Select(r => ProjectFields(JsonNode.Parse(r.RawJson) as JsonObject ?? new JsonObject(), request.SelectFields))
            .ToList();

        return JsonSerializer.SerializeToElement(new
        {
            status = "success",
            result = new
            {
                data,
                meta = new { has_more = page.HasMore, cursor = page.Cursor, took_ms = durationMs },
            },
            connector_metadata = (object?)null,
            execution_metadata = new
            {
                connector_instance_id = $"source_id:{connection.Id}",
                execution_time_ms = durationMs,
            },
        });
    }

    private async Task<JsonElement> ExecuteDirectAsync(
        AtlasConnectorConnectionRecord connection,
        AtlasConnectorExecuteRequest request,
        int durationMs,
        CancellationToken ct)
    {
        var result = await _github.ExecuteDirectAsync(request.Entity, request.Action, request.Params, ct);
        object projected = result.ValueKind == JsonValueKind.Array
            ? result.EnumerateArray()
                .Select(row => ProjectFields(JsonNode.Parse(row.GetRawText()) as JsonObject ?? new JsonObject(), request.SelectFields))
                .ToList()
            : ProjectFields(JsonNode.Parse(result.GetRawText()) as JsonObject ?? new JsonObject(), request.SelectFields);

        return JsonSerializer.SerializeToElement(new
        {
            status = "success",
            result = projected,
            connector_metadata = new { has_next_page = false, end_cursor = (string?)null },
            execution_metadata = new
            {
                connector_instance_id = $"source_id:{connection.Id}",
                execution_time_ms = durationMs,
            },
        });
    }

    private static Dictionary<string, object?> ProjectFields(JsonObject row, IReadOnlyList<string>? fields)
    {
        var selected = fields is { Count: > 0 } ? fields : row.Select(p => p.Key).ToList();
        var dict = new Dictionary<string, object?>();
        foreach (var field in selected)
        {
            if (TryReadField(row, field, out var value))
                dict[field] = value;
        }
        return dict;
    }

    private static bool TryReadField(JsonObject row, string field, out object? value)
    {
        value = null;
        var snake = ToSnakeCase(field);
        if (!row.TryGetPropertyValue(field, out var node) && !row.TryGetPropertyValue(snake, out node))
            return TryReadSyntheticField(row, field, out value);

        value = node switch
        {
            null => null,
            JsonValue v => v.GetValue<object>(),
            _ => node.ToJsonString(),
        };
        return true;
    }

    private static bool TryReadSyntheticField(JsonObject row, string field, out object? value)
    {
        value = field switch
        {
            "defaultBranch" => ReadNestedString(row, "default_branch"),
            "stargazerCount" => ReadNodeValue(row, "stargazers_count"),
            "forkCount" => ReadNodeValue(row, "forks_count"),
            "createdAt" => ReadNodeValue(row, "created_at"),
            "updatedAt" => ReadNodeValue(row, "updated_at"),
            "closedAt" => ReadNodeValue(row, "closed_at"),
            "mergedAt" => ReadNodeValue(row, "merged_at"),
            "abbreviatedOid" => ReadString(row, "sha") is { } sha && sha.Length > 7 ? sha[..7] : ReadString(row, "sha"),
            "messageHeadline" => ReadNestedString(row, "commit", "message")?.Split('\n')[0],
            "committedDate" => ReadNestedString(row, "commit", "committer", "date"),
            "changedFiles" => ReadNodeValue(row, "files"),
            _ => null,
        };
        return value is not null;
    }

    private static string? ExtractSearchQuery(JsonElement parameters)
    {
        if (parameters.ValueKind != JsonValueKind.Object) return null;
        if (parameters.TryGetProperty("query", out var query))
        {
            if (query.ValueKind == JsonValueKind.String) return query.GetString();
            if (query.TryGetProperty("filter", out var filter))
                return FindFirstString(filter);
        }
        return FindFirstString(parameters);
    }

    private static string? FindFirstString(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String) return element.GetString();
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var value = FindFirstString(property.Value);
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
        }
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var value = FindFirstString(item);
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
        }
        return null;
    }

    private static string? ReadString(JsonElement obj, string name)
        => obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out var value) ? value.GetString() : null;

    private static int? ReadInt(JsonElement obj, string name)
        => obj.ValueKind == JsonValueKind.Object
           && obj.TryGetProperty(name, out var value)
           && value.TryGetInt32(out var parsed)
            ? parsed
            : null;

    private static string? ReadString(JsonObject row, string key)
        => row.TryGetPropertyValue(key, out var node) ? node?.GetValue<object>()?.ToString() : null;

    private static object? ReadNodeValue(JsonObject row, string key)
        => row.TryGetPropertyValue(key, out var node)
            ? node is JsonValue value ? value.GetValue<object>() : node?.ToJsonString()
            : null;

    private static string? ReadNestedString(JsonObject row, params string[] path)
    {
        JsonNode? current = row;
        foreach (var segment in path)
        {
            if (current is not JsonObject currentObj || !currentObj.TryGetPropertyValue(segment, out current))
                return null;
        }
        return current?.GetValue<object>()?.ToString();
    }

    private static string ToSnakeCase(string value)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c) && i > 0) builder.Append('_');
            builder.Append(char.ToLowerInvariant(c));
        }
        return builder.ToString();
    }

    private static int ElapsedMs(long started)
        => (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
}
