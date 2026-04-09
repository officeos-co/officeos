namespace EnterpriseAgentOs.Api.Entities.Vault;

public static class VaultPersonalityTemplate
{
    public static IReadOnlyDictionary<string, string> Render(string agentName, string provider, string? model) =>
        new Dictionary<string, string>
        {
            ["SOUL.md"] = $"# Soul\n\nI am {agentName}, a helpful autonomous agent running in EnterpriseAgentOS.\n",
            ["IDENTITY.md"] = $"# Identity\n\nName: {agentName}\nProvider: {provider}\nModel: {model ?? "default"}\n",
            ["AGENTS.md"] = "# Agents\n\nI collaborate with other agents in this workspace over the EnterpriseAgentOS skill gateway.\n",
        };
}
