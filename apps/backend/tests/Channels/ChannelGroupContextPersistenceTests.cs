using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Events;
using OffceOs.Domain.Features.Observability;
using OffceOs.Infrastructure.Features.Observability;
using OffceOs.Tests.Shared;
using Xunit;

namespace OffceOs.Tests.Channels;

public sealed class ChannelGroupContextPersistenceTests
{
    [Fact]
    public async Task Rejected_group_messages_are_logged_only_at_connection_scope_not_as_agent_transcript()
    {
        await using var harness = ChannelTestHarness.CreatePersisting("channel-rejected-log-scope");
        var agentId = Guid.NewGuid();
        harness.SeedAgents(agentId);
        var slack = await harness.CreateConnectionAsync("slack", "Ops Slack");
        await harness.BindAsync(agentId, slack.Id, ChannelTestPayloads.SlackBindingConfig("C-ops", "U-agent"));

        var notified = await harness.Service.RouteInboundAsync(
            slack.Id,
            "U-human",
            ChannelTestPayloads.SlackEnvelope("ambient channel chatter", "C-ops"),
            isGroupMessage: true,
            messageId: "1710000000.000100",
            channelId: "C-ops");

        var repository = new AgentLogRepository(harness.Db);
        var agentLogs = await repository.ListAsync(new AgentLogFilter { AgentId = agentId });
        var channelLogs = await repository.ListAsync(new AgentLogFilter { ChannelConnectionId = slack.Id });

        Assert.Empty(notified);
        Assert.Empty(harness.Notifications.OfType<MessageReceivedEvent>());
        Assert.Empty(agentLogs);
        var rejectedLog = Assert.Single(channelLogs);
        Assert.Null(rejectedLog.AgentId);
        Assert.Equal(AgentLogType.ChannelIn, rejectedLog.Type);
        Assert.Equal(slack.Id, rejectedLog.ChannelConnectionId);
    }

    [Fact]
    public async Task Slack_thread_history_limit_injects_only_recent_messages_for_the_triggered_thread()
    {
        await using var harness = ChannelTestHarness.Create("slack-thread-context");
        var agentId = Guid.NewGuid();
        harness.SeedAgents(agentId);
        var slack = await harness.CreateConnectionAsync("slack", "Ops Slack");
        await harness.BindAsync(
            agentId,
            slack.Id,
            ChannelTestPayloads.SlackBindingConfig(
                "C-ops",
                "U-agent",
                initialHistoryLimit: 2));

        foreach (var (id, text) in new[]
        {
            ("1710000000.000001", "oldest thread line"),
            ("1710000000.000002", "recent thread line one"),
            ("1710000000.000003", "recent thread line two"),
        })
        {
            await harness.Service.RouteInboundAsync(
                slack.Id,
                "U-human",
                ChannelTestPayloads.SlackEnvelope(text, "C-ops", threadTs: "1710000000.000000"),
                isGroupMessage: true,
                messageId: id,
                channelId: "C-ops");
        }

        var notified = await harness.Service.RouteInboundAsync(
            slack.Id,
            "U-human",
            ChannelTestPayloads.SlackEnvelope(
                "please answer from this thread",
                "C-ops",
                mentions: ["U-agent"],
                threadTs: "1710000000.000000"),
            isGroupMessage: true,
            messageId: "1710000000.000004",
            channelId: "C-ops");

        var message = Assert.Single(harness.MessageEvents);
        Assert.Equal([agentId], notified);
        Assert.DoesNotContain("oldest thread line", message.Content);
        Assert.Contains("recent thread line one", message.Content);
        Assert.Contains("recent thread line two", message.Content);
        Assert.Contains("please answer from this thread", message.Content);
    }

    [Fact]
    public async Task Telegram_topic_context_is_isolated_between_agents_and_topics()
    {
        await using var harness = ChannelTestHarness.Create("telegram-topic-context");
        var agentOneId = Guid.NewGuid();
        var agentTwoId = Guid.NewGuid();
        harness.SeedAgents(agentOneId, agentTwoId);
        var telegram = await harness.CreateConnectionAsync("telegram", "Ops Telegram");
        await harness.BindAsync(
            agentOneId,
            telegram.Id,
            ChannelTestPayloads.TelegramBindingConfig(
                "-100ops",
                allowedSenderIds: ["42"],
                activeTopicIds: ["topic-a"]));
        await harness.BindAsync(
            agentTwoId,
            telegram.Id,
            ChannelTestPayloads.TelegramBindingConfig(
                "-100ops",
                allowedSenderIds: ["42"],
                activeTopicIds: ["topic-b"]));

        await harness.Service.RouteInboundAsync(
            telegram.Id,
            "42",
            ChannelTestPayloads.TelegramEnvelope("@openclaw topic a context", "-100ops", "topic-a"),
            isGroupMessage: true,
            messageId: "tg-a-1",
            channelId: "-100ops");
        var notified = await harness.Service.RouteInboundAsync(
            telegram.Id,
            "42",
            ChannelTestPayloads.TelegramEnvelope("@openclaw topic b question", "-100ops", "topic-b"),
            isGroupMessage: true,
            messageId: "tg-b-1",
            channelId: "-100ops");

        var message = Assert.Single(harness.MessageEvents, ev => ev.AgentId == agentTwoId);
        Assert.Equal([agentTwoId], notified);
        Assert.Contains("topic b question", message.Content);
        Assert.DoesNotContain("topic a context", message.Content);
        Assert.DoesNotContain(harness.MessageEvents, ev => ev.AgentId == agentOneId && ev.Content.Contains("topic b question"));
    }
}
