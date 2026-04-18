namespace EnterpriseAgentOs.Domain.DTOs.Sso;

public sealed record ScimUserPayload(
    string ExternalId,
    string Email,
    string? DisplayName
);
