namespace OffceOs.Features.Agents.Domain;

public sealed record AgentFilter
{
    public Guid? Id { get; init; }
    public Guid? OwnerId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public bool IncludeDeleted { get; init; }
}
