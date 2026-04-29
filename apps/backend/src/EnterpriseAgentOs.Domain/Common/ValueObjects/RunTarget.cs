namespace EnterpriseAgentOs.Domain.Common.ValueObjects;

public enum RunTarget
{
    Cloud,
    Runner,
}

public static class RunTargetExtensions
{
    public static string ToStorageString(this RunTarget target) => target switch
    {
        RunTarget.Cloud => "cloud",
        RunTarget.Runner => "runner",
        _ => throw new ArgumentOutOfRangeException(nameof(target)),
    };

    public static RunTarget? ToRunTarget(this string? value) => value switch
    {
        null => null,
        "cloud" => RunTarget.Cloud,
        "runner" => RunTarget.Runner,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown run target: {value}"),
    };
}
