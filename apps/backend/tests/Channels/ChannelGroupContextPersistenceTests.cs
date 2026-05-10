using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Events;
using OffceOs.Domain.Features.Analytics;
using OffceOs.Infrastructure.Features.Analytics;
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
    public async Task Whatsapp_buffers_unprocessed_group_messages_and_injects_bounded_context_only_when_triggered()
    {
        await using var harness = ChannelTestHarness.Create("whatsapp-pending-context");
        var agentId = Guid.NewGuid();
        harness.SeedAgents(agentId);
        var whatsapp = await harness.CreateConnectionAsync("whatsapp", "Ops WhatsApp");
        await harness.BindAsync(
            agentId,
            whatsapp.Id,
            ChannelTestPayloads.WhatsappBindingConfig(
                "120363-ops@g.us",
                allowedSenderIds: ["+15551234567"],
                historyLimit: 2));

        await harness.Service.RouteInboundAsync(
            whatsapp.Id,
            "+15551234567",
            ChannelTestPayloads.WhatsappEnvelope("first ambient update", "120363-ops@g.us"),
            isGroupMessage: true,
            messageId: "wa-1",
            channelId: "120363-ops@g.us");
        await harness.Service.RouteInboundAsync(
            whatsapp.Id,
            "+15551234567",
            ChannelTestPayloads.WhatsappEnvelope("second ambient update", "120363-ops@g.us"),
            isGroupMessage: true,
            messageId: "wa-2",
            channelId: "120363-ops@g.us");
        await harness.Service.RouteInboundAsync(
            whatsapp.Id,
            "+15551234567",
            ChannelTestPayloads.WhatsappEnvelope("third ambient update", "120363-ops@g.us"),
            isGroupMessage: true,
            messageId: "wa-3",
            channelId: "120363-ops@g.us");

        var notified = await harness.Service.RouteInboundAsync(
            whatsapp.Id,
            "+15551234567",
            ChannelTestPayloads.WhatsappEnvelope("@OpenClaw summarize now", "120363-ops@g.us"),
            isGroupMessage: true,
            messageId: "wa-4",
            channelId: "120363-ops@g.us");

        var message = Assert.Single(harness.MessageEvents);
        Assert.Equal([agentId], notified);
        Assert.DoesNotContain("first ambient update", message.Content);
        Assert.Contains("second ambient update", message.Content);
        Assert.Contains("third ambient update", message.Content);
        Assert.Contains("[Chat messages since your last reply - for context]", message.Content);
        Assert.Contains("[Current message - respond to this]", message.Content);
        Assert.Contains("@OpenClaw summarize now", message.Content);
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
    public async Task Teams_history_limit_zero_does_not_remember_ambient_group_messages()
    {
        await using var harness = ChannelTestHarness.Create("teams-history-disabled");
        var agentId = Guid.NewGuid();
        harness.SeedAgents(agentId);
        var teams = await harness.CreateConnectionAsync("teams", "Engineering Teams");
        await harness.BindAsync(
            agentId,
            teams.Id,
            ChannelTestPayloads.TeamsBindingConfig(
                "19:channel@thread.tacv2",
                allowedSenderIds: ["aad-allowed"],
                historyLimit: 0));

        await harness.Service.RouteInboundAsync(
            teams.Id,
            "aad-allowed",
            ChannelTestPayloads.TeamsEnvelope("ambient architecture discussion", "19:team@thread.tacv2", "19:channel@thread.tacv2", mentionsBot: false),
            isGroupMessage: true,
            messageId: "teams-1",
            channelId: "19:channel@thread.tacv2");

        var notified = await harness.Service.RouteInboundAsync(
            teams.Id,
            "aad-allowed",
            ChannelTestPayloads.TeamsEnvelope("<at>OpenClaw</at> summarize", "19:team@thread.tacv2", "19:channel@thread.tacv2", mentionsBot: true),
            isGroupMessage: true,
            messageId: "teams-2",
            channelId: "19:channel@thread.tacv2");

        var message = Assert.Single(harness.MessageEvents);
        Assert.Equal([agentId], notified);
        Assert.DoesNotContain("ambient architecture discussion", message.Content);
        Assert.Contains("summarize", message.Content);
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
