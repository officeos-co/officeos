namespace EnterpriseAgentOs.Domain.Auth;

public sealed record ScimUserPayload(
    string ExternalId,
    string Email,
    string? DisplayName
);
