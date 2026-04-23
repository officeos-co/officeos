
namespace EnterpriseAgentOs.Api.Features.Auth;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AuthQueries
{
    private static readonly TimeSpan MeCacheTtl = TimeSpan.FromMinutes(2);

    public async Task<UserPayload> Me(
        IResolverContext context,
        [Service] IUserRepository users,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var ctxUser = DashboardAuthContextExtensions.GetUser(context);
        var cacheKey = $"auth:me:{ctxUser.Id}";

        if (cache.TryGetValue(cacheKey, out UserPayload? cached) && cached is not null)
            return cached;

        var user = await users.GetByIdAsync(ctxUser.Id, ct) ?? ctxUser;
        var result = new UserPayload(
            user.Id,
            user.Email,
            user.Name,
            user.AvatarUrl,
            user.DisplayName,
            user.Timezone,
            user.NotificationPrefsJson,
            user.Preferences);

        cache.Set(cacheKey, result,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = MeCacheTtl });
        return result;
    }

    public async Task<GdprExportDto> ExportMyData(
        IResolverContext context,
        [Service] IGdprService gdpr,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        return await gdpr.ExportAsync(user.Id, ct);
    }
}
