namespace OffceOs.Application.Features.Agents;

internal sealed class ToolRegistryContext
{
    public required List<IAgentTool> Tools { get; init; }
    public required ToolExecutionContext ToolExecutionContext { get; init; }
    public required List<IAsyncDisposable> IntegrationConnections { get; init; }
    public required HashSet<string> PreloadedToolNames { get; init; }
    public required Dictionary<string, string> PolicyDeniedToolReasons { get; init; }
    public required TurnEventPublisher TurnEventPublisher { get; init; }
    public required string CorrelationId { get; init; }
}

internal sealed class ToolRegistry : IAsyncDisposable
{
    private readonly ToolRegistryContext _toolRegistryContext;
    private readonly HashSet<string> _revealed = new(StringComparer.Ordinal);

    public ToolRegistry(ToolRegistryContext toolRegistryContext)
    {
        _toolRegistryContext = toolRegistryContext;
    }

    public IReadOnlyList<IAgentTool> Tools => _toolRegistryContext.Tools;

    public async ValueTask DisposeAsync()
    {
        foreach (var conn in _toolRegistryContext.IntegrationConnections)
            await conn.DisposeAsync();
    }

    public object[] GetSchemas() => _toolRegistryContext.Tools
        .Where(tool => tool.AlwaysLoad
            || _toolRegistryContext.PreloadedToolNames.Contains(tool.Name)
            || _revealed.Contains(tool.Name))
        .Select(tool => new
        {
            type = "function",
            function = new
            {
                name = tool.Schema.Name,
                description = tool.Schema.Description,
                parameters = tool.Schema.Parameters,
            }
        })
        .ToArray();

    public string GetDeferredToolsMessage()
    {
        var groups = _toolRegistryContext.Tools
            .Where(tool => tool.ShouldDefer
                && !_toolRegistryContext.PreloadedToolNames.Contains(tool.Name)
                && !_revealed.Contains(tool.Name))
            .GroupBy(tool => tool.Kind == AgentToolKind.Integration
                ? ToolKey.Parse(tool.PermissionScope).SkillName
                : tool.Name.StartsWith("browser__", StringComparison.Ordinal) ? "browser" : "builtin")
            .OrderBy(group => group.Key);

        var sb = new StringBuilder();
        sb.AppendLine("<available-deferred-tools>");
        foreach (var group in groups)
        {
            sb.AppendLine($"group: {group.Key}");
            foreach (var tool in group.OrderBy(tool => tool.Name))
                sb.AppendLine($"- {tool.Name}: {tool.SearchHint}");
        }
        sb.Append("</available-deferred-tools>");
        return sb.ToString();
    }

    public void RevealTools(IEnumerable<string> toolNames)
    {
        foreach (var name in toolNames)
            _revealed.Add(name);
    }

    public async Task<AgentResult<ToolResult>> DispatchAsync(string toolName, JsonElement args, CancellationToken ct)
    {
        var tool = _toolRegistryContext.Tools.FirstOrDefault(candidate => candidate.Name == toolName);
        if (tool is null)
        {
            if (_toolRegistryContext.PolicyDeniedToolReasons.TryGetValue(toolName, out var reason))
            {
                await _toolRegistryContext.TurnEventPublisher.PublishToolPolicyDeniedAsync(
                    _toolRegistryContext.ToolExecutionContext.AgentId,
                    _toolRegistryContext.CorrelationId,
                    toolName,
                    reason,
                    ct);
            }

            return new AgentError(AgentErrorCategory.ToolExecution, $"Unknown or denied tool: {toolName}");
        }

        var validation = await tool.ValidateAsync(args, ct);
        if (!validation.IsValid)
            return new AgentError(AgentErrorCategory.ToolExecution, validation.Message ?? $"Invalid input for tool: {toolName}");

        var result = await tool.ExecuteAsync(args, ct);
        if (result.IsFailure) return result;

        var value = result.Value;
        var output = value.Output.Length > tool.MaxResultChars && tool.MaxResultChars > 0
            ? value.Output[..tool.MaxResultChars] + "\n[truncated]"
            : value.Output;
        var error = value.Error is { } errorValue && errorValue.Length > tool.MaxResultChars && tool.MaxResultChars > 0
            ? errorValue[..tool.MaxResultChars] + "\n[truncated]"
            : value.Error;
        return new ToolResult(value.Success, output, error);
    }
}

internal sealed class ToolRegistryRequest
{
    public required IAgentSandbox Sandbox { get; init; }
    public required string SandboxId { get; init; }
    public required string ServiceUrl { get; init; }
    public required Guid AgentId { get; init; }
    public required Guid? WorkspaceId { get; init; }
    public required string CorrelationId { get; init; }
    public required IReadOnlyList<IntegrationDefinitionRecord> Integrations { get; init; }
    public required Func<string, Task<Dictionary<string, string>>> CredentialLoader { get; init; }
}

