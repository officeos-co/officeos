namespace EnterpriseAgentOs.Domain.Features.Channels;

/// <summary>
/// Value object for outbound channel messages. Serialized to JSON by the gateway.
/// </summary>
public sealed record ChannelMessage(string Kind, string Content)
{
    public static ChannelMessage Text(string content) => new("text", content);
}
