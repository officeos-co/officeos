namespace EnterpriseAgentOs.Api.Properties;

public sealed class GoogleOAuthConfig
{
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;
}
