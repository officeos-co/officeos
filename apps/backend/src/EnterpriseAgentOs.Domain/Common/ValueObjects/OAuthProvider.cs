namespace EnterpriseAgentOs.Domain.Common.ValueObjects;

public enum OAuthProvider
{
    Google,
    Microsoft,
}

public static class OAuthProviderExtensions
{
    public static string ToStorageString(this OAuthProvider provider) => provider switch
    {
        OAuthProvider.Google => "google",
        OAuthProvider.Microsoft => "microsoft",
        _ => throw new ArgumentOutOfRangeException(nameof(provider)),
    };

    public static OAuthProvider ToOAuthProvider(this string value) => value switch
    {
        "google" => OAuthProvider.Google,
        "microsoft" => OAuthProvider.Microsoft,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown OAuth provider: {value}"),
    };
}
