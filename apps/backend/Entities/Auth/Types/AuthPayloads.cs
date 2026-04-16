namespace EnterpriseAgentOs.Api.Entities.Auth.Types;

public sealed record UserPayload(
    Guid Id,
    string Email,
    string? Name,
    string? AvatarUrl);
