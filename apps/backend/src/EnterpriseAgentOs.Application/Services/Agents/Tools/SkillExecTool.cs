using System.Text.Json;
using EnterpriseAgentOs.Infrastructure.Adapters.SkillRuntime;

namespace EnterpriseAgentOs.Application.Services.Agents.Tools;

/// <summary>
/// CLI-like interface to execute skills directly via SkillRuntimeClient.
/// No HTTP loopback — calls skill-runtime directly with decrypted credentials.
/// Mirrors the Rust skill_exec tool: --help for discovery, skill action --arg val for execution.
/// </summary>
public sealed class SkillExecTool : IAgentTool
{
    private readonly SkillRuntimeClient _skillRuntimeClient;
    private readonly ISkillService _skillService;
    private readonly IReadOnlyList<SkillRecord> _skills;

    public SkillExecTool(SkillRuntimeClient runtime, ISkillService skillService, IReadOnlyList<SkillRecord> skills)
    {
        _skillRuntimeClient = runtime;
        _skillService = skillService;
        _skills = skills;
    }

    public string Name => "skill_exec";
    public ToolSchema Schema => new("skill_exec",
        "Execute a skill via CLI-like commands. Use '--help' to discover available skills and actions. " +
        "Example: 'notion search --query meetings', 'github list_issues --repo myrepo'.",
        new
        {
            type = "object",
            properties = new
            {
                command = new { type = "string", description = "CLI command (e.g. '--help', 'notion search --query meetings')" }
            },
            required = new[] { "command" }
        });

    public async Task<ToolResult> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var command = args.GetProperty("command").GetString() ?? "";

        if (command.Trim() is "--help" or "")
            return GetHelp();

        // Parse: skill action --arg1 val1 --arg2 val2
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return new ToolResult(false, "", "Usage: skill_exec <skill> <action> [--arg value ...]");

        var skillName = parts[0];
        var action = parts[1];

        if (action == "--help")
            return GetSkillHelp(skillName);

        // Parse CLI args
        var cliArgs = new Dictionary<string, object>();
        for (var i = 2; i < parts.Length; i++)
        {
            if (parts[i].StartsWith("--") && i + 1 < parts.Length)
            {
                var key = parts[i][2..];
                var value = parts[++i];
                // Coerce types: int, bool, or string
                if (int.TryParse(value, out var intVal))
                    cliArgs[key] = intVal;
                else if (bool.TryParse(value, out var boolVal))
                    cliArgs[key] = boolVal;
                else
                    cliArgs[key] = value;
            }
        }

        return await DispatchAsync(skillName, action, cliArgs, ct);
    }

    private ToolResult GetHelp()
    {
        if (_skills.Count == 0)
            return new ToolResult(true, "No skills installed. Install skills from the dashboard.");

        var output = "Available skills:\n\n";
        foreach (var skill in _skills)
        {
            output += $"  - {skill.Name}: {skill.Description}\n";
            var actions = ParseActions(skill.ActionsJson);
            if (actions != null)
            {
                foreach (var a in actions)
                    output += $"      {skill.Name} {a.Name}  — {a.Description}\n";
            }
        }
        output += "\nUse 'skill_exec <skill> --help' for parameter details.";
        return new ToolResult(true, output);
    }

    private ToolResult GetSkillHelp(string skillName)
    {
        var skill = _skills.FirstOrDefault(s =>
            string.Equals(s.Name, skillName, StringComparison.OrdinalIgnoreCase));

        if (skill is null)
            return new ToolResult(false, "", $"Skill '{skillName}' not found. Use '--help' to list available skills.");

        var output = $"# {skill.Name}\n{skill.Description}\n\nActions:\n";
        var actions = ParseActions(skill.ActionsJson);
        if (actions != null)
        {
            foreach (var a in actions)
            {
                output += $"\n  {skill.Name} {a.Name}\n    {a.Description}\n";
                if (a.Params.Count > 0)
                {
                    output += "    Parameters:\n";
                    foreach (var p in a.Params)
                        output += $"      --{p.Name} ({p.Type}){(p.Required ? " [required]" : "")}: {p.Description}\n";
                }
            }
        }
        return new ToolResult(true, output);
    }

    private async Task<ToolResult> DispatchAsync(string skillName, string action, Dictionary<string, object> cliArgs, CancellationToken ct)
    {
        var skill = _skills.FirstOrDefault(s =>
            string.Equals(s.Name, skillName, StringComparison.OrdinalIgnoreCase));

        if (skill is null)
            return new ToolResult(false, "", $"Skill '{skillName}' not found.");

        // Get decrypted credentials for this skill.
        var creds = await _skillService.GetDecryptedCredentialsAsync(skill.Name, ct);
        if (creds is null)
            creds = new Dictionary<string, string>();

        try
        {
            var result = await _skillRuntimeClient.ExecuteAsync(skill.Name, action, cliArgs, creds, ct: ct);

            if (result.Success)
            {
                var output = result.Result.HasValue
                    ? result.Result.Value.ToString()
                    : "Success (no output).";

                if (output.Length > 1_000_000)
                    output = output[..1_000_000] + "\n[truncated]";

                return new ToolResult(true, output);
            }

            return new ToolResult(false, "", result.Error ?? "Skill execution failed.");
        }
        catch (Exception ex)
        {
            return new ToolResult(false, "", $"Skill execution error: {ex.Message}");
        }
    }

    // ── Action schema parsing ────────────────────────────────────────────────

    private record ActionInfo(string Name, string Description, List<ParamInfo> Params);
    private record ParamInfo(string Name, string Type, bool Required, string Description);

    private static List<ActionInfo>? ParseActions(string? actionsJson)
    {
        if (string.IsNullOrEmpty(actionsJson)) return null;

        try
        {
            using var doc = JsonDocument.Parse(actionsJson);
            var actions = new List<ActionInfo>();

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var desc = prop.Value.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                var paramsList = new List<ParamInfo>();

                if (prop.Value.TryGetProperty("params", out var paramsObj) && paramsObj.ValueKind == JsonValueKind.Object)
                {
                    if (paramsObj.TryGetProperty("properties", out var props))
                    {
                        var required = paramsObj.TryGetProperty("required", out var req)
                            ? req.EnumerateArray().Select(r => r.GetString() ?? "").ToHashSet()
                            : new HashSet<string>();

                        foreach (var param in props.EnumerateObject())
                        {
                            var pType = param.Value.TryGetProperty("type", out var t) ? t.GetString() ?? "string" : "string";
                            var pDesc = param.Value.TryGetProperty("description", out var pd) ? pd.GetString() ?? "" : "";
                            paramsList.Add(new ParamInfo(param.Name, pType, required.Contains(param.Name), pDesc));
                        }
                    }
                }

                actions.Add(new ActionInfo(prop.Name, desc, paramsList));
            }

            return actions;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
