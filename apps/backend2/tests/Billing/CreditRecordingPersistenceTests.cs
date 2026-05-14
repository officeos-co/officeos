using OffceOs.Application.Features.Billing;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Billing;
using OffceOs.Database;
using OffceOs.Configuration;
using OffceOs.Infrastructure.Features.Agents;
using OffceOs.Infrastructure.Features.Billing;
using OffceOs.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace OffceOs.Tests.Billing;

public sealed class CreditRecordingPersistenceTests
{
    [Fact]
    public async Task RecordCreditUsageAsync_persists_usage_through_real_repositories()
    {
        var dbName = $"billing-{Guid.NewGuid():N}";
        var ownerId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        await using (var db = TestDbFactory.CreateNamed(dbName))
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
            await new UserSubscriptionRepository(db).AddAsync(UserSubscriptionRecord.CreateDefaultFree(ownerId));
        }

        await using (var db = TestDbFactory.CreateNamed(dbName))
        {
            var service = new CreditRecordingService(
                new StripeConfig(),
                new AgentRepository(db),
                new UserSubscriptionRepository(db),
                new FakeStripeMeteringService(),
                NullLogger<CreditRecordingService>.Instance);

            await service.RecordCreditUsageAsync(agentId, "gpt-4o-mini", rawTokens: 123, CancellationToken.None);
        }

        await using (var db = TestDbFactory.CreateNamed(dbName))
        {
            var persisted = await db.UserSubscriptions.SingleAsync(s => s.UserId == ownerId);
            Assert.Equal(123, persisted.CreditsUsedThisMonth);
        }
    }

}
