using OffceOs.Domain.Common.Primitives;

namespace OffceOs.Application.Features.AgentHarness;

/// <summary>Search file contents using ripgrep.</summary>
internal sealed class ContentSearchTool : IAgentTool
{
    private readonly ToolExecutionContext _toolExecutionContext;
    public ContentSearchTool(ToolExecutionContext context) => _toolExecutionContext = context;

    public string Name => "content_search";
    public AgentToolKind Kind => AgentToolKind.Read;
    public bool IsReadOnly => true;
    public ToolSchema Schema => new("content_search",
        "Search file contents using ripgrep. Supports output modes, glob filters, context lines, file types, limits, offsets, and multiline mode.",
        new
        {
            type = "object",
            properties = new
            {
                pattern = new { type = "string", description = "Regex pattern to search for" },
                path = new { type = "string", description = "File or directory to search in (default '.')" },
                glob = new { type = "string", description = "Glob filter (e.g. '*.ts' or '**/*.tsx')" },
                output_mode = new { type = "string", @enum = new[] { "content", "files_with_matches", "count" }, description = "Default files_with_matches" },
                context = new { type = "integer", description = "Lines before and after each match for content mode" },
                case_sensitive = new { type = "boolean", description = "Case sensitive (default true)" },
                type = new { type = "string", description = "ripgrep file type, e.g. js, py, cs" },
                head_limit = new { type = "integer", description = "Limit output lines/entries (default 250, 0 unlimited)" },
                offset = new { type = "integer", description = "Skip first N output lines/entries before limiting" },
                multiline = new { type = "boolean", description = "Enable multiline matching" }
            },
            required = new[] { "pattern" }
        });

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var pattern = args.GetProperty("pattern").GetString() ?? "";
        var searchPath = args.TryGetProperty("path", out var p) ? p.GetString() ?? "." : ".";
        var outputMode = args.TryGetProperty("output_mode", out var om) ? om.GetString() ?? "files_with_matches" : "files_with_matches";
        var headLimit = args.TryGetProperty("head_limit", out var hl) ? hl.GetInt32() : 250;
        var offset = args.TryGetProperty("offset", out var off) ? Math.Max(0, off.GetInt32()) : 0;

        var flags = new List<string> { "-n" };
        if (outputMode == "files_with_matches") flags.Add("-l");
        if (outputMode == "count") flags.Add("-c");
        if (args.TryGetProperty("case_sensitive", out var cs) && !cs.GetBoolean()) flags.Add("-i");
        if (args.TryGetProperty("multiline", out var ml) && ml.GetBoolean()) flags.Add("-U --multiline-dotall");
        if (args.TryGetProperty("context", out var ctx)) flags.Add($"-C {Math.Max(0, ctx.GetInt32())}");
        if (args.TryGetProperty("glob", out var g) && !string.IsNullOrWhiteSpace(g.GetString())) flags.Add($"--glob {ToolShell.Escape(g.GetString()!)}");
        if (args.TryGetProperty("type", out var typ) && !string.IsNullOrWhiteSpace(typ.GetString())) flags.Add($"--type {ToolShell.Escape(typ.GetString()!)}");

        var pipeline = offset > 0 ? $" | tail -n +{offset + 1}" : "";
        if (headLimit != 0) pipeline += $" | head -n {Math.Clamp(headLimit, 1, 10_000)}";

        var cmd = $"rg {string.Join(' ', flags)} {ToolShell.Escape(pattern)} {ToolShell.Escape(searchPath)}{pipeline}";
        var execResult = await _toolExecutionContext.Sandbox.ExecuteAsync(_toolExecutionContext.SandboxId, _toolExecutionContext.ServiceUrl, cmd, TimeSpan.FromSeconds(30), ct);
        if (execResult.IsFailure)
            return new AgentError(AgentErrorCategory.ToolExecution, $"content_search: {execResult.Error.Message}", execResult.Error.Detail);

        var (output, exitCode) = execResult.Value;
        return exitCode <= 1
            ? new ToolResult(true, string.IsNullOrEmpty(output) ? "No matches found." : output)
            : new ToolResult(false, "", output);
    }
}
