using System.Text.Json;
using EnterpriseAgentOs.Api.Properties;

namespace EnterpriseAgentOs.Api.Entities.Billing;

[ApiController]
[Route("api/billing")]
public sealed class BillingController : ControllerBase
{
    private readonly StripeService _stripe;
    private readonly StripeConfig _config;
    private readonly ILogger<BillingController> _logger;

    public BillingController(StripeService stripe, StripeConfig config, ILogger<BillingController> logger)
    {
        _stripe = stripe;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/billing/subscription — returns the current org's subscription and token usage.
    /// </summary>
    [HttpGet("subscription")]
    public async Task<IActionResult> GetSubscription(CancellationToken ct)
    {
        // TODO: Derive orgId from authenticated session once multi-tenancy is wired up.
        const string orgId = "default";

        var sub = await _stripe.GetOrgSubscriptionAsync(orgId, ct);
        var (remaining, overBudget) = await _stripe.CheckTokenBudgetAsync(orgId, ct);

        return Ok(new
        {
            plan = sub.Plan,
            stripeCustomerId = sub.StripeCustomerId,
            stripeSubscriptionId = sub.StripeSubscriptionId,
            concurrentAgentLimit = sub.ConcurrentAgentLimit,
            tokenBudgetPerMonth = sub.TokenBudgetPerMonth,
            tokensUsedThisMonth = sub.TokensUsedThisMonth,
            tokensRemaining = remaining,
            overBudget,
            periodStart = sub.PeriodStart,
            periodEnd = sub.PeriodEnd,
            isActive = sub.IsActive,
            billingEnabled = _config.Enabled,
        });
    }

    /// <summary>
    /// POST /api/billing/subscribe — create or upgrade a subscription.
    /// Returns 503 when Stripe is not yet configured.
    /// </summary>
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request, CancellationToken ct)
    {
        if (!_config.Enabled)
        {
            return StatusCode(503, new { error = "Billing not configured" });
        }

        if (string.IsNullOrWhiteSpace(request.Plan) ||
            (request.Plan != "free" && request.Plan != "team"))
        {
            return BadRequest(new { error = "plan must be 'free' or 'team'" });
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { error = "email is required" });
        }

        // TODO: Derive orgId from authenticated session.
        const string orgId = "default";

        _logger.LogInformation("TODO: Subscribe org {OrgId} to plan {Plan}", orgId, request.Plan);

        // TODO: Check if customer already exists, create if not, then create/update subscription.
        var customerId = await _stripe.CreateCustomerAsync(orgId, request.Email, ct);
        var subscriptionId = await _stripe.CreateSubscriptionAsync(customerId, request.Plan, ct);

        return Ok(new
        {
            customerId,
            subscriptionId,
            plan = request.Plan,
            message = "TODO: Subscription creation is a stub. Wire up real Stripe SDK to activate.",
        });
    }

    // -------------------------------------------------------------------------
    // User (Individual) billing endpoints
    // -------------------------------------------------------------------------

    /// <summary>
    /// GET /api/billing/user/subscription — returns the current user's individual plan and usage.
    /// </summary>
    [HttpGet("user/subscription")]
    public async Task<IActionResult> GetUserSubscription(CancellationToken ct)
    {
        // TODO: Derive userId from authenticated session once auth is wired up.
        var userId = Guid.Empty; // TODO: replace with real user ID from session

        var sub = await _stripe.GetUserSubscriptionAsync(userId, ct);
        var (remaining, overBudget) = await _stripe.CheckUserTokenBudgetAsync(userId, ct);

        return Ok(new
        {
            plan = sub.Plan,
            billingCycle = sub.BillingCycle,
            concurrentAgentLimit = sub.ConcurrentAgentLimit,
            tokenBudgetPerMonth = sub.TokenBudgetPerMonth,
            tokensUsedThisMonth = sub.TokensUsedThisMonth,
            tokensRemaining = remaining,
            overBudget,
            periodStart = sub.PeriodStart,
            periodEnd = sub.PeriodEnd,
            isActive = sub.IsActive,
            billingEnabled = _config.Enabled,
            availablePlans = new[]
            {
                new
                {
                    plan = PlanLimits.IndividualFree.Plan,
                    concurrentAgents = PlanLimits.IndividualFree.ConcurrentAgents,
                    tokensPerMonth = PlanLimits.IndividualFree.TokensPerMonth,
                    priceMonthly = 0,
                    priceYearly = 0,
                },
                new
                {
                    plan = PlanLimits.IndividualPro.Plan,
                    concurrentAgents = PlanLimits.IndividualPro.ConcurrentAgents,
                    tokensPerMonth = PlanLimits.IndividualPro.TokensPerMonth,
                    priceMonthly = 29,
                    priceYearly = 24,
                },
            },
        });
    }

    /// <summary>
    /// POST /api/billing/user/subscribe — subscribe or upgrade an individual user.
    /// Returns 503 when Stripe is not yet configured.
    /// </summary>
    [HttpPost("user/subscribe")]
    public async Task<IActionResult> UserSubscribe([FromBody] UserSubscribeRequest request, CancellationToken ct)
    {
        if (!_config.Enabled)
        {
            return StatusCode(503, new { error = "Billing not configured" });
        }

        if (string.IsNullOrWhiteSpace(request.Plan) ||
            (request.Plan != "free" && request.Plan != "pro"))
        {
            return BadRequest(new { error = "plan must be 'free' or 'pro'" });
        }

        var billingCycle = string.IsNullOrWhiteSpace(request.BillingCycle) ? "monthly" : request.BillingCycle;
        if (billingCycle != "monthly" && billingCycle != "yearly")
        {
            return BadRequest(new { error = "billingCycle must be 'monthly' or 'yearly'" });
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { error = "email is required" });
        }

        // TODO: Derive userId from authenticated session.
        var userId = Guid.Empty; // TODO: replace with real user ID from session

        _logger.LogInformation("TODO: Subscribe user {UserId} to plan {Plan} billingCycle {BillingCycle}", userId, request.Plan, billingCycle);

        var subscriptionId = await _stripe.CreateUserSubscriptionAsync(userId, request.Email, request.Plan, billingCycle, ct);

        return Ok(new
        {
            subscriptionId,
            plan = request.Plan,
            billingCycle,
            message = "TODO: User subscription creation is a stub. Wire up real Stripe SDK to activate.",
        });
    }

    /// <summary>
    /// POST /api/billing/webhook — receives and processes Stripe webhook events.
    /// Verifies the Stripe-Signature header before processing.
    /// Returns 503 when Stripe is not yet configured.
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        if (!_config.Enabled)
        {
            return StatusCode(503, new { error = "Billing not configured" });
        }

        if (!Request.Headers.TryGetValue("Stripe-Signature", out var signature) ||
            string.IsNullOrEmpty(signature))
        {
            return BadRequest(new { error = "Missing Stripe-Signature header" });
        }

        string payload;
        using (var reader = new StreamReader(Request.Body))
        {
            payload = await reader.ReadToEndAsync(ct);
        }

        try
        {
            await _stripe.HandleWebhookAsync(payload, signature!, ct);
            return Ok(new { received = true });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stripe webhook processing failed");
            return BadRequest(new { error = "Webhook processing failed" });
        }
    }
}

public sealed record SubscribeRequest(string Plan, string Email);
public sealed record UserSubscribeRequest(string Plan, string Email, string? BillingCycle);
