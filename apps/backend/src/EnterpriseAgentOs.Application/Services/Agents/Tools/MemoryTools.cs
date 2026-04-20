using System.Text.Json;

namespace EnterpriseAgentOs.Application.Services.Agents.Tools;

public sealed class MemoryStoreTool : IAgentTool
{
    private readonly IAgentMemoryRepository _repo;
    private readonly Guid _agentId;

    public MemoryStoreTool(IAgentMemoryRepository repo, Guid agentId)
    {
        _repo = repo;
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

    public async Task<ToolResult> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var key = args.GetProperty("key").GetString() ?? "";
        var content = args.GetProperty("content").GetString() ?? "";

        try
        {
            await _repo.UpsertAsync(_agentId, key, content, ct);
            return new ToolResult(true, $"Stored memory '{key}'.");
        }
        catch (Exception ex)
        {
            return new ToolResult(false, "", ex.Message);
        }
    }
}

public sealed class MemoryRecallTool : IAgentTool
{
    private readonly IAgentMemoryRepository _repo;
    private readonly Guid _agentId;

    public MemoryRecallTool(IAgentMemoryRepository repo, Guid agentId)
    {
        _repo = repo;
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

    public async Task<ToolResult> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var query = args.TryGetProperty("query", out var q) ? q.GetString() ?? "" : "";
        var limit = args.TryGetProperty("limit", out var l) ? l.GetInt32() : 5;

        var memories = await _repo.ListAsync(_agentId, ct);

        IEnumerable<AgentMemoryRecord> filtered = memories;
        if (!string.IsNullOrEmpty(query))
        {
            filtered = memories
                .Where(m => m.Key.Contains(query, StringComparison.OrdinalIgnoreCase)
                         || m.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m =>
                    CountOccurrences(m.Key + " " + m.Content, query));
        }

        var results = filtered.Take(limit).ToList();
        if (results.Count == 0)
            return new ToolResult(true, "No memories found.");

        var output = string.Join("\n\n", results.Select(m => $"### {m.Key}\n{m.Content}"));
        return new ToolResult(true, output);
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var idx = 0;
        while ((idx = text.IndexOf(pattern, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            idx += pattern.Length;
        }
        return count;
    }
}

public sealed class MemoryForgetTool : IAgentTool
{
    private readonly IAgentMemoryRepository _repo;
    private readonly Guid _agentId;

    public MemoryForgetTool(IAgentMemoryRepository repo, Guid agentId)
    {
        _repo = repo;
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

    public async Task<ToolResult> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var key = args.GetProperty("key").GetString() ?? "";
        var deleted = await _repo.DeleteAsync(_agentId, key, ct);
        return deleted
            ? new ToolResult(true, $"Forgot memory '{key}'.")
            : new ToolResult(true, $"No memory found with key '{key}'.");
    }
}
