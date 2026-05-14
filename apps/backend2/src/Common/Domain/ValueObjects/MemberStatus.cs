namespace OffceOs.Domain.Common.ValueObjects;

public enum MemberStatus
{
    Active,
    Invited,
}

public static class MemberStatusExtensions
{
    public static string ToStorageString(this MemberStatus status) => status switch
    {
        MemberStatus.Active => "active",
        MemberStatus.Invited => "invited",
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static MemberStatus ToMemberStatus(this string value) => value switch
    {
        "active" => MemberStatus.Active,
        "invited" => MemberStatus.Invited,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown member status: {value}"),
    };
}
