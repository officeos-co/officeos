using OffceOs.Features.Channels.Domain;

namespace OffceOs.Features.Channels.Application;

internal sealed class ChannelGroupContextStore
{
    private static readonly ConcurrentDictionary<string, List<string>> PendingGroupContext = new();

    public void BufferPendingContext(
        AgentChannelBindingRecord binding,
        ChannelBindingConfig? config,
        ChannelInboundContext inbound,
        string channelType)
    {
        var limit = ResolveHistoryLimit(config, channelType);
        if (limit <= 0 || string.IsNullOrWhiteSpace(inbound.Text))
            return;

        var key = BuildContextKey(binding, inbound, channelType);
        var buffer = PendingGroupContext.GetOrAdd(key, _ => []);
        lock (buffer)
        {
            buffer.Add(inbound.Text);
            while (buffer.Count > limit)
                buffer.RemoveAt(0);
        }
    }

    public string BuildAgentMessageContent(
        AgentChannelBindingRecord binding,
        ChannelBindingConfig? config,
        ChannelInboundContext inbound,
        string channelType)
    {
        var current = string.IsNullOrWhiteSpace(inbound.Text) ? inbound.RawText : inbound.Text;
        var limit = ResolveHistoryLimit(config, channelType);
        if (limit <= 0)
            return current;

        var key = BuildContextKey(binding, inbound, channelType);
        if (!PendingGroupContext.TryRemove(key, out var buffer))
            return current;

        List<string> history;
        lock (buffer)
        {
            history = buffer.TakeLast(limit).ToList();
        }

        if (history.Count == 0)
            return current;

        var builder = new StringBuilder();
        builder.AppendLine("[Chat messages since your last reply - for context]");
        foreach (var item in history)
            builder.AppendLine(item);
        builder.AppendLine("[Current message - respond to this]");
        builder.Append(current);
        return builder.ToString();
    }

    private static int ResolveHistoryLimit(ChannelBindingConfig? config, string channelType)
    {
        if (config?.HistoryLimit is { } historyLimit)
            return Math.Max(0, historyLimit);

        if (channelType is "slack" && config?.InitialHistoryLimit is { } initialHistoryLimit)
            return Math.Max(0, initialHistoryLimit);

        return channelType switch
        {
            "slack" => 20,
            "telegram" => 50,
            _ => 0,
        };
    }

    private static string BuildContextKey(
        AgentChannelBindingRecord binding,
        ChannelInboundContext inbound,
        string channelType)
    {
        var target = ChannelRoutingPolicy.ResolveTargetId(inbound, channelType) ?? "unknown";
        var thread = channelType switch
        {
            "slack" => inbound.ThreadTs,
            "telegram" => inbound.MessageThreadId,
            _ => null,
        };

        return $"{binding.ChannelConnectionId:N}:{binding.AgentId:N}:{channelType}:{target}:{thread}";
    }
}
