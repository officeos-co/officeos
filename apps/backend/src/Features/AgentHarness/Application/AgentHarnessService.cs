using OffceOs.Features.ResourceLogs.Application;
using OffceOs.Features.AgentHarness.Domain;
using OffceOs.Features.Agents.Domain;
using OffceOs.Features.AgentHarness.Application.Tools;
namespace OffceOs.Features.AgentHarness.Application;

internal sealed class AgentHarnessService : IAgentHarnessService
{
    private const int MaxIterations = 25;

    private readonly IAgentRepository _agentRepository;
    private readonly IAgentSessionRepository _agentSessionRepository;
    private readonly IAgentSandbox _agentSandbox;
    private readonly IAgentWorkQueueService _agentWorkQueueService;
    private readonly ICodeSessionService _codeSessionService;
    private readonly IResourceLogWriterService _resourceLogWriterService;
    private readonly TurnEventPublisher _turnEventPublisher;
    private readonly TurnContextBuilder _turnContextBuilder;
    private readonly LlmTurnExecutor _llmTurnExecutor;
    private readonly ToolExecutionLoop _toolExecutionLoop;

    public AgentHarnessService(
        IAgentRepository agentRepository,
        IAgentSessionRepository agentSessionRepository,
        IAgentSandbox agentSandbox,
        IAgentWorkQueueService agentWorkQueueService,
        ICodeSessionService codeSessionService,
        IResourceLogWriterService resourceLogWriterService,
        TurnEventPublisher turnEventPublisher,
        TurnContextBuilder turnContextBuilder,
        LlmTurnExecutor llmTurnExecutor,
        ToolExecutionLoop toolExecutionLoop)
    {
        _agentRepository = agentRepository;
        _agentSessionRepository = agentSessionRepository;
        _agentSandbox = agentSandbox;
        _agentWorkQueueService = agentWorkQueueService;
        _codeSessionService = codeSessionService;
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
        if (work.SessionId is null)
        {
            await FailAsync(work.Id, agentId, Guid.Empty, correlationId, "Work item has no session.", ct);
            return;
        }

        var sessionId = work.SessionId.Value;
        var started = Stopwatch.GetTimestamp();
        var toolCalls = 0;

        try
        {
            var agent = await _agentRepository.GetByAsync(
                new AgentFilter { Id = agentId, WorkspaceId = work.WorkspaceId },
                ct);
            if (agent is null)
            {
                await FailAsync(work.Id, agentId, sessionId, correlationId, "Agent not found.", ct);
                return;
            }

            var session = await _agentSessionRepository.GetByAsync(new AgentSessionFilter { Id = sessionId, AgentId = agentId }, ct);
            if (session is null)
            {
                await FailAsync(work.Id, agentId, sessionId, correlationId, "Session not found.", ct);
                return;
            }

            var deployment = await _agentSandbox.CreateAsync(
                session.Id,
                new Dictionary<string, string>(),
                new Dictionary<string, string>
                {
                    ["agent-id"] = agentId.ToString(),
                    ["session-id"] = session.Id.ToString(),
                },
                ct);
            session.MarkRunning(deployment.SandboxId, deployment.ServiceUrl ?? string.Empty, DateTime.UtcNow);
            await _agentSessionRepository.SaveAsync(session, ct);

            try
            {
                await _codeSessionService.PrepareAsync(session, deployment.SandboxId, deployment.ServiceUrl ?? string.Empty, ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                await FailAsync(work.Id, agentId, sessionId, correlationId, ex.Message, ct);
                return;
            }

            await _turnEventPublisher.PublishTurnStartedAsync(agentId, sessionId, correlationId, work.Content, ct);

            await using var tools = await _toolExecutionLoop.CreateSessionAsync(agent, sessionId, deployment.SandboxId, deployment.ServiceUrl ?? string.Empty, correlationId, work.DefinitionId, ct);
            var history = await _turnContextBuilder.BuildAsync(agentId, sessionId, correlationId, work.Content, ct);

            for (var iteration = 0; iteration < MaxIterations; iteration++)
            {
                var llmResult = await _llmTurnExecutor.ExecuteAsync(agent, sessionId, history, tools.Registry, iteration + 1, correlationId, ct);
                if (llmResult.IsFailure)
                {
                    await FailAsync(work.Id, agentId, sessionId, correlationId, llmResult.Error.Message, ct);
                    return;
                }

                var llmTurn = llmResult.Value;
                await _turnEventPublisher.PublishLlmCompletedAsync(
                    agentId,
                    sessionId,
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
                    await _turnEventPublisher.PublishMessageOutAsync(agentId, sessionId, correlationId, llmTurn.AssistantContent, ct);

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
                    await CompleteAsync(work.Id, agentId, session, deployment.SandboxId, deployment.ServiceUrl ?? string.Empty, correlationId, started, iteration + 1, toolCalls, ct);
                    return;
                }

                var toolLoop = await _toolExecutionLoop.ExecuteAsync(
                    agentId,
                    sessionId,
                    correlationId,
                    llmTurn.ToolCalls,
                    tools,
                    history,
                    toolCalls,
                    ct);
                toolCalls = toolLoop.TotalToolCalls;
                if (toolLoop.ShouldStop)
                {
                    await FailAsync(work.Id, agentId, sessionId, correlationId, toolLoop.ErrorMessage ?? "Tool execution stopped.", ct);
                    return;
                }
            }

            await FailAsync(work.Id, agentId, sessionId, correlationId, $"Hit max iterations ({MaxIterations}).", ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            await _resourceLogWriterService
                .ForAgent(agentId)
                .WithCorrelation(correlationId)
                .ErrorAsync(ex, "Agent work {WorkLogId} failed", workLogId, CancellationToken.None);
            await FailAsync(workLogId, agentId, work.SessionId ?? Guid.Empty, correlationId, ex.Message, CancellationToken.None);
        }
    }

    private async Task CompleteAsync(
        Guid workLogId,
        Guid agentId,
        AgentSessionRecord session,
        string sandboxId,
        string serviceUrl,
        string correlationId,
        long started,
        int iterations,
        int toolCalls,
        CancellationToken ct)
    {
        try
        {
            var artifact = await _codeSessionService.FinalizeAsync(session, sandboxId, serviceUrl, ct);
            if (artifact is not null)
            {
                session.RecordGitHubArtifact(artifact.Branch, artifact.CommitSha, artifact.PullRequestUrl, artifact.PullRequestNumber);
                await _agentSessionRepository.SaveAsync(session, ct);
            }
        }
        finally
        {
            await _agentSandbox.TerminateAsync(sandboxId, ct);
        }

        await _agentWorkQueueService.CompleteWorkAsync(workLogId, ct);
        session.MarkCompleted(DateTime.UtcNow);
        await _agentSessionRepository.SaveAsync(session, ct);
        var durationMs = (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        await _turnEventPublisher.PublishTurnCompletedAsync(agentId, session.Id, correlationId, durationMs, iterations, toolCalls, ct);
    }

    private async Task FailAsync(Guid workLogId, Guid agentId, Guid sessionId, string correlationId, string error, CancellationToken ct)
    {
        await _agentWorkQueueService.FailWorkAsync(workLogId, error, ct);
        if (sessionId != Guid.Empty)
        {
            var session = await _agentSessionRepository.GetByAsync(new AgentSessionFilter { Id = sessionId }, ct);
            if (session is not null)
            {
                if (!string.IsNullOrWhiteSpace(session.SandboxId))
                    await _agentSandbox.TerminateAsync(session.SandboxId, ct);
                session.MarkFailed(error, DateTime.UtcNow);
                await _agentSessionRepository.SaveAsync(session, ct);
            }
        }
        await _turnEventPublisher.PublishErrorAsync(agentId, sessionId, correlationId, error, ct);
    }
}
