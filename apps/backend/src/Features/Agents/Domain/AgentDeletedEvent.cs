using OffceOs.Common.Domain;

namespace OffceOs.Features.Agents.Domain;

public sealed record AgentDeletedEvent(Guid AgentId, Guid? OwnerId) : DomainEvent;
