// Dashboard mutation set — add more from Entities/Billing/BillingController.cs as dashboard needs them

namespace EnterpriseAgentOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLMutations))]
public class BillingMutations
{
    private static async Task InvalidateBillingCacheAsync(IDistributedCache cache, Guid userId, CancellationToken ct)
    {
        await cache.RemoveAsync($"billing:dashboard:{userId}", ct);
        await cache.RemoveAsync($"billing:sub:{userId}", ct);
    }

    private static GraphQLException BillingProviderGraphQlException(BillingProviderException ex) =>
        new(ErrorBuilder.New()
            .SetMessage(ex.Message)
            .SetCode("BILLING_PROVIDER_ERROR")
            .Build());

    [GraphQLDescription("Initiates a Stripe Checkout session for the given plan (free or pro) and billing cycle (monthly or yearly). Returns the checkout URL.")]
    public async Task<SubscribeResultPayload> SubscribeUser(
        string plan,
        string billingCycle,
        [Service] UserContext user,
        [Service] IUserBillingService userBilling,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        SubscriptionPlan planEnum;
        try { planEnum = plan.ToSubscriptionPlan(); }
        catch (ArgumentOutOfRangeException)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("plan must be 'free' or 'pro'")
                    .SetCode("BAD_INPUT")
                    .Build());
        }
        if (planEnum != SubscriptionPlan.Free && planEnum != SubscriptionPlan.Pro)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("plan must be 'free' or 'pro'")
                    .SetCode("BAD_INPUT")
                    .Build());
        }
        BillingCycle cycleEnum;
        try { cycleEnum = billingCycle.ToBillingCycle(); }
        catch (ArgumentOutOfRangeException)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("billingCycle must be 'monthly' or 'yearly'")
                    .SetCode("BAD_INPUT")
                    .Build());
        }

        var checkoutUrl = await userBilling.CreateCheckoutSessionAsync(
            user.Id, user.Email, plan, billingCycle, ct);
        await InvalidateBillingCacheAsync(cache, user.Id, ct);
        return new SubscribeResultPayload(checkoutUrl);
    }

    [GraphQLDescription("Cancels the user's subscription by disabling overage. Returns the updated subscription state.")]
    public async Task<UserSubscriptionPayload> CancelUserSubscription(
        [Service] UserContext user,
        [Service] IUserBillingService userBilling,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            await userBilling.CancelSubscriptionAsync(user.Id, user.Email, ct);
        }
        catch (BillingProviderException ex)
        {
            throw BillingProviderGraphQlException(ex);
        }

        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        await InvalidateBillingCacheAsync(cache, user.Id, ct);
        return new UserSubscriptionPayload(
            sub.Id, sub.UserId, sub.Plan.ToStorageString(), sub.BillingCycle.ToStorageString(),
            sub.ConcurrentAgentLimit, sub.CreditBudgetPerMonth,
            sub.CreditsUsedThisMonth, remaining, overBudget,
            sub.OverageEnabled, sub.Period.Start, sub.Period.End, sub.IsActive);
    }

    /// <summary>
    /// Turns extra-usage (Stripe metered overage) on or off. Replaces the
    /// previous "auto-reload" card in the billing UI with a single boolean
    /// toggle — when on, usage above the credit budget is billed metered.
    /// </summary>
    [GraphQLDescription("Turns extra-usage (Stripe metered overage) on or off. When enabled, usage above the credit budget is billed metered.")]
    public async Task<bool> SetExtraUsageEnabled(
        bool enabled,
        [Service] UserContext user,
        [Service] IUserBillingService userBilling,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            await userBilling.EnableOverageAsync(user.Id, user.Email, enabled, ct);
        }
        catch (BillingProviderException ex)
        {
            throw BillingProviderGraphQlException(ex);
        }

        await InvalidateBillingCacheAsync(cache, user.Id, ct);
        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        return sub.OverageEnabled;
    }

    [GraphQLDescription("Deprecated: use setExtraUsageEnabled. Toggles overage and returns full subscription state.")]
    [Obsolete("Use setExtraUsageEnabled. Kept for backwards compatibility.")]
    public async Task<UserSubscriptionPayload> ToggleOverage(
        bool enabled,
        [Service] UserContext user,
        [Service] IUserBillingService userBilling,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            await userBilling.EnableOverageAsync(user.Id, user.Email, enabled, ct);
        }
        catch (BillingProviderException ex)
        {
            throw BillingProviderGraphQlException(ex);
        }

        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        await InvalidateBillingCacheAsync(cache, user.Id, ct);
        return new UserSubscriptionPayload(
            sub.Id, sub.UserId, sub.Plan.ToStorageString(), sub.BillingCycle.ToStorageString(),
            sub.ConcurrentAgentLimit, sub.CreditBudgetPerMonth,
            sub.CreditsUsedThisMonth, remaining, overBudget,
            sub.OverageEnabled, sub.Period.Start, sub.Period.End, sub.IsActive);
    }
}
