using EnterpriseAgentOs.Api.Properties;

namespace EnterpriseAgentOs.Api.Entities.Billing;

/// <summary>
/// Stripe billing service stub.
/// All methods are placeholders — real Stripe SDK calls will replace these TODOs
/// once the Stripe NuGet package is added and keys are configured.
/// </summary>
public sealed class StripeService
{
    private readonly StripeConfig _config;
    private readonly ILogger<StripeService> _logger;

    public StripeService(StripeConfig config, ILogger<StripeService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Creates a Stripe customer for the given org.
    /// </summary>
    /// <returns>Stripe customer ID placeholder.</returns>
    /// <remarks>TODO: Replace with Stripe SDK CustomerService.CreateAsync()</remarks>
    public Task<string> CreateCustomerAsync(string orgId, string email, CancellationToken ct = default)
    {
        _logger.LogInformation("TODO: Create Stripe customer for org {OrgId} email {Email}", orgId, email);
        // TODO: var options = new CustomerCreateOptions { Email = email, Metadata = new Dictionary<string, string> { ["orgId"] = orgId } };
        // TODO: var service = new CustomerService(); return await service.CreateAsync(options, cancellationToken: ct);
        return Task.FromResult($"cus_placeholder_{orgId}");
    }

    /// <summary>
    /// Creates a Stripe subscription for the given customer on the given plan ("free" or "team").
    /// </summary>
    /// <returns>Stripe subscription ID placeholder.</returns>
    /// <remarks>TODO: Replace with Stripe SDK SubscriptionService.CreateAsync()</remarks>
    public Task<string> CreateSubscriptionAsync(string customerId, string plan, CancellationToken ct = default)
    {
        var priceId = plan == "team" ? _config.TeamPriceId : _config.FreePriceId;
        _logger.LogInformation("TODO: Create Stripe subscription for customer {CustomerId} plan {Plan} priceId {PriceId}", customerId, plan, priceId);
        // TODO: var options = new SubscriptionCreateOptions { Customer = customerId, Items = [ new SubscriptionItemOptions { Price = priceId } ] };
        // TODO: var service = new SubscriptionService(); return (await service.CreateAsync(options, cancellationToken: ct)).Id;
        return Task.FromResult($"sub_placeholder_{customerId}_{plan}");
    }

    /// <summary>
    /// Reports metered token usage to Stripe for overage billing.
    /// Overage rate = model cost × 1.3 (never lose money on overage).
    /// </summary>
    /// <remarks>TODO: Replace with Stripe SDK UsageRecordService.CreateAsync()</remarks>
    public Task RecordTokenUsageAsync(string subscriptionId, long tokens, CancellationToken ct = default)
    {
        _logger.LogInformation("TODO: Record {Tokens} tokens for subscription {SubscriptionId}", tokens, subscriptionId);
        // TODO: Find the metered subscription item for TeamOveragePriceId, then:
        // TODO: var options = new UsageRecordCreateOptions { Quantity = tokens, Action = "increment" };
        // TODO: var service = new UsageRecordService(); await service.CreateAsync(subscriptionItemId, options, cancellationToken: ct);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the <see cref="OrgSubscription"/> for the given org.
    /// Falls back to a default Free-tier subscription when billing is not yet set up.
    /// </summary>
    /// <remarks>TODO: Persist OrgSubscription in EF Core and load from DB.</remarks>
    public Task<OrgSubscription> GetOrgSubscriptionAsync(string orgId, CancellationToken ct = default)
    {
        _logger.LogDebug("TODO: Load subscription for org {OrgId} from database", orgId);
        // TODO: return await _db.OrgSubscriptions.FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct)
        //       ?? CreateDefaultFreeSubscription(orgId);
        return Task.FromResult(CreateDefaultFreeSubscription(orgId));
    }

    /// <summary>
    /// Returns how many tokens remain for the org this period, and whether they are over-budget.
    /// Over-budget does NOT hard-cut — it triggers Stripe metered billing (Cursor model).
    /// </summary>
    public async Task<(long Remaining, bool OverBudget)> CheckTokenBudgetAsync(string orgId, CancellationToken ct = default)
    {
        var sub = await GetOrgSubscriptionAsync(orgId, ct);
        var remaining = sub.TokenBudgetPerMonth - sub.TokensUsedThisMonth;
        var overBudget = remaining < 0;
        return (remaining, overBudget);
    }

    /// <summary>
    /// Processes an incoming Stripe webhook event after verifying the signature.
    /// </summary>
    /// <remarks>
    /// TODO: Use Stripe SDK EventUtility.ConstructEvent() to verify signature, then dispatch
    ///       on event.Type: "customer.subscription.updated", "invoice.payment_succeeded", etc.
    /// </remarks>
    public Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        _logger.LogInformation("TODO: Verify Stripe webhook signature and dispatch event");
        // TODO: var stripeEvent = EventUtility.ConstructEvent(payload, signature, _config.WebhookSecret);
        // TODO: switch (stripeEvent.Type) { case "customer.subscription.updated": ... }
        return Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // User subscription methods (Individual Free / Pro track)
    // -------------------------------------------------------------------------

    /// <summary>Returns the UserSubscription for the given user. Defaults to Free if not found.</summary>
    /// <remarks>TODO: Load from EF Core DB: _db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId, ct)</remarks>
    public Task<UserSubscription> GetUserSubscriptionAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogDebug("TODO: Load user subscription for user {UserId} from database", userId);
        // TODO: return await _db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId, ct)
        //       ?? CreateDefaultFreeUserSubscription(userId);
        return Task.FromResult(CreateDefaultFreeUserSubscription(userId));
    }

    /// <summary>Creates or upgrades a user subscription to the given plan + billing cycle.</summary>
    /// <returns>Stripe checkout URL or placeholder subscription ID.</returns>
    /// <remarks>TODO: Replace with Stripe SDK SubscriptionService.CreateAsync() for the right price ID.</remarks>
    public Task<string> CreateUserSubscriptionAsync(Guid userId, string email, string plan, string billingCycle, CancellationToken ct = default)
    {
        var priceId = (plan, billingCycle) switch
        {
            ("pro", "yearly")  => _config.ProYearlyPriceId,
            ("pro", "monthly") => _config.ProMonthlyPriceId,
            _                  => _config.FreePriceId,
        };

        _logger.LogInformation(
            "TODO: Create user subscription for user {UserId} email {Email} plan {Plan} billingCycle {BillingCycle} priceId {PriceId}",
            userId, email, plan, billingCycle, priceId);

        // TODO: var customerId = await CreateCustomerAsync(userId.ToString(), email, ct);
        // TODO: var options = new SubscriptionCreateOptions { Customer = customerId, Items = [ new SubscriptionItemOptions { Price = priceId } ] };
        // TODO: var service = new SubscriptionService(); return (await service.CreateAsync(options, cancellationToken: ct)).Id;
        return Task.FromResult($"sub_user_placeholder_{userId}_{plan}_{billingCycle}");
    }

    /// <summary>Returns remaining tokens and whether the user is over-budget (triggers overage billing).</summary>
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
