namespace OffceOs.Application.Features.AgentHarness;

internal sealed class ToolRegistryContext
{
    public required List<IAgentTool> Tools { get; init; }
    public required List<IAsyncDisposable> IntegrationConnections { get; init; }
    public required HashSet<string> PreloadedToolNames { get; init; }
    public required Dictionary<string, string> PolicyDeniedToolReasons { get; init; }
    public required AgentHarnessToolPermissionPolicy PermissionPolicy { get; init; }
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
        foreach (var connection in _toolRegistryContext.IntegrationConnections)
            await connection.DisposeAsync();
    }

    public object[] GetSchemas() => _toolRegistryContext.Tools
        .Where(tool => _toolRegistryContext.PermissionPolicy.AlwaysLoad(tool)
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
            },
        })
        .ToArray();

    public string GetDeferredToolsMessage()
    {
        var groups = _toolRegistryContext.Tools
            .Where(tool => _toolRegistryContext.PermissionPolicy.ShouldDefer(tool)
                && !_toolRegistryContext.PreloadedToolNames.Contains(tool.Name)
                && !_revealed.Contains(tool.Name))
            .GroupBy(_toolRegistryContext.PermissionPolicy.GroupFor)
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
            var detail = _toolRegistryContext.PolicyDeniedToolReasons.TryGetValue(toolName, out var reason)
                ? $" ({reason})"
                : string.Empty;
            return new AgentError(AgentErrorCategory.ToolExecution, $"Unknown or denied tool: {toolName}{detail}");
        }

        var validation = await tool.ValidateAsync(args, ct);
        if (!validation.IsValid)
            return new AgentError(AgentErrorCategory.ToolExecution, validation.Message ?? $"Invalid input for tool: {toolName}");

        var result = await tool.ExecuteAsync(args, ct);
        if (result.IsFailure)
            return result;

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
    public required Guid? OwnerId { get; init; }
    public required Guid? DefinitionId { get; init; }
}

internal sealed class ToolRegistryFactory
{
    private readonly IAgentMemoryService _agentMemoryService;
    private readonly IAgentRoutineRepository _agentRoutineRepository;
    private readonly IAgentRoutineService _agentRoutineService;
    private readonly AgentTaskStore _agentTaskStore;
    private readonly IBrowserToolService _browserToolService;
    private readonly IAgentDefinitionRepository _agentDefinitionRepository;
    private readonly AgentDefinitionParser _agentDefinitionParser;
    private readonly IIntegrationDefinitionService _integrationDefinitionService;
    private readonly IIntegrationClientManager _integrationClientManager;
    private readonly IChannelService _channelService;
    private readonly IPublisher _publisher;
    private readonly TurnEventPublisher _turnEventPublisher;
    private readonly AgentHarnessToolPermissionPolicy _agentHarnessToolPermissionPolicy;
    private readonly ILogger<ToolRegistryFactory> _logger;

    public ToolRegistryFactory(
        IAgentMemoryService agentMemoryService,
        IAgentRoutineRepository agentRoutineRepository,
        IAgentRoutineService agentRoutineService,
        AgentTaskStore agentTaskStore,
        IBrowserToolService browserToolService,
        IAgentDefinitionRepository agentDefinitionRepository,
        AgentDefinitionParser agentDefinitionParser,
        IIntegrationDefinitionService integrationDefinitionService,
        IIntegrationClientManager integrationClientManager,
        IChannelService channelService,
        IPublisher publisher,
        TurnEventPublisher turnEventPublisher,
        AgentHarnessToolPermissionPolicy agentHarnessToolPermissionPolicy,
        ILogger<ToolRegistryFactory> logger)
    {
        _agentMemoryService = agentMemoryService;
        _agentRoutineRepository = agentRoutineRepository;
        _agentRoutineService = agentRoutineService;
        _agentTaskStore = agentTaskStore;
        _browserToolService = browserToolService;
        _agentDefinitionRepository = agentDefinitionRepository;
        _agentDefinitionParser = agentDefinitionParser;
        _integrationDefinitionService = integrationDefinitionService;
        _integrationClientManager = integrationClientManager;
        _channelService = channelService;
        _publisher = publisher;
        _turnEventPublisher = turnEventPublisher;
        _agentHarnessToolPermissionPolicy = agentHarnessToolPermissionPolicy;
        _logger = logger;
    }

