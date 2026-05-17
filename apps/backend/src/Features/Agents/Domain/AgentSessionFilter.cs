
namespace OffceOs.Features.Agents.Domain;

public sealed record AgentSessionFilter
{
    public Guid? Id { get; init; }
    public Guid? AgentId { get; init; }
    public Guid? OwnerId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public string? CorrelationId { get; init; }
    public Guid? RoutineId { get; init; }
    public Guid? TriggerId { get; init; }
    public SessionStatus? Status { get; init; }
}
