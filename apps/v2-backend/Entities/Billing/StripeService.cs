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

    public Task<OrgSubscription> GetOrgSubscriptionAsync(string orgId, CancellationToken ct = default)
    {
        _logger.LogDebug("Returning default free subscription for org {OrgId} (org subscriptions stored in-memory)", orgId);
        return Task.FromResult(CreateDefaultFreeSubscription(orgId));
    }

    public async Task<(long Remaining, bool OverBudget)> CheckCreditBudgetAsync(string orgId, CancellationToken ct = default)
    {
        var sub = await GetOrgSubscriptionAsync(orgId, ct);
        var remaining = sub.CreditBudgetPerMonth - sub.CreditsUsedThisMonth;
        return (remaining, remaining < 0);
    }

    // -------------------------------------------------------------------------
    // User (Individual) billing
    // -------------------------------------------------------------------------

    public async Task<UserSubscription> GetUserSubscriptionAsync(Guid userId, CancellationToken ct = default)
    {
        var sub = await _db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        return sub ?? CreateDefaultFreeUserSubscription(userId);
    }

    public async Task<(long Remaining, bool OverBudget)> CheckUserCreditBudgetAsync(Guid userId, CancellationToken ct = default)
    {
        var sub = await GetUserSubscriptionAsync(userId, ct);
        var remaining = sub.CreditBudgetPerMonth - sub.CreditsUsedThisMonth;
        return (remaining, remaining < 0);
    }

    /// <summary>
    /// Called by the LLM proxy after each completion. Converts raw tokens to normalized
    /// credits via <see cref="ModelCostWeights"/>, increments the owner's monthly counter,
    /// and fires a Stripe Billing Meter Event if overage is enabled and the budget is exceeded.
    /// Never throws — billing failures must not block agent execution.
    /// </summary>
    public async Task RecordCreditUsageAsync(Guid agentId, string model, long rawTokens, CancellationToken ct = default)
    {
        var agent = await _db.Agents.FirstOrDefaultAsync(a => a.Id == agentId, ct);
        if (agent?.OwnerId is null) return;

        var credits = ModelCostWeights.ToCredits(model, rawTokens);
        var sub = await _db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == agent.OwnerId.Value, ct);
        if (sub is null) return;

        sub.CreditsUsedThisMonth += credits;
        await _db.SaveChangesAsync(ct);

        _logger.LogDebug(
            "Agent {AgentId} used {Credits} credits ({RawTokens} raw tokens on {Model}). " +
            "User {UserId}: {Used}/{Budget} credits this month.",
            agentId, credits, rawTokens, model, agent.OwnerId, sub.CreditsUsedThisMonth, sub.CreditBudgetPerMonth);

        // Fire Stripe meter event for overage if enabled and over budget
        if (sub.OverageEnabled
            && sub.StripeOverageItemId is not null
            && sub.StripeCustomerId is not null
            && sub.CreditsUsedThisMonth > sub.CreditBudgetPerMonth)
        {
            var overageCredits = sub.CreditsUsedThisMonth - sub.CreditBudgetPerMonth;
            var meterEventName = sub.Plan == "pro" ? "pro_credits_used" : "free_credits_used";
            await FireMeterEventAsync(meterEventName, sub.StripeCustomerId, overageCredits, ct);
        }
    }

    /// <summary>
    /// Enables or disables pay-as-you-go overage billing for an individual user.
    /// When enabling, attaches a metered subscription item to the user's Stripe subscription
    /// (creating a free subscription first if the user has no paid plan).
    /// When disabling, deletes the subscription item.
    /// </summary>
    public async Task EnableUserOverageAsync(Guid userId, string email, bool enabled, CancellationToken ct = default)
    {
        var sub = await _db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (sub is null)
        {
            sub = CreateDefaultFreeUserSubscription(userId);
            await _db.UserSubscriptions.AddAsync(sub, ct);
        }

        if (enabled)
        {
            if (sub.OverageEnabled) return; // already enabled

            var customerId = await GetOrCreateStripeCustomerAsync(userId, email, ct);
            var overagePriceId = sub.Plan == "pro" ? _config.ProOveragePriceId : _config.FreeOveragePriceId;

            string subscriptionId;
            if (sub.StripeSubscriptionId is not null)
            {
                // Add overage item to existing subscription
                subscriptionId = sub.StripeSubscriptionId;
            }
            else
            {
                // Free user with no subscription yet — create one with just the metered overage price
                var subOptions = new SubscriptionCreateOptions
                {
                    Customer = customerId,
                    Items = [new SubscriptionItemOptions { Price = overagePriceId }],
                };
                var subService = new SubscriptionService();
                var newSub = await subService.CreateAsync(subOptions, cancellationToken: ct);
                sub.StripeSubscriptionId = newSub.Id;

                // The overage item is the first (and only) item on this subscription
                sub.StripeOverageItemId = newSub.Items.Data[0].Id;
                sub.StripeCustomerId = customerId;
                sub.OverageEnabled = true;
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Overage enabled for user {UserId} (new subscription {SubId})", userId, newSub.Id);
                return;
            }

            // Add overage item to existing subscription
            var siOptions = new SubscriptionItemCreateOptions
            {
                Subscription = subscriptionId,
                Price = overagePriceId,
            };
            var siService = new SubscriptionItemService();
            var item = await siService.CreateAsync(siOptions, cancellationToken: ct);

            sub.StripeOverageItemId = item.Id;
            sub.StripeCustomerId = customerId;
            sub.OverageEnabled = true;
            _logger.LogInformation("Overage enabled for user {UserId}, subscription item {ItemId}", userId, item.Id);
        }
        else
        {
            if (!sub.OverageEnabled || sub.StripeOverageItemId is null) return;

            var siService = new SubscriptionItemService();
            await siService.DeleteAsync(
                sub.StripeOverageItemId,
                new SubscriptionItemDeleteOptions { ClearUsage = true },
                cancellationToken: ct);

            sub.StripeOverageItemId = null;
            sub.OverageEnabled = false;
            _logger.LogInformation("Overage disabled for user {UserId}", userId);
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Enables or disables pay-as-you-go overage billing for an org (Team plan).
    /// </summary>
    public async Task EnableOrgOverageAsync(string orgId, string email, bool enabled, CancellationToken ct = default)
    {
        var sub = await _db.OrgSubscriptions.FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct);
        if (sub is null)
        {
            sub = CreateDefaultFreeSubscription(orgId);
            await _db.OrgSubscriptions.AddAsync(sub, ct);
        }

        if (enabled)
        {
            if (sub.OverageEnabled) return;

            var options = new CustomerCreateOptions
            {
                Email = email,
                Metadata = new Dictionary<string, string> { ["type"] = "org", ["orgId"] = orgId },
            };
            var customerSvc = new CustomerService();
            var customer = sub.StripeCustomerId is not null
                ? null
                : await customerSvc.CreateAsync(options, cancellationToken: ct);

            var customerId = sub.StripeCustomerId ?? customer!.Id;

            if (sub.StripeSubscriptionId is not null)
            {
                var siOptions = new SubscriptionItemCreateOptions
                {
                    Subscription = sub.StripeSubscriptionId,
                    Price = _config.TeamOveragePriceId,
                };
                var siService = new SubscriptionItemService();
                var item = await siService.CreateAsync(siOptions, cancellationToken: ct);
                sub.StripeOverageItemId = item.Id;
            }
            else
            {
                var subOptions = new SubscriptionCreateOptions
                {
                    Customer = customerId,
                    Items = [new SubscriptionItemOptions { Price = _config.TeamOveragePriceId }],
                };
                var subService = new SubscriptionService();
                var newSub = await subService.CreateAsync(subOptions, cancellationToken: ct);
                sub.StripeSubscriptionId = newSub.Id;
                sub.StripeOverageItemId = newSub.Items.Data[0].Id;
            }

            sub.StripeCustomerId = customerId;
            sub.OverageEnabled = true;
            _logger.LogInformation("Overage enabled for org {OrgId}", orgId);
        }
        else
        {
            if (!sub.OverageEnabled || sub.StripeOverageItemId is null) return;

            var siService = new SubscriptionItemService();
            await siService.DeleteAsync(
                sub.StripeOverageItemId,
                new SubscriptionItemDeleteOptions { ClearUsage = true },
                cancellationToken: ct);

            sub.StripeOverageItemId = null;
            sub.OverageEnabled = false;
            _logger.LogInformation("Overage disabled for org {OrgId}", orgId);
        }

        await _db.SaveChangesAsync(ct);
    }

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
            ("pro", "yearly")  => _config.ProYearlyPriceId,
            ("pro", "monthly") => _config.ProMonthlyPriceId,
            _                  => _config.FreePriceId,
        };

        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = customerId,
            LineItems =
            [
                new SessionLineItemOptions { Price = priceId, Quantity = 1 },
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
                sub.CreditBudgetPerMonth = limits.CreditsPerMonth;
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

                var planName = stripeSub.Metadata?.TryGetValue("plan", out var metaPlan) == true ? metaPlan : sub.Plan;
                var limits = PlanLimits.ForIndividualPlan(planName);
                sub.Plan = limits.Plan;
                sub.ConcurrentAgentLimit = limits.ConcurrentAgents;
                sub.CreditBudgetPerMonth = limits.CreditsPerMonth;
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
                sub.CreditBudgetPerMonth = freeLimits.CreditsPerMonth;
                sub.StripeOverageItemId = null;
                sub.OverageEnabled = false;
                sub.IsActive = false;

                _logger.LogInformation("Subscription deleted for user {UserId}, downgraded to free", sub.UserId);
                break;
            }

            case "invoice.payment_succeeded":
            {
                var invoice = stripeEvent.Data.Object as Invoice;
                if (invoice is null) break;

                var subscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;

                var sub = await _db.UserSubscriptions.FirstOrDefaultAsync(
                    s => s.StripeSubscriptionId == subscriptionId, ct);
                if (sub is null) break;

                sub.CreditsUsedThisMonth = 0;
                if (invoice.Lines?.Data?.Count > 0)
                {
                    var period = invoice.Lines.Data[0].Period;
                    if (period is not null)
                    {
                        sub.PeriodStart = period.Start;
                        sub.PeriodEnd = period.End;
                    }
                }

                _logger.LogInformation("Invoice payment succeeded for user {UserId}, credits reset", sub.UserId);
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

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

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

    private async Task FireMeterEventAsync(string eventName, string customerId, long credits, CancellationToken ct)
    {
        var client = new StripeClient(_config.SecretKey);
        await client.V2.Billing.MeterEvents.CreateAsync(
            new Stripe.V2.Billing.MeterEventCreateOptions
            {
                EventName = eventName,
                Payload = new Dictionary<string, string>
                {
                    ["stripe_customer_id"] = customerId,
                    ["value"] = credits.ToString(),
                },
            },
            cancellationToken: ct);

        _logger.LogInformation(
            "Fired Stripe meter event {EventName} for customer {CustomerId}: {Credits} overage credits",
            eventName, customerId, credits);
    }

    private static OrgSubscription CreateDefaultFreeSubscription(string orgId) => new()
    {
        OrganizationId = orgId,
        Plan = "free",
        ConcurrentAgentLimit = PlanLimits.OrgFree.ConcurrentAgents,
        CreditBudgetPerMonth = PlanLimits.OrgFree.CreditsPerMonth,
        CreditsUsedThisMonth = 0,
        PeriodStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc),
        PeriodEnd = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1),
        IsActive = true,
    };

    private static UserSubscription CreateDefaultFreeUserSubscription(Guid userId)
    {
        var limits = PlanLimits.IndividualFree;
        return new UserSubscription
        {
            UserId = userId,
            Plan = limits.Plan,
            BillingCycle = "monthly",
            ConcurrentAgentLimit = limits.ConcurrentAgents,
            CreditBudgetPerMonth = limits.CreditsPerMonth,
            CreditsUsedThisMonth = 0,
            PeriodStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEnd = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1),
            IsActive = true,
        };
    }
}
