using EnterpriseAgentOs.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseAgentOs.Application.Services;

internal sealed class AgentLogService : IAgentLogService
{
    private static readonly string[] SecretKeySubstrings =
    [
        "apikey", "token", "secret", "password", "credential", "key"
    ];

    private readonly IAgentLogRepository _agentLogRepository;
    private readonly ITopicEventSender _topicEventSender;
    private readonly IAgentRepository _agentRepository;
    private readonly IPostHogService _postHogService;
    private readonly ILogger<AgentLogService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public AgentLogService(
        IAgentLogRepository agentLogRepository,
        ITopicEventSender topicEventSender,
        IAgentRepository agentRepository,
        IPostHogService postHogService,
        ILogger<AgentLogService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _agentLogRepository = agentLogRepository;
        _topicEventSender = topicEventSender;
        _agentRepository = agentRepository;
        _postHogService = postHogService;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public Task<List<AgentLogRecord>> ListForAgentAsync(Guid agentId, DateTime? before, int limit, CancellationToken ct = default)
        => _agentLogRepository.ListAsync(agentId, before, limit, ct);

    public async Task<GlobalLogsPage> ListGlobalAsync(GlobalLogFiltersInput filters, CancellationToken ct = default)
    {
        var limit = Math.Clamp(filters.Limit, 1, 200);
        var skip = Math.Max(filters.Skip, 0);
        var (rows, total) = await _agentLogRepository.ListGlobalAsync(filters.Search, filters.AgentName, filters.Type, skip, limit, ct);
        var items = rows.Select(r => r.Log.ToDto(r.AgentName)).ToList();
        return new GlobalLogsPage(items, total);
    }

    public async Task<AgentLogRecord> AppendAsync(AgentLogRecord record, CancellationToken ct = default)
    {
        var saved = await _agentLogRepository.AppendAsync(record, ct);
        await _topicEventSender.SendAsync($"agent-log:{saved.AgentId}", saved.ToDto(), ct);
        return saved;
    }

    public async Task<AgentLogRecord> SendMessageAsync(Guid agentId, string content, Guid userId, CancellationToken ct = default)
    {
        var agent = await _agentRepository.GetAsync(agentId, ct);
        if (agent is null) throw new InvalidOperationException($"Agent {agentId} not found");

        var correlationId = Guid.NewGuid().ToString("N");

        var record = await AppendAsync(new AgentLogRecord
        {
            AgentId = agentId,
            Type = AgentLogType.MessageIn,
            Content = content,
            CorrelationId = correlationId,
            Time = DateTime.UtcNow,
        });

        if (string.IsNullOrEmpty(agent.PodName))
        {
            _logger.LogWarning("Agent {AgentId} has no pod, message queued only", agentId);
            return record;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var turnService = scope.ServiceProvider.GetRequiredService<AgentTurnService>();
                await turnService.RunTurnAsync(agentId, content, correlationId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Turn failed for agent {AgentId}", agentId);

                try
                {
                    using var errorScope = _serviceScopeFactory.CreateScope();
                    var repo = errorScope.ServiceProvider.GetRequiredService<IAgentLogRepository>();
                    var sender = errorScope.ServiceProvider.GetRequiredService<ITopicEventSender>();

                    var errorRecord = await repo.AppendAsync(new AgentLogRecord
                    {
                        AgentId = agentId,
                        Type = AgentLogType.Error,
                        Content = $"Turn failed: {ex.Message}",
                        CorrelationId = correlationId,
                        Time = DateTime.UtcNow,
                    });
                    await sender.SendAsync($"agent-log:{agentId}", errorRecord.ToDto(), CancellationToken.None);
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Failed to log error for agent {AgentId}", agentId);
                }
            }
        }, CancellationToken.None);

        return record;
    }

    // ── Audit (merged from Entities/Audit) ───────────────────────────────

    public async Task RecordToolCallAsync(
        Guid agentId,
        Guid? userId,
        string skillName,
        string action,
        string paramsJson,
        string? resultSummary,
        long durationMs,
        CancellationToken ct = default)
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

        await _agentLogRepository.AppendPairAsync(toolCall, toolResult, ct);
        await _topicEventSender.SendAsync($"agent-log:{agentId}", toolCall.ToDto(), ct);
        await _topicEventSender.SendAsync($"agent-log:{agentId}", toolResult.ToDto(), ct);
    }

    public async Task<(List<AgentLogRecord> Items, int Total)> GetAuditLogAsync(
        Guid agentId, int limit, int offset, CancellationToken ct = default)
    {
        return await _agentLogRepository.GetToolCallsAsync(agentId, limit, offset, ct);
    }

    public async Task<Dictionary<string, AgentLogRecord>> GetResultsByCorrelationAsync(
        Guid agentId, IReadOnlyCollection<string> correlationIds, CancellationToken ct = default)
    {
        return await _agentLogRepository.GetResultsByCorrelationAsync(agentId, correlationIds, ct);
    }

    // ── Secret redaction ─────────────────────────────────────────────────

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
                    dict[prop.Name] = "[REDACTED]";
                else
                    dict[prop.Name] = JsonElementToObject(prop.Value);
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
