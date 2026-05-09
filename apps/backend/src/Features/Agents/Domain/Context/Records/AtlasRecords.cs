namespace EnterpriseAgentOs.Domain.Features.Agents.Integrations;

public enum IntegrationProviderType
{
    GitHub,
}

public enum IntegrationConnectionStatus
{
    NeedsAuth,
    Indexing,
    Ready,
    Failed,
}

public enum IntegrationIndexJobStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
}

public enum IntegrationIndexEntityStatus
{
    Initializing,
    Indexing,
    Preview,
    Ready,
    Failed,
}

public enum IntegrationRequestType
{
    Direct,
    Search,
}

public sealed class IntegrationConnectionRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public IntegrationProviderType Provider { get; init; }
    public string WorkspaceName { get; init; } = "default";
    public string DisplayName { get; init; } = string.Empty;
    public string RepositoriesJson { get; init; } = "[]";
    public string EntitiesJson { get; init; } = "[]";
    public IntegrationConnectionStatus Status { get; init; } = IntegrationConnectionStatus.NeedsAuth;
    public string? Error { get; init; }
    public Guid CreatedById { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<IntegrationIndexEntityStatusRecord> EntityStatuses { get; init; } = [];
}

public sealed class IntegrationIndexEntityStatusRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ConnectionId { get; init; }
    public string Entity { get; init; } = string.Empty;
    public IntegrationIndexEntityStatus Status { get; init; } = IntegrationIndexEntityStatus.Initializing;
    public int RecordCount { get; init; }
    public string? Error { get; init; }
    public DateTime? LastSyncedAt { get; init; }
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

public sealed class IntegrationIndexJobRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ConnectionId { get; init; }
    public IntegrationIndexJobStatus Status { get; init; } = IntegrationIndexJobStatus.Queued;
    public string? Error { get; init; }
    public int RecordsIndexed { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public sealed class IntegrationIndexedRecordRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ConnectionId { get; init; }
    public string Entity { get; init; } = string.Empty;
    public string ExternalId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string SearchText { get; init; } = string.Empty;
    public string RawJson { get; init; } = "{}";
    public DateTime? ExternalUpdatedAt { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

public sealed class IntegrationActivityRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ConnectionId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string? Entity { get; init; }
    public string Message { get; init; } = string.Empty;
    public string DetailsJson { get; init; } = "{}";
    public bool Success { get; init; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public sealed class IntegrationRequestHistoryRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ConnectionId { get; init; }
    public IntegrationRequestType Type { get; init; }
    public string Entity { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string ParamsJson { get; init; } = "{}";
    public bool Success { get; init; }
    public int DurationMs { get; init; }
    public string? Error { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