internal sealed class ToolRegistryFactory
{
    private readonly IAgentMemoryService _agentMemoryService;
    private readonly IAgentRoutineRepository _agentRoutineRepository;
    private readonly IAgentRunRepository _agentRunRepository;
    private readonly AgentTaskStore _agentTaskStore;
    private readonly IIntegrationClientManager _integrationClientManager;
    private readonly IBrowserToolService _browserToolService;
    private readonly IAgentDefinitionRepository _agentDefinitionRepository;
    private readonly AgentDefinitionParser _agentDefinitionParser;
    private readonly IOrganizationPolicyService _organizationPolicyService;
    private readonly IIntegrationExecutionService _integrationExecutionService;
    private readonly TurnEventPublisher _turnEventPublisher;
    private readonly ILogger<ToolRegistryFactory> _logger;

    public ToolRegistryFactory(
        IAgentMemoryService memoryService,
        IAgentRoutineRepository agentRoutineRepository,
        IAgentRunRepository agentRunRepository,
        AgentTaskStore taskStore,
        IIntegrationClientManager integrationClientManager,
        IBrowserToolService browserToolService,
        IAgentDefinitionRepository agentDefinitionRepository,
        AgentDefinitionParser agentDefinitionParser,
        IOrganizationPolicyService organizationPolicyService,
        IIntegrationExecutionService integrationExecution,
        TurnEventPublisher events,
        ILogger<ToolRegistryFactory> logger)
    {
        _agentMemoryService = memoryService;
        _agentRoutineRepository = agentRoutineRepository;
        _agentRunRepository = agentRunRepository;
        _agentTaskStore = taskStore;
        _integrationClientManager = integrationClientManager;
        _browserToolService = browserToolService;
        _agentDefinitionRepository = agentDefinitionRepository;
        _agentDefinitionParser = agentDefinitionParser;
        _organizationPolicyService = organizationPolicyService;
        _integrationExecutionService = integrationExecution;
        _turnEventPublisher = events;
        _logger = logger;
    }

