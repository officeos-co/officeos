namespace EnterpriseAgentOs.Application.Services.Agents.Tools;

/// <summary>Result from a tool execution.</summary>
public record ToolResult(bool Success, string Output, string? Error = null);

/// <summary>Schema for an OpenAI function tool definition.</summary>
public record ToolSchema(string Name, string Description, object Parameters);

/// <summary>An agent tool that can be dispatched by the turn loop.</summary>
public interface IAgentTool
{
    string Name { get; }
    ToolSchema Schema { get; }
    Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default);
}
