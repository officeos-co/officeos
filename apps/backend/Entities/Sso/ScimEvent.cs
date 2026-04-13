namespace EnterpriseAgentOs.Api.Entities.Sso;

public sealed record ScimEvent(
    string EventType,
    string ExternalId,
    string? Email,
    string? DisplayName
);
