using System.Text.Json;

namespace EnterpriseAgentOs.Application.Features.AgentTurn.Tools;

internal sealed class MemoryStoreTool : IAgentTool
{
    private readonly IAgentMemoryRepository _agentMemoryRepository;
    private readonly Guid _agentId;

    public MemoryStoreTool(IAgentMemoryRepository repo, Guid agentId)
    {
        _agentMemoryRepository = repo;
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
            await _agentMemoryRepository.UpsertAsync(_agentId, key, content, ct);
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
    private readonly IAgentMemoryRepository _agentMemoryRepository;
    private readonly Guid _agentId;

    public MemoryRecallTool(IAgentMemoryRepository repo, Guid agentId)
    {
        _agentMemoryRepository = repo;
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
            var memories = await _agentMemoryRepository.ListAsync(_agentId, ct);

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
        catch (Exception ex)
        {
            return new AgentError(AgentErrorCategory.Memory, $"memory_recall: {ex.Message}", ex.ToString());
        }
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

internal sealed class MemoryForgetTool : IAgentTool
{
    private readonly IAgentMemoryRepository _agentMemoryRepository;
    private readonly Guid _agentId;

    public MemoryForgetTool(IAgentMemoryRepository repo, Guid agentId)
    {
        _agentMemoryRepository = repo;
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
            var deleted = await _agentMemoryRepository.DeleteAsync(_agentId, key, ct);
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
