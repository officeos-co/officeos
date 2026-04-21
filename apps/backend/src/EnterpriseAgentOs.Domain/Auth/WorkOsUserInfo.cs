namespace EnterpriseAgentOs.Domain.Auth;

public sealed record WorkOsUserInfo(
    string Id,
    string Email,
    string? FirstName,
    string? LastName,
    string OrganizationId
);
