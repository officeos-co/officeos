namespace EnterpriseAgentOs.Configuration;

public sealed record SessionAuthConfig
{
    public string[] SkipPrefixes { get; init; } = [];
}
