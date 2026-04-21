namespace EnterpriseAgentOs.Domain.Auth;

public sealed class SsoConnection
{
    public Guid Id { get; init; }
    public string OrganizationId { get; init; } = string.Empty;
    public string WorkOsConnectionId { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
