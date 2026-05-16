namespace OffceOs.Domain.Common.ValueObjects;

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

}
