namespace OffceOs.Domain.Common.ValueObjects;

public enum WorkspaceOwnerKind
{
    Personal,
}

public static class WorkspaceOwnerKindExtensions
{
    public static string ToStorageString(this WorkspaceOwnerKind kind) => kind switch
    {
        WorkspaceOwnerKind.Personal => "personal",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static WorkspaceOwnerKind ToWorkspaceOwnerKind(this string value) => value switch
    {
        "personal" => WorkspaceOwnerKind.Personal,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown workspace owner kind: {value}"),
    };
}
