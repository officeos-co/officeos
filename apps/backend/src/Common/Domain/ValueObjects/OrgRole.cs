namespace OffceOs.Domain.Common.ValueObjects;

public enum OrgRole
{
    Owner,
    Admin,
    Member,
}

public static class OrgRoleExtensions
{
    public static string ToStorageString(this OrgRole role) => role switch
    {
        OrgRole.Owner => "Owner",
        OrgRole.Admin => "Admin",
        OrgRole.Member => "Member",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    public static OrgRole ToOrgRole(this string value) => value switch
    {
        "Owner" => OrgRole.Owner,
        "Admin" => OrgRole.Admin,
        "Member" => OrgRole.Member,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown org role: {value}"),
    };
}
