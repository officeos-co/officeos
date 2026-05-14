namespace OffceOs.Domain.Features.Browser;

public sealed record BrowserSessionFilter
{
    public Guid? Id { get; init; }
    public Guid? AgentId { get; init; }
    public string? RuntimeSessionId { get; init; }
}