    public async Task<ToolRegistry> CreateAsync(ToolRegistryRequest request, CancellationToken ct)
    {
        var context = new ToolExecutionContext(request.AgentId, request.SandboxId, request.ServiceUrl, request.Sandbox);
        var tools = new List<IAgentTool>
        {
            new ShellTool(context),
            new FileReadTool(context),
            new FileWriteTool(context),
            new FileEditTool(context),
            new ContentSearchTool(context),
            new GlobSearchTool(context),
            new MemoryStoreTool(_agentMemoryService, request.AgentId),
            new MemoryRecallTool(_agentMemoryService, request.AgentId),
            new MemoryForgetTool(_agentMemoryService, request.AgentId),
            new AskUserQuestionTool(),
            new TaskCreateTool(_agentTaskStore, request.AgentId),
            new TaskListTool(_agentTaskStore, request.AgentId),
            new TaskGetTool(_agentTaskStore, request.AgentId),
            new TaskUpdateTool(_agentTaskStore, request.AgentId),
            new RoutineCreateTool(_agentRoutineRepository, request.AgentId),
            new RoutineListTool(_agentRoutineRepository, request.AgentId),
            new RoutineDeleteTool(_agentRoutineRepository, request.AgentId),
            new AgentSpawnTool(_agentRunRepository, request.AgentId),
            new HttpRequestTool(),
            new WebFetchTool(),
        };
        var preloadedToolNames = new HashSet<string>(StringComparer.Ordinal);

        var definitionStart = Stopwatch.GetTimestamp();
        var definition = await _agentDefinitionRepository.GetByAsync(
            new AgentDefinitionFilter { AgentId = request.AgentId, ActiveOnly = true },
            ct);
        var definitionConfig = definition is null
            ? _agentDefinitionParser.CreateDefaultConfig(
                "agent",
                ProviderRegistry.DefaultModel,
                null,
                request.Integrations.Select(integration => integration.Name).ToList())
            : _agentDefinitionParser.Parse(definition.ConfigJson);
        var toolsetPolicy = new AgentToolsetPermissionPolicy(definitionConfig);
        await _turnEventPublisher.PublishDiagnosticAsync(
            request.AgentId,
            request.CorrelationId,
            $"Tool setup: agent definition loaded ({definitionConfig.Tools.Count} toolsets)",
            (int)Stopwatch.GetElapsedTime(definitionStart).TotalMilliseconds,
            ct);

        var browserStart = Stopwatch.GetTimestamp();
        try
        {
            var browserTools = await _browserToolService.CreateForTurnAsync(request.AgentId, ct);
            if (browserTools.Count == 0)
            {
                await _turnEventPublisher.PublishDiagnosticAsync(
                    request.AgentId,
                    request.CorrelationId,
                    "Tool setup: browser unavailable",
                    (int)Stopwatch.GetElapsedTime(browserStart).TotalMilliseconds,
                    ct);
                _logger.LogDebug("Browser runtime unavailable for agent {AgentId}; continuing turn without browser tools", request.AgentId);
            }
            else
            {
                tools.AddRange(browserTools);
                foreach (var tool in browserTools)
                    preloadedToolNames.Add(tool.Name);
                await _turnEventPublisher.PublishDiagnosticAsync(
                    request.AgentId,
                    request.CorrelationId,
                    "Tool setup: browser tools discovered",
                    (int)Stopwatch.GetElapsedTime(browserStart).TotalMilliseconds,
                    ct);
            }
        }
        catch (Exception ex)
        {
            await _turnEventPublisher.PublishDiagnosticAsync(
                request.AgentId,
                request.CorrelationId,
                "Tool setup: browser unavailable",
                (int)Stopwatch.GetElapsedTime(browserStart).TotalMilliseconds,
                ct);
            _logger.LogWarning(ex, "Browser tools unavailable for agent {AgentId}; continuing turn without browser tools", request.AgentId);
        }

        var integrationConnections = new List<IAsyncDisposable>();
        if (request.Integrations.Any(integration =>
            integration.Entities.Count > 0
            && toolsetPolicy.AllowsIntegrationTool(integration.Name, IntegrationIndexAccess.ToolName)))
        {
            tools.Add(new IntegrationExecuteTool(_integrationExecutionService));
        }

        foreach (var server in request.Integrations)
        {
            if (server.Tools.Count > 0)
            {
                var lazyConnection = new LazyIntegrationConnection(
                    server,
                    request.CredentialLoader,
                    _integrationClientManager,
                    _turnEventPublisher,
                    request.AgentId,
                    request.CorrelationId);
                foreach (var catalogTool in server.Tools)
                    tools.Add(new LazyIntegrationTool(server, catalogTool, lazyConnection));
                tools.Add(new LazyListIntegrationResourcesTool(server, lazyConnection));
                tools.Add(new LazyReadIntegrationResourceTool(server, lazyConnection));
                integrationConnections.Add(lazyConnection);
                await _turnEventPublisher.PublishDiagnosticAsync(
                    request.AgentId,
                    request.CorrelationId,
                    $"Tool setup: integration catalog loaded ({server.Name}, {server.Tools.Count} tools)",
                    0,
                    ct);
                continue;
            }

            var credentialStart = Stopwatch.GetTimestamp();
            var creds = await request.CredentialLoader(server.Name);
            await _turnEventPublisher.PublishDiagnosticAsync(
                request.AgentId,
                request.CorrelationId,
                $"Tool setup: integration credentials loaded ({server.Name})",
                (int)Stopwatch.GetElapsedTime(credentialStart).TotalMilliseconds,
                ct);

            var connectStart = Stopwatch.GetTimestamp();
            var result = await _integrationClientManager.ConnectAsync(server, creds, ct);
            await _turnEventPublisher.PublishDiagnosticAsync(
                request.AgentId,
                request.CorrelationId,
                $"Tool setup: integration connected ({server.Name}, {result.Tools.Count} tools)",
                (int)Stopwatch.GetElapsedTime(connectStart).TotalMilliseconds,
                ct);
            if (result.Tools.Count == 0)
                _logger.LogWarning("Assigned integration {Server} discovered no callable tools for agent {AgentId}", server.Name, request.AgentId);
            foreach (var discovered in result.Tools)
                tools.Add(new IntegrationTool(discovered));
            tools.Add(new ListIntegrationResourcesTool(server.Name, result.NativeClient));
            tools.Add(new ReadIntegrationResourceTool(server.Name, result.NativeClient));
            integrationConnections.Add(result);
        }

        var policyDeniedToolReasons = new Dictionary<string, string>(StringComparer.Ordinal);
        var allowedByAgentDefinition = new List<IAgentTool>();
        foreach (var tool in tools)
        {
            if (toolsetPolicy.IsAllowed(tool))
                allowedByAgentDefinition.Add(tool);
            else
                policyDeniedToolReasons[tool.Name] = "tool is not allowed by agent definition";
        }
        tools = allowedByAgentDefinition;

        var policy = await _organizationPolicyService.GetEffectiveForWorkspaceAsync(request.WorkspaceId, ct);
        if (policy is not null)
        {
            var allowedByPolicy = new List<IAgentTool>();
            foreach (var tool in tools)
            {
                var denialReason = AgentToolsetPermissionPolicy.GetOrganizationPolicyDenialReason(tool, policy);
                if (denialReason is null)
                    allowedByPolicy.Add(tool);
                else
                    policyDeniedToolReasons[tool.Name] = denialReason;
            }
            tools = allowedByPolicy;
        }

        tools.Add(new ToolSearchTool(tools));
        preloadedToolNames.IntersectWith(tools.Select(tool => tool.Name));
        return new ToolRegistry(new ToolRegistryContext
        {
            Tools = tools,
            ToolExecutionContext = context,
            IntegrationConnections = integrationConnections,
            PreloadedToolNames = preloadedToolNames,
            PolicyDeniedToolReasons = policyDeniedToolReasons,
            TurnEventPublisher = _turnEventPublisher,
            CorrelationId = request.CorrelationId,
        });
    }
}
