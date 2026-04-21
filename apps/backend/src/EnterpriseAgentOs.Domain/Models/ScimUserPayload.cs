namespace EnterpriseAgentOs.Domain.Models;

public sealed record ScimUserPayload(
    string ExternalId,
    string Email,
    string? DisplayName
);
