using OffceOs.Application.Features.Context;
using OffceOs.Domain.Common.Primitives;
namespace OffceOs.Application.Features.AgentHarness;

internal sealed class MemoryStoreTool : IAgentTool
{
    private readonly IAgentMemoryService _agentMemoryService;
    private readonly Guid _agentId;

    public MemoryStoreTool(IAgentMemoryService memoryService, Guid agentId)
    {
        _agentMemoryService = memoryService;
        _agentId = agentId;
    }

    public string Name => "memory_store";
    public ToolSchema Schema => new("memory_store",
        "Store a fact in long-term memory. Use this to remember important information across conversations.",
        new
        {
            type = "object",
            properties = new
            {
                key = new { type = "string", description = "Memory key (unique identifier)" },
                content = new { type = "string", description = "Content to store" }
            },
            required = new[] { "key", "content" }
        });

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var key = args.GetProperty("key").GetString() ?? "";
        var content = args.GetProperty("content").GetString() ?? "";

        try
        {
            await _agentMemoryService.StoreAsync(_agentId, key, content, ct);
            return new ToolResult(true, $"Stored memory '{key}'.");
        }
        catch (Exception ex)
        {
            return new AgentError(AgentErrorCategory.Memory, $"memory_store: {ex.Message}", ex.ToString());
        }
    }
}

internal sealed class MemoryRecallTool : IAgentTool
{
    private readonly IAgentMemoryService _agentMemoryService;
    private readonly Guid _agentId;

    public MemoryRecallTool(IAgentMemoryService memoryService, Guid agentId)
    {
        _agentMemoryService = memoryService;
        _agentId = agentId;
    }

    public string Name => "memory_recall";
    public ToolSchema Schema => new("memory_recall",
        "Search long-term memory by keyword. Returns matching memories ranked by relevance.",
        new
        {
            type = "object",
            properties = new
            {
                query = new { type = "string", description = "Search keyword (optional, lists all if empty)" },
                limit = new { type = "integer", description = "Max results (default 5)" }
            }
        });

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var query = args.TryGetProperty("query", out var q) ? q.GetString() ?? "" : "";
        var limit = args.TryGetProperty("limit", out var l) ? l.GetInt32() : 5;

        try
        {
            var memories = await _agentMemoryService.RecallAsync(_agentId, query, limit, ct);
            if (memories.Count == 0)
                return new ToolResult(true, "No memories found.");

            var output = string.Join("\n\n", memories.Select(m => m.FormatPromptSection()));
            return new ToolResult(true, output);
        }
        catch (Exception ex)
        {
            return new AgentError(AgentErrorCategory.Memory, $"memory_recall: {ex.Message}", ex.ToString());
        }
    }
}

internal sealed class MemoryForgetTool : IAgentTool
{
    private readonly IAgentMemoryService _agentMemoryService;
    private readonly Guid _agentId;

    public MemoryForgetTool(IAgentMemoryService memoryService, Guid agentId)
    {
        _agentMemoryService = memoryService;
        _agentId = agentId;
    }

    public string Name => "memory_forget";
    public ToolSchema Schema => new("memory_forget",
        "Remove a memory by key.",
        new
        {
            type = "object",
            properties = new
            {
                key = new { type = "string", description = "Memory key to remove" }
            },
            required = new[] { "key" }
        });

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var key = args.GetProperty("key").GetString() ?? "";

        try
        {
            var deleted = await _agentMemoryService.ForgetAsync(_agentId, key, ct);
            return deleted
                ? new ToolResult(true, $"Forgot memory '{key}'.")
                : new ToolResult(true, $"No memory found with key '{key}'.");
        }
        catch (Exception ex)
        {
            return new AgentError(AgentErrorCategory.Memory, $"memory_forget: {ex.Message}", ex.ToString());
        }
    }
}
