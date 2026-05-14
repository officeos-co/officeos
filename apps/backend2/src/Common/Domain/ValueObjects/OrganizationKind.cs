namespace OffceOs.Domain.Common.ValueObjects;

public enum OrganizationKind
{
    Individual,
    Shared,
}

public static class OrganizationKindExtensions
{
    public static string ToStorageString(this OrganizationKind kind) => kind switch
    {
        OrganizationKind.Individual => "individual",
        OrganizationKind.Shared => "shared",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static OrganizationKind ToOrganizationKind(this string? value) => value switch
    {
        null or "" => OrganizationKind.Individual,
        "individual" => OrganizationKind.Individual,
        "shared" => OrganizationKind.Shared,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown organization kind: {value}"),
    };
}
