namespace OffceOs.Features.ResourceLogs.Application;

public interface IResourceLogWriterService
{
    ResourceLogScope ForAgent(Guid agentId);
    ResourceLogScope ForChannel(Guid channelConnectionId);
    ResourceLogScope ForWorkspace(Guid workspaceId);
    ResourceLogScope ForControlPlane(Guid? workspaceId = null);
    ResourceLogScope ForResource(string resourceKind, Guid resourceId);
}
