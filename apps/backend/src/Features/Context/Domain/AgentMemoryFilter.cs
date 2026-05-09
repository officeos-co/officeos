namespace OffceOs.Domain.Features.Context;

public sealed record AgentMemoryFilter
{
    public Guid? AgentId { get; init; }
    public string? Key { get; init; }
}
