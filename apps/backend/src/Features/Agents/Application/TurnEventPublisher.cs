namespace OffceOs.Application.Features.Agents;

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

    public Task PublishTurnStartedAsync(Guid agentId, string correlationId, string userMessage, CancellationToken ct)
        => _publisher.Publish(new TurnStartedEvent(agentId, correlationId, userMessage), ct);

    public Task PublishPodConnectedAsync(Guid agentId, string correlationId, int durationMs, CancellationToken ct)
        => _publisher.Publish(new PodConnectedEvent(agentId, correlationId, durationMs), ct);

    public Task PublishDiagnosticAsync(Guid agentId, string correlationId, string message, int durationMs, CancellationToken ct)
        => _publisher.Publish(new TurnDiagnosticEvent(agentId, correlationId, message, durationMs), ct);

    public Task PublishLlmCompletedAsync(
        Guid agentId,
        string correlationId,
        string provider,
        string model,
        int durationMs,
        int inputTokens,
        int outputTokens,
        CancellationToken ct)
        => _publisher.Publish(new LlmCallCompletedEvent(agentId, correlationId, provider, model, durationMs, inputTokens, outputTokens), ct);

    public Task PublishMessageOutAsync(Guid agentId, string correlationId, string content, CancellationToken ct)
        => _publisher.Publish(new MessageOutEvent(agentId, correlationId, content), ct);

    public Task PublishToolCallStartedAsync(Guid agentId, string correlationId, string toolName, string argsJson, CancellationToken ct)
        => _publisher.Publish(new ToolCallStartedEvent(agentId, correlationId, toolName, argsJson), ct);

    public Task PublishToolCallCompletedAsync(
        Guid agentId,
        string correlationId,
        string toolName,
        bool success,
        string output,
        int durationMs,
        CancellationToken ct)
        => _publisher.Publish(new ToolCallCompletedEvent(agentId, correlationId, toolName, success, output, durationMs), ct);

    public Task PublishToolPolicyDeniedAsync(
        Guid agentId,
        string correlationId,
        string toolName,
        string reason,
        CancellationToken ct)
        => _publisher.Publish(new AgentToolPolicyDeniedEvent(agentId, correlationId, toolName, reason), ct);

    public Task PublishTurnCompletedAsync(
        Guid agentId,
        string correlationId,
        int durationMs,
        int iterations,
        int toolCallCount,
        CancellationToken ct)
        => _publisher.Publish(new TurnCompletedEvent(agentId, correlationId, durationMs, iterations, toolCallCount), ct);

    public Task PublishErrorAsync(Guid agentId, string correlationId, string message, CancellationToken ct)
        => _publisher.Publish(new AgentErrorOccurredEvent(agentId, correlationId, message), ct);
}
