namespace EnterpriseAgentOs.Domain.Billing;

public interface IStripeWebhookService
{
    Task HandleAsync(string payload, string signature, CancellationToken ct = default);
}
