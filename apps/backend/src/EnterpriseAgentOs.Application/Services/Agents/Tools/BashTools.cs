using System.Text.Json;

namespace EnterpriseAgentOs.Application.Services.Agents.Tools;

/// <summary>Execute a shell command in the agent's OS.</summary>
public sealed class ShellTool : IAgentTool
{
    private readonly PodConnection _pod;
    public ShellTool(PodConnection pod) => _pod = pod;

    public string Name => "shell";
    public ToolSchema Schema => new("shell",
        "Execute a shell command in the agent's operating system. Use for package management, running scripts, system tasks.",
        new
        {
            type = "object",
            properties = new
            {
                command = new { type = "string", description = "The shell command to execute" },
                timeout_secs = new { type = "integer", description = "Timeout in seconds (default 60)" }
            },
            required = new[] { "command" }
        });

    public async Task<ToolResult> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var command = args.GetProperty("command").GetString() ?? "";
        var timeout = args.TryGetProperty("timeout_secs", out var t) ? t.GetInt32() : 60;
        var (output, exitCode) = await _pod.ExecuteAsync(command, ct);
        return exitCode == 0
            ? new ToolResult(true, output)
            : new ToolResult(false, output, $"exit code {exitCode}");
    }
}

/// <summary>Read a file with line numbers.</summary>
public sealed class FileReadTool : IAgentTool
{
    private readonly PodConnection _pod;
    public FileReadTool(PodConnection pod) => _pod = pod;

    public string Name => "file_read";
    public ToolSchema Schema => new("file_read",
        "Read file contents with line numbers. Supports partial reads with offset and limit.",
        new
        {
            type = "object",
            properties = new
            {
                path = new { type = "string", description = "File path to read" },
                offset = new { type = "integer", description = "Start line (1-based, default 1)" },
                limit = new { type = "integer", description = "Max lines to read" }
            },
            required = new[] { "path" }
        });

    public async Task<ToolResult> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var path = args.GetProperty("path").GetString() ?? "";
        var offset = args.TryGetProperty("offset", out var o) ? o.GetInt32() : 1;
        var limit = args.TryGetProperty("limit", out var l) ? l.GetInt32() : 0;

        var cmd = limit > 0
            ? $"cat -n {ShellEscape(path)} | sed -n '{offset},{offset + limit - 1}p'"
            : offset > 1
                ? $"cat -n {ShellEscape(path)} | sed -n '{offset},$p'"
                : $"cat -n {ShellEscape(path)}";

        var (output, exitCode) = await _pod.ExecuteAsync(cmd, ct);
        return exitCode == 0
            ? new ToolResult(true, output)
            : new ToolResult(false, "", output);
    }

    private static string ShellEscape(string s) => "'" + s.Replace("'", "'\\''") + "'";
}

/// <summary>Write a file (creates parent directories).</summary>
public sealed class FileWriteTool : IAgentTool
{
    private readonly PodConnection _pod;
    public FileWriteTool(PodConnection pod) => _pod = pod;

    public string Name => "file_write";
    public ToolSchema Schema => new("file_write",
        "Write content to a file. Creates parent directories if needed. Overwrites existing files.",
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

    public async Task<ToolResult> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var path = args.GetProperty("path").GetString() ?? "";
        var content = args.GetProperty("content").GetString() ?? "";

        var cmd = $"mkdir -p \"$(dirname {ShellEscape(path)})\" && cat > {ShellEscape(path)} << 'EAOS_EOF'\n{content}\nEAOS_EOF";
        var (output, exitCode) = await _pod.ExecuteAsync(cmd, ct);
        return exitCode == 0
            ? new ToolResult(true, $"Wrote {content.Length} bytes to {path}")
            : new ToolResult(false, "", output);
    }

    private static string ShellEscape(string s) => "'" + s.Replace("'", "'\\''") + "'";
}

/// <summary>Exact string replacement in a file (must match exactly once).</summary>
public sealed class FileEditTool : IAgentTool
{
    private readonly PodConnection _pod;
    public FileEditTool(PodConnection pod) => _pod = pod;

    public string Name => "file_edit";
    public ToolSchema Schema => new("file_edit",
        "Replace an exact string in a file. The old_string must appear exactly once. Case-sensitive.",
        new
        {
            type = "object",
            properties = new
            {
                path = new { type = "string", description = "File path to edit" },
                old_string = new { type = "string", description = "Exact string to find (must appear once)" },
                new_string = new { type = "string", description = "Replacement string" }
            },
            required = new[] { "path", "old_string", "new_string" }
        });

    public async Task<ToolResult> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var path = args.GetProperty("path").GetString() ?? "";
        var oldStr = args.GetProperty("old_string").GetString() ?? "";
        var newStr = args.GetProperty("new_string").GetString() ?? "";

