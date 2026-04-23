namespace EnterpriseAgentOs.Api.Features.Auth;

public sealed record UserPayload(
    Guid Id,
    string Email,
    string? Name,
    string? AvatarUrl,
    string? DisplayName,
    string? Timezone,
    string? NotificationPrefsJson,
    string? Preferences);

public sealed record UpdateProfileInput(
    string? Name,
    string? DisplayName,
    string? Timezone,
    string? NotificationPrefsJson,
    string? Preferences);
