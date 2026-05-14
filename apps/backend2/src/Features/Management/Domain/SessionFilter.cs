namespace OffceOs.Domain.Features.Management;

public sealed record SessionFilter
{
    public Guid? Id { get; init; }
    public Guid? UserId { get; init; }
    public string? TokenHash { get; init; }
}
