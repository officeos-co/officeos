using OffceOs.Features.ResourceLogs.Application;
using OffceOs.Features.AgentHarness.Domain;
using OffceOs.Features.Agents.Domain;
using OffceOs.Common.Application;
using OffceOs.Features.AgentHarness.Application;

namespace OffceOs.Features.AgentHarness.EventHandlers;

internal sealed class QueueAgentWorkHandler : INotificationHandler<MessageReceivedEvent>
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentWorkQueueService _agentWorkQueueService;
    private readonly IResourceLogWriterService _resourceLogWriterService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public QueueAgentWorkHandler(
        IAgentRepository agentRepository,
        IAgentWorkQueueService agentWorkQueueService,
        IResourceLogWriterService resourceLogWriterService,
        IServiceScopeFactory serviceScopeFactory)
    {
        _agentRepository = agentRepository;
        _agentWorkQueueService = agentWorkQueueService;
        _resourceLogWriterService = resourceLogWriterService;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task Handle(MessageReceivedEvent notification, CancellationToken ct)
    {
        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = notification.AgentId }, ct);
        if (agent is null)
        {
            await _resourceLogWriterService
                .ForControlPlane()
                .WarningAsync("Ignoring message for missing agent {AgentId}", notification.AgentId, ct);
            return;
        }

        var work = await _agentWorkQueueService.QueueWorkAsync(new QueueAgentWorkRequest(
            notification.AgentId,
            agent.WorkspaceId,
            notification.Content,
            notification.CorrelationId,
            AgentWorkPurposeKinds.Normalize(notification.Purpose),
            notification.DefinitionId), ct);

        BackgroundWork.Run<IAgentHarnessService>(
            _serviceScopeFactory,
            harness => harness.RunWorkAsync(work.Id, CancellationToken.None));
    }
}
