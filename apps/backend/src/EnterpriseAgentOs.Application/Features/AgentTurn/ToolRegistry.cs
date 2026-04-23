using System.Text.Json;

namespace EnterpriseAgentOs.Application.Features.AgentTurn;

/// <summary>
/// Registry of all agent tools. Creates tool instances per-turn with the appropriate dependencies.
/// </summary>
internal sealed class ToolRegistry
{
    private readonly List<IAgentTool> _tools;

    public ToolRegistry(List<IAgentTool> tools) => _tools = tools;

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
    public static ToolRegistry Create(
        PodConnection pod,
        IAgentMemoryRepository memoryRepo,
        Guid agentId,
        SkillRuntimeClient skillRuntime,
        ISkillService skillService,
        IReadOnlyList<SkillRecord> skillDetails)
    {
        return new ToolRegistry(new List<IAgentTool>
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
            // Skill tools (direct to skill-runtime, no HTTP loopback)
            new SkillExecTool(skillRuntime, skillService, skillDetails),
            new SkillReadTool(skillDetails),
        });
    }
}
