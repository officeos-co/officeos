using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Agents;

namespace OffceOs.Tests.Shared;

public sealed class FakeAgentRepository : IAgentRepository
{
    private readonly AgentRecord? _agent;
    private readonly bool _returnDefaultAgent;

    public FakeAgentRepository(AgentRecord? agent = null, bool returnDefaultAgent = true)
    {
        _agent = agent;
        _returnDefaultAgent = returnDefaultAgent;
    }

    public Task<IReadOnlyList<AgentRecord>> ListAsync(AgentFilter filter, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AgentRecord>>(_agent is null ? [] : [_agent]);

    public Task<AgentRecord?> GetByAsync(AgentFilter filter, CancellationToken ct = default)
    {
        if (_agent is not null)
        {
            return Task.FromResult(
                (!filter.Id.HasValue || _agent.Id == filter.Id.Value)
                && (!filter.OwnerId.HasValue || _agent.OwnerId == filter.OwnerId.Value)
                    ? _agent
                    : null);
        }

        if (!_returnDefaultAgent)
            return Task.FromResult<AgentRecord?>(null);

        return Task.FromResult<AgentRecord?>(new AgentRecord
        {
            Id = filter.Id ?? Guid.NewGuid(),
            OwnerId = filter.OwnerId ?? TestIds.OwnerId,
            WorkspaceId = filter.WorkspaceId ?? TestIds.WorkspaceId,
            Name = "Test Agent",
            Provider = "openai",
            Status = AgentStatus.Idle,
        });
    }

    public Task AddAsync(AgentRecord record, CancellationToken ct = default) => Task.CompletedTask;
    public Task UpdateAsync(AgentRecord record, CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> SoftDeleteAsync(AgentFilter filter, CancellationToken ct = default) => Task.FromResult(false);
    public Task UpdateStatusAsync(AgentFilter filter, AgentStatus status, CancellationToken ct = default) => Task.CompletedTask;
    public Task HardDeleteAsync(AgentFilter filter, CancellationToken ct = default) => Task.CompletedTask;
}
