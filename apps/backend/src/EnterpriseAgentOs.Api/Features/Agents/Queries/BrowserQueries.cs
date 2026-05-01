namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLQueries))]
public class BrowserQueries
{
    [GraphQLDescription("Returns the internal browser session state for an agent. Does not start a browser.")]
    public async Task<BrowserSessionState> GetAgentBrowser(
        Guid agentId,
        IResolverContext context,
        [Service] IBrowserService browser,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await browser.GetStateAsync(agentId, ct)
            ?? new BrowserSessionState(agentId, null, "not_started", null, null, null, null, null, null);
    }

    [GraphQLDescription("Returns a view URL for the agent browser, lazily starting it when needed.")]
    public async Task<string?> GetAgentBrowserViewUrl(
        Guid agentId,
        IResolverContext context,
        [Service] IBrowserService browser,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await browser.GetViewUrlAsync(agentId, ct);
    }
}
