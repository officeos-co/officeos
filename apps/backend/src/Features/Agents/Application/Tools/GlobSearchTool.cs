namespace OffceOs.Application.Features.Agents;

/// <summary>Find files by glob pattern.</summary>
internal sealed class GlobSearchTool : IAgentTool
{
    private readonly ToolExecutionContext _toolExecutionContext;
    public GlobSearchTool(ToolExecutionContext context) => _toolExecutionContext = context;

    public string Name => "glob_search";
    public AgentToolKind Kind => AgentToolKind.Read;
    public bool IsReadOnly => true;
    public ToolSchema Schema => new("glob_search",
        "Fast file pattern matching. Returns matching file paths sorted by modification time.",
        new
        {
            type = "object",
            properties = new
            {
                pattern = new { type = "string", description = "Glob pattern (e.g. '**/*.cs')" },
                path = new { type = "string", description = "Directory to search in (default '.')" },
                limit = new { type = "integer", description = "Max files (default 100)" }
            },
            required = new[] { "pattern" }
        });

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var pattern = args.GetProperty("pattern").GetString() ?? "";
        var path = args.TryGetProperty("path", out var p) ? p.GetString() ?? "." : ".";
        var limit = args.TryGetProperty("limit", out var l) ? Math.Clamp(l.GetInt32(), 1, 1000) : 100;

        var cmd = $"python3 - <<'PY'\nfrom pathlib import Path\nbase = Path({ToolShell.Escape(path)})\nitems = [p for p in base.glob({ToolShell.Escape(pattern)}) if p.is_file()]\nitems.sort(key=lambda p: p.stat().st_mtime, reverse=True)\nfor p in items[:{limit}]:\n    print(p)\nif len(items) > {limit}:\n    print(f'[truncated: {{len(items)-{limit}}} more files]')\nPY";
        var execResult = await _toolExecutionContext.Sandbox.ExecuteAsync(_toolExecutionContext.SandboxId, _toolExecutionContext.ServiceUrl, cmd, TimeSpan.FromSeconds(30), ct);
        if (execResult.IsFailure)
            return new AgentError(AgentErrorCategory.ToolExecution, $"glob_search: {execResult.Error.Message}", execResult.Error.Detail);

        var (output, exitCode) = execResult.Value;
        return exitCode == 0
            ? new ToolResult(true, string.IsNullOrEmpty(output) ? "No files found." : output)
            : new ToolResult(false, "", output);
    }
}
