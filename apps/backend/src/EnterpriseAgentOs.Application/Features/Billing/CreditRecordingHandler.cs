using MediatR;

namespace EnterpriseAgentOs.Application.Features.Billing;

/// <summary>
/// Bridges LlmCallCompletedEvent → CreditRecordingService.
/// Records credit usage and updates the Redis billing guard cache.
/// </summary>
internal sealed class CreditRecordingHandler : INotificationHandler<LlmCallCompletedEvent>
{
    private readonly ICreditRecordingService _creditRecording;
    private readonly IBillingGuard _billingGuard;
    private readonly ILogger<CreditRecordingHandler> _logger;

    public CreditRecordingHandler(
        ICreditRecordingService creditRecording,
        IBillingGuard billingGuard,
        ILogger<CreditRecordingHandler> logger)
    {
        _creditRecording = creditRecording;
        _billingGuard = billingGuard;
        _logger = logger;
    }

    public async Task Handle(LlmCallCompletedEvent e, CancellationToken ct)
    {
        var rawTokens = (long)(e.InputTokens ?? 0) + (e.OutputTokens ?? 0);
        if (rawTokens <= 0) return;

        try
        {
            await _creditRecording.RecordCreditUsageAsync(e.AgentId, e.Model, rawTokens, ct);
            await _billingGuard.RefreshCacheAsync(e.AgentId, ct);
        }
        catch (Exception ex)
        {
            // Credit recording must not kill the agent turn — log and continue.
            _logger.LogError(ex, "Failed to record credit usage for agent {AgentId}", e.AgentId);
        }
    }
}
