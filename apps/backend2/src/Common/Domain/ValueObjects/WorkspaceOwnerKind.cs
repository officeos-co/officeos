namespace OffceOs.Domain.Common.ValueObjects;

public enum WorkspaceOwnerKind
{
    Personal,
    Organization,
}

public static class WorkspaceOwnerKindExtensions
{
    public static string ToStorageString(this WorkspaceOwnerKind kind) => kind switch
    {
        WorkspaceOwnerKind.Personal => "personal",
        WorkspaceOwnerKind.Organization => "organization",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static WorkspaceOwnerKind ToWorkspaceOwnerKind(this string value) => value switch
    {
        "personal" => WorkspaceOwnerKind.Personal,
        "organization" => WorkspaceOwnerKind.Organization,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown workspace owner kind: {value}"),
    };
}
