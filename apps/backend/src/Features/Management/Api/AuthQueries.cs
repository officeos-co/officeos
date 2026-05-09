
namespace EnterpriseAgentOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AuthQueries
{
    private static readonly TimeSpan MeCacheTtl = TimeSpan.FromMinutes(2);

    [GraphQLDescription("Returns the authenticated user's profile including email, name, avatar, display name, timezone, and notification preferences. Cached for 2 minutes.")]
    public async Task<UserPayload> Me(
        [Service] UserContext user,
        [Service] IUserRepository users,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        var cacheKey = $"auth:me:{user.Id}";

        var cached = await cache.GetJsonAsync<UserPayload>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var record = await users.GetByAsync(new UserFilter { Id = user.Id }, ct) ?? user.Record;
        var result = new UserPayload(
            record.Id,
            record.Email,
            record.Name,
            record.AvatarUrl,
            record.DisplayName,
            record.Timezone,
            record.NotificationPrefsJson,
            record.Preferences);

        await cache.SetJsonAsync(cacheKey, result, MeCacheTtl, ct);
        return result;
    }

    [GraphQLDescription("GDPR data export. Returns all user data (profile, agents, conversations, audit entries, skill credentials) as a single payload.")]
    public async Task<GdprExportDto> ExportMyData(
        [Service] UserContext user,
        [Service] IGdprService gdpr,
        CancellationToken ct)
    {
        return await gdpr.ExportAsync(user.Id, ct);
    }
}
