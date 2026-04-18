namespace EnterpriseAgentOs.Domain.Interfaces.Sso;

public sealed record WorkOsUserInfo(
    string Id,
    string Email,
    string? FirstName,
    string? LastName,
    string OrganizationId
);
