namespace OffceOs.Domain.Features.Agents;

public sealed record AgentRunFilter
{
    public Guid? Id { get; init; }
    public Guid? AgentId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public Guid? ParentRunId { get; init; }
    public Guid? DefinitionId { get; init; }
    public string? Kind { get; init; }
    public string? Purpose { get; init; }
    public string? Status { get; init; }
}
