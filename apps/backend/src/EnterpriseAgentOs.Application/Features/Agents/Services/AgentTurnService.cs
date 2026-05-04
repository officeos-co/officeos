using System.Diagnostics;

namespace EnterpriseAgentOs.Application.Features.Agents;

/// <summary>
/// Coordinates one user-visible agent turn.
/// </summary>
/// <remarks>
/// <para><strong>Responsible for:</strong> ordering the turn workflow: load the agent, begin the run,
/// build context, enforce billing checkpoints, execute LLM iterations, execute requested tools,
/// publish turn-level completion/error outcomes, and complete the run.</para>
/// <para><strong>Responsible only for:</strong> orchestration decisions that define the turn loop itself.
/// It delegates billing, event publishing, run persistence, context building, LLM execution, and tool
/// execution to focused collaborators.</para>
/// <para><strong>Acceptance criteria:</strong> this class should change only when the turn workflow order,
/// stop conditions, or iteration policy changes. Provider parsing, token usage calculation, billing
/// persistence, context reconstruction, and tool dispatch details must change in their owning classes.</para>
/// </remarks>
internal sealed class AgentTurnService
{
    private const int MaxIterations = 25;

    private readonly IAgentRepository _agentRepository;
    private readonly AgentRunLifecycle _runLifecycle;
    private readonly TurnEventPublisher _events;
    private readonly TurnContextBuilder _contextBuilder;
    private readonly BillingCheckpoint _billing;
    private readonly LlmTurnExecutor _llmTurnExecutor;
    private readonly ToolExecutionLoop _toolExecutionLoop;
    private readonly ILogger<AgentTurnService> _logger;

    public AgentTurnService(
        IAgentRepository agentRepository,
        AgentRunLifecycle runLifecycle,
        TurnEventPublisher events,
        TurnContextBuilder contextBuilder,
        BillingCheckpoint billing,
        LlmTurnExecutor llmTurnExecutor,
        ToolExecutionLoop toolExecutionLoop,
        ILogger<AgentTurnService> logger)
    {
        _agentRepository = agentRepository;
        _runLifecycle = runLifecycle;
        _events = events;
        _contextBuilder = contextBuilder;
        _billing = billing;
        _llmTurnExecutor = llmTurnExecutor;
        _toolExecutionLoop = toolExecutionLoop;
        _logger = logger;
    }

    public async Task RunTurnAsync(Guid agentId, string userMessage, string correlationId, CancellationToken ct)
    {
        try
        {
            var turnStart = Stopwatch.GetTimestamp();

            var agentLoadStart = Stopwatch.GetTimestamp();
            var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = agentId }, ct);
            if (agent is null)
            {
                await _events.PublishErrorAsync(agentId, correlationId, $"Agent {agentId} not found", ct);
                return;
            }

            if (string.IsNullOrEmpty(agent.PodName))
            {
                await _events.PublishErrorAsync(agentId, correlationId, $"Agent {agentId} has no pod", ct);
                return;
            }

            var runStart = Stopwatch.GetTimestamp();
            using var run = await _runLifecycle.BeginAsync(agentId, correlationId, userMessage, ct);

            await _events.PublishTurnStartedAsync(agentId, correlationId, userMessage, ct);
            await _events.PublishDiagnosticAsync(
                agentId,
                correlationId,
                "Turn setup: agent loaded",
                ElapsedMs(agentLoadStart),
                ct);
            await _events.PublishDiagnosticAsync(
                agentId,
                correlationId,
                "Turn setup: run begun",
                ElapsedMs(runStart),
                ct);
            await _events.PublishPodConnectedAsync(agentId, correlationId, 0, ct);

            var toolSessionStart = Stopwatch.GetTimestamp();
            await using var tools = await _toolExecutionLoop.CreateSessionAsync(agent, correlationId, ct);
            await _events.PublishDiagnosticAsync(
                agentId,
                correlationId,
                "Turn setup: tool session ready",
                ElapsedMs(toolSessionStart),
                ct);

            var contextStart = Stopwatch.GetTimestamp();
            var history = await _contextBuilder.BuildAsync(agentId, correlationId, userMessage, ct);
            await _events.PublishDiagnosticAsync(
                agentId,
                correlationId,
                "Turn setup: context built",
                ElapsedMs(contextStart),
                ct);
            var totalToolCalls = 0;

