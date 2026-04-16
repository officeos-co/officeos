using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Entities.Analytics.GraphQL;

[ExtendObjectType(typeof(GraphQLMutations))]
public class AnalyticsMutations
{
    public async Task<bool> CaptureEvent(
        CaptureEventInput input,
        IResolverContext context,
        [Service] IAnalyticsService analytics,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            return true; // silently drop — analytics never surfaces errors
        }
        await analytics.CaptureAsync(
            user.Id.ToString(),
            input.Name,
            input.Properties,
            ct);
        return true;
    }

    public async Task<bool> IdentifyUser(
        IResolverContext context,
        [Service] IAnalyticsService analytics,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var traits = new Dictionary<string, object?>
        {
            ["email"] = user.Email,
            ["name"] = user.Name,
        };
        await analytics.IdentifyAsync(user.Id.ToString(), traits, ct);
        return true;
    }
}
