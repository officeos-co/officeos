namespace OffceOs.Domain.Common.ValueObjects;

public enum WorkspaceRole
{
    Admin,
    Editor,
    Viewer,
}

public static class WorkspaceRoleExtensions
{
    public static string ToStorageString(this WorkspaceRole role) => role switch
    {
        WorkspaceRole.Admin => "Admin",
        WorkspaceRole.Editor => "Editor",
        WorkspaceRole.Viewer => "Viewer",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    public static WorkspaceRole ToWorkspaceRole(this string value) => value switch
    {
        "Owner" => WorkspaceRole.Admin,
        "Admin" => WorkspaceRole.Admin,
        "Editor" => WorkspaceRole.Editor,
        "Viewer" => WorkspaceRole.Viewer,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown workspace role: {value}"),
    };

    public static bool CanEdit(this WorkspaceRole role) => role is WorkspaceRole.Admin or WorkspaceRole.Editor;

    public static bool CanAdminister(this WorkspaceRole role) => role is WorkspaceRole.Admin;
}
