namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLMutations))]
public class BrowserMutations
{
    [GraphQLDescription("Starts or reuses the internal browser session for an agent.")]
    public async Task<BrowserSessionState> StartAgentBrowser(
        Guid agentId,
        IResolverContext context,
        [Service] IBrowserService browser,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await browser.GetOrCreateAsync(agentId, ct);
    }

    [GraphQLDescription("Restarts the internal browser session for an agent.")]
    public async Task<BrowserSessionState> RestartAgentBrowser(
        Guid agentId,
        IResolverContext context,
        [Service] IBrowserService browser,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await browser.RestartAsync(agentId, ct);
    }

    [GraphQLDescription("Stops the internal browser session for an agent.")]
    public async Task<bool> StopAgentBrowser(
        Guid agentId,
        IResolverContext context,
        [Service] IBrowserService browser,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        await browser.StopAsync(agentId, ct);
        return true;
    }
}
