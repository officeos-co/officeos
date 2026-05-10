using OffceOs.Application.Features.Billing;
using OffceOs.Domain.Features.Billing;

namespace OffceOs.Tests.Shared;

public sealed class FakeStripeMeteringService : IStripeMeteringService
{
    public List<(string EventName, string CustomerId, long Credits)> Events { get; } = [];

    public Task FireMeterEventAsync(string eventName, string customerId, long credits, CancellationToken ct = default)
    {
        Events.Add((eventName, customerId, credits));
        return Task.CompletedTask;
    }
}

public sealed class FakeUserSubscriptionRepository : IUserSubscriptionRepository
{
    public FakeUserSubscriptionRepository(UserSubscriptionRecord? current = null) => Current = current;

    public UserSubscriptionRecord? Current { get; private set; }
    public int AddCount { get; private set; }
    public int GetCount { get; private set; }
    public int UpdateCount { get; private set; }

    public Task<UserSubscriptionRecord?> GetByAsync(UserSubscriptionFilter filter, CancellationToken ct = default)
    {
        GetCount++;
        return Task.FromResult(
            Current is not null
            && (!filter.Id.HasValue || Current.Id == filter.Id.Value)
            && (!filter.UserId.HasValue || Current.UserId == filter.UserId.Value)
                ? Current
                : null);
    }

    public Task AddAsync(UserSubscriptionRecord sub, CancellationToken ct = default)
    {
        Current = sub;
        AddCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(UserSubscriptionRecord sub, CancellationToken ct = default)
    {
        Current = sub;
        UpdateCount++;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
