namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLMutations))]
public class AgentResourceMutations
{
    public async Task<BrowserResourcePayload> CreateBrowserResource(
        CreateBrowserResourceInput input,
        IResolverContext context,
        [Service] IAgentResourceRepository resources,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var row = await resources.CreateBrowserResourceAsync(
            BrowserResourceRecord.Create(user.Id, input.DisplayName ?? "Browser"),
            ct);
        return new BrowserResourcePayload(row.Id, row.OwnerId, row.DisplayName, row.CurrentAgentId, row.CreatedAt, row.UpdatedAt);
    }

    public async Task<bool> DeleteBrowserResource(
        Guid id,
        IResolverContext context,
        [Service] IAgentResourceRepository resources,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        return await resources.DeleteBrowserResourceAsync(id, user.Id, ct);
    }

}
