using OffceOs.Application.Features.Billing;
using OffceOs.Configuration;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Billing;
using Microsoft.Extensions.Logging.Abstractions;

namespace OffceOs.Tests.Shared;

internal static class BillingGuardTestFactory
{
    public static (BillingGuard Guard, Guid AgentId) CreateGuard(
        AgentRecord agent,
        FakeUserSubscriptionRepository subscriptions,
        BillingPolicyConfig? policy = null) =>
        (new BillingGuard(
            new InMemoryDistributedCache(),
            new FakeAgentRepository(agent),
            subscriptions,
            NullLogger<BillingGuard>.Instance,
            policy ?? new BillingPolicyConfig()),
            agent.Id);
}

internal static class CreditRecordingServiceTestFactory
{
    public static CreditRecordingService CreateService(
        IAgentRepository agents,
        IUserSubscriptionRepository subscriptions,
        IStripeMeteringService stripe,
        CustomLlmProviderConfig? customLlmProviderConfig = null) =>
        new(
            new StripeConfig(),
            agents,
            subscriptions,
            stripe,
            NullLogger<CreditRecordingService>.Instance,
            customLlmProviderConfig);
}

public static class AgentRecordFactory
{
    public static AgentRecord Agent(Guid id, Guid? ownerId) => new()
    {
        Id = id,
        Name = "Test agent",
        Provider = "openai",
        Model = "gpt-4o-mini",
        OwnerId = ownerId,
        PodName = "pod",
    };
}
