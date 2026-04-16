namespace EnterpriseAgentOs.Api.Entities.Auth.Types;

public sealed record UserPayload(
    Guid Id,
    string Email,
    string? Name,
    string? AvatarUrl,
    string? DisplayName,
    string? Timezone,
    string? NotificationPrefsJson);

public sealed record UpdateProfileInput(
    string? Name,
    string? DisplayName,
    string? Timezone,
    string? NotificationPrefsJson);
