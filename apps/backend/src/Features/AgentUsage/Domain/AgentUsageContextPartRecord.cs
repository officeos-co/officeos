namespace OffceOs.Domain.Features.AgentUsage;

public sealed record AgentUsageContextPartRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CallId { get; init; }
    public string Kind { get; init; } = AgentUsageContextPartKinds.Other;
    public string Label { get; init; } = string.Empty;
    public string? Role { get; init; }
    public string? Tool { get; init; }
    public string? Integration { get; init; }
    public long Tokens { get; init; }
    public bool EstimatedTokens { get; init; } = true;
    public int CharacterCount { get; init; }
}
