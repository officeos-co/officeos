namespace EnterpriseAgentOs.Api.Properties;

public sealed class LiteLlmConfig
{
    public string BaseUrl { get; init; } = "http://litellm-service:4000";
    public bool Enabled { get; init; } = true;
}
