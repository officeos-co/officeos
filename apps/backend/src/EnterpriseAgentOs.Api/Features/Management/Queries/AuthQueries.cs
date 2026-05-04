
namespace EnterpriseAgentOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AuthQueries
{
    private static readonly TimeSpan MeCacheTtl = TimeSpan.FromMinutes(2);

    [GraphQLDescription("Returns the authenticated user's profile including email, name, avatar, display name, timezone, and notification preferences. Cached for 2 minutes.")]
    public async Task<UserPayload> Me(
        IResolverContext context,
        [Service] IUserRepository users,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        var ctxUser = DashboardAuthContextExtensions.GetUser(context);
        var cacheKey = $"auth:me:{ctxUser.Id}";

        var cached = await cache.GetJsonAsync<UserPayload>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var user = await users.GetByAsync(new UserFilter { Id = ctxUser.Id }, ct) ?? ctxUser;
        var result = new UserPayload(
            user.Id,
            user.Email,
            user.Name,
            user.AvatarUrl,
            user.DisplayName,
            user.Timezone,
            user.NotificationPrefsJson,
            user.Preferences);

        await cache.SetJsonAsync(cacheKey, result, MeCacheTtl, ct);
        return result;
    }

    [GraphQLDescription("GDPR data export. Returns all user data (profile, agents, conversations, audit entries, skill credentials) as a single payload.")]
    public async Task<GdprExportDto> ExportMyData(
        IResolverContext context,
        [Service] IGdprService gdpr,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        return await gdpr.ExportAsync(user.Id, ct);
    }
}
