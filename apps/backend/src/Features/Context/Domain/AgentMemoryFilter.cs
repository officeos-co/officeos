namespace OffceOs.Features.Context.Domain;

public sealed record AgentMemoryFilter
{
    public Guid? AgentId { get; init; }
    public string? Key { get; init; }
}
