namespace OffceOs.Features.Management.Domain;

public sealed record UserFilter
{
    public Guid? Id { get; init; }
    public string? Email { get; init; }
    public string? GoogleSubjectId { get; init; }
    public string? GitHubSubjectId { get; init; }
}
