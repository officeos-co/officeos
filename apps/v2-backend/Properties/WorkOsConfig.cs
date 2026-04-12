namespace EnterpriseAgentOs.Api.Properties;

public sealed class WorkOsConfig
{
    public string ApiKey { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;
    public bool Enabled { get; init; } = false;
}
