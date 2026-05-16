using OffceOs.Common.Domain;

namespace OffceOs.Features.Agents.Domain;

public sealed record AgentCreatedEvent(Guid AgentId, string Provider, string? Model, Guid? OwnerId) : DomainEvent;
