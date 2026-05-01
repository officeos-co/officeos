using System.Text.RegularExpressions;

namespace EnterpriseAgentOs.Application.Features.Agents;

internal static partial class ToolShell
{
    public static string Escape(string s) => "'" + s.Replace("'", "'\\''") + "'";
    public static string Base64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));

    [GeneratedRegex(@"(^|[;&|]\s*)(sudo\s+)?(rm\s+(-[^\s]*r[^\s]*f|-[^\s]*f[^\s]*r)|mkfs|dd\s+if=|:\(\)\s*\{|\bshutdown\b|\breboot\b|\bdrop\s+table\b)", RegexOptions.IgnoreCase)]
    public static partial Regex DestructiveCommandRegex();
}

/// <summary>Execute a shell command in the agent's OS.</summary>
internal sealed class ShellTool : IAgentTool
{
    private readonly ToolExecutionContext _context;
    public ShellTool(ToolExecutionContext context) => _context = context;

    public string Name => "shell";
    public AgentToolKind Kind => AgentToolKind.Execute;
    public ToolSchema Schema => new("shell",
        "Execute a shell command in the agent operating system. Include a short description of why the command is being run. Destructive commands require explicit user instruction.",
        new
        {
            type = "object",
            properties = new
            {
                command = new { type = "string", description = "The shell command to execute" },
                description = new { type = "string", description = "Brief reason for running this command" },
                timeout_secs = new { type = "integer", description = "Timeout in seconds (default 60, max 300)" }
            },
            required = new[] { "command" }
        });

    public Task<ToolValidationResult> ValidateAsync(JsonElement args, CancellationToken ct = default)
    {
        var command = args.TryGetProperty("command", out var c) ? c.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(command))
            return Task.FromResult(ToolValidationResult.Invalid("shell command is required."));

        if (ToolShell.DestructiveCommandRegex().IsMatch(command))
            return Task.FromResult(ToolValidationResult.Invalid("Potentially destructive shell command blocked. Ask the user for explicit confirmation before running it."));

        return Task.FromResult(ToolValidationResult.Valid);
    }

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var command = args.GetProperty("command").GetString() ?? "";
        var timeoutSecs = args.TryGetProperty("timeout_secs", out var t) ? Math.Clamp(t.GetInt32(), 1, 300) : 60;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSecs));

        var execResult = await _context.PodConnection.ExecuteAsync(command, cts.Token);
        if (execResult.IsFailure)
            return new AgentError(AgentErrorCategory.ToolExecution, $"shell: {execResult.Error.Message}", execResult.Error.Detail);

        var (output, exitCode) = execResult.Value;
        return exitCode == 0
            ? new ToolResult(true, output)
            : new ToolResult(false, output, $"exit code {exitCode}");
    }
}

/// <summary>Read a file with line numbers.</summary>
internal sealed class FileReadTool : IAgentTool
{
    private const int DefaultLimit = 2000;
    private readonly ToolExecutionContext _context;
    public FileReadTool(ToolExecutionContext context) => _context = context;

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
        var execResult = await _context.PodConnection.ExecuteAsync(cmd, ct);
        if (execResult.IsFailure)
            return new AgentError(AgentErrorCategory.ToolExecution, $"file_read: {execResult.Error.Message}", execResult.Error.Detail);

        var (output, exitCode) = execResult.Value;
        if (exitCode == 0)
        {
            _context.MarkFileRead(path);
            return new ToolResult(true, string.IsNullOrEmpty(output) ? "[empty file]" : output);
        }

        return new ToolResult(false, "", output);
    }
}

/// <summary>Write a file after reading existing contents.</summary>
internal sealed class FileWriteTool : IAgentTool
{
    private readonly ToolExecutionContext _context;
    public FileWriteTool(ToolExecutionContext context) => _context = context;

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

        var exists = await _context.PodConnection.ExecuteAsync($"test -e {ToolShell.Escape(path)}", ct);
        if (exists.IsSuccess && exists.Value.ExitCode == 0 && !_context.WasFileRead(path))
            return ToolValidationResult.Invalid($"file_write refused to overwrite {path}; read the existing file with file_read first.");

        return ToolValidationResult.Valid;
    }

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var path = args.GetProperty("path").GetString() ?? "";
        var content = args.GetProperty("content").GetString() ?? "";
        var payload = ToolShell.Base64(content);

        var cmd = $"mkdir -p \"$(dirname {ToolShell.Escape(path)})\" && printf %s {ToolShell.Escape(payload)} | base64 -d > {ToolShell.Escape(path)}";
        var execResult = await _context.PodConnection.ExecuteAsync(cmd, ct);
        if (execResult.IsFailure)
            return new AgentError(AgentErrorCategory.ToolExecution, $"file_write: {execResult.Error.Message}", execResult.Error.Detail);

        var (_, exitCode) = execResult.Value;
        return exitCode == 0
            ? new ToolResult(true, $"Wrote {Encoding.UTF8.GetByteCount(content)} bytes to {path}.")
            : new ToolResult(false, "", execResult.Value.Output);
    }
}

