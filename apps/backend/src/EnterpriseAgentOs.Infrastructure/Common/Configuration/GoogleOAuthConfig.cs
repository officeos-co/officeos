namespace EnterpriseAgentOs.Infrastructure.Common.Configuration;

public sealed class GoogleOAuthConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}
