namespace OffceOs.Features.Channels.Domain;

public enum ChannelType
{
    Internal,
    Slack,
    Telegram,
}

public static class ChannelTypeExtensions
{
    public static string ToStorageString(this ChannelType type) => type switch
    {
        ChannelType.Internal => "internal",
        ChannelType.Slack => "slack",
        ChannelType.Telegram => "telegram",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static ChannelType ToChannelType(this string value) => value.ToLowerInvariant() switch
    {
        "internal" => ChannelType.Internal,
        "slack" => ChannelType.Slack,
        "telegram" => ChannelType.Telegram,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown channel type: {value}"),
    };
}
