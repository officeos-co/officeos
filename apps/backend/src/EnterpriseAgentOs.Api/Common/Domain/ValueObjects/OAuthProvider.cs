namespace EnterpriseAgentOs.Domain.Common.ValueObjects;

public enum OAuthProvider
{
    Google,
    Microsoft,
    GitHub,
}

public static class OAuthProviderExtensions
{
    public static string ToStorageString(this OAuthProvider provider) => provider switch
    {
        OAuthProvider.Google => "google",
        OAuthProvider.Microsoft => "microsoft",
        OAuthProvider.GitHub => "github",
        _ => throw new ArgumentOutOfRangeException(nameof(provider)),
    };

    public static OAuthProvider ToOAuthProvider(this string value) => value switch
    {
        "google" => OAuthProvider.Google,
        "microsoft" => OAuthProvider.Microsoft,
        "github" => OAuthProvider.GitHub,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown OAuth provider: {value}"),
    };
}
