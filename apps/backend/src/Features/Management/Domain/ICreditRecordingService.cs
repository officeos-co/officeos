namespace EnterpriseAgentOs.Domain.Features.Management;

/// <summary>
/// Records normalized credit usage from LLM proxy calls and fires Stripe Billing
/// Meter Events when a user's monthly credit budget is exceeded with overage enabled.
/// </summary>
public interface ICreditRecordingService
{
    Task RecordCreditUsageAsync(Guid agentId, string model, long rawTokens, CancellationToken ct = default);
}

public interface IStripeMeteringService
{
    Task FireMeterEventAsync(string eventName, string customerId, long credits, CancellationToken ct = default);
}
