namespace EnterpriseAgentOs.Infrastructure.Features.Billing;

internal sealed class StripeWebhookService : IStripeWebhookService
{
    private readonly StripeConfig _stripeConfig;
    private readonly EaosDbContext _eaosDbContext;
    private readonly ILogger<StripeWebhookService> _logger;

    public StripeWebhookService(StripeConfig config, EaosDbContext db, ILogger<StripeWebhookService> logger)
    {
        _stripeConfig = config;
        _eaosDbContext = db;
        _logger = logger;
    }

    public async Task HandleAsync(string payload, string signature, CancellationToken ct = default)
    {
        var stripeEvent = EventUtility.ConstructEvent(payload, signature, _stripeConfig.WebhookSecret);
        _logger.LogInformation("Stripe webhook event {EventType} received", stripeEvent.Type);

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                await OnCheckoutCompletedAsync(stripeEvent, ct);
                break;
            case "customer.subscription.updated":
                await OnSubscriptionUpdatedAsync(stripeEvent, ct);
                break;
            case "customer.subscription.deleted":
                await OnSubscriptionDeletedAsync(stripeEvent, ct);
                break;
            case "invoice.payment_succeeded":
                await OnInvoiceSucceededAsync(stripeEvent, ct);
                break;
            case "invoice.payment_failed":
                OnInvoiceFailed(stripeEvent);
                break;
            default:
                _logger.LogDebug("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                break;
        }

        await _eaosDbContext.SaveChangesAsync(ct);
    }

    private async Task OnCheckoutCompletedAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Session session || session.Metadata is null) return;

        if (!session.Metadata.TryGetValue("userId", out var userIdStr) ||
            !Guid.TryParse(userIdStr, out var userId))
            return;

        session.Metadata.TryGetValue("plan", out var plan);
        session.Metadata.TryGetValue("billingCycle", out var billingCycle);

        var limits = PlanLimits.ForIndividualPlan(plan ?? "free");

        var sub = await _eaosDbContext.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (sub is null)
        {
            sub = new UserSubscriptionEntity { UserId = userId };
            await _eaosDbContext.UserSubscriptions.AddAsync(sub, ct);
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
    }

    private async Task OnSubscriptionUpdatedAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Subscription stripeSub) return;

        var sub = await _eaosDbContext.UserSubscriptions.FirstOrDefaultAsync(
            s => s.StripeSubscriptionId == stripeSub.Id, ct);
        if (sub is null) return;

        var planName = stripeSub.Metadata?.TryGetValue("plan", out var metaPlan) == true ? metaPlan : sub.Plan;
        var limits = PlanLimits.ForIndividualPlan(planName);
        sub.Plan = limits.Plan;
        sub.ConcurrentAgentLimit = limits.ConcurrentAgents;
        sub.CreditBudgetPerMonth = limits.CreditsPerMonth;
        sub.IsActive = stripeSub.Status == "active";

        _logger.LogInformation("Subscription updated for user {UserId} to {Plan}", sub.UserId, limits.Plan);
    }

    private async Task OnSubscriptionDeletedAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Subscription stripeSub) return;

        var sub = await _eaosDbContext.UserSubscriptions.FirstOrDefaultAsync(
            s => s.StripeSubscriptionId == stripeSub.Id, ct);
        if (sub is null) return;

        var free = PlanLimits.IndividualFree;
        sub.Plan = free.Plan;
        sub.ConcurrentAgentLimit = free.ConcurrentAgents;
        sub.CreditBudgetPerMonth = free.CreditsPerMonth;
        sub.StripeOverageItemId = null;
        sub.OverageEnabled = false;
        sub.IsActive = false;

        _logger.LogInformation("Subscription deleted for user {UserId}, downgraded to free", sub.UserId);
    }

    private async Task OnInvoiceSucceededAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Invoice invoice) return;

        var subscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;
        var sub = await _eaosDbContext.UserSubscriptions.FirstOrDefaultAsync(
            s => s.StripeSubscriptionId == subscriptionId, ct);
        if (sub is null) return;

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
    }

    private void OnInvoiceFailed(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is not Invoice invoice) return;
        var subscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;
        _logger.LogWarning("Stripe invoice payment failed for subscription {SubscriptionId}", subscriptionId);
    }
}
