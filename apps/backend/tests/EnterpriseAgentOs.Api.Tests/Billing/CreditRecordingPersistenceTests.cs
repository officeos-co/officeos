using EnterpriseAgentOs.Application.Features.Management;
using EnterpriseAgentOs.Domain.Features.Agents;
using EnterpriseAgentOs.Domain.Features.Management;
using EnterpriseAgentOs.Infrastructure.Common;
using EnterpriseAgentOs.Infrastructure.Common.Configuration;
using EnterpriseAgentOs.Infrastructure.Features.Agents;
using EnterpriseAgentOs.Infrastructure.Features.Management;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EnterpriseAgentOs.Api.Tests.Billing;

public sealed class CreditRecordingPersistenceTests
{
    [Fact]
    public async Task RecordCreditUsageAsync_persists_usage_through_real_repositories()
    {
        var dbName = $"billing-{Guid.NewGuid():N}";
        var ownerId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        await using (var db = CreateDb(dbName))
        {
            await db.Database.EnsureCreatedAsync();
            await new AgentRepository(db).AddAsync(new AgentRecord
            {
                Id = agentId,
                Name = "Billing agent",
                Provider = "openai",
                Model = "gpt-4o-mini",
                OwnerId = ownerId,
            });
            await new UserSubscriptionRepository(db).AddAsync(UserSubscription.CreateDefaultFree(ownerId));
        }

        await using (var db = CreateDb(dbName))
        {
            var service = new CreditRecordingService(
                new StripeConfig(),
                new AgentRepository(db),
                new UserSubscriptionRepository(db),
                new FakeStripeMeteringService(),
                NullLogger<CreditRecordingService>.Instance);

            await service.RecordCreditUsageAsync(agentId, "gpt-4o-mini", rawTokens: 123, CancellationToken.None);
        }

        await using (var db = CreateDb(dbName))
        {
            var persisted = await db.UserSubscriptions.SingleAsync(s => s.UserId == ownerId);
            Assert.Equal(123, persisted.CreditsUsedThisMonth);
        }
    }

    private static EaosDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<EaosDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new EaosDbContext(options);
    }

    private sealed class FakeStripeMeteringService : IStripeMeteringService
    {
        public Task FireMeterEventAsync(string eventName, string customerId, long credits, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
