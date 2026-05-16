using OffceOs.Common.Domain;

namespace OffceOs.Features.Agents.Domain;

public sealed record AgentUpdatedEvent(Guid AgentId) : DomainEvent;
