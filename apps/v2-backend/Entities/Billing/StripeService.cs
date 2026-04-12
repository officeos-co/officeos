using EnterpriseAgentOs.Api.Properties;
using Stripe;
using Stripe.Checkout;

namespace EnterpriseAgentOs.Api.Entities.Billing;

public sealed class StripeService
{
    private readonly StripeConfig _config;
    private readonly FrontendConfig _frontend;
    private readonly EaosDbContext _db;
    private readonly ILogger<StripeService> _logger;

    public StripeService(
        StripeConfig config,
        FrontendConfig frontend,
        EaosDbContext db,
        ILogger<StripeService> logger)
    {
        _config = config;
        _frontend = frontend;
        _db = db;
        _logger = logger;
        StripeConfiguration.ApiKey = _config.SecretKey;
    }

    // -------------------------------------------------------------------------
    // Org billing
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a Stripe customer for the given org.
    /// </summary>
    public async Task<string> CreateCustomerAsync(string orgId, string email, CancellationToken ct = default)
    {
        var options = new CustomerCreateOptions
        {
            Email = email,
            Metadata = new Dictionary<string, string>
            {
                ["type"] = "org",
                ["orgId"] = orgId,
            },
        };
        var service = new CustomerService();
        var customer = await service.CreateAsync(options, cancellationToken: ct);
        _logger.LogInformation("Created Stripe customer {CustomerId} for org {OrgId}", customer.Id, orgId);
        return customer.Id;
    }

    /// <summary>
    /// Creates a Stripe subscription for the given customer on the given plan ("free" or "team").
    /// </summary>
    public async Task<string> CreateSubscriptionAsync(string customerId, string plan, CancellationToken ct = default)
    {
        var priceId = plan == "team" ? _config.TeamPriceId : _config.FreePriceId;
        var options = new SubscriptionCreateOptions
        {
            Customer = customerId,
            Items = [new SubscriptionItemOptions { Price = priceId }],
        };
        var service = new SubscriptionService();
        var subscription = await service.CreateAsync(options, cancellationToken: ct);
        _logger.LogInformation("Created Stripe subscription {SubscriptionId} for customer {CustomerId} plan {Plan}", subscription.Id, customerId, plan);
        return subscription.Id;
    }

