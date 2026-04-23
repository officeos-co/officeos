namespace EnterpriseAgentOs.Domain.Features.Billing;

public interface IStripeWebhookService
{
    Task HandleAsync(string payload, string signature, CancellationToken ct = default);
}
