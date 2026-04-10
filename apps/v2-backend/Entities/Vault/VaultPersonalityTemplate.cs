namespace EnterpriseAgentOs.Api.Entities.Vault;

public static class VaultPersonalityTemplate
{
    private static readonly string[] TemplateFiles = ["SOUL.md", "IDENTITY.md", "AGENTS.md"];

    private static readonly string TemplateDirectory = System.IO.Path.Combine(
        AppContext.BaseDirectory, "Entities", "Vault", "Templates");

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

        var rendered = new Dictionary<string, string>(TemplateFiles.Length);
        foreach (var file in TemplateFiles)
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
