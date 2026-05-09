namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class AgentResourceService : IAgentResourceService
{
    private readonly IAgentResourceRepository _resources;

    public AgentResourceService(IAgentResourceRepository resources)
    {
        _resources = resources;
    }

    public Task<BrowserResourceRecord> CreateBrowserResourceAsync(
        Guid ownerId,
        string? displayName,
        CancellationToken ct = default) =>
        _resources.CreateBrowserResourceAsync(BrowserResourceRecord.Create(ownerId, displayName ?? "Browser"), ct);

    public Task<bool> DeleteBrowserResourceAsync(Guid id, Guid ownerId, CancellationToken ct = default) =>
        _resources.DeleteBrowserResourceAsync(id, ownerId, ct);
}
