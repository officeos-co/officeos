namespace EnterpriseAgentOs.Domain.Features.Management;

public interface IStripeWebhookService
{
    Task HandleAsync(string payload, string signature, CancellationToken ct = default);
}
