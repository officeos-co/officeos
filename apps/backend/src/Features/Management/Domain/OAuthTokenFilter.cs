namespace OffceOs.Domain.Features.Management;

public sealed record OAuthTokenFilter
{
    public Guid? Id { get; init; }
    public Guid? UserId { get; init; }
    public string? Provider { get; init; }
    public string? Email { get; init; }
}
