namespace OffceOs.Infrastructure.Features.AgentHarness;

internal interface IAgentWorkspaceStore
{
    Task RestoreAsync(string sandboxId, string serviceUrl, CancellationToken ct = default);

    Task CheckpointAsync(string sandboxId, string serviceUrl, CancellationToken ct = default);
}
