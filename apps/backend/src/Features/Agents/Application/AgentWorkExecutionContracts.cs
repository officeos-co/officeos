namespace OffceOs.Application.Features.Agents;

internal interface IAgentHarnessService
{
    Task RunWorkAsync(Guid workLogId, CancellationToken ct = default);
}
