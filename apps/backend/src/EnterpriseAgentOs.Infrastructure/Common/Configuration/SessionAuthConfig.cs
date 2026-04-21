namespace EnterpriseAgentOs.Infrastructure.Common.Configuration;

public sealed record SessionAuthConfig
{
    public string[] SkipPrefixes { get; init; } = [];
}
