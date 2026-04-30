namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class McpServerEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TransportType { get; set; } = "Stdio";
    public string? Command { get; set; }
    public string? Args { get; set; }
    public string? Url { get; set; }
    public string? Logo { get; set; }
    public string? Category { get; set; }
    public string? CredentialFieldsJson { get; set; }
    public bool IsBuiltin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