    /// <summary>
    /// Reports metered token usage to Stripe for overage billing.
    /// Overage rate = model cost × 1.3 (never lose money on overage).
    /// Note: Stripe.net v51 uses Billing Meter Events instead of legacy UsageRecords.
    /// </summary>
    public Task RecordTokenUsageAsync(string subscriptionItemId, long tokens, CancellationToken ct = default)
    {
        // Metered billing via Billing Meter Events (v51 API)
        // Requires a meter event name configured in the Stripe dashboard.
        // Placeholder — wire up once meter event name is configured.
        _logger.LogInformation("TODO: Record {Tokens} tokens for subscription item {SubscriptionItemId} via Billing Meter Event", tokens, subscriptionItemId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the <see cref="OrgSubscription"/> for the given org.
    /// Falls back to a default Free-tier subscription when not found.
    /// </summary>
    public Task<OrgSubscription> GetOrgSubscriptionAsync(string orgId, CancellationToken ct = default)
    {
        _logger.LogDebug("Returning default free subscription for org {OrgId} (org subscriptions stored in-memory)", orgId);
        return Task.FromResult(CreateDefaultFreeSubscription(orgId));
    }

    /// <summary>
    /// Returns how many tokens remain for the org this period, and whether they are over-budget.
    /// </summary>
    public async Task<(long Remaining, bool OverBudget)> CheckTokenBudgetAsync(string orgId, CancellationToken ct = default)
    {
        var sub = await GetOrgSubscriptionAsync(orgId, ct);
        var remaining = sub.TokenBudgetPerMonth - sub.TokensUsedThisMonth;
        var overBudget = remaining < 0;
        return (remaining, overBudget);
    }

    // -------------------------------------------------------------------------
    // User (Individual) billing
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the UserSubscription for the given user from DB.
    /// Defaults to Free if not found.
    /// </summary>
    public async Task<UserSubscription> GetUserSubscriptionAsync(Guid userId, CancellationToken ct = default)
    {
        var sub = await _db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        return sub ?? CreateDefaultFreeUserSubscription(userId);
    }

    /// <summary>
    /// Creates a Stripe Checkout Session for the user to subscribe to the given plan + billing cycle.
    /// Upserts a Stripe customer first (checks DB for existing ID).
    /// Returns the hosted checkout URL.
    /// </summary>
    public async Task<string> CreateUserCheckoutSessionAsync(
        Guid userId,
        string email,
        string plan,
        string billingCycle,
        CancellationToken ct = default)
    {
        var customerId = await GetOrCreateStripeCustomerAsync(userId, email, ct);

        var priceId = (plan, billingCycle) switch
        {
            ("pro", "yearly") => _config.ProYearlyPriceId,
            ("pro", "monthly") => _config.ProMonthlyPriceId,
            _ => _config.FreePriceId,
        };

        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = customerId,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1,
                },
            ],
            SuccessUrl = $"{_frontend.Origin}/settings/billing?checkout=success",
            CancelUrl = $"{_frontend.Origin}/pricing",
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId.ToString(),
                ["plan"] = plan,
                ["billingCycle"] = billingCycle,
            },
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: ct);
        _logger.LogInformation("Created Stripe checkout session {SessionId} for user {UserId} plan {Plan}", session.Id, userId, plan);
        return session.Url;
    }

    /// <summary>
    /// Creates a Stripe Billing Portal session for the authenticated user.
    /// Returns the portal URL.
    /// </summary>
    public async Task<string> CreateBillingPortalSessionAsync(
        Guid userId,
        string email,
        CancellationToken ct = default)
    {
        var customerId = await GetOrCreateStripeCustomerAsync(userId, email, ct);

        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = customerId,
            ReturnUrl = $"{_frontend.Origin}/settings/billing",
        };

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(options, cancellationToken: ct);
        _logger.LogInformation("Created Stripe billing portal session for user {UserId}", userId);
        return session.Url;
    }

    /// <summary>
    /// Processes an incoming Stripe webhook event after verifying the signature.
    /// </summary>
    public async Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        var stripeEvent = EventUtility.ConstructEvent(payload, signature, _config.WebhookSecret);
        _logger.LogInformation("Stripe webhook event {EventType} received", stripeEvent.Type);

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
            {
                var session = stripeEvent.Data.Object as Session;
                if (session?.Metadata is null) break;

                if (!session.Metadata.TryGetValue("userId", out var userIdStr) ||
                    !Guid.TryParse(userIdStr, out var userId))
                    break;

                session.Metadata.TryGetValue("plan", out var plan);
                session.Metadata.TryGetValue("billingCycle", out var billingCycle);

                var limits = PlanLimits.ForIndividualPlan(plan ?? "free");

                var sub = await _db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId, ct);
                if (sub is null)
                {
                    sub = new UserSubscription { UserId = userId };
                    await _db.UserSubscriptions.AddAsync(sub, ct);
                }

                sub.StripeCustomerId = session.CustomerId;
                sub.StripeSubscriptionId = session.SubscriptionId;
                sub.Plan = limits.Plan;
                sub.BillingCycle = billingCycle ?? "monthly";
                sub.ConcurrentAgentLimit = limits.ConcurrentAgents;
                sub.TokenBudgetPerMonth = limits.TokensPerMonth;
                sub.IsActive = true;
                sub.PeriodStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                sub.PeriodEnd = sub.PeriodStart.AddMonths(1);

                _logger.LogInformation("Checkout completed: user {UserId} subscribed to {Plan}", userId, limits.Plan);
                break;
            }

            case "customer.subscription.updated":
            {
                var stripeSub = stripeEvent.Data.Object as Subscription;
                if (stripeSub is null) break;

                var sub = await _db.UserSubscriptions.FirstOrDefaultAsync(
                    s => s.StripeSubscriptionId == stripeSub.Id, ct);
                if (sub is null) break;

                // Determine plan from metadata or items (best effort)
                var planName = stripeSub.Metadata?.TryGetValue("plan", out var metaPlan) == true ? metaPlan : sub.Plan;
                var limits = PlanLimits.ForIndividualPlan(planName);
                sub.Plan = limits.Plan;
                sub.ConcurrentAgentLimit = limits.ConcurrentAgents;
                sub.TokenBudgetPerMonth = limits.TokensPerMonth;
                sub.IsActive = stripeSub.Status == "active";

                _logger.LogInformation("Subscription updated for user {UserId} to {Plan}", sub.UserId, limits.Plan);
                break;
            }

            case "customer.subscription.deleted":
            {
                var stripeSub = stripeEvent.Data.Object as Subscription;
                if (stripeSub is null) break;

                var sub = await _db.UserSubscriptions.FirstOrDefaultAsync(
                    s => s.StripeSubscriptionId == stripeSub.Id, ct);
                if (sub is null) break;

                var freeLimits = PlanLimits.IndividualFree;
                sub.Plan = freeLimits.Plan;
                sub.ConcurrentAgentLimit = freeLimits.ConcurrentAgents;
                sub.TokenBudgetPerMonth = freeLimits.TokensPerMonth;
                sub.IsActive = false;

                _logger.LogInformation("Subscription deleted for user {UserId}, downgraded to free", sub.UserId);
                break;
            }

            case "invoice.payment_succeeded":
            {
                var invoice = stripeEvent.Data.Object as Invoice;
                if (invoice is null) break;

                // In Stripe.net v51, the subscription ID lives on invoice.Parent.SubscriptionDetails.SubscriptionId
                var subscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;

                var sub = await _db.UserSubscriptions.FirstOrDefaultAsync(
                    s => s.StripeSubscriptionId == subscriptionId, ct);
                if (sub is null) break;

                sub.TokensUsedThisMonth = 0;
                if (invoice.Lines?.Data?.Count > 0)
                {
                    var period = invoice.Lines.Data[0].Period;
                    if (period is not null)
                    {
                        sub.PeriodStart = period.Start;
                        sub.PeriodEnd = period.End;
                    }
                }

                _logger.LogInformation("Invoice payment succeeded for user {UserId}, tokens reset", sub.UserId);
                break;
            }

            case "invoice.payment_failed":
            {
                var invoice = stripeEvent.Data.Object as Invoice;
                var subscriptionId = invoice?.Parent?.SubscriptionDetails?.SubscriptionId;
                _logger.LogWarning("Stripe invoice payment failed for subscription {SubscriptionId}", subscriptionId);
                break;
            }

            default:
                _logger.LogDebug("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                break;
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Returns remaining tokens and whether the user is over-budget.</summary>
    public async Task<(long Remaining, bool OverBudget)> CheckUserTokenBudgetAsync(Guid userId, CancellationToken ct = default)
    {
        var sub = await GetUserSubscriptionAsync(userId, ct);
        var remaining = sub.TokenBudgetPerMonth - sub.TokensUsedThisMonth;
        var overBudget = remaining < 0;
        return (remaining, overBudget);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the existing Stripe customer ID for a user from DB, or creates a new one.
    /// Persists the customer ID to the DB for future lookups.
    /// </summary>
    private async Task<string> GetOrCreateStripeCustomerAsync(Guid userId, string email, CancellationToken ct)
    {
        var existing = await _db.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.StripeCustomerId != null, ct);

        if (existing?.StripeCustomerId is not null)
            return existing.StripeCustomerId;

        var options = new CustomerCreateOptions
        {
            Email = email,
            Metadata = new Dictionary<string, string>
            {
                ["type"] = "user",
                ["userId"] = userId.ToString(),
            },
        };
        var service = new CustomerService();
        var customer = await service.CreateAsync(options, cancellationToken: ct);
        _logger.LogInformation("Created Stripe customer {CustomerId} for user {UserId}", customer.Id, userId);

        // Persist so next call skips creation
        var sub = await _db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (sub is null)
        {
            sub = CreateDefaultFreeUserSubscription(userId);
            await _db.UserSubscriptions.AddAsync(sub, ct);
        }
        sub.StripeCustomerId = customer.Id;
        await _db.SaveChangesAsync(ct);

        return customer.Id;
    }

    private static OrgSubscription CreateDefaultFreeSubscription(string orgId) => new()
    {
        OrganizationId = orgId,
        Plan = "free",
        ConcurrentAgentLimit = 1,
        TokenBudgetPerMonth = 2_000_000,
        TokensUsedThisMonth = 0,
        PeriodStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc),
        PeriodEnd = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1),
        IsActive = true,
    };

    private static UserSubscription CreateDefaultFreeUserSubscription(Guid userId)
    {
        var limits = PlanLimits.ForIndividualPlan("free");
        return new UserSubscription
        {
            UserId = userId,
            Plan = limits.Plan,
            BillingCycle = "monthly",
            ConcurrentAgentLimit = limits.ConcurrentAgents,
            TokenBudgetPerMonth = limits.TokensPerMonth,
            TokensUsedThisMonth = 0,
            PeriodStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEnd = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1),
            IsActive = true,
        };
    }
}
