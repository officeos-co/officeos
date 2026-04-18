namespace EnterpriseAgentOs.Application.Services.Billing;

public sealed class UserBillingService : IUserBillingService
{
    private readonly EnterpriseAgentOs.Infrastructure.Configuration.StripeConfig _config;
    private readonly EnterpriseAgentOs.Infrastructure.Configuration.FrontendConfig _frontend;
    private readonly EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext _db;
    private readonly ILogger<UserBillingService> _logger;

    public UserBillingService(
        EnterpriseAgentOs.Infrastructure.Configuration.StripeConfig config,
        EnterpriseAgentOs.Infrastructure.Configuration.FrontendConfig frontend,
        EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext db,
        ILogger<UserBillingService> logger)
    {
        _config = config;
        _frontend = frontend;
        _db = db;
        _logger = logger;
        StripeConfiguration.ApiKey = _config.SecretKey;
    }

    public async Task<UserSubscription> GetSubscriptionAsync(Guid userId, CancellationToken ct = default)
    {
        var sub = await _db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        return sub ?? DefaultFree(userId);
    }

    public async Task<(long Remaining, bool OverBudget)> CheckCreditBudgetAsync(Guid userId, CancellationToken ct = default)
    {
        var sub = await GetSubscriptionAsync(userId, ct);
        var remaining = sub.CreditBudgetPerMonth - sub.CreditsUsedThisMonth;
        return (remaining, remaining < 0);
    }

    public async Task<string> CreateCheckoutSessionAsync(
        Guid userId, string email, string plan, string billingCycle, CancellationToken ct = default)
    {
        var customerId = await GetOrCreateCustomerAsync(userId, email, ct);

        var priceId = (plan, billingCycle) switch
        {
            ("pro", "yearly")  => _config.ProYearlyPriceId,
            ("pro", "monthly") => _config.ProMonthlyPriceId,
            _                  => _config.FreePriceId,
        };

        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = customerId,
            LineItems = [new SessionLineItemOptions { Price = priceId, Quantity = 1 }],
            SuccessUrl = $"{_frontend.Origin}/settings/billing?checkout=success",
            CancelUrl = $"{_frontend.Origin}/pricing",
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId.ToString(),
                ["plan"] = plan,
                ["billingCycle"] = billingCycle,
            },
        };

        var session = await new SessionService().CreateAsync(options, cancellationToken: ct);
        _logger.LogInformation("Created checkout session {SessionId} for user {UserId} plan {Plan}", session.Id, userId, plan);
        return session.Url;
    }

    public async Task<string> CreatePortalSessionAsync(Guid userId, string email, CancellationToken ct = default)
    {
        var customerId = await GetOrCreateCustomerAsync(userId, email, ct);
        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = customerId,
            ReturnUrl = $"{_frontend.Origin}/settings/billing",
        };
        var session = await new Stripe.BillingPortal.SessionService().CreateAsync(options, cancellationToken: ct);
        return session.Url;
    }

    public async Task EnableOverageAsync(Guid userId, string email, bool enabled, CancellationToken ct = default)
    {
        var sub = await _db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (sub is null)
        {
            sub = DefaultFree(userId);
            await _db.UserSubscriptions.AddAsync(sub, ct);
        }

        if (enabled)
        {
            if (sub.OverageEnabled) return;

            var customerId = await GetOrCreateCustomerAsync(userId, email, ct);
            var priceId = sub.Plan == "pro" ? _config.ProOveragePriceId : _config.FreeOveragePriceId;

            if (sub.StripeSubscriptionId is not null)
            {
                var item = await new SubscriptionItemService().CreateAsync(
                    new SubscriptionItemCreateOptions { Subscription = sub.StripeSubscriptionId, Price = priceId },
                    cancellationToken: ct);
                sub.StripeOverageItemId = item.Id;
            }
            else
            {
                var newSub = await new SubscriptionService().CreateAsync(
                    new SubscriptionCreateOptions
                    {
                        Customer = customerId,
                        Items = [new SubscriptionItemOptions { Price = priceId }],
                    },
                    cancellationToken: ct);
                sub.StripeSubscriptionId = newSub.Id;
                sub.StripeOverageItemId = newSub.Items.Data[0].Id;
            }

            sub.StripeCustomerId = customerId;
            sub.OverageEnabled = true;
            _logger.LogInformation("Overage enabled for user {UserId}", userId);
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
            _logger.LogInformation("Overage disabled for user {UserId}", userId);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<InvoicePayload>> ListInvoicesAsync(
        Guid userId, CancellationToken ct = default)
    {
        var sub = await _db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (sub?.StripeCustomerId is null || !_config.Enabled)
        {
            return Array.Empty<InvoicePayload>();
        }
        try
        {
            var invoices = await new InvoiceService().ListAsync(
                new InvoiceListOptions { Customer = sub.StripeCustomerId, Limit = 24 },
                cancellationToken: ct);
            return invoices.Data.Select(i => new InvoicePayload(
                i.Id,
                i.Created,
                ((decimal)i.Total / 100m).ToString("0.00"),
                (i.Currency ?? "eur").ToUpperInvariant(),
                i.Status ?? "unknown",
                i.HostedInvoiceUrl,
                i.InvoicePdf)).ToList();
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe invoice list failed for user {UserId}", userId);
            return Array.Empty<InvoicePayload>();
        }
    }

    private async Task<string> GetOrCreateCustomerAsync(Guid userId, string email, CancellationToken ct)
    {
        var existing = await _db.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.StripeCustomerId != null, ct);

        if (existing?.StripeCustomerId is not null)
            return existing.StripeCustomerId;

        var customer = await new CustomerService().CreateAsync(
            new CustomerCreateOptions
            {
                Email = email,
                Metadata = new Dictionary<string, string> { ["type"] = "user", ["userId"] = userId.ToString() },
            },
            cancellationToken: ct);

        _logger.LogInformation("Created Stripe customer {CustomerId} for user {UserId}", customer.Id, userId);

        var sub = await _db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (sub is null)
        {
            sub = DefaultFree(userId);
            await _db.UserSubscriptions.AddAsync(sub, ct);
        }
        sub.StripeCustomerId = customer.Id;
        await _db.SaveChangesAsync(ct);

        return customer.Id;
    }

    private static UserSubscription DefaultFree(Guid userId)
    {
        var limits = PlanLimits.IndividualFree;
        var now = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return new UserSubscription
        {
            UserId = userId,
            Plan = limits.Plan,
            BillingCycle = "monthly",
            ConcurrentAgentLimit = limits.ConcurrentAgents,
            CreditBudgetPerMonth = limits.CreditsPerMonth,
            PeriodStart = now,
            PeriodEnd = now.AddMonths(1),
            IsActive = true,
        };
    }
}
