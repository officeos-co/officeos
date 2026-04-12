namespace EnterpriseAgentOs.Api.Properties;

public sealed class StripeConfig
{
    public string SecretKey { get; set; } = string.Empty; // STRIPE_SECRET_KEY_PLACEHOLDER
    public string WebhookSecret { get; set; } = string.Empty;
    public string FreePriceId { get; set; } = string.Empty; // Stripe price ID for Free tier
    public string TeamPriceId { get; set; } = string.Empty; // Stripe price ID for Team tier
    public string TeamOveragePriceId { get; set; } = string.Empty; // metered price for overage
    public string ProMonthlyPriceId { get; init; } = string.Empty; // Stripe price ID for Individual Pro monthly ($29/mo)
    public string ProYearlyPriceId  { get; init; } = string.Empty; // Stripe price ID for Individual Pro yearly ($24/mo)
    public bool Enabled { get; set; } = false;
}
