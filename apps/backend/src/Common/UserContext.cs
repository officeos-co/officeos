namespace OffceOs.Api.Common;

public sealed class UserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private UserRecord? _user;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public UserRecord Record => _user ??= GetRequiredUser();

    public Guid Id => Record.Id;
    public string Email => Record.Email;
    public string? Name => Record.Name;
    public string? AvatarUrl => Record.AvatarUrl;
    public string? DisplayName => Record.DisplayName;
    public string? Timezone => Record.Timezone;
    public string? NotificationPrefsJson => Record.NotificationPrefsJson;
    public string? Preferences => Record.Preferences;

    private UserRecord GetRequiredUser()
    {
        var http = _httpContextAccessor.HttpContext;
        return http?.Items["User"] as UserRecord
            ?? throw new InvalidOperationException("Unauthenticated.");
    }
}
