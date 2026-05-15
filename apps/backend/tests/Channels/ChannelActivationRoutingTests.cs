using OffceOs.Domain.Features.AgentHarness;
using OffceOs.Tests.Shared;
using Xunit;

namespace OffceOs.Tests.Channels;

public sealed class ChannelActivationRoutingTests
{
    [Fact]
    public async Task Slack_channel_message_without_matching_agent_mention_is_ignored()
    {
        await using var harness = ChannelTestHarness.Create("slack-mention-gate");
        var agentOneId = Guid.NewGuid();
        var agentTwoId = Guid.NewGuid();
        harness.SeedAgents(agentOneId, agentTwoId);
        var slack = await harness.CreateConnectionAsync("slack", "Ops Slack");
        await harness.BindAsync(agentOneId, slack.Id, ChannelTestPayloads.SlackBindingConfig("C-ops", "U-agent-one"));
        await harness.BindAsync(agentTwoId, slack.Id, ChannelTestPayloads.SlackBindingConfig("C-ops", "U-agent-two"));

        var notified = await harness.Service.RouteInboundAsync(
            slack.Id,
            "U-human",
            ChannelTestPayloads.SlackEnvelope("standup update", "C-ops"),
            isGroupMessage: true,
            messageId: "1710000000.000100",
            channelId: "C-ops");

        harness.AssertNoAgentActivation(notified, agentOneId, agentTwoId);
    }

    [Fact]
    public async Task Slack_channel_message_with_agent_mention_routes_only_matching_agent()
    {
        await using var harness = ChannelTestHarness.Create("slack-agent-mention");
        var agentOneId = Guid.NewGuid();
        var agentTwoId = Guid.NewGuid();
        harness.SeedAgents(agentOneId, agentTwoId);
        var slack = await harness.CreateConnectionAsync("slack", "Ops Slack");
        await harness.BindAsync(agentOneId, slack.Id, ChannelTestPayloads.SlackBindingConfig("C-ops", "U-agent-one"));
        await harness.BindAsync(agentTwoId, slack.Id, ChannelTestPayloads.SlackBindingConfig("C-ops", "U-agent-two"));

        var notified = await harness.Service.RouteInboundAsync(
            slack.Id,
            "U-human",
            ChannelTestPayloads.SlackEnvelope(
                "please summarize the deploy",
                "C-ops",
                mentions: ["U-agent-one"]),
            isGroupMessage: true,
            messageId: "1710000000.000200",
            channelId: "C-ops");

        Assert.Equal([agentOneId], notified);
        Assert.Equal([agentOneId], harness.MessageEvents.Select(ev => ev.AgentId));
        Assert.DoesNotContain(harness.MessageEvents, ev => ev.AgentId == agentTwoId);
    }

    [Fact]
    public async Task Slack_thread_reply_to_agent_routes_only_that_agent_without_new_mention()
    {
        await using var harness = ChannelTestHarness.Create("slack-thread-gate");
        var agentOneId = Guid.NewGuid();
        var agentTwoId = Guid.NewGuid();
        harness.SeedAgents(agentOneId, agentTwoId);
        var slack = await harness.CreateConnectionAsync("slack", "Ops Slack");
        await harness.BindAsync(agentOneId, slack.Id, ChannelTestPayloads.SlackBindingConfig("C-ops", "U-agent-one"));
        await harness.BindAsync(agentTwoId, slack.Id, ChannelTestPayloads.SlackBindingConfig("C-ops", "U-agent-two"));

        var notified = await harness.Service.RouteInboundAsync(
            slack.Id,
            "U-human",
            ChannelTestPayloads.SlackEnvelope(
                "yes, continue",
                "C-ops",
                threadTs: "1710000000.000200",
                replyToBotAgentId: agentOneId),
            isGroupMessage: true,
            messageId: "1710000001.000200",
            channelId: "C-ops");

        Assert.Equal([agentOneId], notified);
        Assert.Equal([agentOneId], harness.MessageEvents.Select(ev => ev.AgentId));
    }

    [Theory]
    [InlineData("-100ops", "42", "summarize this", null, false)]
    [InlineData("-100unknown", "42", "@openclaw summarize this", null, false)]
    [InlineData("-100ops", "666", "@openclaw summarize this", null, false)]
    [InlineData("-100ops", "42", "@openclaw summarize this", null, true)]
    [InlineData("-100ops", "42", "continue", "topic-incident-1", true)]
    public async Task Telegram_group_messages_require_group_sender_and_mention_or_active_topic_reply(
        string groupId,
        string senderIdentifier,
        string text,
        string? activeTopicId,
        bool shouldNotify)
    {
        await using var harness = ChannelTestHarness.Create("telegram-activation");
        var agentId = Guid.NewGuid();
        harness.SeedAgents(agentId);
        var telegram = await harness.CreateConnectionAsync("telegram", "Ops Telegram");
        await harness.BindAsync(
            agentId,
            telegram.Id,
            ChannelTestPayloads.TelegramBindingConfig(
                "-100ops",
                allowedSenderIds: ["42"],
                activeTopicIds: ["topic-incident-1"]));

        var notified = await harness.Service.RouteInboundAsync(
            telegram.Id,
            senderIdentifier,
            ChannelTestPayloads.TelegramEnvelope(text, groupId, activeTopicId),
            isGroupMessage: true,
            messageId: "tg-message-1",
            channelId: groupId);

        if (shouldNotify)
        {
            Assert.Equal([agentId], notified);
            Assert.Equal([agentId], harness.MessageEvents.Select(ev => ev.AgentId));
        }
        else
        {
            harness.AssertNoAgentActivation(notified, agentId);
        }
    }
}
