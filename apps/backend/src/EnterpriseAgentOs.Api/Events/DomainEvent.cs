using MediatR;

namespace EnterpriseAgentOs.Domain.Events;

public abstract record DomainEvent : INotification
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
