using OffceOs.Features.AgentHarness.Domain;

namespace OffceOs.Features.AgentHarness.Application;

/// <summary>
/// Publishes typed events produced by the agent turn application flow.
/// </summary>
/// <remarks>
/// <para><strong>Responsible for:</strong> translating application-layer milestones into domain
/// events and sending them through MediatR.</para>
/// <para><strong>Responsible only for:</strong> event publication. It does not decide turn state,
/// mutate runs, calculate billing, or execute LLM/tool work.</para>
/// <para><strong>Acceptance criteria:</strong> this class should change only when event contracts,
/// event ordering at a single publication point, or event naming changes.</para>
/// </remarks>
internal sealed class TurnEventPublisher
{
    private readonly IPublisher _publisher;

    public TurnEventPublisher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task PublishTurnStartedAsync(Guid agentId, Guid sessionId, string correlationId, string userMessage, CancellationToken ct)
        => _publisher.Publish(new TurnStartedEvent(agentId, sessionId, correlationId, userMessage), ct);

    public Task PublishDiagnosticAsync(Guid agentId, Guid sessionId, string correlationId, string message, int durationMs, CancellationToken ct)
        => _publisher.Publish(new TurnDiagnosticEvent(agentId, sessionId, correlationId, message, durationMs), ct);

    public Task PublishLlmCompletedAsync(
        Guid agentId,
        Guid sessionId,
        string correlationId,
        string provider,
        string model,
        int durationMs,
        int inputTokens,
        int outputTokens,
        int? cacheReadTokens,
        int? cacheWriteTokens,
        int? reasoningTokens,
        bool estimatedTokens,
        string activity,
        IReadOnlyList<LlmUsageContextPartMessage> contextParts,
        CancellationToken ct)
        => _publisher.Publish(new LlmCallCompletedEvent(
            agentId,
            sessionId,
            correlationId,
            provider,
            model,
            durationMs,
            inputTokens,
            outputTokens,
            cacheReadTokens,
            cacheWriteTokens,
            reasoningTokens,
            estimatedTokens,
            contextParts), ct);

    public Task PublishMessageOutAsync(Guid agentId, Guid sessionId, string correlationId, string content, CancellationToken ct)
        => _publisher.Publish(new MessageOutEvent(agentId, sessionId, correlationId, content), ct);

    public Task PublishToolCallStartedAsync(Guid agentId, Guid sessionId, string correlationId, string toolName, string argsJson, CancellationToken ct)
        => _publisher.Publish(new ToolCallStartedEvent(agentId, sessionId, correlationId, toolName, argsJson), ct);

    public Task PublishToolCallCompletedAsync(
        Guid agentId,
        Guid sessionId,
        string correlationId,
        string toolName,
        bool success,
        string output,
        int durationMs,
        CancellationToken ct)
        => _publisher.Publish(new ToolCallCompletedEvent(agentId, sessionId, correlationId, toolName, success, output, durationMs), ct);

    public Task PublishTurnCompletedAsync(
        Guid agentId,
        Guid sessionId,
        string correlationId,
        int durationMs,
        int iterations,
        int toolCallCount,
        CancellationToken ct)
        => _publisher.Publish(new TurnCompletedEvent(agentId, sessionId, correlationId, durationMs, iterations, toolCallCount), ct);

    public Task PublishErrorAsync(Guid agentId, Guid sessionId, string correlationId, string message, CancellationToken ct)
        => _publisher.Publish(new AgentErrorOccurredEvent(agentId, sessionId, correlationId, message), ct);
}
