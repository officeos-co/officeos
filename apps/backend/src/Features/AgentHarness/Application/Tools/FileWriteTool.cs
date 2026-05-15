namespace OffceOs.Application.Features.AgentHarness;

/// <summary>Write a file after reading existing contents.</summary>
internal sealed class FileWriteTool : IAgentTool
{
    private readonly ToolExecutionContext _toolExecutionContext;
    public FileWriteTool(ToolExecutionContext context) => _toolExecutionContext = context;

    public string Name => "file_write";
    public AgentToolKind Kind => AgentToolKind.Write;
    public ToolSchema Schema => new("file_write",
        "Create or completely overwrite a file. If the file already exists, file_read must be used first. Prefer file_edit for modifying existing files.",
        new
        {
            type = "object",
            properties = new
            {
                path = new { type = "string", description = "File path to write" },
                content = new { type = "string", description = "Content to write" }
            },
            required = new[] { "path", "content" }
        });

    public async Task<ToolValidationResult> ValidateAsync(JsonElement args, CancellationToken ct = default)
    {
        var path = args.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(path))
            return ToolValidationResult.Invalid("file_write path is required.");

        var exists = await _toolExecutionContext.Sandbox.ExecuteAsync(_toolExecutionContext.SandboxId, _toolExecutionContext.ServiceUrl, $"test -e {ToolShell.Escape(path)}", TimeSpan.FromSeconds(10), ct);
        if (exists.IsSuccess && exists.Value.ExitCode == 0 && !_toolExecutionContext.WasFileRead(path))
            return ToolValidationResult.Invalid($"file_write refused to overwrite {path}; read the existing file with file_read first.");

        return ToolValidationResult.Valid;
    }

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var path = args.GetProperty("path").GetString() ?? "";
        var content = args.GetProperty("content").GetString() ?? "";
        var payload = ToolShell.Base64(content);

        var cmd = $"mkdir -p \"$(dirname {ToolShell.Escape(path)})\" && printf %s {ToolShell.Escape(payload)} | base64 -d > {ToolShell.Escape(path)}";
        var execResult = await _toolExecutionContext.Sandbox.ExecuteAsync(_toolExecutionContext.SandboxId, _toolExecutionContext.ServiceUrl, cmd, TimeSpan.FromSeconds(30), ct);
        if (execResult.IsFailure)
            return new AgentError(AgentErrorCategory.ToolExecution, $"file_write: {execResult.Error.Message}", execResult.Error.Detail);

        var (_, exitCode) = execResult.Value;
        return exitCode == 0
            ? new ToolResult(true, $"Wrote {Encoding.UTF8.GetByteCount(content)} bytes to {path}.")
            : new ToolResult(false, "", execResult.Value.Output);
    }
}
