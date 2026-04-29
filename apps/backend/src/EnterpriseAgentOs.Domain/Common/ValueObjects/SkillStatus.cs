namespace EnterpriseAgentOs.Domain.Common.ValueObjects;

public enum SkillStatus
{
    Active,
    Disabled,
    Building,
    Failed,
}

public static class SkillStatusExtensions
{
    public static string ToStorageString(this SkillStatus status) => status switch
    {
        SkillStatus.Active => "active",
        SkillStatus.Disabled => "disabled",
        SkillStatus.Building => "building",
        SkillStatus.Failed => "failed",
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static SkillStatus ToSkillStatus(this string value) => value switch
    {
        "active" => SkillStatus.Active,
        "disabled" => SkillStatus.Disabled,
        "building" => SkillStatus.Building,
        "failed" => SkillStatus.Failed,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown skill status: {value}"),
    };
}
