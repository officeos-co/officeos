namespace OffceOs.Features.AgentHarness.Application;

internal interface IAgentHarnessService
{
    Task RunWorkAsync(Guid workLogId, CancellationToken ct = default);
}
