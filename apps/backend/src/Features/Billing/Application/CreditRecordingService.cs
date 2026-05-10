namespace OffceOs.Application.Features.Billing;

internal sealed class CreditRecordingService : ICreditRecordingService
{
    private readonly StripeConfig _stripeConfig;
    private readonly IAgentRepository _agentRepository;
    private readonly IUserSubscriptionRepository _userSubscriptionRepository;
    private readonly IStripeMeteringService _stripeMeteringService;
    private readonly ILogger<CreditRecordingService> _logger;
    private readonly CustomLlmProviderConfig _customLlmProviderConfig;

    public CreditRecordingService(
        StripeConfig config,
        IAgentRepository agentRepo,
        IUserSubscriptionRepository subRepo,
        IStripeMeteringService stripeMeteringService,
        ILogger<CreditRecordingService> logger,
        CustomLlmProviderConfig? customLlmProviderConfig = null)
    {
        _stripeConfig = config;
        _agentRepository = agentRepo;
        _userSubscriptionRepository = subRepo;
        _stripeMeteringService = stripeMeteringService;
        _logger = logger;
        _customLlmProviderConfig = customLlmProviderConfig ?? new CustomLlmProviderConfig();
        StripeConfiguration.ApiKey = _stripeConfig.SecretKey;
    }

    public async Task RecordCreditUsageAsync(Guid agentId, string model, long rawTokens, CancellationToken ct = default)
    {
        if (rawTokens <= 0)
            throw new InvalidOperationException($"Refusing to record non-positive LLM usage for agent {agentId}.");

        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = agentId }, ct);
        if (agent is null)
            throw new InvalidOperationException($"Cannot record LLM usage because agent {agentId} was not found.");
        if (agent.OwnerId is null)
            throw new InvalidOperationException($"Cannot record LLM usage because agent {agentId} has no owner.");

        var credits = rawTokens * GetCostWeight(model);
        var previousCreditsUsed = 0L;
        var sub = await _userSubscriptionRepository.GetByAsync(new UserSubscriptionFilter { UserId = agent.OwnerId.Value }, ct);
        var createdSubscription = sub is null;
        if (sub is null)
        {
            sub = UserSubscriptionRecord.CreateDefaultFree(agent.OwnerId.Value);
            _logger.LogWarning(
                "Created missing free subscription while recording usage for agent {AgentId} user {UserId}",
                agentId, agent.OwnerId.Value);
        }

        previousCreditsUsed = sub.CreditsUsedThisMonth;

        sub.RecordCredits(credits);
        if (createdSubscription)
            await _userSubscriptionRepository.AddAsync(sub, ct);
        else
            await _userSubscriptionRepository.UpdateAsync(sub, ct);

        _logger.LogDebug(
            "Agent {AgentId} used {Credits} credits ({RawTokens} raw tokens on {Model}). " +
            "User {UserId}: {Used}/{Budget} credits this month.",
            agentId, credits, rawTokens, model, agent.OwnerId, sub.CreditsUsedThisMonth, sub.CreditBudgetPerMonth);

        if (sub.OverageEnabled && sub.CreditsUsedThisMonth > sub.CreditBudgetPerMonth)
        {
            if (string.IsNullOrWhiteSpace(sub.StripeCustomerId) || string.IsNullOrWhiteSpace(sub.StripeOverageItemId))
            {
                throw new BillingProviderException(
                    $"Extra usage is enabled for user {sub.UserId}, but Stripe metering is not configured.");
            }

            var previousOverage = Math.Max(0, previousCreditsUsed - sub.CreditBudgetPerMonth);
            var currentOverage = sub.CreditsUsedThisMonth - sub.CreditBudgetPerMonth;
            var overageCredits = currentOverage - previousOverage;
            if (overageCredits <= 0) return;

            var eventName = sub.Plan == SubscriptionPlan.Pro ? "pro_credits_used" : "free_credits_used";
            await _stripeMeteringService.FireMeterEventAsync(eventName, sub.StripeCustomerId, overageCredits, ct);
        }
    }

    private int GetCostWeight(string model)
    {
        if (_customLlmProviderConfig.IsConfigured &&
            string.Equals(model, _customLlmProviderConfig.ModelId.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return _customLlmProviderConfig.EffectiveCostWeight;
        }

        return ProviderRegistry.GetCostWeight(model);
    }
}
