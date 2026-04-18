using EnterpriseAgentOs.Domain.DTOs.Gdpr;

namespace EnterpriseAgentOs.Api.GraphQL.Queries;

[ExtendObjectType(typeof(GraphQLQueries))]
public class GdprQueries
{
    /// <summary>
    /// Returns a full JSON export of all data for the authenticated user.
    /// </summary>
    public async Task<GdprExportDto> ExportMyData(
        IResolverContext context,
        [Service] IGdprService gdpr,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        return await gdpr.ExportAsync(user.Id, ct);
    }
}
