namespace OffceOs.Features.Management.Domain;

public sealed record SessionFilter
{
    public Guid? Id { get; init; }
    public Guid? UserId { get; init; }
    public string? TokenHash { get; init; }
}
