namespace OffceOs.Application.Features.Channels;

internal sealed class AgentChannelBinder
{
    private readonly IChannelRepository _channelRepository;

    public AgentChannelBinder(IChannelRepository channelRepository)
    {
        _channelRepository = channelRepository;
    }

    public async Task BindByConnectionIdsAsync(Guid agentId, IReadOnlyList<Guid>? channelConnectionIds, CancellationToken ct = default)
    {
        if (channelConnectionIds is not { Count: > 0 }) return;

        foreach (var channelConnectionId in channelConnectionIds.Distinct())
        {
            var match = await _channelRepository.GetConnectionByAsync(
                new ChannelConnectionFilter { Id = channelConnectionId },
                ct);
            if (match is null)
                throw new InvalidOperationException("Channel connection not found.");

            try
            {
                await _channelRepository.CreateBindingAsync(new AgentChannelBindingRecord
                {
                    AgentId = agentId,
                    ChannelConnectionId = match.Id,
                }, ct);
            }
            catch (DbUpdateException)
            {
                // Already bound; treat binding-by-slug as idempotent.
            }
        }
    }
}
