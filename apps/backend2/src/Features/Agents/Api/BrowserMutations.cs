namespace OffceOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLMutations))]
public class BrowserMutations
{
    [GraphQLDescription("Starts or reuses the internal browser session for an agent.")]
    public async Task<BrowserSessionState> StartAgentBrowser(
        Guid agentId,
        [Service] IBrowserService browser,
        CancellationToken ct)
    {
        return await browser.GetOrCreateAsync(agentId, ct);
    }

    [GraphQLDescription("Restarts the internal browser session for an agent.")]
    public async Task<BrowserSessionState> RestartAgentBrowser(
        Guid agentId,
        [Service] IBrowserService browser,
        CancellationToken ct)
    {
        return await browser.RestartAsync(agentId, ct);
    }

    [GraphQLDescription("Stops the internal browser session for an agent.")]
    public async Task<bool> StopAgentBrowser(
        Guid agentId,
        [Service] IBrowserService browser,
        CancellationToken ct)
    {
        await browser.StopAsync(agentId, ct);
        return true;
    }
}
