namespace EnterpriseAgentOs.Domain.Services;

/// <summary>
/// Domain knowledge about LLM providers — which require BYOK keys vs platform-managed.
/// </summary>
public static class KnownProviders
{
    private static readonly HashSet<string> KeylessSet =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "ollama", "anthropic", "google", "xai", "openai",
        };

    /// <summary>
    /// Returns true if the provider does NOT require a user-supplied API key.
    /// These providers use platform-managed keys.
    /// </summary>
    public static bool IsKeyless(string name) => KeylessSet.Contains(name);
}
