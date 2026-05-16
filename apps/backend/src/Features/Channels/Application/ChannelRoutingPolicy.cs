using OffceOs.Features.Channels.Domain;

namespace OffceOs.Features.Channels.Application;

internal static class ChannelRoutingPolicy
{
    public static ChannelBindingConfig? ParseBindingConfig(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<ChannelBindingConfig>(configJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        }
        catch
        {
            return null;
        }
    }

    public static ChannelRouteResult ShouldActivateBinding(
        AgentChannelBindingRecord binding,
        ChannelBindingConfig? config,
        ChannelInboundContext inbound,
        string channelType,
        string senderIdentifier)
    {
        if (!inbound.IsGroupMessage || !HasActivationPolicy(config))
            return ChannelRouteResult.Active;

        if (!TargetMatches(config, inbound, channelType))
            return ChannelRouteResult.Rejected;

        if (!SenderAllowed(config, senderIdentifier))
            return ChannelRouteResult.Rejected;

        if (!RequiresMention(config))
            return ChannelRouteResult.Active;

        if (IsActivated(binding, config, inbound, channelType))
            return ChannelRouteResult.Active;

        return ChannelRouteResult.Buffered;
    }

    public static string? ResolveTargetId(ChannelInboundContext inbound, string channelType) =>
        channelType switch
        {
            "slack" => inbound.ChannelId,
            "telegram" => inbound.GroupId ?? inbound.ChannelId,
            _ => inbound.ChannelId,
        };

    private static bool HasActivationPolicy(ChannelBindingConfig? config) =>
        config is not null &&
        (!string.IsNullOrWhiteSpace(config.PlatformId) ||
         config.RequireMention.HasValue ||
         !string.IsNullOrWhiteSpace(config.BotUserId) ||
         !string.IsNullOrWhiteSpace(config.BotMention) ||
         config.AllowedSenderIds is { Count: > 0 } ||
         config.AllowedGroupIds is { Count: > 0 } ||
         config.MentionPatterns is { Count: > 0 } ||
         config.ActiveTopicIds is { Count: > 0 });

    private static bool TargetMatches(ChannelBindingConfig? config, ChannelInboundContext inbound, string channelType)
    {
        if (config is null)
            return true;

        var targetId = ResolveTargetId(inbound, channelType);
        if (!string.IsNullOrWhiteSpace(config.PlatformId) &&
            !string.Equals(config.PlatformId, targetId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (config.AllowedGroupIds is { Count: > 0 } allowedGroups &&
            !ContainsIgnoreCase(allowedGroups, targetId) &&
            !ContainsIgnoreCase(allowedGroups, "*"))
            return false;

        if (channelType is "telegram" &&
            !string.IsNullOrWhiteSpace(inbound.MessageThreadId) &&
            config.ActiveTopicIds is { Count: > 0 } activeTopicIds &&
            !ContainsIgnoreCase(activeTopicIds, inbound.MessageThreadId))
            return false;

        return true;
    }

    private static bool SenderAllowed(ChannelBindingConfig? config, string senderIdentifier)
    {
        if (config?.AllowedSenderIds is not { Count: > 0 } allowedSenders)
            return true;

        return ContainsIgnoreCase(allowedSenders, senderIdentifier) || ContainsIgnoreCase(allowedSenders, "*");
    }

    private static bool RequiresMention(ChannelBindingConfig? config) =>
        config?.RequireMention ?? true;

    private static bool IsActivated(
        AgentChannelBindingRecord binding,
        ChannelBindingConfig? config,
        ChannelInboundContext inbound,
        string channelType)
    {
        if (inbound.ReplyToBotAgentId == binding.AgentId)
            return true;

        if (channelType is "telegram" &&
            !string.IsNullOrWhiteSpace(inbound.MessageThreadId) &&
            config?.ActiveTopicIds is { Count: > 0 } activeTopicIds &&
            ContainsIgnoreCase(activeTopicIds, inbound.MessageThreadId))
            return true;

        if (!string.IsNullOrWhiteSpace(config?.BotUserId) &&
            inbound.Mentions.Any(mention => string.Equals(mention, config.BotUserId, StringComparison.OrdinalIgnoreCase)))
            return true;

        if (!string.IsNullOrWhiteSpace(config?.BotMention) &&
            inbound.Text.Contains(config.BotMention, StringComparison.OrdinalIgnoreCase))
            return true;

        if (config?.MentionPatterns is { Count: > 0 } mentionPatterns &&
            mentionPatterns.Any(pattern => !string.IsNullOrWhiteSpace(pattern) &&
                inbound.Text.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }

    private static bool ContainsIgnoreCase(IEnumerable<string> values, string? candidate) =>
        !string.IsNullOrWhiteSpace(candidate) &&
        values.Any(value => string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase));

    public readonly record struct ChannelRouteResult(bool Route, bool Buffer)
    {
        public static ChannelRouteResult Active => new(true, false);
        public static ChannelRouteResult Buffered => new(false, true);
        public static ChannelRouteResult Rejected => new(false, false);
    }
}

internal sealed record ChannelInboundContext(
    string RawText,
    string Text,
    bool IsGroupMessage,
    string? Platform,
    string? ChatType,
    string? ChannelId,
    string? ConversationId,
    string? GroupId,
    string? TeamId,
    IReadOnlyList<string> Mentions,
    bool MentionsBot,
    string? ThreadTs,
    Guid? ReplyToBotAgentId,
    string? MessageThreadId)
{
    public static ChannelInboundContext Parse(string messageText, bool isGroupMessage, string? fallbackChannelId)
    {
        if (string.IsNullOrEmpty(messageText) || messageText[0] != '{')
            return TextOnly(messageText, isGroupMessage, fallbackChannelId);

        try
        {
            using var doc = JsonDocument.Parse(messageText);
            var root = doc.RootElement;
            return new ChannelInboundContext(
                messageText,
                GetString(root, "text") ?? messageText,
                isGroupMessage,
                GetString(root, "platform"),
                GetString(root, "chatType"),
                GetString(root, "channelId") ?? fallbackChannelId,
                GetString(root, "conversationId"),
                GetString(root, "groupId"),
                GetString(root, "teamId"),
                GetStringArray(root, "mentions"),
                GetBool(root, "mentionsBot"),
                GetString(root, "threadTs"),
                GetGuid(root, "replyToBotAgentId"),
                GetString(root, "messageThreadId"));
        }
        catch
        {
            return TextOnly(messageText, isGroupMessage, fallbackChannelId);
        }
    }

    private static ChannelInboundContext TextOnly(string messageText, bool isGroupMessage, string? fallbackChannelId) =>
        new(
            messageText,
            messageText,
            isGroupMessage,
            null,
            null,
            fallbackChannelId,
            null,
            null,
            null,
            [],
            false,
            null,
            null,
            null);

    private static string? GetString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static bool GetBool(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.True;

    private static Guid? GetGuid(JsonElement root, string propertyName)
    {
        var value = GetString(root, propertyName);
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    private static IReadOnlyList<string> GetStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Array)
            return [];

        return property.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToList();
    }
}
