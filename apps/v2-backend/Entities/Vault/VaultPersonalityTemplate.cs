namespace EnterpriseAgentOs.Api.Entities.Vault;

public static class VaultPersonalityTemplate
{
    /// <summary>
    /// Files that must appear first, in this order. Any additional .md files
    /// discovered in <see cref="TemplateDirectory"/> are appended alphabetically.
    /// </summary>
    private static readonly string[] PreferredOrder = ["SOUL.md", "IDENTITY.md", "AGENTS.md"];

    private static readonly string TemplateDirectory = System.IO.Path.Combine(
        AppContext.BaseDirectory, "Entities", "Vault", "Templates");

    /// <summary>
    /// Returns the template file names in deterministic order:
    /// SOUL.md → IDENTITY.md → AGENTS.md → any other .md files (alphabetical).
    /// Adding a new .md file to the Templates directory is all that is required —
    /// no code change needed.
    /// </summary>
    private static IEnumerable<string> ResolveTemplateFiles()
    {
        if (!Directory.Exists(TemplateDirectory))
            return PreferredOrder; // fall back to the hard-coded list so the error path stays clear

        var all = Directory.GetFiles(TemplateDirectory, "*.md")
            .Select(System.IO.Path.GetFileName)
            .OfType<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Emit preferred files first (only if they actually exist on disk).
        var ordered = PreferredOrder.Where(all.Contains).ToList();

        // Append any remaining .md files in alphabetical order.
        var extras = all
            .Except(PreferredOrder, StringComparer.OrdinalIgnoreCase)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

        ordered.AddRange(extras);
        return ordered;
    }

    public static IReadOnlyDictionary<string, string> Render(
        Guid agentId,
        string agentName,
        string provider,
        string? model)
    {
        var tokens = new Dictionary<string, string>
        {
            ["{{agent_name}}"] = agentName,
            ["{{agent_id}}"] = agentId.ToString(),
            ["{{provider}}"] = provider,
            ["{{model}}"] = model ?? "default",
        };

        var templateFiles = ResolveTemplateFiles().ToList();
        var rendered = new Dictionary<string, string>(templateFiles.Count);
        foreach (var file in templateFiles)
        {
            var path = System.IO.Path.Combine(TemplateDirectory, file);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Vault personality template not found: {path}. " +
                    "Templates must be present in Entities/Vault/Templates and copied to output.",
                    path);
            }
            var raw = File.ReadAllText(path);
            foreach (var (token, value) in tokens)
            {
                raw = raw.Replace(token, value);
            }
            rendered[file] = raw;
        }
        return rendered;
    }
}
