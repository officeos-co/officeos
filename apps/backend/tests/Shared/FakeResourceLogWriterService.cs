using OffceOs.Application.Features.ResourceLogs;
using OffceOs.Domain.Features.ResourceLogs;

namespace OffceOs.Tests.Shared;

public sealed class FakeResourceLogWriterService : IResourceLogWriterService
{
    private readonly IResourceLogService _resourceLogService;

    public FakeResourceLogWriterService(IResourceLogService? resourceLogService = null)
        => _resourceLogService = resourceLogService ?? new FakeResourceLogService();

    public ResourceLogScope ForAgent(Guid agentId) =>
        new(_resourceLogService, ResourceLogKinds.Agent, resourceId: agentId, agentId: agentId);

    public ResourceLogScope ForChannel(Guid channelConnectionId) =>
        new(_resourceLogService, ResourceLogKinds.Channel, resourceId: channelConnectionId, channelConnectionId: channelConnectionId);

    public ResourceLogScope ForWorkspace(Guid workspaceId) =>
        new(_resourceLogService, ResourceLogKinds.Workspace, resourceId: workspaceId, workspaceId: workspaceId);

    public ResourceLogScope ForControlPlane(Guid? workspaceId = null) =>
        new(_resourceLogService, ResourceLogKinds.ControlPlane, resourceName: "control-plane", workspaceId: workspaceId);

    public ResourceLogScope ForResource(string resourceKind, Guid resourceId) =>
        new(_resourceLogService, resourceKind, resourceId: resourceId);
}
