namespace EnterpriseAgentOs.Api.Properties;

public sealed class SkillGatewayConfig
{
    /// <summary>
    /// Base URL the agent pods use to reach this backend's skill gateway,
    /// e.g. <c>http://eaos-backend-prod:8000</c> in-cluster. Must be
    /// cluster-local so the bearer token never leaves the network.
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// How often zeroclaw polls <c>/api/agents/me/capabilities</c>.
    /// </summary>
    public int RefreshSeconds { get; init; } = 30;
}
