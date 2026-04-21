namespace EnterpriseAgentOs.Infrastructure.Common.Configuration;

public sealed class SkillRuntimeConfig
{
    /// <summary>
    /// Base URL of the skill-runtime service,
    /// e.g. <c>http://eaos-skill-runtime:3001</c> in-cluster.
    /// </summary>
    public string Url { get; set; } = "http://localhost:3001";
}
