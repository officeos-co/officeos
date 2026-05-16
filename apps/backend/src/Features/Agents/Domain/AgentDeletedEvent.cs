using OffceOs.Common.Domain;

namespace OffceOs.Features.Agents.Domain;

public sealed record AgentDeletedEvent(Guid AgentId, string? PodName, bool HasPod, Guid? OwnerId) : DomainEvent;
