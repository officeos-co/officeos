namespace OffceOs.Application.Features.Agents;

internal sealed class OpenCodeRunService : IAgentRunExecutionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string DefaultOpenCodeModel = "openai/gpt-5.2";

    private readonly IAgentRepository _agentRepository;
    private readonly IAgentRunRepository _agentRunRepository;
    private readonly IAgentLogService _agentLogService;
    private readonly IOpenCodeProcessService _openCodeProcessService;
    private readonly ILogger<OpenCodeRunService> _logger;

    public OpenCodeRunService(
        IAgentRepository agentRepository,
        IAgentRunRepository agentRunRepository,
        IAgentLogService agentLogService,
        IOpenCodeProcessService openCodeProcessService,
        ILogger<OpenCodeRunService> logger)
    {
        _agentRepository = agentRepository;
        _agentRunRepository = agentRunRepository;
        _agentLogService = agentLogService;
        _openCodeProcessService = openCodeProcessService;
        _logger = logger;
    }

    public async Task<AgentRunExecutionResult> CreateAsync(CreateAgentRunExecutionRequest request, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        var agent = await ResolveAgentAsync(request.AgentRef, workspaceId, ct)
            ?? throw new InvalidOperationException($"Agent '{request.AgentRef}' was not found.");

        var engineRef = NormalizeEngineRef(request.EngineRef);
        var now = DateTime.UtcNow;
        var run = await _agentRunRepository.CreateAsync(new AgentRunRecord
        {
            AgentId = agent.Id,
            WorkspaceId = workspaceId,
            Kind = "opencode",
            Purpose = AgentRunPurposeKinds.Manual,
            DefinitionId = agent.ActiveDefinitionId,
            Status = "queued",
            Name = request.AgentRef,
            Description = engineRef,
            Prompt = request.Task.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        }, ct);

        await _agentLogService.AppendAsync(new AgentLogRecord
        {
            AgentId = agent.Id,
            WorkspaceId = workspaceId,
            Type = AgentLogType.MessageIn,
            Content = request.Task.Trim(),
            RunId = run.Id,
            CorrelationId = run.Id.ToString("N"),
        }, ct);

        return new AgentRunExecutionResult(run, "opencode", engineRef);
    }

    public Task<IReadOnlyList<AgentRunRecord>> ListAsync(Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        _ = ownerId;
        return _agentRunRepository.ListAsync(new AgentRunFilter { WorkspaceId = workspaceId }, 100, ct);
    }

    public async Task<AgentRunRecord?> GetAsync(Guid runId, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        _ = ownerId;
        return await _agentRunRepository.GetByAsync(new AgentRunFilter { Id = runId, WorkspaceId = workspaceId }, ct);
    }

    public async Task<bool> CancelAsync(Guid runId, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        var run = await GetAsync(runId, ownerId, workspaceId, ct);
        if (run is null)
            return false;

        if (run.Status is "completed" or "failed" or "canceled")
            return true;

        run.Status = run.Status == "queued" ? "canceled" : "cancel-requested";
        run.UpdatedAt = DateTime.UtcNow;
        if (run.Status == "canceled")
            run.CompletedAt = DateTime.UtcNow;
        await _agentRunRepository.UpdateAsync(run, ct);
        return true;
    }

    public async Task<AgentRunLogResult> LogsAsync(Guid runId, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        _ = ownerId;
        var page = await _agentLogService.ListAsync(new AgentLogQueryRequest(
            WorkspaceId: workspaceId,
            RunId: runId,
            Limit: 500,
            Sort: AgentLogSort.TimeAscending), ct);
        return new AgentRunLogResult(page.Items);
    }

    public async Task ExecuteQueuedRunAsync(AgentRunRecord run, CancellationToken ct = default)
    {
        if (run.Status != "queued" || run.Kind != "opencode")
            return;

        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = run.AgentId, WorkspaceId = run.WorkspaceId }, ct);
        if (agent is null)
        {
            await CompleteAsync(run, "failed", null, "Agent not found.", ct);
            return;
        }

        run.Status = "running";
        run.UpdatedAt = DateTime.UtcNow;
        await _agentRunRepository.UpdateAsync(run, ct);
        await AppendRunSystemLogAsync(run, agent.Id, "Run started.", ct);

        var workspace = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "officeos-runs", run.Id.ToString("N"));
        Directory.CreateDirectory(workspace);

        try
        {
            var model = ResolveModel(agent);
            var reportedError = false;
            var reportedErrorLines = new List<string>();
            var result = await _openCodeProcessService.RunAsync(
                new ProcessRunRequest(
                    "opencode",
                    ["run", "--format", "json", "--print-logs", "--log-level", "WARN", "--model", model, run.Prompt],
                    workspace,
                    new Dictionary<string, string>()),
                async (line, token) =>
                {
                    var entry = await AppendOpenCodeEventAsync(run, agent, line, "stdout", token);
                    if (entry?.Severity == ResourceLogSeverityKinds.Error)
                    {
                        reportedError = true;
                        reportedErrorLines.Add(entry.Content);
                    }
                },
                async (line, token) =>
                {
                    var entry = await AppendOpenCodeEventAsync(run, agent, line, "stderr", token);
                    if (entry?.Severity == ResourceLogSeverityKinds.Error)
                    {
                        reportedError = true;
                        reportedErrorLines.Add(entry.Content);
                    }
                },
                ct);

            if (result.ExitCode == 0 && !reportedError)
                await CompleteAsync(run, "completed", run.Result, null, ct);
            else
                await CompleteAsync(run, "failed", null, OpenCodeFailureMessage(result, reportedErrorLines), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCode run {RunId} failed", run.Id);
            await CompleteAsync(run, "failed", null, ex.Message, ct);
        }
    }

    private async Task<OpenCodeLogEntry?> AppendOpenCodeEventAsync(AgentRunRecord run, AgentRecord agent, string line, string source, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var entry = ParseOpenCodeLine(line, source);
        if (entry.Type == AgentLogType.MessageOut)
            run.Result = entry.Content;

        await _agentLogService.AppendAsync(new AgentLogRecord
        {
            AgentId = agent.Id,
            WorkspaceId = run.WorkspaceId,
            ResourceKind = ResourceLogKinds.Run,
            ResourceId = run.Id,
            ResourceName = run.Id.ToString("N"),
            ParentResourceKind = ResourceLogKinds.Agent,
            ParentResourceId = agent.Id,
            Type = entry.Type,
            Severity = entry.Severity,
            Tool = entry.Tool,
            Content = entry.Content,
            RunId = run.Id,
            CorrelationId = run.Id.ToString("N"),
            MetadataJson = JsonSerializer.Serialize(entry.Metadata, JsonOptions),
        }, ct);
        return entry;
    }

    private static OpenCodeLogEntry ParseOpenCodeLine(string line, string source)
    {
        if (source == "stderr")
            return ParseOpenCodeDiagnosticLine(line);

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            var kind = TryGetString(root, "type");
            if (TryFindObject(root, "part", out var part))
                return ParseOpenCodePart(part, kind, source);

            var content = FirstString(root, "message", "content", "text", "error", "status") ?? root.GetRawText();
            var metadata = new Dictionary<string, object?>
            {
                ["source"] = source,
                ["opencodeType"] = kind,
            };

            return kind?.ToLowerInvariant() switch
            {
                "tool_call" or "tool-call" or "tool_call_start" or "tool-call-start" => new(AgentLogType.ToolCall, content, ResourceLogSeverityKinds.Info, TryGetToolName(root), metadata),
                "tool_result" or "tool-result" or "tool_call_complete" or "tool-call-complete" => new(AgentLogType.ToolResult, content, ResourceLogSeverityKinds.Info, TryGetToolName(root), metadata),
                "tool_call_error" or "tool-call-error" or "error" => new(AgentLogType.Error, content, ResourceLogSeverityKinds.Error, TryGetToolName(root), metadata),
                "content" or "message" or "assistant" or "result" => new(AgentLogType.MessageOut, content, ResourceLogSeverityKinds.Info, null, metadata),
                "thinking" => new(AgentLogType.ModelCall, content, ResourceLogSeverityKinds.Debug, null, metadata),
                "status" or "done" => new(AgentLogType.System, content, ResourceLogSeverityKinds.Debug, null, metadata),
                _ => new(AgentLogType.System, content, ResourceLogSeverityKinds.Debug, null, metadata),
            };
        }
        catch (JsonException)
        {
            return new OpenCodeLogEntry(
                AgentLogType.System,
                line,
                ResourceLogSeverityKinds.Debug,
                null,
                new Dictionary<string, object?> { ["source"] = source });
        }
    }

    private static OpenCodeLogEntry ParseOpenCodeDiagnosticLine(string line)
    {
        var severity = line.StartsWith("ERROR ", StringComparison.Ordinal) || LooksLikeOpenCodeException(line)
            ? ResourceLogSeverityKinds.Error
            : line.StartsWith("WARN ", StringComparison.Ordinal)
                ? ResourceLogSeverityKinds.Warning
                : line.StartsWith("DEBUG ", StringComparison.Ordinal)
                    ? ResourceLogSeverityKinds.Debug
                    : ResourceLogSeverityKinds.Info;

        return new OpenCodeLogEntry(
            severity == ResourceLogSeverityKinds.Error ? AgentLogType.Error : AgentLogType.System,
            line,
            severity,
            null,
            new Dictionary<string, object?> { ["source"] = "stderr" });
    }

    private static bool LooksLikeOpenCodeException(string line)
    {
        return line.Contains("ProviderModelNotFoundError", StringComparison.Ordinal)
            || line.Contains("Error:", StringComparison.Ordinal)
            || line.Contains("Exception:", StringComparison.Ordinal);
    }

    private static string OpenCodeFailureMessage(ProcessRunResult result, IReadOnlyList<string> reportedErrorLines)
    {
        if (reportedErrorLines.Count > 0)
            return reportedErrorLines.Last();

        return string.IsNullOrWhiteSpace(result.StandardError)
            ? $"OpenCode exited with {result.ExitCode}."
            : result.StandardError;
    }

    private static OpenCodeLogEntry ParseOpenCodePart(JsonElement part, string? eventType, string source)
    {
        var partType = TryGetString(part, "type");
        var metadata = new Dictionary<string, object?>
        {
            ["source"] = source,
            ["opencodeType"] = eventType,
            ["partType"] = partType,
            ["partId"] = TryGetString(part, "id"),
            ["messageId"] = TryGetString(part, "messageID"),
            ["callId"] = TryGetString(part, "callID"),
        };

        if (partType?.Equals("tool", StringComparison.OrdinalIgnoreCase) == true)
        {
            var tool = TryGetString(part, "tool");
            var state = part.TryGetProperty("state", out var stateElement) ? stateElement : default;
            var status = state.ValueKind == JsonValueKind.Object ? TryGetString(state, "status") : null;
            var input = state.ValueKind == JsonValueKind.Object && state.TryGetProperty("input", out var inputElement)
                ? inputElement.GetRawText()
                : part.GetRawText();
            var output = state.ValueKind == JsonValueKind.Object
                ? FirstString(state, "output", "result", "error")
                : null;
            var isCompleted = status is "completed" or "error";
            var type = isCompleted ? AgentLogType.ToolResult : AgentLogType.ToolCall;
            var severity = status == "error" ? ResourceLogSeverityKinds.Error : ResourceLogSeverityKinds.Info;
            var content = isCompleted
                ? string.IsNullOrWhiteSpace(output) ? state.GetRawText() : output
                : input;

            metadata["toolStatus"] = status;
            return new OpenCodeLogEntry(type, content, severity, tool, metadata);
        }

        if (partType?.Equals("text", StringComparison.OrdinalIgnoreCase) == true)
        {
            return new OpenCodeLogEntry(
                AgentLogType.MessageOut,
                FirstString(part, "text", "content") ?? part.GetRawText(),
                ResourceLogSeverityKinds.Info,
                null,
                metadata);
        }

        if (partType?.Equals("reasoning", StringComparison.OrdinalIgnoreCase) == true)
        {
            return new OpenCodeLogEntry(
                AgentLogType.ModelCall,
                FirstString(part, "text", "content") ?? part.GetRawText(),
                ResourceLogSeverityKinds.Debug,
                null,
                metadata);
        }

        return new OpenCodeLogEntry(
            AgentLogType.System,
            part.GetRawText(),
            ResourceLogSeverityKinds.Debug,
            null,
            metadata);
    }

    private static string? TryGetToolName(JsonElement root)
    {
        if (TryGetString(root, "tool") is { } tool)
            return tool;

        if (TryFindObject(root, "toolCall", out var toolCall))
            return TryGetString(toolCall, "name");

        return null;
    }

    private static string? FirstString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var value = TryGetString(element, propertyName);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
            return null;

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => property.ToString(),
            _ => null,
        };
    }

    private static bool TryFindObject(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty(propertyName, out value) && value.ValueKind == JsonValueKind.Object)
                return true;

            foreach (var property in element.EnumerateObject())
            {
                if (TryFindObject(property.Value, propertyName, out value))
                    return true;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (TryFindObject(item, propertyName, out value))
                    return true;
            }
        }

        value = default;
        return false;
    }

    private sealed record OpenCodeLogEntry(
        AgentLogType Type,
        string Content,
        string Severity,
        string? Tool,
        IReadOnlyDictionary<string, object?> Metadata);

    private async Task CompleteAsync(AgentRunRecord run, string status, string? result, string? error, CancellationToken ct)
    {
        run.Status = status;
        run.Result = result;
        run.Error = error;
        run.CompletedAt = DateTime.UtcNow;
        run.UpdatedAt = DateTime.UtcNow;
        await _agentRunRepository.UpdateAsync(run, ct);
        await AppendRunSystemLogAsync(run, run.AgentId, error is null ? $"Run {status}." : $"Run {status}: {error}", ct);
    }

    private Task AppendRunSystemLogAsync(AgentRunRecord run, Guid agentId, string content, CancellationToken ct)
    {
        return _agentLogService.AppendAsync(new AgentLogRecord
        {
            AgentId = agentId,
            WorkspaceId = run.WorkspaceId,
            ResourceKind = ResourceLogKinds.Run,
            ResourceId = run.Id,
            ResourceName = run.Id.ToString("N"),
            ParentResourceKind = ResourceLogKinds.Agent,
            ParentResourceId = agentId,
            Type = content.Contains("failed", StringComparison.OrdinalIgnoreCase) ? AgentLogType.Error : AgentLogType.System,
            Content = content,
            RunId = run.Id,
            CorrelationId = run.Id.ToString("N"),
            MetadataJson = JsonSerializer.Serialize(new { run.Status, run.Kind }),
        }, ct);
    }

    private async Task<AgentRecord?> ResolveAgentAsync(string agentRef, Guid workspaceId, CancellationToken ct)
    {
        if (Guid.TryParse(agentRef, out var agentId))
            return await _agentRepository.GetByAsync(new AgentFilter { Id = agentId, WorkspaceId = workspaceId }, ct);

        var agents = await _agentRepository.ListAsync(new AgentFilter { WorkspaceId = workspaceId }, ct);
        return agents.FirstOrDefault(agent => string.Equals(agent.Name, agentRef, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeEngineRef(string? engineRef)
    {
        if (string.IsNullOrWhiteSpace(engineRef))
            return "opencode";

        var trimmed = engineRef.Trim();
        if (!trimmed.Equals("opencode", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only the OpenCode engine is supported in v1.");

        return "opencode";
    }

    private static string ResolveModel(AgentRecord agent)
    {
        if (string.IsNullOrWhiteSpace(agent.Model) ||
            agent.Model.Equals(ProviderRegistry.DefaultModel, StringComparison.OrdinalIgnoreCase) ||
            agent.Model.Equals("gpt-4o-mini", StringComparison.OrdinalIgnoreCase) ||
            agent.Model.Equals("gpt-4o", StringComparison.OrdinalIgnoreCase))
        {
            return DefaultOpenCodeModel;
        }

        return agent.Model.Contains('/', StringComparison.Ordinal)
            ? agent.Model
            : $"{agent.Provider}/{agent.Model}";
    }
}
