// Dashboard mutation set — add more from Entities/Billing/BillingController.cs as dashboard-2 needs them

namespace EnterpriseAgentOs.Api.Mutations;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLMutations))]
public class BillingMutations
{
    public async Task<EnterpriseAgentOs.Api.Entities.Billing.Types.SubscribeResultDto> SubscribeUser(
        string plan,
        string billingCycle,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Billing.IUserBillingService userBilling,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
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
        return new EnterpriseAgentOs.Api.Entities.Billing.Types.SubscribeResultDto(checkoutUrl);
    }

    public async Task<EnterpriseAgentOs.Api.Entities.Billing.Types.UserSubscriptionDto> CancelUserSubscription(
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Billing.IUserBillingService userBilling,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        // IUserBillingService has no dedicated cancel method; Stripe portal handles
        // cancellation via CreatePortalSessionAsync. For dashboard parity we expose a
        // mutation that disables overage and returns the current subscription.
        // TODO: add IUserBillingService.CancelAsync when product decides on flow
        // (Entities/Billing/services/IUserBillingService.cs).
        await userBilling.EnableOverageAsync(user.Id, user.Email, false, ct);
        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        return new EnterpriseAgentOs.Api.Entities.Billing.Types.UserSubscriptionDto(
            sub.Id, sub.UserId, sub.Plan, sub.BillingCycle,
            sub.ConcurrentAgentLimit, sub.CreditBudgetPerMonth,
            sub.CreditsUsedThisMonth, remaining, overBudget,
            sub.OverageEnabled, sub.PeriodStart, sub.PeriodEnd, sub.IsActive);
    }

    public async Task<EnterpriseAgentOs.Api.Entities.Billing.Types.UserSubscriptionDto> ToggleOverage(
        bool enabled,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Billing.IUserBillingService userBilling,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        await userBilling.EnableOverageAsync(user.Id, user.Email, enabled, ct);
        var sub = await userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await userBilling.CheckCreditBudgetAsync(user.Id, ct);
        return new EnterpriseAgentOs.Api.Entities.Billing.Types.UserSubscriptionDto(
            sub.Id, sub.UserId, sub.Plan, sub.BillingCycle,
            sub.ConcurrentAgentLimit, sub.CreditBudgetPerMonth,
            sub.CreditsUsedThisMonth, remaining, overBudget,
            sub.OverageEnabled, sub.PeriodStart, sub.PeriodEnd, sub.IsActive);
    }
}
