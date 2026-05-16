using OffceOs.Application.Features.ResourceLogs;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Common.ValueObjects;
namespace OffceOs.Application.Features.AgentHarness;

internal sealed class AgentHarnessService : IAgentHarnessService
{
    private const int MaxIterations = 25;

    private readonly IAgentRepository _agentRepository;
    private readonly IAgentSessionRepository _agentSessionRepository;
    private readonly IAgentWorkQueueService _agentWorkQueueService;
    private readonly IResourceLogWriterService _resourceLogWriterService;
    private readonly TurnEventPublisher _turnEventPublisher;
    private readonly TurnContextBuilder _turnContextBuilder;
    private readonly LlmTurnExecutor _llmTurnExecutor;
    private readonly ToolExecutionLoop _toolExecutionLoop;

    public AgentHarnessService(
        IAgentRepository agentRepository,
        IAgentSessionRepository agentSessionRepository,
        IAgentWorkQueueService agentWorkQueueService,
        IResourceLogWriterService resourceLogWriterService,
        TurnEventPublisher turnEventPublisher,
        TurnContextBuilder turnContextBuilder,
        LlmTurnExecutor llmTurnExecutor,
        ToolExecutionLoop toolExecutionLoop)
    {
        _agentRepository = agentRepository;
        _agentSessionRepository = agentSessionRepository;
        _agentWorkQueueService = agentWorkQueueService;
        _resourceLogWriterService = resourceLogWriterService;
        _turnEventPublisher = turnEventPublisher;
        _turnContextBuilder = turnContextBuilder;
        _llmTurnExecutor = llmTurnExecutor;
        _toolExecutionLoop = toolExecutionLoop;
    }

    public async Task RunWorkAsync(Guid workLogId, CancellationToken ct = default)
    {
        var work = await _agentWorkQueueService.StartWorkAsync(workLogId, ct);
        if (work is null || work.AgentId is null)
            return;

        var agentId = work.AgentId.Value;
        var correlationId = string.IsNullOrWhiteSpace(work.CorrelationId)
            ? work.Id.ToString("N")
            : work.CorrelationId;
        var started = Stopwatch.GetTimestamp();
        var toolCalls = 0;

        try
        {
            var agent = await _agentRepository.GetByAsync(
                new AgentFilter { Id = agentId, WorkspaceId = work.WorkspaceId },
                ct);
            if (agent is null)
            {
                await FailAsync(work.Id, agentId, correlationId, "Agent not found.", ct);
                return;
            }

            await EnsureActiveSessionAsync(agentId, ct);
            await _turnEventPublisher.PublishTurnStartedAsync(agentId, correlationId, work.Content, ct);

            await using var tools = await _toolExecutionLoop.CreateSessionAsync(agent, correlationId, work.DefinitionId, ct);
            var history = await _turnContextBuilder.BuildAsync(agentId, correlationId, work.Content, ct);

            for (var iteration = 0; iteration < MaxIterations; iteration++)
            {
                var llmResult = await _llmTurnExecutor.ExecuteAsync(agent, history, tools.Registry, iteration + 1, correlationId, ct);
                if (llmResult.IsFailure)
                {
                    await FailAsync(work.Id, agentId, correlationId, llmResult.Error.Message, ct);
                    return;
                }

                var llmTurn = llmResult.Value;
                await _turnEventPublisher.PublishLlmCompletedAsync(
                    agentId,
                    correlationId,
                    agent.Provider,
                    llmTurn.Model,
                    llmTurn.DurationMs,
                    llmTurn.Usage.InputTokens,
                    llmTurn.Usage.OutputTokens,
                    llmTurn.Usage.CacheReadTokens,
                    llmTurn.Usage.CacheWriteTokens,
                    llmTurn.Usage.ReasoningTokens,
                    llmTurn.Usage.EstimatedTokens,
                    "agent-work",
                    llmTurn.Usage.ContextParts,
                    ct);

                if (!string.IsNullOrWhiteSpace(llmTurn.AssistantContent))
                    await _turnEventPublisher.PublishMessageOutAsync(agentId, correlationId, llmTurn.AssistantContent, ct);

                history.Push(new ChatMessage
                {
                    Role = "assistant",
                    Content = llmTurn.AssistantContent,
                    ToolCalls = llmTurn.ToolCalls.Count > 0
                        ? llmTurn.ToolCalls.Select(toolCall => new ChatToolCall
                        {
                            Id = toolCall.Id,
                            Name = toolCall.Name,
                            Arguments = toolCall.Arguments,
                        }).ToList()
                        : null,
                });

                if (llmTurn.ToolCalls.Count == 0)
                {
                    await CompleteAsync(work.Id, agentId, correlationId, started, iteration + 1, toolCalls, ct);
                    return;
                }

                var toolLoop = await _toolExecutionLoop.ExecuteAsync(
                    agentId,
                    correlationId,
                    llmTurn.ToolCalls,
                    tools,
                    history,
                    toolCalls,
                    ct);
                toolCalls = toolLoop.TotalToolCalls;
                if (toolLoop.ShouldStop)
                {
                    await FailAsync(work.Id, agentId, correlationId, toolLoop.ErrorMessage ?? "Tool execution stopped.", ct);
                    return;
                }
            }

            await FailAsync(work.Id, agentId, correlationId, $"Hit max iterations ({MaxIterations}).", ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            await _resourceLogWriterService
                .ForAgent(agentId)
                .WithCorrelation(correlationId)
                .ErrorAsync(ex, "Agent work {WorkLogId} failed", workLogId, CancellationToken.None);
            await FailAsync(workLogId, agentId, correlationId, ex.Message, CancellationToken.None);
        }
    }

    private async Task EnsureActiveSessionAsync(Guid agentId, CancellationToken ct)
    {
        var session = await _agentSessionRepository.GetByAsync(new AgentSessionFilter
        {
            AgentId = agentId,
            Status = SessionStatus.Active,
        }, ct);
        if (session is null)
        {
            session = AgentSessionRecord.Create(agentId);
            await _agentSessionRepository.CreateAsync(session, ct);
        }

        session.IncrementMessageCount();
        await _agentSessionRepository.SaveChangesAsync(ct);
    }

    private async Task CompleteAsync(
        Guid workLogId,
        Guid agentId,
        string correlationId,
        long started,
        int iterations,
        int toolCalls,
        CancellationToken ct)
    {
        await _agentWorkQueueService.CompleteWorkAsync(workLogId, ct);
        var durationMs = (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        await _turnEventPublisher.PublishTurnCompletedAsync(agentId, correlationId, durationMs, iterations, toolCalls, ct);
    }

    private async Task FailAsync(Guid workLogId, Guid agentId, string correlationId, string error, CancellationToken ct)
    {
        await _agentWorkQueueService.FailWorkAsync(workLogId, error, ct);
        await _turnEventPublisher.PublishErrorAsync(agentId, correlationId, error, ct);
    }
}
