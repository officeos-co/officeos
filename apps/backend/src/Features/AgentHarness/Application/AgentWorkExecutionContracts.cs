namespace OffceOs.Application.Features.AgentHarness;

internal interface IAgentHarnessService
{
    Task RunWorkAsync(Guid workLogId, CancellationToken ct = default);
}
