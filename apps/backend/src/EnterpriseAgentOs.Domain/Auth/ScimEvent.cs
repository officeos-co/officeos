namespace EnterpriseAgentOs.Domain.Auth;

public sealed record ScimEvent(
    string EventType,
    string ExternalId,
    string? Email,
    string? DisplayName
);
