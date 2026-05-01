using System.Text.Json;

namespace EnterpriseAgentOs.Application.Features.Agents;

/// <summary>
/// Registry of all agent tools. Creates tool instances per-turn with the appropriate dependencies.
/// </summary>
internal sealed class ToolRegistry : IAsyncDisposable
{
    private readonly List<IAgentTool> _tools;
    private readonly List<IAsyncDisposable> _mcpConnections;
    private readonly ToolExecutionContext _context;

    public ToolRegistry(List<IAgentTool> tools, ToolExecutionContext context, List<IAsyncDisposable>? mcpConnections = null)
    {
        _tools = tools;
        _context = context;
        _mcpConnections = mcpConnections ?? [];
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var conn in _mcpConnections)
            await conn.DisposeAsync();
    }

    public IReadOnlyList<IAgentTool> Tools => _tools;

    /// <summary>Get all tool schemas for the LLM tools array.</summary>
    public object[] GetSchemas() => _tools.Select(t => new
    {
        type = "function",
        function = new
        {
            name = t.Schema.Name,
            description = t.Schema.Description,
            parameters = t.Schema.Parameters,
        }
    }).ToArray();

    /// <summary>Dispatch a tool call by name.</summary>
    public async Task<AgentResult<ToolResult>> DispatchAsync(string toolName, JsonElement args, CancellationToken ct)
    {
        var tool = _tools.FirstOrDefault(t => t.Name == toolName);
        if (tool is null)
            return new AgentError(AgentErrorCategory.ToolExecution, $"Unknown tool: {toolName}");

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
    private readonly AgentTaskStore _taskStore;
    private readonly IMcpClientManager _mcpClientManager;
    private readonly IBrowserService _browserService;
    private readonly IBrowserRuntimeClient _browserRuntime;

    public ToolRegistryFactory(
        IAgentMemoryRepository memoryRepo,
        IAgentCronJobRepository cronJobRepository,
        AgentTaskStore taskStore,
        IMcpClientManager mcpClientManager,
        IBrowserService browserService,
        IBrowserRuntimeClient browserRuntime)
    {
        _memoryRepo = memoryRepo;
        _cronJobRepository = cronJobRepository;
        _taskStore = taskStore;
        _mcpClientManager = mcpClientManager;
        _browserService = browserService;
        _browserRuntime = browserRuntime;
    }

    public async Task<ToolRegistry> CreateAsync(
        PodConnection pod,
        Guid agentId,
        IReadOnlyList<McpServerRecord> mcpServers,
            Func<string, Task<Dictionary<string, string>>> credentialLoader,
            CancellationToken ct)
    {
        var context = new ToolExecutionContext(agentId, pod);
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
            // HTTP tools (backend)
            new HttpRequestTool(),
            new WebFetchTool(),
        };

        try
        {
            if (await _browserRuntime.IsAvailableAsync(ct))
            {
                var browserTools = await _browserRuntime.ListToolsAsync(ct);
                foreach (var discovered in browserTools.Where(BrowserMcpTool.ShouldExpose))
                    tools.Add(new BrowserMcpTool(discovered, _browserService, _browserRuntime, agentId));
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

        tools.Add(new ToolSearchTool(tools));

        return new ToolRegistry(tools, context, mcpConnections);
    }
}
