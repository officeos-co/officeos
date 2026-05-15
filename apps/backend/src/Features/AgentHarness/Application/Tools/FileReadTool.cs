namespace OffceOs.Application.Features.AgentHarness;

/// <summary>Read a file with line numbers.</summary>
internal sealed class FileReadTool : IAgentTool
{
    private const int DefaultLimit = 2000;
    private readonly ToolExecutionContext _toolExecutionContext;
    public FileReadTool(ToolExecutionContext context) => _toolExecutionContext = context;

    public string Name => "file_read";
    public AgentToolKind Kind => AgentToolKind.Read;
    public bool IsReadOnly => true;
    public ToolSchema Schema => new("file_read",
        "Read file contents with cat -n style line numbers. Supports partial reads with offset and limit.",
        new
        {
            type = "object",
            properties = new
            {
                path = new { type = "string", description = "File path to read" },
                offset = new { type = "integer", description = "Start line (1-based, default 1)" },
                limit = new { type = "integer", description = $"Max lines to read (default {DefaultLimit})" }
            },
            required = new[] { "path" }
        });

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var path = args.GetProperty("path").GetString() ?? "";
        var offset = args.TryGetProperty("offset", out var o) ? Math.Max(1, o.GetInt32()) : 1;
        var limit = args.TryGetProperty("limit", out var l) ? Math.Clamp(l.GetInt32(), 1, 10_000) : DefaultLimit;

        var cmd = $"if [ -d {ToolShell.Escape(path)} ]; then echo 'Error: path is a directory' >&2; exit 2; fi; cat -n {ToolShell.Escape(path)} | sed -n '{offset},{offset + limit - 1}p'";
        var execResult = await _toolExecutionContext.Sandbox.ExecuteAsync(_toolExecutionContext.SandboxId, _toolExecutionContext.ServiceUrl, cmd, TimeSpan.FromSeconds(30), ct);
        if (execResult.IsFailure)
            return new AgentError(AgentErrorCategory.ToolExecution, $"file_read: {execResult.Error.Message}", execResult.Error.Detail);

        var (output, exitCode) = execResult.Value;
        if (exitCode == 0)
        {
            _toolExecutionContext.MarkFileRead(path);
            return new ToolResult(true, string.IsNullOrEmpty(output) ? "[empty file]" : output);
        }

        return new ToolResult(false, "", output);
    }
}
