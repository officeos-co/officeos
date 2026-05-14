namespace OffceOs.Application.Features.Agents;

/// <summary>
/// Enforces billing checks around paid LLM work.
/// </summary>
/// <remarks>
/// <para><strong>Responsible for:</strong> checking quota before an LLM call and recording usage after
/// a successful LLM call.</para>
/// <para><strong>Responsible only for:</strong> billing checkpoint behavior. It does not execute LLM
/// requests, calculate token usage, decide turn completion, or publish normal turn events.</para>
/// <para><strong>Acceptance criteria:</strong> this class should change only when billing enforcement,
/// usage recording policy, quota behavior, or billing failure handling changes.</para>
/// </remarks>
internal sealed class BillingCheckpoint
{
    private readonly IBillingGuard _billingGuard;
    private readonly ICreditRecordingService _creditRecordingService;
    private readonly ILogger<BillingCheckpoint> _logger;

    public BillingCheckpoint(
        IBillingGuard billingGuard,
        ICreditRecordingService creditRecording,
        ILogger<BillingCheckpoint> logger)
    {
        _billingGuard = billingGuard;
        _creditRecordingService = creditRecording;
        _logger = logger;
    }

    public async Task CheckBeforeLlmCallAsync(Guid agentId, CancellationToken ct)
    {
        var quota = await _billingGuard.CheckQuotaAsync(agentId, ct);
        if (quota.Exceeded)
            throw new QuotaExceededException(
                quota.Reason ?? $"Agent {agentId} has reached the credit limit for this billing period.");
    }

    public async Task RecordAfterLlmCallAsync(Guid agentId, string correlationId, string model, long rawTokens, CancellationToken ct)
    {
        try
        {
            await _creditRecordingService.RecordCreditUsageAsync(agentId, model, rawTokens, ct);
            await _billingGuard.RefreshCacheAsync(agentId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Billing failed after LLM call for agent {AgentId} correlation {CorrelationId}",
                agentId,
                correlationId);
            throw;
        }
    }
}
