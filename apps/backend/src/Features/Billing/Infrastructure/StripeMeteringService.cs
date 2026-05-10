namespace OffceOs.Infrastructure.Features.Billing;

internal sealed class StripeMeteringService : IStripeMeteringService
{
    private readonly StripeConfig _stripeConfig;
    private readonly ILogger<StripeMeteringService> _logger;

    public StripeMeteringService(StripeConfig stripeConfig, ILogger<StripeMeteringService> logger)
    {
        _stripeConfig = stripeConfig;
        _logger = logger;
    }

    public async Task FireMeterEventAsync(string eventName, string customerId, long credits, CancellationToken ct = default)
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
