namespace EnterpriseAgentOs.Api.Entities.Providers;

public static class KnownModels
{
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ByProvider { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["openai"] = new[] { "gpt-4o", "gpt-4o-mini", "gpt-4.1", "o3", "o4-mini" },
            ["anthropic"] = new[] { "claude-opus-4-6", "claude-sonnet-4-6", "claude-haiku-4-5" },
            ["google"] = new[] { "gemini-2.5-pro", "gemini-2.5-flash" },
            ["xai"] = new[] { "grok-4" },
            ["groq"] = new[] { "llama-3.3-70b-versatile", "mixtral-8x7b-32768" },
            ["deepseek"] = new[] { "deepseek-chat", "deepseek-reasoner" },
            ["openrouter"] = new[]
            {
                "anthropic/claude-sonnet-4.6",
                "openai/gpt-4o",
                "google/gemini-2.5-pro",
            },
            ["ollama"] = new[] { "llama3.2", "qwen2.5", "mistral" },
        };

    public static IReadOnlyList<string> For(string provider) =>
        ByProvider.TryGetValue(provider, out var models) ? models : Array.Empty<string>();

    public static bool IsValid(string provider, string model) =>
        For(provider).Contains(model, StringComparer.OrdinalIgnoreCase);
}
