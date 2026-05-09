namespace OffceOs.Domain.Common.ValueObjects;

public enum WorkspaceRole
{
    Owner,
    Admin,
    Editor,
    Viewer,
}

public static class WorkspaceRoleExtensions
{
    public static string ToStorageString(this WorkspaceRole role) => role switch
    {
        WorkspaceRole.Owner => "Owner",
        WorkspaceRole.Admin => "Admin",
        WorkspaceRole.Editor => "Editor",
        WorkspaceRole.Viewer => "Viewer",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    public static WorkspaceRole ToWorkspaceRole(this string value) => value switch
    {
        "Owner" => WorkspaceRole.Owner,
        "Admin" => WorkspaceRole.Admin,
        "Editor" => WorkspaceRole.Editor,
        "Viewer" => WorkspaceRole.Viewer,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown workspace role: {value}"),
    };

    public static bool CanEdit(this WorkspaceRole role) => role is WorkspaceRole.Owner or WorkspaceRole.Admin or WorkspaceRole.Editor;

    public static bool CanAdminister(this WorkspaceRole role) => role is WorkspaceRole.Owner or WorkspaceRole.Admin;
}
