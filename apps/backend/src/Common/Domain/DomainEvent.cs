namespace OffceOs.Common.Domain;

public abstract record DomainEvent : INotification
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
