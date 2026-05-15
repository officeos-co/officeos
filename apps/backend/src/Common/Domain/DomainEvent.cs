namespace OffceOs.Domain.Common;

public abstract record DomainEvent : INotification
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
