namespace EnterpriseAgentOs.Application.Services.Providers;

public static class KnownModels
{
    /// <summary>
    /// All models exposed to agent creation / selection UI.
    /// "auto" is the smart-routing sentinel value.
    /// </summary>
    public static readonly IReadOnlyList<string> SupportedModels =
    [
        "auto",
        "claude-haiku-4-5", "claude-sonnet-4-6", "claude-opus-4-6",
        "gpt-4o", "gpt-4o-mini",
        "gemini-2.5-pro", "gemini-2.5-flash",
        "grok-4",
    ];

    /// <summary>
    /// Per-provider model list — only OpenAI has BYOK so only openai is meaningful here.
    /// Kept for the <c>GET /api/providers/{name}/models</c> endpoint.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ByProvider { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["openai"] = new[] { "gpt-4o", "gpt-4o-mini" },
        };

    public static IReadOnlyList<string> For(string provider) =>
        ByProvider.TryGetValue(provider, out var models) ? models : Array.Empty<string>();

    public static bool IsValid(string model) =>
        SupportedModels.Contains(model, StringComparer.OrdinalIgnoreCase);

    public const string DefaultModel = "auto";

    private static readonly Dictionary<string, string> DisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["auto"] = "Auto (smart routing)",
        ["claude-haiku-4-5"] = "Claude Haiku 4.5",
        ["claude-sonnet-4-6"] = "Claude Sonnet 4.6",
        ["claude-opus-4-6"] = "Claude Opus 4.6",
        ["gpt-4o"] = "GPT-4o",
        ["gpt-4o-mini"] = "GPT-4o Mini",
        ["gemini-2.5-pro"] = "Gemini 2.5 Pro",
        ["gemini-2.5-flash"] = "Gemini 2.5 Flash",
        ["grok-4"] = "Grok 4",
    };

    public static string GetDisplayName(string model) =>
        DisplayNames.TryGetValue(model, out var name) ? name : model;
}
