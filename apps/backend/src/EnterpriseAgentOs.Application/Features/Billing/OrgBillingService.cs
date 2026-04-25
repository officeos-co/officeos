namespace EnterpriseAgentOs.Application.Features.Billing;

internal sealed class OrgBillingService : IOrgBillingService
{
    private readonly StripeConfig _stripeConfig;
    private readonly IOrgSubscriptionRepository _orgSubscriptionRepository;
    private readonly ILogger<OrgBillingService> _logger;

    public OrgBillingService(StripeConfig config, IOrgSubscriptionRepository repo, ILogger<OrgBillingService> logger)
    {
        _stripeConfig = config;
        _orgSubscriptionRepository = repo;
        _logger = logger;
        StripeConfiguration.ApiKey = _stripeConfig.SecretKey;
    }

    public async Task<OrgSubscription> GetSubscriptionAsync(string orgId, CancellationToken ct = default)
    {
        var sub = await _orgSubscriptionRepository.GetByOrganizationIdAsync(orgId, ct);
        return sub ?? OrgSubscription.CreateDefaultFree(orgId);
    }

    public async Task<CreditBudgetResult> CheckCreditBudgetAsync(string orgId, CancellationToken ct = default)
    {
        var sub = await GetSubscriptionAsync(orgId, ct);
        return sub.CheckBudget();
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
        //TODO: This is domain logic
        var priceId = (plan, billingCycle) switch
        {
            ("team", "yearly") => _stripeConfig.TeamYearlyPriceId,
            ("team", _)        => _stripeConfig.TeamMonthlyPriceId,
            _                  => _stripeConfig.FreePriceId,
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
        var sub = await _orgSubscriptionRepository.GetByOrganizationIdAsync(orgId, ct);
        if (sub is null)
        {
            sub = OrgSubscription.CreateDefaultFree(orgId);
            await _orgSubscriptionRepository.AddAsync(sub, ct);
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
                        Price = _stripeConfig.TeamOveragePriceId,
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
                        Items = [new SubscriptionItemOptions { Price = _stripeConfig.TeamOveragePriceId }],
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

        await _orgSubscriptionRepository.SaveChangesAsync(ct);
    }

}