    public async Task<ToolRegistry> CreateAsync(ToolRegistryRequest request, CancellationToken ct)
    {
        var context = new ToolExecutionContext(request.AgentId, request.SandboxId, request.ServiceUrl, request.Sandbox);
        var integrationConnections = new List<IAsyncDisposable>();
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
            new RoutineCreateTool(_agentRoutineService, request.AgentId, request.OwnerId, request.WorkspaceId),
            new RoutineListTool(_agentRoutineRepository, request.AgentId),
            new RoutineDeleteTool(_agentRoutineRepository, request.AgentId),
            new AgentSpawnTool(_publisher, request.AgentId, request.DefinitionId),
            new InternalChannelSendTool(_channelService, request.AgentId),
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
        var toolsetPolicy = new AgentToolsetPermissionPolicy(definitionConfig, _agentHarnessToolPermissionPolicy);
        await _turnEventPublisher.PublishDiagnosticAsync(
            request.AgentId,
            request.CorrelationId,
            $"Tool setup: agent definition loaded ({definitionConfig.Tools.Count} toolsets)",
            (int)Stopwatch.GetElapsedTime(definitionStart).TotalMilliseconds,
            ct);

        await AddBrowserToolsAsync(request, tools, preloadedToolNames, ct);
        await AddIntegrationToolsAsync(request, tools, integrationConnections, ct);

        var denied = new Dictionary<string, string>(StringComparer.Ordinal);
        tools = tools.Where(tool =>
        {
            var allowed = toolsetPolicy.IsAllowed(tool) || _agentHarnessToolPermissionPolicy.IsSelfManagementTool(tool);
            if (!allowed)
                denied[tool.Name] = "tool is not allowed by agent definition";
            return allowed;
        }).ToList();

        tools.Add(new ToolSearchTool(tools));
        preloadedToolNames.IntersectWith(tools.Select(tool => tool.Name));
        return new ToolRegistry(new ToolRegistryContext
        {
            Tools = tools,
            IntegrationConnections = integrationConnections,
            PreloadedToolNames = preloadedToolNames,
            PolicyDeniedToolReasons = denied,
            PermissionPolicy = _agentHarnessToolPermissionPolicy,
        });
    }

    private async Task AddBrowserToolsAsync(
        ToolRegistryRequest request,
        List<IAgentTool> tools,
        HashSet<string> preloadedToolNames,
        CancellationToken ct)
    {
        try
        {
            var browserTools = await _browserToolService.CreateForTurnAsync(request.AgentId, ct);
            tools.AddRange(browserTools);
            foreach (var tool in browserTools)
                preloadedToolNames.Add(tool.Name);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Browser tools unavailable for agent {AgentId}", request.AgentId);
        }
    }

    private async Task AddIntegrationToolsAsync(
        ToolRegistryRequest request,
        List<IAgentTool> tools,
        List<IAsyncDisposable> integrationConnections,
        CancellationToken ct)
    {
        foreach (var server in request.Integrations)
        {
            try
            {
                var credentials = await _integrationDefinitionService.GetDecryptedCredentialAsync(server.Name, request.OwnerId, request.WorkspaceId, ct);
                var connection = await _integrationClientManager.ConnectAsync(server, credentials, ct);
                integrationConnections.Add(connection);
                if (connection.Tools.Count > 0)
                {
                    tools.AddRange(connection.Tools.Select(discovered => new IntegrationTool(discovered)));
                    continue;
                }
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Integration {IntegrationName} unavailable for agent {AgentId}", server.Name, request.AgentId);
            }

            foreach (var catalogTool in server.Tools)
                tools.Add(new UnavailableIntegrationTool(server, catalogTool));
        }
    }
}
