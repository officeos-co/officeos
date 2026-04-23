namespace EnterpriseAgentOs.Infrastructure.Features.Channels;

/// <summary>
/// Resolves channel adapters by platform type string.
/// </summary>
public sealed class ChannelAdapterRegistry
{
    private readonly Dictionary<string, IChannelAdapter> _adapters;

    public ChannelAdapterRegistry(IEnumerable<IChannelAdapter> adapters)
    {
        _adapters = adapters.ToDictionary(
            a => a.ChannelType,
            a => a,
            StringComparer.OrdinalIgnoreCase);
    }

    public IChannelAdapter? GetAdapter(string channelType)
        => _adapters.GetValueOrDefault(channelType);
}
