namespace EnterpriseAgentOs.Configuration;

public sealed class PlatformKeysConfig
{
    public string AnthropicApiKey { get; init; } = string.Empty;
    public string GeminiApiKey { get; init; } = string.Empty;
    public string XaiApiKey { get; init; } = string.Empty;
    public string OpenAiApiKey { get; init; } = string.Empty;

    /// <summary>
    /// Resolves a platform key by its config property name (e.g. "AnthropicApiKey").
    /// Used by ProviderService to map ProviderDefinition.PlatformKeyConfigName → actual value.
    /// </summary>
    public string? GetKey(string? configName) => configName switch
    {
        nameof(AnthropicApiKey) => NullIfEmpty(AnthropicApiKey),
        nameof(GeminiApiKey) => NullIfEmpty(GeminiApiKey),
        nameof(XaiApiKey) => NullIfEmpty(XaiApiKey),
        nameof(OpenAiApiKey) => NullIfEmpty(OpenAiApiKey),
        _ => null,
    };

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
