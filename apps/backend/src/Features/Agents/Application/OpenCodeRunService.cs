namespace OffceOs.Application.Features.Agents;

internal sealed class OpenCodeRunService : IControlPlaneRunService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

    public async Task<ControlPlaneRunResult> CreateAsync(CreateControlPlaneRunRequest request, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
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

        return new ControlPlaneRunResult(run, "opencode", engineRef);
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

    public async Task<ControlPlaneRunLogResult> LogsAsync(Guid runId, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        _ = ownerId;
        var entries = await _agentLogService.ListForRunAsync(runId, workspaceId, 500, ct);
        return new ControlPlaneRunLogResult(entries);
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

        var workspace = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "officeos-runs", run.Id.ToString("N"));
        Directory.CreateDirectory(workspace);

        try
        {
            var model = ResolveModel(agent);
            var result = await _openCodeProcessService.RunAsync(
                new ProcessRunRequest(
                    "opencode",
                    ["run", "--format", "json", "--dir", workspace, "--model", model, "--agent", agent.Name, run.Prompt],
                    workspace,
                    new Dictionary<string, string>()),
                (line, token) => AppendOpenCodeEventAsync(run, agent, line, token),
                ct);

            if (result.ExitCode == 0)
                await CompleteAsync(run, "completed", run.Result, null, ct);
            else
                await CompleteAsync(run, "failed", null, string.IsNullOrWhiteSpace(result.StandardError) ? $"OpenCode exited with {result.ExitCode}." : result.StandardError, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCode run {RunId} failed", run.Id);
            await CompleteAsync(run, "failed", null, ex.Message, ct);
        }
    }

    private async Task AppendOpenCodeEventAsync(AgentRunRecord run, AgentRecord agent, string line, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        var (type, content) = ParseOpenCodeLine(line);
        if (type == AgentLogType.MessageOut)
            run.Result = content;

        await _agentLogService.AppendAsync(new AgentLogRecord
        {
            AgentId = agent.Id,
            WorkspaceId = run.WorkspaceId,
            Type = type,
            Content = content,
            RunId = run.Id,
            CorrelationId = run.Id.ToString("N"),
        }, ct);
    }

    private static (AgentLogType Type, string Content) ParseOpenCodeLine(string line)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            var kind = root.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
            var content = root.TryGetProperty("message", out var message) ? message.GetString()
                : root.TryGetProperty("content", out var contentElement) ? contentElement.GetString()
                : root.TryGetProperty("text", out var text) ? text.GetString()
                : root.GetRawText();

            return kind?.ToLowerInvariant() switch
            {
                "tool_call" or "tool-call" => (AgentLogType.ToolCall, content ?? line),
                "tool_result" or "tool-result" => (AgentLogType.ToolResult, content ?? line),
                "error" => (AgentLogType.Error, content ?? line),
                "message" or "assistant" or "result" => (AgentLogType.MessageOut, content ?? line),
                _ => (AgentLogType.System, content ?? line),
            };
        }
        catch (JsonException)
        {
            return (AgentLogType.System, line);
        }
    }

    private async Task CompleteAsync(AgentRunRecord run, string status, string? result, string? error, CancellationToken ct)
    {
        run.Status = status;
        run.Result = result;
        run.Error = error;
        run.CompletedAt = DateTime.UtcNow;
        run.UpdatedAt = DateTime.UtcNow;
        await _agentRunRepository.UpdateAsync(run, ct);
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
        => string.IsNullOrWhiteSpace(agent.Model) || agent.Model.Equals(ProviderRegistry.DefaultModel, StringComparison.OrdinalIgnoreCase)
            ? $"{agent.Provider}/{ProviderRegistry.DefaultModel}"
            : $"{agent.Provider}/{agent.Model}";
}
