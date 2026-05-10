namespace OffceOs.Domain.Common.ValueObjects;

public enum OAuthProvider
{
    Google,
    Microsoft,
    GitHub,
    OpenAiCodex,
}

public static class OAuthProviderExtensions
{
    public static string ToStorageString(this OAuthProvider provider) => provider switch
    {
        OAuthProvider.Google => "google",
        OAuthProvider.Microsoft => "microsoft",
        OAuthProvider.GitHub => "github",
        OAuthProvider.OpenAiCodex => "openai-codex",
        _ => throw new ArgumentOutOfRangeException(nameof(provider)),
    };

    public static OAuthProvider ToOAuthProvider(this string value) => value switch
    {
        "google" => OAuthProvider.Google,
        "microsoft" => OAuthProvider.Microsoft,
        "github" => OAuthProvider.GitHub,
        "openai-codex" => OAuthProvider.OpenAiCodex,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown OAuth provider: {value}"),
    };
}
