namespace OffceOs.Domain.Features.Providers;

public sealed record OrganizationProviderProfileFilter
{
    public Guid? Id { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public string? Provider { get; init; }
    public bool? Enabled { get; init; }
}
