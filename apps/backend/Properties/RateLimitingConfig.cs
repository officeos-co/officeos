namespace EnterpriseAgentOs.Api.Properties;

public sealed class RateLimitingConfig
{
    public int SkillExecPerAgentPerHour { get; set; } = 60;
    public int EmailPerAgentPerHour { get; set; } = 10;
    public int WindowSeconds { get; set; } = 3600;
}
