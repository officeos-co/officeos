namespace EnterpriseAgentOs.Domain.Events;

public sealed record AtlasConnectionCreatedEvent(Guid ConnectionId, string Provider, string WorkspaceName) : DomainEvent;

public sealed record AtlasConnectionUpdatedEvent(Guid ConnectionId, string Provider) : DomainEvent;

public sealed record AtlasIndexRequestedEvent(Guid ConnectionId, Guid JobId) : DomainEvent;

public sealed record AtlasIndexCompletedEvent(Guid ConnectionId, Guid JobId, bool Success, int RecordsIndexed, string? Error) : DomainEvent;

public sealed record AtlasConnectorExecutedEvent(Guid ConnectionId, string Entity, string Action, bool Success, int DurationMs) : DomainEvent;
