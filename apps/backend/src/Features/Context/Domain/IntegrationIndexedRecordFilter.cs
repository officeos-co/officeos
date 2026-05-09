namespace OffceOs.Domain.Features.Context;

public sealed record IntegrationIndexedRecordFilter
{
    public Guid? Id { get; init; }
    public Guid? ConnectionId { get; init; }
    public string? Entity { get; init; }
    public string? Query { get; init; }
    public string? Cursor { get; init; }
    public int Limit { get; init; } = 20;
}
