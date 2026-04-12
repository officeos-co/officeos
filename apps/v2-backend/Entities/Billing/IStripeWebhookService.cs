namespace EnterpriseAgentOs.Api.Entities.Billing;

public interface IStripeWebhookService
{
    Task HandleAsync(string payload, string signature, CancellationToken ct = default);
}
