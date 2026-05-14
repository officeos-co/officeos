namespace OffceOs.Domain.Events;

public abstract record DomainEvent : INotification
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
