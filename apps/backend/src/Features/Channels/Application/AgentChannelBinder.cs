namespace EnterpriseAgentOs.Application.Features.Channels;

internal sealed class AgentChannelBinder
{
    private readonly IChannelRepository _channelRepository;

    public AgentChannelBinder(IChannelRepository channelRepository)
    {
        _channelRepository = channelRepository;
    }

    public async Task BindBySlugsAsync(Guid agentId, IReadOnlyList<string>? channelSlugs, CancellationToken ct = default)
    {
        if (channelSlugs is not { Count: > 0 }) return;

        var connections = await _channelRepository.ListConnectionsAsync(ct: ct);
        foreach (var slug in channelSlugs)
        {
            var match = connections.FirstOrDefault(c =>
                string.Equals(c.ChannelType.ToStorageString(), slug, StringComparison.OrdinalIgnoreCase));
            if (match is null) continue;

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
