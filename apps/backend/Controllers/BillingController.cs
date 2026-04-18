namespace EnterpriseAgentOs.Api.Controllers;

/// <summary>
/// Billing REST surface. Dashboard-facing endpoints (subscription, plans,
/// overage, portal) have moved to GraphQL. Only the Stripe webhook — called
/// by Stripe's servers, not the dashboard — remains as REST.
/// </summary>
[ApiController]
[Route("api/billing")]
public sealed class BillingController : ControllerBase
{
    private readonly IStripeWebhookService _webhook;
    private readonly EnterpriseAgentOs.Infrastructure.Configuration.StripeConfig _config;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        IStripeWebhookService webhook,
        EnterpriseAgentOs.Infrastructure.Configuration.StripeConfig config,
        ILogger<BillingController> logger)
    {
        _webhook = webhook;
        _config = config;
        _logger = logger;
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
