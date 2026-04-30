namespace EnterpriseAgentOs.Domain.Features.Mcp;

public sealed class McpServerRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public McpTransportType TransportType { get; init; }
    public string? Command { get; init; }
    public string? Args { get; init; }
    public string? Url { get; init; }
    public string? Logo { get; init; }
    public string? Category { get; init; }
    public string? CredentialFieldsJson { get; init; }
    public bool IsBuiltin { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
