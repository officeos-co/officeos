namespace EnterpriseAgentOs.Domain.Common.ValueObjects;

public enum SkillSource
{
    Builtin,
    Upload,
    GitHub,
}

public static class SkillSourceExtensions
{
    public static string ToStorageString(this SkillSource source) => source switch
    {
        SkillSource.Builtin => "builtin",
        SkillSource.Upload => "upload",
        SkillSource.GitHub => "github",
        _ => throw new ArgumentOutOfRangeException(nameof(source)),
    };

    public static SkillSource ToSkillSource(this string value) => value switch
    {
        "builtin" => SkillSource.Builtin,
        "upload" => SkillSource.Upload,
        "github" => SkillSource.GitHub,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown skill source: {value}"),
    };
}
