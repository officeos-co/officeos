using OffceOs.Application.Features.Observability;
using OffceOs.Domain.Features.Observability;

namespace OffceOs.Tests.Shared;

public sealed class FakeAgentLogService : IAgentLogService
{
    public Task<AgentLogPage> ListAsync(AgentLogQueryRequest request, CancellationToken ct = default) =>
        Task.FromResult(new AgentLogPage([], 0));

    public Task<IReadOnlyDictionary<Guid, string?>> GetLastRelevantMessagesAsync(LastRelevantLogQueryRequest request, CancellationToken ct = default)
    {
        var ids = (request.AgentIds ?? []).Concat(request.ChannelConnectionIds ?? []).Distinct();
        return Task.FromResult<IReadOnlyDictionary<Guid, string?>>(
            ids.ToDictionary(id => id, _ => (string?)null));
    }

    public Task<AgentLogRecord> AppendAsync(AgentLogRecord record, CancellationToken ct = default) =>
        Task.FromResult(record);
}
