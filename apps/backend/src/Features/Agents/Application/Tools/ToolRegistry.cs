namespace OffceOs.Application.Features.Agents;

/// <summary>
/// Registry of all agent tools. Creates tool instances per-turn with the appropriate dependencies.
/// </summary>
internal sealed class ToolRegistry : IAsyncDisposable
{
    private readonly List<IAgentTool> _tools;
    private readonly HashSet<string> _preloadedToolNames;
    private readonly HashSet<string> _revealed = new(StringComparer.Ordinal);
    private readonly List<IAsyncDisposable> _integrationConnections;
    private readonly ToolExecutionContext _toolExecutionContext;
    private readonly IReadOnlyDictionary<string, string> _policyDeniedToolReasons;
    private readonly TurnEventPublisher? _turnEventPublisher;
    private readonly string? _correlationId;

    public ToolRegistry(
        List<IAgentTool> tools,
        ToolExecutionContext context,
        List<IAsyncDisposable>? integrationConnections = null,
        IEnumerable<string>? preloadedToolNames = null,
        IReadOnlyDictionary<string, string>? policyDeniedToolReasons = null,
        TurnEventPublisher? turnEventPublisher = null,
        string? correlationId = null)
    {
        _tools = tools;
        _toolExecutionContext = context;
        _integrationConnections = integrationConnections ?? [];
        _preloadedToolNames = (preloadedToolNames ?? []).ToHashSet(StringComparer.Ordinal);
        _policyDeniedToolReasons = policyDeniedToolReasons ?? new Dictionary<string, string>();
        _turnEventPublisher = turnEventPublisher;
        _correlationId = correlationId;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var conn in _integrationConnections)
            await conn.DisposeAsync();
    }

    public IReadOnlyList<IAgentTool> Tools => _tools;

    /// <summary>Get loaded tool schemas for the LLM tools array.</summary>
    public object[] GetSchemas() => _tools
        .Where(t => t.AlwaysLoad || _preloadedToolNames.Contains(t.Name) || _revealed.Contains(t.Name))
        .Select(ToSchema)
        .ToArray();

    public string GetDeferredToolsMessage()
    {
        var groups = _tools
            .Where(t => t.ShouldDefer && !_preloadedToolNames.Contains(t.Name) && !_revealed.Contains(t.Name))
            .GroupBy(t => t.Kind == AgentToolKind.Integration
                ? ToolKey.Parse(t.PermissionScope).SkillName
                : t.Name.StartsWith("browser__", StringComparison.Ordinal) ? "browser" : "builtin")
            .OrderBy(g => g.Key);

        var sb = new StringBuilder();
        sb.AppendLine("<available-deferred-tools>");
        foreach (var group in groups)
        {
            sb.AppendLine($"group: {group.Key}");
            foreach (var tool in group.OrderBy(t => t.Name))
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

    private static object ToSchema(IAgentTool t) => new
    {
        type = "function",
        function = new
        {
            name = t.Schema.Name,
            description = t.Schema.Description,
            parameters = t.Schema.Parameters,
        }
    };

    /// <summary>Dispatch a tool call by name.</summary>
    public async Task<AgentResult<ToolResult>> DispatchAsync(string toolName, JsonElement args, CancellationToken ct)
    {
        var tool = _tools.FirstOrDefault(t => t.Name == toolName);
        if (tool is null)
        {
            if (_turnEventPublisher is not null
                && _correlationId is not null
                && _policyDeniedToolReasons.TryGetValue(toolName, out var reason))
            {
                await _turnEventPublisher.PublishToolPolicyDeniedAsync(
                    _toolExecutionContext.AgentId,
                    _correlationId,
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
        var output = Truncate(value.Output, tool.MaxResultChars);
        var error = value.Error is null ? null : Truncate(value.Error, tool.MaxResultChars);
        return new ToolResult(value.Success, output, error);
    }

    private static string Truncate(string value, int maxChars)
        => maxChars > 0 && value.Length > maxChars
            ? value[..maxChars] + "\n[truncated]"
            : value;

}

/// <summary>
/// Builds a per-turn tool registry and owns tool construction dependencies.
/// </summary>
internal sealed class ToolRegistryFactory
{
    private readonly IAgentMemoryService _agentMemoryService;
    private readonly IAgentCronJobRepository _agentCronJobRepository;
    private readonly IAgentRunRepository _agentRunRepository;
    private readonly AgentTaskStore _agentTaskStore;
    private readonly IIntegrationClientManager _integrationClientManager;
    private readonly IBrowserToolContextFactory _browserToolContextFactory;
    private readonly IAgentDefinitionRepository _agentDefinitionRepository;
    private readonly AgentDefinitionParser _agentDefinitionParser;
    private readonly IOrganizationPolicyService _organizationPolicyService;
    private readonly IIntegrationExecutionService _integrationExecutionService;
    private readonly TurnEventPublisher _turnEventPublisher;
    private readonly ILogger<ToolRegistryFactory> _logger;

    public ToolRegistryFactory(
        IAgentMemoryService memoryService,
        IAgentCronJobRepository cronJobRepository,
        IAgentRunRepository agentRunRepository,
        AgentTaskStore taskStore,
        IIntegrationClientManager integrationClientManager,
        IBrowserToolContextFactory browserToolContextFactory,
        IAgentDefinitionRepository agentDefinitionRepository,
        AgentDefinitionParser agentDefinitionParser,
        IOrganizationPolicyService organizationPolicyService,
        IIntegrationExecutionService integrationExecution,
        TurnEventPublisher events,
        ILogger<ToolRegistryFactory> logger)
    {
        _agentMemoryService = memoryService;
        _agentCronJobRepository = cronJobRepository;
        _agentRunRepository = agentRunRepository;
        _agentTaskStore = taskStore;
        _integrationClientManager = integrationClientManager;
        _browserToolContextFactory = browserToolContextFactory;
        _agentDefinitionRepository = agentDefinitionRepository;
        _agentDefinitionParser = agentDefinitionParser;
        _organizationPolicyService = organizationPolicyService;
        _integrationExecutionService = integrationExecution;
        _turnEventPublisher = events;
        _logger = logger;
    }

    public async Task<ToolRegistry> CreateAsync(
        IAgentSandbox sandbox,
        string sandboxId,
        string serviceUrl,
        Guid agentId,
        Guid? workspaceId,
        string correlationId,
        IReadOnlyList<IntegrationDefinitionRecord> integrations,
            Func<string, Task<Dictionary<string, string>>> credentialLoader,
            CancellationToken ct)
    {
        var context = new ToolExecutionContext(agentId, sandboxId, serviceUrl, sandbox);
        var tools = new List<IAgentTool>
        {
            // Bash tools (execute via pod PTY)
            new ShellTool(context),
            new FileReadTool(context),
            new FileWriteTool(context),
            new FileEditTool(context),
            new ContentSearchTool(context),
            new GlobSearchTool(context),
            // Memory tools (Postgres)
            new MemoryStoreTool(_agentMemoryService, agentId),
            new MemoryRecallTool(_agentMemoryService, agentId),
            new MemoryForgetTool(_agentMemoryService, agentId),
            // Session/task orchestration
            new AskUserQuestionTool(),
            new TaskCreateTool(_agentTaskStore, agentId),
            new TaskListTool(_agentTaskStore, agentId),
            new TaskGetTool(_agentTaskStore, agentId),
            new TaskUpdateTool(_agentTaskStore, agentId),
            new CronCreateTool(_agentCronJobRepository, agentId),
            new CronListTool(_agentCronJobRepository, agentId),
            new CronDeleteTool(_agentCronJobRepository, agentId),
            new AgentSpawnTool(_agentRunRepository, agentId),
            // HTTP tools (backend)
            new HttpRequestTool(),
            new WebFetchTool(),
        };
        var preloadedToolNames = new HashSet<string>(StringComparer.Ordinal);

        var definitionStart = Stopwatch.GetTimestamp();
        var definition = await _agentDefinitionRepository.GetByAsync(
            new AgentDefinitionFilter { AgentId = agentId, ActiveOnly = true },
            ct);
        var definitionConfig = definition is null
            ? _agentDefinitionParser.CreateDefaultConfig("agent", ProviderRegistry.DefaultModel, null, integrations.Select(integration => integration.Name).ToList())
            : _agentDefinitionParser.Parse(definition.ConfigJson);
        var toolsetPolicy = new AgentToolsetPermissionPolicy(definitionConfig);
        await _turnEventPublisher.PublishDiagnosticAsync(
            agentId,
            correlationId,
            $"Tool setup: agent definition loaded ({definitionConfig.Tools.Count} toolsets)",
            ElapsedMs(definitionStart),
            ct);

        var browserStart = Stopwatch.GetTimestamp();
        BrowserToolContext? browserContext = null;
        var browserFailed = false;
        try
        {
            browserContext = await _browserToolContextFactory.CreateForTurnAsync(ct);
        }
        catch (Exception ex)
        {
            browserFailed = true;
            await _turnEventPublisher.PublishDiagnosticAsync(
                agentId,
                correlationId,
                "Tool setup: browser unavailable",
                ElapsedMs(browserStart),
                ct);
            // Browser is an internal optional runtime. If it is down, omit the
            // tools for this turn instead of failing the whole agent loop.
            _logger.LogWarning(ex, "Browser tools unavailable for agent {AgentId}; continuing turn without browser tools", agentId);
        }
        if (browserContext is null && !browserFailed)
        {
            await _turnEventPublisher.PublishDiagnosticAsync(
                agentId,
                correlationId,
                "Tool setup: browser unavailable",
                ElapsedMs(browserStart),
                ct);
            _logger.LogDebug("Browser runtime unavailable for agent {AgentId}; continuing turn without browser tools", agentId);
        }
        else if (browserContext is { } availableBrowserContext)
        {
            await _turnEventPublisher.PublishDiagnosticAsync(
                agentId,
                correlationId,
                "Tool setup: browser tools discovered",
                ElapsedMs(browserStart),
                ct);
            AddBrowserTools(availableBrowserContext, agentId, tools, preloadedToolNames);
        }

        var integrationConnections = new List<IAsyncDisposable>();
        if (HasEnabledIndexedIntegration(integrations, toolsetPolicy))
            tools.Add(new IntegrationExecuteTool(_integrationExecutionService));

        foreach (var server in integrations)
        {
            if (!string.IsNullOrWhiteSpace(server.ToolsJson))
            {
                var lazyConnection = new LazyIntegrationConnection(
                    server,
                    credentialLoader,
                    _integrationClientManager,
                    _turnEventPublisher,
                    agentId,
                    correlationId);

                var catalogTools = ParseIntegrationCatalogTools(server).ToList();
                foreach (var catalogTool in catalogTools)
                    tools.Add(new LazyIntegrationTool(server, catalogTool, lazyConnection));
                tools.Add(new LazyListIntegrationResourcesTool(server, lazyConnection));
                tools.Add(new LazyReadIntegrationResourceTool(server, lazyConnection));
                integrationConnections.Add(lazyConnection);

                await _turnEventPublisher.PublishDiagnosticAsync(
                    agentId,
                    correlationId,
                    $"Tool setup: integration catalog loaded ({server.Name}, {catalogTools.Count} tools)",
                    0,
                    ct);
                continue;
            }

            var credentialStart = Stopwatch.GetTimestamp();
            var creds = await credentialLoader(server.Name);
            await _turnEventPublisher.PublishDiagnosticAsync(
                agentId,
                correlationId,
                $"Tool setup: integration credentials loaded ({server.Name})",
                ElapsedMs(credentialStart),
                ct);

            var connectStart = Stopwatch.GetTimestamp();
            var result = await _integrationClientManager.ConnectAsync(server, creds, ct);
            await _turnEventPublisher.PublishDiagnosticAsync(
                agentId,
                correlationId,
                $"Tool setup: integration connected ({server.Name}, {result.Tools.Count} tools)",
                ElapsedMs(connectStart),
                ct);
            if (result.Tools.Count == 0)
            {
                _logger.LogWarning(
                    "Assigned integration {Server} discovered no callable tools for agent {AgentId}",
                    server.Name,
                    agentId);
            }
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

        var policy = await _organizationPolicyService.GetEffectiveForWorkspaceAsync(workspaceId, ct);
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

        preloadedToolNames.IntersectWith(tools.Select(t => t.Name));
        return new ToolRegistry(
            tools,
            context,
            integrationConnections,
            preloadedToolNames,
            policyDeniedToolReasons,
            _turnEventPublisher,
            correlationId);
    }

    private static int ElapsedMs(long startTimestamp)
        => (int)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

    private static bool HasEnabledIndexedIntegration(
        IReadOnlyList<IntegrationDefinitionRecord> integrations,
        AgentToolsetPermissionPolicy toolsetPolicy)
    {
        return integrations.Any(integration =>
            integration.Entities.Count > 0
            && toolsetPolicy.AllowsIntegrationTool(integration.Name, IntegrationIndexAccess.ToolName));
    }

    private static IEnumerable<IntegrationCatalogTool> ParseIntegrationCatalogTools(IntegrationDefinitionRecord server)
    {
        if (string.IsNullOrWhiteSpace(server.ToolsJson))
            yield break;

        JsonElement parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<JsonElement>(server.ToolsJson);
        }
        catch
        {
            yield break;
        }

        if (parsed.ValueKind != JsonValueKind.Array)
            yield break;

        foreach (var item in parsed.EnumerateArray())
        {
            var name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var description = item.TryGetProperty("description", out var descProp)
                ? descProp.GetString() ?? name
                : name;
            JsonElement? parameters = null;
            if (item.TryGetProperty("parameters", out var parametersProp)
                || item.TryGetProperty("inputSchema", out parametersProp)
                || item.TryGetProperty("input_schema", out parametersProp))
            {
                parameters = JsonSerializer.Deserialize<JsonElement>(parametersProp.GetRawText());
            }

            yield return new IntegrationCatalogTool(name, description, parameters);
        }
    }

    private static void AddBrowserTools(
        BrowserToolContext browserContext,
        Guid agentId,
        List<IAgentTool> tools,
        HashSet<string> preloadedToolNames)
    {
        var browserTools = CreateBrowserTools(browserContext, agentId);
        tools.AddRange(browserTools);
        foreach (var tool in browserTools)
            preloadedToolNames.Add(tool.Name);
    }

    internal static IReadOnlyList<IAgentTool> CreateBrowserTools(BrowserToolContext browser, Guid agentId)
        =>
        [
            new BrowserNavigateTool(browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserGetSessionTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserObserveTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserScreenshotTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserGetConsoleTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserGetPageErrorsTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserGetRequestFailuresTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserStopTraceTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserListAuthProfilesTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserGetAuthProfileTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserListDownloadsTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserListTabsTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserActivateTabTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserCloseTabTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserExecuteActionTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserSaveAuthStateTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserSaveAuthProfileTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserRequestHumanTakeoverTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserGetNetworkLogTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserEvalJsTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserWaitForSelectorTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserGetHtmlTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserFindElementsTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserDragDropTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserSetViewportTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserGetCookiesTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserSetCookiesTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserGetLocalStorageTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserSetLocalStorageTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserExportScriptTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserCdpAttachTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserFindByVisionTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
        ];
}
