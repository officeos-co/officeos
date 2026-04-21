// Dashboard mutation set — add more from Entities/Billing/BillingController.cs as dashboard needs them

namespace EnterpriseAgentOs.Api.GraphQL;

[ExtendObjectType(typeof(GraphQLMutations))]
public class BillingMutations
{
    private static void InvalidateBillingCache(IMemoryCache cache, Guid userId)
    {
        cache.Remove($"billing:dashboard:{userId}");
        cache.Remove($"billing:sub:{userId}");
    }

    public async Task<SubscribeResultDto> SubscribeUser(
        string plan,
        string billingCycle,
        IResolverContext context,
        [Service] IUserBillingService userBilling,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        if (plan != "free" && plan != "pro")
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("plan must be 'free' or 'pro'")
                    .SetCode("BAD_INPUT")
                    .Build());
        }
        if (billingCycle != "monthly" && billingCycle != "yearly")
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("billingCycle must be 'monthly' or 'yearly'")
                    .SetCode("BAD_INPUT")
                    .Build());
        }

        var checkoutUrl = await userBilling.CreateCheckoutSessionAsync(
            user.Id, user.Email, plan, billingCycle, ct);
        InvalidateBillingCache(cache, user.Id);
        return new SubscribeResultDto(checkoutUrl);
    }

    public async Task<UserSubscriptionDto> CancelUserSubscription(
        IResolverContext context,
        [Service] IUserBillingService userBilling,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        // IUserBillingService has no dedicated cancel method; Stripe portal handles
        // cancellation via CreatePortalSessionAsync. For dashboard parity we expose a
        // mutation that disables overage and returns the current subscription.
        // TODO: add IUserBillingService.CancelAsync when product decides on flow
        // (Entities/Billing/services/IUserBillingService.cs).
        await userBilling.EnableOverageAsync(user.Id, user.Email, false, ct);
        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        InvalidateBillingCache(cache, user.Id);
        return new UserSubscriptionDto(
            sub.Id, sub.UserId, sub.Plan, sub.BillingCycle,
            sub.ConcurrentAgentLimit, sub.CreditBudgetPerMonth,
            sub.CreditsUsedThisMonth, remaining, overBudget,
            sub.OverageEnabled, sub.PeriodStart, sub.PeriodEnd, sub.IsActive);
    }

    /// <summary>
    /// Turns extra-usage (Stripe metered overage) on or off. Replaces the
    /// previous "auto-reload" card in the billing UI with a single boolean
    /// toggle — when on, usage above the credit budget is billed metered.
    /// </summary>
    public async Task<bool> SetExtraUsageEnabled(
        bool enabled,
        IResolverContext context,
        [Service] IUserBillingService userBilling,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        await userBilling.EnableOverageAsync(user.Id, user.Email, enabled, ct);
        InvalidateBillingCache(cache, user.Id);
        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        return sub.OverageEnabled;
    }

    [Obsolete("Use setExtraUsageEnabled. Kept for backwards compatibility.")]
    public async Task<UserSubscriptionDto> ToggleOverage(
        bool enabled,
        IResolverContext context,
        [Service] IUserBillingService userBilling,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        await userBilling.EnableOverageAsync(user.Id, user.Email, enabled, ct);
        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        InvalidateBillingCache(cache, user.Id);
        return new UserSubscriptionDto(
            sub.Id, sub.UserId, sub.Plan, sub.BillingCycle,
            sub.ConcurrentAgentLimit, sub.CreditBudgetPerMonth,
            sub.CreditsUsedThisMonth, remaining, overBudget,
            sub.OverageEnabled, sub.PeriodStart, sub.PeriodEnd, sub.IsActive);
    }
}
