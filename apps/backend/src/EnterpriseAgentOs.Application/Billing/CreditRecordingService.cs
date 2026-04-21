namespace EnterpriseAgentOs.Application.Billing;

internal sealed class CreditRecordingService : ICreditRecordingService
{
    private readonly StripeConfig _stripeConfig;
    private readonly IAgentRepository _agentRepository;
    private readonly IUserSubscriptionRepository _userSubscriptionRepository;
    private readonly ILogger<CreditRecordingService> _logger;

    public CreditRecordingService(
        StripeConfig config,
        IAgentRepository agentRepo,
        IUserSubscriptionRepository subRepo,
        ILogger<CreditRecordingService> logger)
    {
        _stripeConfig = config;
        _agentRepository = agentRepo;
        _userSubscriptionRepository = subRepo;
        _logger = logger;
        StripeConfiguration.ApiKey = _stripeConfig.SecretKey;
    }

    public async Task RecordCreditUsageAsync(Guid agentId, string model, long rawTokens, CancellationToken ct = default)
    {
        var agent = await _agentRepository.GetAsync(agentId, ct);
        if (agent?.OwnerId is null) return;

        var credits = ModelCostWeights.ToCredits(model, rawTokens);
        var sub = await _userSubscriptionRepository.GetByUserIdAsync(agent.OwnerId.Value, ct);
        if (sub is null) return;

        sub.RecordCredits(credits);
        await _userSubscriptionRepository.SaveChangesAsync(ct);

        _logger.LogDebug(
            "Agent {AgentId} used {Credits} credits ({RawTokens} raw tokens on {Model}). " +
            "User {UserId}: {Used}/{Budget} credits this month.",
            agentId, credits, rawTokens, model, agent.OwnerId, sub.CreditsUsedThisMonth, sub.CreditBudgetPerMonth);

        if (sub.OverageEnabled
            && sub.StripeOverageItemId is not null
            && sub.StripeCustomerId is not null
            && sub.CreditsUsedThisMonth > sub.CreditBudgetPerMonth)
        {
            var overageCredits = sub.CreditsUsedThisMonth - sub.CreditBudgetPerMonth;
            var eventName = sub.Plan == "pro" ? "pro_credits_used" : "free_credits_used";
            await FireMeterEventAsync(eventName, sub.StripeCustomerId, overageCredits, ct);
        }
    }

    private async Task FireMeterEventAsync(string eventName, string customerId, long credits, CancellationToken ct)
    {
        var client = new StripeClient(_stripeConfig.SecretKey);
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
}
