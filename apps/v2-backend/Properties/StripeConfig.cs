namespace EnterpriseAgentOs.Api.Properties;

public sealed class StripeConfig
{
    public string SecretKey { get; set; } = string.Empty; // STRIPE_SECRET_KEY_PLACEHOLDER
    public string WebhookSecret { get; set; } = string.Empty;
    public string FreePriceId { get; set; } = string.Empty; // Stripe price ID for Free tier
    public string TeamMonthlyPriceId { get; set; } = string.Empty; // Stripe price ID for Team monthly
    public string TeamYearlyPriceId { get; set; } = string.Empty; // Stripe price ID for Team yearly
    public string TeamOveragePriceId { get; set; } = string.Empty; // metered price for Team overage
    public string FreeOveragePriceId { get; set; } = string.Empty; // metered price for Free overage ($5/1M credits)
    public string ProOveragePriceId  { get; set; } = string.Empty; // metered price for Pro overage ($3/1M credits)
    public string ProMonthlyPriceId { get; init; } = string.Empty; // Stripe price ID for Individual Pro monthly ($20/mo)
    public string ProYearlyPriceId  { get; init; } = string.Empty; // Stripe price ID for Individual Pro yearly ($16/mo)
    public bool Enabled { get; set; } = false;
}
