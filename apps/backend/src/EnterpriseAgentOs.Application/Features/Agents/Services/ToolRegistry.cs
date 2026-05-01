using System.Text.Json;

namespace EnterpriseAgentOs.Application.Features.Agents;

/// <summary>
/// Registry of all agent tools. Creates tool instances per-turn with the appropriate dependencies.
/// </summary>
internal sealed class ToolRegistry : IAsyncDisposable
{
    private readonly List<IAgentTool> _tools;
    private readonly List<IAsyncDisposable> _mcpConnections;

    public ToolRegistry(List<IAgentTool> tools, List<IAsyncDisposable>? mcpConnections = null)
    {
        _tools = tools;
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

        return await tool.ExecuteAsync(args, ct);
    }

}

/// <summary>
/// Builds a per-turn tool registry and owns tool construction dependencies.
/// </summary>
internal sealed class ToolRegistryFactory
{
    private readonly IAgentMemoryRepository _memoryRepo;
    private readonly IMcpClientManager _mcpClientManager;
    private readonly IBrowserService _browserService;
    private readonly IBrowserRuntimeClient _browserRuntime;

    public ToolRegistryFactory(
        IAgentMemoryRepository memoryRepo,
        IMcpClientManager mcpClientManager,
        IBrowserService browserService,
        IBrowserRuntimeClient browserRuntime)
    {
        _memoryRepo = memoryRepo;
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
        var tools = new List<IAgentTool>
        {
            // Bash tools (execute via pod PTY)
            new ShellTool(pod),
            new FileReadTool(pod),
            new FileWriteTool(pod),
            new FileEditTool(pod),
            new ContentSearchTool(pod),
            new GlobSearchTool(pod),
            // Memory tools (Postgres)
            new MemoryStoreTool(_memoryRepo, agentId),
            new MemoryRecallTool(_memoryRepo, agentId),
            new MemoryForgetTool(_memoryRepo, agentId),
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
            mcpConnections.Add(result);
        }

        return new ToolRegistry(tools, mcpConnections);
    }
}