/// <summary>Exact string replacement in a file.</summary>
internal sealed class FileEditTool : IAgentTool
{
    private readonly ToolExecutionContext _context;
    public FileEditTool(ToolExecutionContext context) => _context = context;

    public string Name => "file_edit";
    public AgentToolKind Kind => AgentToolKind.Write;
    public ToolSchema Schema => new("file_edit",
        "Replace an exact string in a file. file_read must be used first. By default old_string must appear exactly once; set replace_all to update every occurrence.",
        new
        {
            type = "object",
            properties = new
            {
                path = new { type = "string", description = "File path to edit" },
                old_string = new { type = "string", description = "Exact string to find" },
                new_string = new { type = "string", description = "Replacement string" },
                replace_all = new { type = "boolean", description = "Replace every occurrence instead of requiring exactly one match" }
            },
            required = new[] { "path", "old_string", "new_string" }
        });

    public Task<ToolValidationResult> ValidateAsync(JsonElement args, CancellationToken ct = default)
    {
        var path = args.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "";
        var oldStr = args.TryGetProperty("old_string", out var o) ? o.GetString() ?? "" : "";
        var newStr = args.TryGetProperty("new_string", out var n) ? n.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(path)) return Task.FromResult(ToolValidationResult.Invalid("file_edit path is required."));
        if (oldStr.Length == 0) return Task.FromResult(ToolValidationResult.Invalid("file_edit old_string must not be empty."));
        if (oldStr == newStr) return Task.FromResult(ToolValidationResult.Invalid("No changes to make: old_string and new_string are identical."));
        if (!_context.WasFileRead(path)) return Task.FromResult(ToolValidationResult.Invalid($"file_edit refused to edit {path}; read the file with file_read first."));
        return Task.FromResult(ToolValidationResult.Valid);
    }

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var path = args.GetProperty("path").GetString() ?? "";
        var oldStr = args.GetProperty("old_string").GetString() ?? "";
        var newStr = args.GetProperty("new_string").GetString() ?? "";
        var replaceAll = args.TryGetProperty("replace_all", out var r) && r.GetBoolean();

        var payload = JsonSerializer.Serialize(new { path, old_string = oldStr, new_string = newStr, replace_all = replaceAll });
        var cmd = $"python3 - <<'PY'\nimport json\np = json.loads({ToolShell.Escape(payload)})\npath = p['path']\nwith open(path, 'r', encoding='utf-8') as f:\n    content = f.read()\ncount = content.count(p['old_string'])\nif count == 0:\n    raise SystemExit('old_string not found in ' + path)\nif not p['replace_all'] and count != 1:\n    raise SystemExit(f'old_string found {{count}} times in {{path}}, must be exactly 1 or use replace_all')\nupdated = content.replace(p['old_string'], p['new_string'] if p['replace_all'] else p['new_string'], -1 if p['replace_all'] else 1)\nwith open(path, 'w', encoding='utf-8') as f:\n    f.write(updated)\nprint(f'Edited {{path}}: replaced {{count if p[\"replace_all\"] else 1}} occurrence(s)')\nPY";

        var execResult = await _context.PodConnection.ExecuteAsync(cmd, ct);
        if (execResult.IsFailure)
            return new AgentError(AgentErrorCategory.ToolExecution, $"file_edit: {execResult.Error.Message}", execResult.Error.Detail);

        var (output, exitCode) = execResult.Value;
        return exitCode == 0
            ? new ToolResult(true, output.Trim())
            : new ToolResult(false, "", output);
    }
}

/// <summary>Search file contents using ripgrep.</summary>
internal sealed class ContentSearchTool : IAgentTool
{
    private readonly ToolExecutionContext _context;
    public ContentSearchTool(ToolExecutionContext context) => _context = context;

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
        var execResult = await _context.PodConnection.ExecuteAsync(cmd, ct);
        if (execResult.IsFailure)
            return new AgentError(AgentErrorCategory.ToolExecution, $"content_search: {execResult.Error.Message}", execResult.Error.Detail);

        var (output, exitCode) = execResult.Value;
        return exitCode <= 1
            ? new ToolResult(true, string.IsNullOrEmpty(output) ? "No matches found." : output)
            : new ToolResult(false, "", output);
    }
}

/// <summary>Find files by glob pattern.</summary>
internal sealed class GlobSearchTool : IAgentTool
{
    private readonly ToolExecutionContext _context;
    public GlobSearchTool(ToolExecutionContext context) => _context = context;

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
        var execResult = await _context.PodConnection.ExecuteAsync(cmd, ct);
        if (execResult.IsFailure)
            return new AgentError(AgentErrorCategory.ToolExecution, $"glob_search: {execResult.Error.Message}", execResult.Error.Detail);

        var (output, exitCode) = execResult.Value;
        return exitCode == 0
            ? new ToolResult(true, string.IsNullOrEmpty(output) ? "No files found." : output)
            : new ToolResult(false, "", output);
    }
}