        // Count occurrences first
        var countCmd = $"grep -cF {ShellEscape(oldStr)} {ShellEscape(path)}";
        var (countOut, _) = await _pod.ExecuteAsync(countCmd, ct);
        if (int.TryParse(countOut.Trim(), out var count))
        {
            if (count == 0) return new ToolResult(false, "", $"old_string not found in {path}");
            if (count > 1) return new ToolResult(false, "", $"old_string found {count} times in {path}, must be exactly 1");
        }

        // Use python for reliable string replacement
        var pyCmd = $"python3 -c \"\nimport sys\nwith open({PyEscape(path)}, 'r') as f: content = f.read()\ncontent = content.replace({PyEscape(oldStr)}, {PyEscape(newStr)}, 1)\nwith open({PyEscape(path)}, 'w') as f: f.write(content)\nprint(f'Edited {{len(content)}} bytes')\n\"";
        var (output, exitCode) = await _pod.ExecuteAsync(pyCmd, ct);
        return exitCode == 0
            ? new ToolResult(true, output.Trim())
            : new ToolResult(false, "", output);
    }

    private static string ShellEscape(string s) => "'" + s.Replace("'", "'\\''") + "'";
    private static string PyEscape(string s) => "'" + s.Replace("\\", "\\\\").Replace("'", "\\'") + "'";
}

/// <summary>Search file contents using grep/ripgrep.</summary>
public sealed class ContentSearchTool : IAgentTool
{
    private readonly PodConnection _pod;
    public ContentSearchTool(PodConnection pod) => _pod = pod;

    public string Name => "content_search";
    public ToolSchema Schema => new("content_search",
        "Search file contents using regex. Uses ripgrep if available, falls back to grep.",
        new
        {
            type = "object",
            properties = new
            {
                pattern = new { type = "string", description = "Regex pattern to search for" },
                path = new { type = "string", description = "Directory to search in (default '.')" },
                include = new { type = "string", description = "Glob filter (e.g. '*.ts')" },
                case_sensitive = new { type = "boolean", description = "Case sensitive (default true)" },
                max_results = new { type = "integer", description = "Max results (default 1000)" }
            },
            required = new[] { "pattern" }
        });

    public async Task<ToolResult> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var pattern = args.GetProperty("pattern").GetString() ?? "";
        var searchPath = args.TryGetProperty("path", out var p) ? p.GetString() ?? "." : ".";
        var include = args.TryGetProperty("include", out var i) ? i.GetString() : null;
        var caseSensitive = !args.TryGetProperty("case_sensitive", out var cs) || cs.GetBoolean();
        var maxResults = args.TryGetProperty("max_results", out var m) ? m.GetInt32() : 1000;

        var caseFlag = caseSensitive ? "" : " -i";
        var includeFlag = include != null ? $" --include={ShellEscape(include)}" : "";

        var cmd = $"(command -v rg > /dev/null && rg -n{caseFlag} --max-count {maxResults} {ShellEscape(pattern)} {ShellEscape(searchPath)}) || grep -rn{caseFlag}{includeFlag} {ShellEscape(pattern)} {ShellEscape(searchPath)} | head -n {maxResults}";
        var (output, exitCode) = await _pod.ExecuteAsync(cmd, ct);

        return exitCode <= 1
            ? new ToolResult(true, string.IsNullOrEmpty(output) ? "No matches found." : output)
            : new ToolResult(false, "", output);
    }

    private static string ShellEscape(string s) => "'" + s.Replace("'", "'\\''") + "'";
}

/// <summary>Find files by glob pattern.</summary>
public sealed class GlobSearchTool : IAgentTool
{
    private readonly PodConnection _pod;
    public GlobSearchTool(PodConnection pod) => _pod = pod;

    public string Name => "glob_search";
    public ToolSchema Schema => new("glob_search",
        "Find files matching a glob pattern.",
        new
        {
            type = "object",
            properties = new
            {
                pattern = new { type = "string", description = "Glob pattern (e.g. '**/*.rs')" }
            },
            required = new[] { "pattern" }
        });

    public async Task<ToolResult> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var pattern = args.GetProperty("pattern").GetString() ?? "";
        var cmd = $"find . -path {ShellEscape("./" + pattern)} -type f 2>/dev/null | head -n 1000 | sort";
        var (output, exitCode) = await _pod.ExecuteAsync(cmd, ct);
        return new ToolResult(true, string.IsNullOrEmpty(output) ? "No files found." : output);
    }

    private static string ShellEscape(string s) => "'" + s.Replace("'", "'\\''") + "'";
}
