namespace OffceOs.Application.Features.Agents;

internal sealed class OpenCodeAgentWorkService : IAgentWorkExecutionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string DefaultOpenCodeModel = "openai/gpt-5.2";

    private readonly IAgentRepository _agentRepository;
    private readonly IAgentLogService _agentLogService;
    private readonly IOpenCodeProcessService _openCodeProcessService;
    private readonly ILogger<OpenCodeAgentWorkService> _logger;

    public OpenCodeAgentWorkService(
        IAgentRepository agentRepository,
        IAgentLogService agentLogService,
        IOpenCodeProcessService openCodeProcessService,
        ILogger<OpenCodeAgentWorkService> logger)
    {
        _agentRepository = agentRepository;
        _agentLogService = agentLogService;
        _openCodeProcessService = openCodeProcessService;
        _logger = logger;
    }

    public async Task ExecuteQueuedWorkAsync(AgentLogRecord work, CancellationToken ct = default)
    {
        if (work.Type != AgentLogType.MessageIn
            || work.WorkStatus != AgentWorkStatusKinds.Running
            || !work.AgentId.HasValue)
        {
            return;
        }

        var agent = await _agentRepository.GetByAsync(
            new AgentFilter { Id = work.AgentId.Value, WorkspaceId = work.WorkspaceId },
            ct);
        if (agent is null)
        {
            await _agentLogService.FailWorkAsync(work.Id, "Agent not found.", ct);
            return;
        }

        await AppendWorkSystemLogAsync(work, agent.Id, "Work started.", ct);
        await AppendBootstrapPromptLogAsync(work, agent.Id, ct);

        var workspace = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "officeos-agent-work", work.Id.ToString("N"));
        Directory.CreateDirectory(workspace);

        try
        {
            await WriteOpenCodeContextAsync(workspace, agent, ct);
            var model = ResolveModel(agent);
            var reportedError = false;
            var reportedErrorLines = new List<string>();
            var result = await _openCodeProcessService.RunAsync(
                new ProcessRunRequest(
                    "opencode",
                    ["run", "--format", "json", "--print-logs", "--log-level", "WARN", "--model", model, work.Content],
                    workspace,
                    new Dictionary<string, string>()),
                async (line, token) =>
                {
                    var entry = await AppendOpenCodeEventAsync(work, agent, line, "stdout", token);
                    if (entry?.Severity == ResourceLogSeverityKinds.Error)
                    {
                        reportedError = true;
                        reportedErrorLines.Add(entry.Content);
                    }
                },
                async (line, token) =>
                {
                    var entry = await AppendOpenCodeEventAsync(work, agent, line, "stderr", token);
                    if (entry?.Severity == ResourceLogSeverityKinds.Error)
                    {
                        reportedError = true;
                        reportedErrorLines.Add(entry.Content);
                    }
                },
                ct);

            if (result.ExitCode == 0 && !reportedError)
            {
                await _agentLogService.CompleteWorkAsync(work.Id, ct);
                await AppendWorkSystemLogAsync(work, agent.Id, "Work completed.", ct);
            }
            else
            {
                var failure = OpenCodeFailureMessage(result, reportedErrorLines);
                await _agentLogService.FailWorkAsync(work.Id, failure, ct);
                await AppendWorkSystemLogAsync(work, agent.Id, $"Work failed: {failure}", ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCode work {WorkLogId} failed", work.Id);
            await _agentLogService.FailWorkAsync(work.Id, ex.Message, ct);
            await AppendWorkSystemLogAsync(work, agent.Id, $"Work failed: {ex.Message}", ct);
        }
    }

    private static Task WriteOpenCodeContextAsync(string workspace, AgentRecord agent, CancellationToken ct)
    {
        var context = $"""
        # OfficeOS Agent Context

        Agent name: {agent.Name}
        Provider: {agent.Provider}
        Model: {agent.Model}

        ## Role Instructions

        These instructions define how the agent should behave during real tasks.
        They are context, not a user task by themselves.

        {agent.Prompt}
        """;

        return File.WriteAllTextAsync(Path.Combine(workspace, "AGENTS.md"), context, ct);
    }

    private async Task<OpenCodeLogEntry?> AppendOpenCodeEventAsync(
        AgentLogRecord work,
        AgentRecord agent,
        string line,
        string source,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var entry = ParseOpenCodeLine(line, source);

        await _agentLogService.AppendAsync(new AgentLogRecord
        {
            AgentId = agent.Id,
            WorkspaceId = work.WorkspaceId,
            ResourceKind = ResourceLogKinds.Agent,
            ResourceId = agent.Id,
            Type = entry.Type,
            Severity = entry.Severity,
            Tool = entry.Tool,
            Content = entry.Content,
            CorrelationId = work.CorrelationId,
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

    private Task AppendWorkSystemLogAsync(AgentLogRecord work, Guid agentId, string content, CancellationToken ct)
    {
        return _agentLogService.AppendAsync(new AgentLogRecord
        {
            AgentId = agentId,
            WorkspaceId = work.WorkspaceId,
            ResourceKind = ResourceLogKinds.Agent,
            ResourceId = agentId,
            Type = content.Contains("failed", StringComparison.OrdinalIgnoreCase) ? AgentLogType.Error : AgentLogType.System,
            Content = content,
            CorrelationId = work.CorrelationId,
            MetadataJson = JsonSerializer.Serialize(new
            {
                WorkLogId = work.Id,
                Status = work.WorkStatus,
                Purpose = work.WorkPurpose,
            }),
        }, ct);
    }

    private Task AppendBootstrapPromptLogAsync(AgentLogRecord work, Guid agentId, CancellationToken ct)
    {
        if (!string.Equals(work.WorkPurpose, AgentWorkPurposeKinds.Bootstrap, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        return _agentLogService.AppendAsync(new AgentLogRecord
        {
            AgentId = agentId,
            WorkspaceId = work.WorkspaceId,
            ResourceKind = ResourceLogKinds.Agent,
            ResourceId = agentId,
            Type = AgentLogType.MessageIn,
            Severity = ResourceLogSeverityKinds.Info,
            Content = $"Bootstrap prompt: {work.Content}",
            CorrelationId = work.CorrelationId,
            MetadataJson = JsonSerializer.Serialize(new { Purpose = work.WorkPurpose }),
        }, ct);
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
