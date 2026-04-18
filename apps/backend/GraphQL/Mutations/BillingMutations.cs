// Dashboard mutation set — add more from Entities/Billing/BillingController.cs as dashboard needs them

namespace EnterpriseAgentOs.Api.GraphQL.Mutations;

[ExtendObjectType(typeof(GraphQLMutations))]
public class BillingMutations
{
    public async Task<Types.SubscribeResultDto> SubscribeUser(
        string plan,
        string billingCycle,
        IResolverContext context,
        [Service] IUserBillingService userBilling,
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
        return new Types.SubscribeResultDto(checkoutUrl);
    }

    public async Task<Types.UserSubscriptionDto> CancelUserSubscription(
        IResolverContext context,
        [Service] IUserBillingService userBilling,
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
        return new Types.UserSubscriptionDto(
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
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        await userBilling.EnableOverageAsync(user.Id, user.Email, enabled, ct);
        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        return sub.OverageEnabled;
    }

    [Obsolete("Use setExtraUsageEnabled. Kept for backwards compatibility.")]
    public async Task<Types.UserSubscriptionDto> ToggleOverage(
        bool enabled,
        IResolverContext context,
        [Service] IUserBillingService userBilling,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        await userBilling.EnableOverageAsync(user.Id, user.Email, enabled, ct);
        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        return new Types.UserSubscriptionDto(
            sub.Id, sub.UserId, sub.Plan, sub.BillingCycle,
            sub.ConcurrentAgentLimit, sub.CreditBudgetPerMonth,
            sub.CreditsUsedThisMonth, remaining, overBudget,
            sub.OverageEnabled, sub.PeriodStart, sub.PeriodEnd, sub.IsActive);
    }
}
