using EnterpriseAgentOs.Api.Properties;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseAgentOs.Api.Entities.Billing;

[ApiController]
[Route("api/billing")]
public sealed class BillingController : ControllerBase
{
    private readonly IUserBillingService _userBilling;
    private readonly IOrgBillingService _orgBilling;
    private readonly IStripeWebhookService _webhook;
    private readonly StripeConfig _config;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        IUserBillingService userBilling,
        IOrgBillingService orgBilling,
        IStripeWebhookService webhook,
        StripeConfig config,
        ILogger<BillingController> logger)
    {
        _userBilling = userBilling;
        _orgBilling = orgBilling;
        _webhook = webhook;
        _config = config;
        _logger = logger;
    }

    // ── Org ──────────────────────────────────────────────────────────────────

    [HttpGet("subscription")]
    public async Task<IActionResult> GetSubscription(CancellationToken ct)
    {
        const string orgId = "default";
        var sub = await _orgBilling.GetSubscriptionAsync(orgId, ct);
        var (remaining, overBudget) = await _orgBilling.CheckCreditBudgetAsync(orgId, ct);

        return Ok(new
        {
            plan = sub.Plan,
            stripeCustomerId = sub.StripeCustomerId,
            stripeSubscriptionId = sub.StripeSubscriptionId,
            concurrentAgentLimit = sub.ConcurrentAgentLimit,
            creditBudgetPerMonth = sub.CreditBudgetPerMonth,
            creditsUsedThisMonth = sub.CreditsUsedThisMonth,
            creditsRemaining = remaining,
            overageEnabled = sub.OverageEnabled,
            overBudget,
            periodStart = sub.PeriodStart,
            periodEnd = sub.PeriodEnd,
            isActive = sub.IsActive,
            billingEnabled = _config.Enabled,
        });
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request, CancellationToken ct)
    {
        if (!_config.Enabled)
            return StatusCode(503, new { error = "Billing not configured" });

        if (string.IsNullOrWhiteSpace(request.Plan) || (request.Plan != "free" && request.Plan != "team"))
            return BadRequest(new { error = "plan must be 'free' or 'team'" });

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "email is required" });

        const string orgId = "default";
        var customerId = await _orgBilling.CreateCustomerAsync(orgId, request.Email, ct);
        var subscriptionId = await _orgBilling.CreateSubscriptionAsync(customerId, request.Plan, ct);

        return Ok(new { customerId, subscriptionId, plan = request.Plan });
    }

    [HttpPost("org/overage")]
    public async Task<IActionResult> OrgOverage([FromBody] OverageRequest request, CancellationToken ct)
    {
        if (!_config.Enabled)
            return StatusCode(503, new { error = "Billing not configured" });

        if (HttpContext.Items["User"] is not UserRecord user)
            return Unauthorized();

        await _orgBilling.EnableOverageAsync("default", user.Email, request.Enabled, ct);
        return Ok(new { overageEnabled = request.Enabled });
    }

    // ── User ─────────────────────────────────────────────────────────────────

    [HttpGet("user/subscription")]
    public async Task<IActionResult> GetUserSubscription(CancellationToken ct)
    {
        if (HttpContext.Items["User"] is not UserRecord user)
            return Unauthorized();

        var sub = await _userBilling.GetSubscriptionAsync(user.Id, ct);
        var (remaining, overBudget) = await _userBilling.CheckCreditBudgetAsync(user.Id, ct);

        return Ok(new
        {
            plan = sub.Plan,
            billingCycle = sub.BillingCycle,
            stripeCustomerId = sub.StripeCustomerId,
            stripeSubscriptionId = sub.StripeSubscriptionId,
            concurrentAgentLimit = sub.ConcurrentAgentLimit,
            creditBudgetPerMonth = sub.CreditBudgetPerMonth,
            creditsUsedThisMonth = sub.CreditsUsedThisMonth,
            creditsRemaining = remaining,
            overageEnabled = sub.OverageEnabled,
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
                    creditsPerMonth = PlanLimits.IndividualFree.CreditsPerMonth,
                    priceMonthly = 0,
                    priceYearly = 0,
                },
                new
                {
                    plan = PlanLimits.IndividualPro.Plan,
                    concurrentAgents = PlanLimits.IndividualPro.ConcurrentAgents,
                    creditsPerMonth = PlanLimits.IndividualPro.CreditsPerMonth,
                    priceMonthly = 20,
                    priceYearly = 16,
                },
            },
        });
    }

    [HttpPost("user/subscribe")]
    public async Task<IActionResult> UserSubscribe([FromBody] UserSubscribeRequest request, CancellationToken ct)
    {
        if (!_config.Enabled)
            return StatusCode(503, new { error = "Billing not configured" });

        if (HttpContext.Items["User"] is not UserRecord user)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Plan) || (request.Plan != "free" && request.Plan != "pro"))
            return BadRequest(new { error = "plan must be 'free' or 'pro'" });

        var billingCycle = string.IsNullOrWhiteSpace(request.BillingCycle) ? "monthly" : request.BillingCycle;
        if (billingCycle != "monthly" && billingCycle != "yearly")
            return BadRequest(new { error = "billingCycle must be 'monthly' or 'yearly'" });

        var checkoutUrl = await _userBilling.CreateCheckoutSessionAsync(user.Id, user.Email, request.Plan, billingCycle, ct);
        return Ok(new { checkoutUrl });
    }

    [HttpGet("user/portal")]
    public async Task<IActionResult> UserPortal(CancellationToken ct)
    {
        if (!_config.Enabled)
            return StatusCode(503, new { error = "Billing not configured" });

        if (HttpContext.Items["User"] is not UserRecord user)
            return Unauthorized();

        var portalUrl = await _userBilling.CreatePortalSessionAsync(user.Id, user.Email, ct);
        return Ok(new { portalUrl });
    }

    [HttpPost("user/overage")]
    public async Task<IActionResult> UserOverage([FromBody] OverageRequest request, CancellationToken ct)
    {
        if (!_config.Enabled)
            return StatusCode(503, new { error = "Billing not configured" });

        if (HttpContext.Items["User"] is not UserRecord user)
            return Unauthorized();

        await _userBilling.EnableOverageAsync(user.Id, user.Email, request.Enabled, ct);
        return Ok(new { overageEnabled = request.Enabled });
    }

    // ── Webhook ───────────────────────────────────────────────────────────────

    [HttpPost("webhook")]
    [DisableRequestSizeLimit]
    [Consumes("application/json")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        if (!_config.Enabled)
            return StatusCode(503, new { error = "Billing not configured" });

        if (!Request.Headers.TryGetValue("Stripe-Signature", out var signature) || string.IsNullOrEmpty(signature))
            return BadRequest(new { error = "Missing Stripe-Signature header" });

        string payload;
        using (var reader = new StreamReader(Request.Body))
            payload = await reader.ReadToEndAsync(ct);

        try
        {
            await _webhook.HandleAsync(payload, signature!, ct);
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
public sealed record UserSubscribeRequest(string Plan, string? BillingCycle);
public sealed record OverageRequest(bool Enabled);
