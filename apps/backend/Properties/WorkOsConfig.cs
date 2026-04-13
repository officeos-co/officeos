namespace EnterpriseAgentOs.Api.Properties;

public sealed class WorkOsConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public bool Enabled { get; set; } = false;
}
