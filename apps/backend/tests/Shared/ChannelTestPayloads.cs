
namespace OffceOs.Tests.Shared;

internal static class ChannelTestPayloads
{
    public static string SlackBindingConfig(
        string channelId,
        string botUserId,
        int? initialHistoryLimit = null,
        string replyToMode = "all") =>
        JsonSerializer.Serialize(new
        {
            platformId = channelId,
            requireMention = true,
            botUserId,
            replyToMode,
            initialHistoryLimit,
        });

    public static string SlackEnvelope(
        string text,
        string channelId,
        IReadOnlyList<string>? mentions = null,
        string? threadTs = null,
        Guid? replyToBotAgentId = null) =>
        JsonSerializer.Serialize(new
        {
            text,
            platform = "slack",
            chatType = "channel",
            channelId,
            mentions = mentions ?? [],
            threadTs,
            replyToBotAgentId,
        });

    public static string TelegramBindingConfig(
        string groupId,
        IReadOnlyList<string>? allowedSenderIds = null,
        IReadOnlyList<string>? activeTopicIds = null,
        int? historyLimit = null) =>
        JsonSerializer.Serialize(new
        {
            platformId = groupId,
            requireMention = true,
            allowedGroupIds = new[] { groupId },
            allowedSenderIds = allowedSenderIds ?? [],
            mentionPatterns = new[] { "@openclaw" },
            activeTopicIds = activeTopicIds ?? [],
            historyLimit,
        });

    public static string TelegramEnvelope(string text, string groupId, string? messageThreadId = null) =>
        JsonSerializer.Serialize(new
        {
            text,
            platform = "telegram",
            chatType = "group",
            groupId,
            messageThreadId,
        });
}
