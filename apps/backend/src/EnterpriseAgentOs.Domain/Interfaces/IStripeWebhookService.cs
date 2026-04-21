namespace EnterpriseAgentOs.Domain.Interfaces;

public interface IStripeWebhookService
{
    Task HandleAsync(string payload, string signature, CancellationToken ct = default);
}
