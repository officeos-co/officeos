namespace EnterpriseAgentOs.Domain.Interfaces;

public sealed record ScimEvent(
    string EventType,
    string ExternalId,
    string? Email,
    string? DisplayName
);
