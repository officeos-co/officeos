namespace EnterpriseAgentOs.Api.Features.Billing;

/// <summary>
/// Billing REST surface. Dashboard-facing endpoints (subscription, plans,
/// overage, portal) have moved to  Only the Stripe webhook — called
/// by Stripe's servers, not the dashboard — remains as REST.
/// </summary>
[ApiController]
[Route("api/billing")]
public sealed class BillingController : ControllerBase
{
    private readonly IStripeWebhookService _stripeWebhookService;
    private readonly StripeConfig _stripeConfig;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        IStripeWebhookService webhook,
        StripeConfig config,
        ILogger<BillingController> logger)
    {
        _stripeWebhookService = webhook;
        _stripeConfig = config;
        _logger = logger;
    }

    // ── Webhook ───────────────────────────────────────────────────────────────

    [HttpPost("webhook")]
    [DisableRequestSizeLimit]
    [Consumes("application/json")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        if (!_stripeConfig.Enabled)
            return StatusCode(503, new { error = "Billing not configured" });

        if (!Request.Headers.TryGetValue("Stripe-Signature", out var signature) || string.IsNullOrEmpty(signature))
            return BadRequest(new { error = "Missing Stripe-Signature header" });

        string payload;
        using (var reader = new StreamReader(Request.Body))
            payload = await reader.ReadToEndAsync(ct);

        try
        {
            await _stripeWebhookService.HandleAsync(payload, signature!, ct);
            return Ok(new { received = true });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stripe webhook processing failed");
            return BadRequest(new { error = "Webhook processing failed" });
        }
    }
}
