namespace EnterpriseAgentOs.Api.Properties;

public sealed class PostHogConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string Host { get; set; } = "https://eu.i.posthog.com";
    public bool Enabled { get; set; } = true;
}