            for (var i = 0; i < MaxIterations; i++)
            {
                try
                {
                    var billingCheckStart = Stopwatch.GetTimestamp();
                    await _billing.CheckBeforeLlmCallAsync(agentId, ct);
                    await _events.PublishDiagnosticAsync(
                        agentId,
                        correlationId,
                        $"Iteration {i + 1}: billing check complete",
                        ElapsedMs(billingCheckStart),
                        ct);
                }
                catch (QuotaExceededException ex)
                {
                    await _events.PublishErrorAsync(agentId, correlationId, ex.Message, ct);
                    var quotaMs = (int)Stopwatch.GetElapsedTime(turnStart).TotalMilliseconds;
                    await _events.PublishTurnCompletedAsync(agentId, correlationId, quotaMs, i, totalToolCalls, ct);
                    await _runLifecycle.FailAsync(run, ex.Message, ct);
                    return;
                }

                var llmResult = await _llmTurnExecutor.ExecuteAsync(agent, history, tools.Registry, i + 1, correlationId, ct);
                if (llmResult.IsFailure)
                {
                    await _events.PublishErrorAsync(agentId, correlationId, llmResult.Error.Message, ct);
                    await _runLifecycle.FailAsync(run, llmResult.Error.Message, ct);
                    return;
                }

                var llmTurn = llmResult.Value;
                try
                {
                    var billingRecordStart = Stopwatch.GetTimestamp();
                    await _billing.RecordAfterLlmCallAsync(agentId, correlationId, llmTurn.Model, llmTurn.Usage.TotalTokens, ct);
                    await _events.PublishDiagnosticAsync(
                        agentId,
                        correlationId,
                        $"Iteration {i + 1}: billing record complete",
                        ElapsedMs(billingRecordStart),
                        ct);
                }
                catch
                {
                    await _events.PublishErrorAsync(
                        agentId,
                        correlationId,
                        "LLM usage could not be recorded; refusing to continue the turn.",
                        ct);
                    await _runLifecycle.FailAsync(run, "LLM usage could not be recorded.", ct);
                    return;
                }

                await _events.PublishLlmCompletedAsync(
                    agentId,
                    correlationId,
                    llmTurn.Model,
                    llmTurn.DurationMs,
                    llmTurn.Usage.InputTokens,
                    llmTurn.Usage.OutputTokens,
                    ct);

                if (!string.IsNullOrEmpty(llmTurn.AssistantContent))
                {
                    await _events.PublishMessageOutAsync(agentId, correlationId, llmTurn.AssistantContent, ct);
                }

                history.Push(new ChatMessage
                {
                    Role = "assistant",
                    Content = llmTurn.AssistantContent,
                    ToolCalls = llmTurn.ToolCalls.Count > 0
                        ? llmTurn.ToolCalls.Select(tc => new ChatToolCall { Id = tc.Id, Name = tc.Name, Arguments = tc.Arguments }).ToList()
                        : null,
                });

                if (llmTurn.ToolCalls.Count == 0)
                {
                    var totalMs = (int)Stopwatch.GetElapsedTime(turnStart).TotalMilliseconds;
                    await _events.PublishTurnCompletedAsync(agentId, correlationId, totalMs, i + 1, totalToolCalls, ct);
                    await _runLifecycle.CompleteAsync(run, llmTurn.AssistantContent, ct);
                    return;
                }

                var toolLoop = await _toolExecutionLoop.ExecuteAsync(
                    agentId,
                    correlationId,
                    llmTurn.ToolCalls,
                    tools,
                    history,
                    totalToolCalls,
                    ct);

                totalToolCalls = toolLoop.TotalToolCalls;
                if (toolLoop.ShouldStop)
                {
                    await _events.PublishErrorAsync(agentId, correlationId, toolLoop.ErrorMessage!, ct);
                    var breakMs = (int)Stopwatch.GetElapsedTime(turnStart).TotalMilliseconds;
                    await _events.PublishTurnCompletedAsync(agentId, correlationId, breakMs, i + 1, totalToolCalls, ct);
                    await _runLifecycle.FailAsync(run, toolLoop.ErrorMessage!, ct);
                    return;
                }
            }

            var maxIterationError = $"Hit max iterations ({MaxIterations})";
            await _events.PublishErrorAsync(agentId, correlationId, maxIterationError, ct);
            var maxMs = (int)Stopwatch.GetElapsedTime(turnStart).TotalMilliseconds;
            await _events.PublishTurnCompletedAsync(agentId, correlationId, maxMs, MaxIterations, totalToolCalls, ct);
            await _runLifecycle.FailAsync(run, maxIterationError, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in turn for agent {AgentId} correlation {CorrelationId}", agentId, correlationId);
            try
            {
                await _events.PublishErrorAsync(agentId, correlationId, $"Internal error: {ex.Message}", ct);
            }
            catch (Exception pubEx)
            {
                _logger.LogError(pubEx, "Failed to publish error event for agent {AgentId}", agentId);
            }
        }
    }

    private static int ElapsedMs(long startTimestamp)
        => (int)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

}
