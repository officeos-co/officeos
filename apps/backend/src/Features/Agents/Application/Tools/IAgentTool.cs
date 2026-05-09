namespace EnterpriseAgentOs.Application.Features.Agents;

/// <summary>Result from a tool execution.</summary>
public record ToolResult(bool Success, string Output, string? Error = null);

/// <summary>Schema for an OpenAI function tool definition.</summary>
public record ToolSchema(string Name, string Description, object Parameters);

public enum AgentToolKind
{
    Read,
    Write,
    Execute,
    Network,
    Memory,
    Integration,
    Planning,
}

internal sealed class ToolExecutionContext
{
    private readonly HashSet<string> _readFiles = new(StringComparer.Ordinal);

    public ToolExecutionContext(Guid agentId, string sandboxId, string serviceUrl, IAgentSandbox sandbox)
    {
        AgentId = agentId;
        SandboxId = sandboxId;
        ServiceUrl = serviceUrl;
        Sandbox = sandbox;
    }

    public Guid AgentId { get; }
    public string SandboxId { get; }
    public string ServiceUrl { get; }
    public IAgentSandbox Sandbox { get; }

    public void MarkFileRead(string path) => _readFiles.Add(NormalizePath(path));
    public bool WasFileRead(string path) => _readFiles.Contains(NormalizePath(path));

    private static string NormalizePath(string path) => path.Trim();
}

public sealed record ToolValidationResult(bool IsValid, string? Message = null)
{
    public static ToolValidationResult Valid { get; } = new(true);
    public static ToolValidationResult Invalid(string message) => new(false, message);
}

/// <summary>An agent tool that can be dispatched by the turn loop.</summary>
public interface IAgentTool
{
    string Name { get; }
    ToolSchema Schema { get; }
    AgentToolKind Kind => AgentToolKind.Execute;
    string RuntimeName => Name;
    string PermissionScope => ToolPermissionPolicy.ScopeFor(this);
    bool ShouldDefer => ToolPermissionPolicy.ShouldDefer(this);
    bool AlwaysLoad => !ShouldDefer;
    string SearchHint => Schema.Description;
    bool IsReadOnly => false;
    bool IsConcurrencySafe => false;
    int MaxResultChars => 100_000;
    Task<ToolValidationResult> ValidateAsync(JsonElement args, CancellationToken ct = default)
        => Task.FromResult(ToolValidationResult.Valid);
    Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default);
}

internal static class ToolPermissionPolicy
{
    private static readonly HashSet<string> CoreToolNames = new(StringComparer.Ordinal)
    {
        "tool_search",
        "shell",
        "file_read",
        "file_write",
        "file_edit",
        "content_search",
        "glob_search",
        "memory_store",
        "memory_recall",
        "memory_forget",
        "ask_user_question",
        "task_create",
        "task_list",
        "task_get",
        "task_update",
        "http_request",
        "web_fetch",
        "integration_execute",
    };

    public static string ScopeFor(IAgentTool tool)
    {
        if (tool.Name.StartsWith("browser__", StringComparison.Ordinal))
            return $"browser:{tool.Name}";

        if (tool.Name.Contains("__", StringComparison.Ordinal))
        {
            var parts = tool.Name.Split("__", 2, StringSplitOptions.TrimEntries);
            return parts.Length == 2 ? $"{parts[0]}:{parts[1]}" : tool.Name;
        }

        return $"builtin:{tool.Name}";
    }

    public static bool ShouldDefer(IAgentTool tool)
        => tool.Kind is AgentToolKind.Integration
           || tool.Name.StartsWith("browser__", StringComparison.Ordinal)
           || tool.Name.StartsWith("cron_", StringComparison.Ordinal)
           || !CoreToolNames.Contains(tool.Name);
}
