using OffceOs.Domain.Common;

namespace OffceOs.Domain.Features.Agents;

public sealed record AgentDeletedEvent(Guid AgentId, string? PodName, bool HasPod, Guid? OwnerId) : DomainEvent;
