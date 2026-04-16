using System.Text.Json;
using EnterpriseAgentOs.Api.Database.Models;

namespace EnterpriseAgentOs.Api.Entities.Audit;

public sealed class AuditService : IAuditService
{
    private static readonly string[] SecretKeySubstrings =
    [
        "apikey", "token", "secret", "password", "credential", "key"
    ];

    private readonly IAuditRepository _repository;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IAuditRepository repository, ILogger<AuditService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task RecordToolCallAsync(
        Guid agentId,
        Guid? userId,
        string skillName,
        string action,
        string paramsJson,
        string? resultSummary,
        long durationMs)
    {
        var redacted = RedactSecrets(paramsJson);
        var correlationId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        var toolCall = new AgentLogRecord
        {
            AgentId = agentId,
            Time = now,
            Type = AgentLogType.ToolCall,
            Tool = action,
            Integration = skillName,
            Content = redacted,
            CorrelationId = correlationId,
        };

        var toolResult = new AgentLogRecord
        {
            AgentId = agentId,
            Time = now.AddMilliseconds(1),
            Type = AgentLogType.ToolResult,
            Tool = action,
            Integration = skillName,
            Content = resultSummary ?? string.Empty,
            DurationMs = (int)Math.Min(durationMs, int.MaxValue),
            CorrelationId = correlationId,
        };

        await _repository.AddPairAsync(toolCall, toolResult);
    }

    public async Task<(List<AgentLogRecord> Items, int Total)> GetAuditLogAsync(
        Guid agentId, int limit, int offset)
    {
        return await _repository.GetByAgentAsync(agentId, limit, offset);
    }

    /// <summary>
    /// Parses paramsJson and replaces any value whose key contains a secret keyword
    /// (case-insensitive substring match) with "[REDACTED]".
    /// </summary>
    private string RedactSecrets(string paramsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(paramsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return paramsJson;

            var dict = new Dictionary<string, object?>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (IsSecretKey(prop.Name))
                {
                    dict[prop.Name] = "[REDACTED]";
                }
                else
                {
                    dict[prop.Name] = JsonElementToObject(prop.Value);
                }
            }

            return JsonSerializer.Serialize(dict);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to redact params JSON; storing as-is");
            return paramsJson;
        }
    }

    private static bool IsSecretKey(string key)
    {
        var lower = key.ToLowerInvariant();
        return SecretKeySubstrings.Any(s => lower.Contains(s));
    }

    private static object? JsonElementToObject(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.TryGetInt64(out var l) ? (object)l : el.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Object => el.EnumerateObject().ToDictionary(p => p.Name, p => JsonElementToObject(p.Value)),
        JsonValueKind.Array => el.EnumerateArray().Select(JsonElementToObject).ToList(),
        _ => el.GetRawText(),
    };
}
