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

    /// <summary>
    /// Build a tool registry for a specific agent turn.
    /// Pod tools need the PodConnection, memory tools need the repo + agentId, etc.
    /// </summary>
    public static async Task<ToolRegistry> CreateAsync(
        PodConnection pod,
        IAgentMemoryRepository memoryRepo,
        Guid agentId,
        IMcpClientManager mcpClientManager,
        IReadOnlyList<McpServerRecord> mcpServers,
        IBrowserService browserService,
        IBrowserRuntimeClient browserRuntime,
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
            new MemoryStoreTool(memoryRepo, agentId),
            new MemoryRecallTool(memoryRepo, agentId),
            new MemoryForgetTool(memoryRepo, agentId),
            // HTTP tools (backend)
            new HttpRequestTool(),
            new WebFetchTool(),
        };

        try
        {
            if (await browserRuntime.IsAvailableAsync(ct))
            {
                var browserTools = await browserRuntime.ListToolsAsync(ct);
                foreach (var discovered in browserTools.Where(BrowserMcpTool.ShouldExpose))
                    tools.Add(new BrowserMcpTool(discovered, browserService, browserRuntime, agentId));
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
            var result = await mcpClientManager.ConnectAsync(server, creds, ct);
            foreach (var discovered in result.Tools)
                tools.Add(new McpTool(discovered));
            mcpConnections.Add(result);
        }

        return new ToolRegistry(tools, mcpConnections);
    }
}
