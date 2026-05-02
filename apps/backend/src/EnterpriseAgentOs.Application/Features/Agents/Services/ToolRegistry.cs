using System.Text.Json;

namespace EnterpriseAgentOs.Application.Features.Agents;

/// <summary>
/// Registry of all agent tools. Creates tool instances per-turn with the appropriate dependencies.
/// </summary>
internal sealed class ToolRegistry : IAsyncDisposable
{
    private readonly List<IAgentTool> _tools;
    private readonly HashSet<string> _preloadedToolNames;
    private readonly HashSet<string> _revealed = new(StringComparer.Ordinal);
    private readonly List<IAsyncDisposable> _mcpConnections;
    private readonly ToolExecutionContext _context;

    public ToolRegistry(
        List<IAgentTool> tools,
        ToolExecutionContext context,
        List<IAsyncDisposable>? mcpConnections = null,
        IEnumerable<string>? preloadedToolNames = null)
    {
        _tools = tools;
        _context = context;
        _mcpConnections = mcpConnections ?? [];
        _preloadedToolNames = (preloadedToolNames ?? []).ToHashSet(StringComparer.Ordinal);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var conn in _mcpConnections)
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
            .GroupBy(t => t.Kind == AgentToolKind.Mcp
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
            return new AgentError(AgentErrorCategory.ToolExecution, $"Unknown or denied tool: {toolName}");

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
    private readonly IAgentMemoryRepository _memoryRepo;
    private readonly IAgentCronJobRepository _cronJobRepository;
    private readonly IAgentRunRepository _agentRunRepository;
    private readonly AgentTaskStore _taskStore;
    private readonly IMcpClientManager _mcpClientManager;
    private readonly IBrowserToolContextFactory _browserToolContextFactory;
    private readonly IAgentToolPermissionRepository _permissionRepository;

    public ToolRegistryFactory(
        IAgentMemoryRepository memoryRepo,
        IAgentCronJobRepository cronJobRepository,
        IAgentRunRepository agentRunRepository,
        AgentTaskStore taskStore,
        IMcpClientManager mcpClientManager,
        IBrowserToolContextFactory browserToolContextFactory,
        IAgentToolPermissionRepository permissionRepository)
    {
        _memoryRepo = memoryRepo;
        _cronJobRepository = cronJobRepository;
        _agentRunRepository = agentRunRepository;
        _taskStore = taskStore;
        _mcpClientManager = mcpClientManager;
        _browserToolContextFactory = browserToolContextFactory;
        _permissionRepository = permissionRepository;
    }

    public async Task<ToolRegistry> CreateAsync(
        IAgentSandbox sandbox,
        string sandboxId,
        string serviceUrl,
        Guid agentId,
        IReadOnlyList<McpServerRecord> mcpServers,
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
            new MemoryStoreTool(_memoryRepo, agentId),
            new MemoryRecallTool(_memoryRepo, agentId),
            new MemoryForgetTool(_memoryRepo, agentId),
            // Session/task orchestration
            new AskUserQuestionTool(),
            new TaskCreateTool(_taskStore, agentId),
            new TaskListTool(_taskStore, agentId),
            new TaskGetTool(_taskStore, agentId),
            new TaskUpdateTool(_taskStore, agentId),
            new CronCreateTool(_cronJobRepository, agentId),
            new CronListTool(_cronJobRepository, agentId),
            new CronDeleteTool(_cronJobRepository, agentId),
            new AgentSpawnTool(_agentRunRepository, agentId),
            // HTTP tools (backend)
            new HttpRequestTool(),
            new WebFetchTool(),
        };
        var preloadedToolNames = new HashSet<string>(StringComparer.Ordinal);

        try
        {
            var browserContext = await _browserToolContextFactory.CreateForTurnAsync(ct);
            if (browserContext is not null)
            {
                var browserTools = CreateBrowserTools(browserContext, agentId);
                tools.AddRange(browserTools);
                foreach (var tool in browserTools)
                    preloadedToolNames.Add(tool.Name);
            }
        }
        catch
        {
            // Browser is an internal optional runtime. If it is down, omit the
            // tools for this turn instead of failing the whole agent loop.
        }

        var mcpConnections = new List<IAsyncDisposable>();
        foreach (var server in mcpServers)
        {
            var creds = await credentialLoader(server.Name);
            var result = await _mcpClientManager.ConnectAsync(server, creds, ct);
            foreach (var discovered in result.Tools)
                tools.Add(new McpTool(discovered));
            tools.Add(new ListMcpResourcesTool(server.Name, result.NativeClient));
            tools.Add(new ReadMcpResourceTool(server.Name, result.NativeClient));
            mcpConnections.Add(result);
        }

        var permissions = await _permissionRepository.ListForAgentAsync(agentId, ct);
        var resolver = new AgentToolPermissionResolver(permissions);
        tools = tools.Where(resolver.IsAllowed).ToList();
        tools.Add(new ToolSearchTool(tools));

        preloadedToolNames.IntersectWith(tools.Select(t => t.Name));
        return new ToolRegistry(tools, context, mcpConnections, preloadedToolNames);
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
