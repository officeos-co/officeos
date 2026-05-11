namespace OffceOs.Domain.Features.AgentRoutines;

public sealed record AgentRoutineFilter
{
    public Guid? Id { get; init; }
    public Guid? AgentId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public bool? Enabled { get; init; }
}
