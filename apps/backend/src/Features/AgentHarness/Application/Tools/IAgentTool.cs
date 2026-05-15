namespace OffceOs.Application.Features.AgentHarness;

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
    string? PermissionScopeOverride => null;
    bool? ShouldDeferOverride => null;
    string SearchHint => Schema.Description;
    bool IsReadOnly => false;
    bool IsConcurrencySafe => false;
    int MaxResultChars => 100_000;
    Task<ToolValidationResult> ValidateAsync(JsonElement args, CancellationToken ct = default)
        => Task.FromResult(ToolValidationResult.Valid);
    Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default);
}
