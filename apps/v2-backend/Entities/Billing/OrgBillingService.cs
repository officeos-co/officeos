using EnterpriseAgentOs.Api.Properties;
using Stripe;

namespace EnterpriseAgentOs.Api.Entities.Billing;

public sealed class OrgBillingService : IOrgBillingService
{
    private readonly StripeConfig _config;
    private readonly EaosDbContext _db;
    private readonly ILogger<OrgBillingService> _logger;

    public OrgBillingService(StripeConfig config, EaosDbContext db, ILogger<OrgBillingService> logger)
    {
        _config = config;
        _db = db;
        _logger = logger;
        StripeConfiguration.ApiKey = _config.SecretKey;
    }

    public async Task<OrgSubscription> GetSubscriptionAsync(string orgId, CancellationToken ct = default)
    {
        var sub = await _db.OrgSubscriptions.FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct);
        return sub ?? DefaultFree(orgId);
    }

    public async Task<(long Remaining, bool OverBudget)> CheckCreditBudgetAsync(string orgId, CancellationToken ct = default)
    {
        var sub = await GetSubscriptionAsync(orgId, ct);
        var remaining = sub.CreditBudgetPerMonth - sub.CreditsUsedThisMonth;
        return (remaining, remaining < 0);
    }

    public async Task<string> CreateCustomerAsync(string orgId, string email, CancellationToken ct = default)
    {
        var customer = await new CustomerService().CreateAsync(
            new CustomerCreateOptions
            {
                Email = email,
                Metadata = new Dictionary<string, string> { ["type"] = "org", ["orgId"] = orgId },
            },
            cancellationToken: ct);

        _logger.LogInformation("Created Stripe customer {CustomerId} for org {OrgId}", customer.Id, orgId);
        return customer.Id;
    }

    public async Task<string> CreateSubscriptionAsync(string customerId, string plan, string billingCycle = "monthly", CancellationToken ct = default)
    {
        var priceId = (plan, billingCycle) switch
        {
            ("team", "yearly") => _config.TeamYearlyPriceId,
            ("team", _)        => _config.TeamMonthlyPriceId,
            _                  => _config.FreePriceId,
        };
        var sub = await new SubscriptionService().CreateAsync(
            new SubscriptionCreateOptions
            {
                Customer = customerId,
                Items = [new SubscriptionItemOptions { Price = priceId }],
            },
            cancellationToken: ct);

        _logger.LogInformation("Created Stripe subscription {SubId} for customer {CustomerId} plan {Plan}", sub.Id, customerId, plan);
        return sub.Id;
    }

    public async Task EnableOverageAsync(string orgId, string email, bool enabled, CancellationToken ct = default)
    {
        var sub = await _db.OrgSubscriptions.FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct);
        if (sub is null)
        {
            sub = DefaultFree(orgId);
            await _db.OrgSubscriptions.AddAsync(sub, ct);
        }

        if (enabled)
        {
            if (sub.OverageEnabled) return;

            if (sub.StripeCustomerId is null)
            {
                sub.StripeCustomerId = await CreateCustomerAsync(orgId, email, ct);
            }

            if (sub.StripeSubscriptionId is not null)
            {
                var item = await new SubscriptionItemService().CreateAsync(
                    new SubscriptionItemCreateOptions
                    {
                        Subscription = sub.StripeSubscriptionId,
                        Price = _config.TeamOveragePriceId,
                    },
                    cancellationToken: ct);
                sub.StripeOverageItemId = item.Id;
            }
            else
            {
                var newSub = await new SubscriptionService().CreateAsync(
                    new SubscriptionCreateOptions
                    {
                        Customer = sub.StripeCustomerId,
                        Items = [new SubscriptionItemOptions { Price = _config.TeamOveragePriceId }],
                    },
                    cancellationToken: ct);
                sub.StripeSubscriptionId = newSub.Id;
                sub.StripeOverageItemId = newSub.Items.Data[0].Id;
            }

            sub.OverageEnabled = true;
            _logger.LogInformation("Overage enabled for org {OrgId}", orgId);
        }
        else
        {
            if (!sub.OverageEnabled || sub.StripeOverageItemId is null) return;

            await new SubscriptionItemService().DeleteAsync(
                sub.StripeOverageItemId,
                new SubscriptionItemDeleteOptions { ClearUsage = true },
                cancellationToken: ct);

            sub.StripeOverageItemId = null;
            sub.OverageEnabled = false;
            _logger.LogInformation("Overage disabled for org {OrgId}", orgId);
        }

        await _db.SaveChangesAsync(ct);
    }

    private static OrgSubscription DefaultFree(string orgId)
    {
        var now = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return new OrgSubscription
        {
            OrganizationId = orgId,
            Plan = PlanLimits.OrgFree.Plan,
            ConcurrentAgentLimit = PlanLimits.OrgFree.ConcurrentAgents,
            CreditBudgetPerMonth = PlanLimits.OrgFree.CreditsPerMonth,
            PeriodStart = now,
            PeriodEnd = now.AddMonths(1),
            IsActive = true,
        };
    }
}
