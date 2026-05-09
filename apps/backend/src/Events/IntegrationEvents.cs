namespace OffceOs.Domain.Events;

public sealed record IntegrationConnectionCreatedEvent(Guid ConnectionId, string Provider, string WorkspaceName) : DomainEvent;

public sealed record IntegrationConnectionUpdatedEvent(Guid ConnectionId, string Provider) : DomainEvent;

public sealed record IntegrationIndexRequestedEvent(Guid ConnectionId, Guid JobId) : DomainEvent;

public sealed record IntegrationIndexCompletedEvent(Guid ConnectionId, Guid JobId, bool Success, int RecordsIndexed, string? Error) : DomainEvent;

public sealed record IntegrationExecutedEvent(Guid ConnectionId, string Entity, string Action, bool Success, int DurationMs) : DomainEvent;
