namespace EnterpriseAgentOs.Application.Features.Agents;

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

        var execResult = await _context.Sandbox.ExecuteAsync(_context.SandboxId, _context.ServiceUrl, cmd, TimeSpan.FromSeconds(30), ct);
        if (execResult.IsFailure)
            return new AgentError(AgentErrorCategory.ToolExecution, $"file_edit: {execResult.Error.Message}", execResult.Error.Detail);

        var (output, exitCode) = execResult.Value;
        return exitCode == 0
            ? new ToolResult(true, output.Trim())
            : new ToolResult(false, "", output);
    }
}
