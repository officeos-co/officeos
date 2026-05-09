namespace EnterpriseAgentOs.Domain.Common.ValueObjects;

public enum ChannelType
{
    Slack,
    Telegram,
    Discord,
    WhatsApp,
    Teams,
    GoogleChat,
}

public static class ChannelTypeExtensions
{
    public static string ToStorageString(this ChannelType type) => type switch
    {
        ChannelType.Slack => "slack",
        ChannelType.Telegram => "telegram",
        ChannelType.Discord => "discord",
        ChannelType.WhatsApp => "whatsapp",
        ChannelType.Teams => "teams",
        ChannelType.GoogleChat => "google-chat",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static ChannelType ToChannelType(this string value) => value.ToLowerInvariant() switch
    {
        "slack" => ChannelType.Slack,
        "telegram" => ChannelType.Telegram,
        "discord" => ChannelType.Discord,
        "whatsapp" => ChannelType.WhatsApp,
        "teams" => ChannelType.Teams,
        "google-chat" => ChannelType.GoogleChat,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown channel type: {value}"),
    };
}
