namespace EnterpriseAgentOs.Api.Entities.Sso;

public sealed record ScimUserPayload(
    string ExternalId,
    string Email,
    string? DisplayName
);
