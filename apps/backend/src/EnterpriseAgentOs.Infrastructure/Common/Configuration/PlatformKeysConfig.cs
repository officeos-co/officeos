namespace EnterpriseAgentOs.Infrastructure.Common.Configuration;

public sealed class PlatformKeysConfig
{
    public string AnthropicApiKey { get; init; } = string.Empty;
    public string GeminiApiKey { get; init; } = string.Empty;
    public string XaiApiKey { get; init; } = string.Empty;
    public string OpenAiApiKey { get; init; } = string.Empty; // platform OpenAI key (fallback when no BYOK)
}
