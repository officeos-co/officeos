namespace EnterpriseAgentOs.Domain.Features.Atlas;

public enum AtlasConnectorProvider
{
    GitHub,
}

public enum AtlasConnectorStatus
{
    NeedsAuth,
    Indexing,
    Ready,
    Failed,
}

public enum AtlasIndexJobStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
}

public enum AtlasEntityStatus
{
    Initializing,
    Indexing,
    Preview,
    Ready,
    Failed,
}

public enum AtlasRequestType
{
    Direct,
    Search,
}

public sealed class AtlasConnectorConnectionRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public AtlasConnectorProvider Provider { get; init; }
    public string WorkspaceName { get; init; } = "default";
    public string DisplayName { get; init; } = string.Empty;
    public string RepositoriesJson { get; init; } = "[]";
    public string EntitiesJson { get; init; } = "[]";
    public AtlasConnectorStatus Status { get; init; } = AtlasConnectorStatus.NeedsAuth;
    public string? Error { get; init; }
    public Guid CreatedById { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<AtlasEntityStatusRecord> EntityStatuses { get; init; } = [];
}

public sealed class AtlasEntityStatusRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ConnectionId { get; init; }
    public string Entity { get; init; } = string.Empty;
    public AtlasEntityStatus Status { get; init; } = AtlasEntityStatus.Initializing;
    public int RecordCount { get; init; }
    public string? Error { get; init; }
    public DateTime? LastSyncedAt { get; init; }
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

public sealed class AtlasIndexJobRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ConnectionId { get; init; }
    public AtlasIndexJobStatus Status { get; init; } = AtlasIndexJobStatus.Queued;
    public string? Error { get; init; }
    public int RecordsIndexed { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public sealed class AtlasIndexedRecordRecord
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

public sealed class AtlasActivityRecord
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

public sealed class AtlasRequestHistoryRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ConnectionId { get; init; }
    public AtlasRequestType Type { get; init; }
    public string Entity { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string ParamsJson { get; init; } = "{}";
    public bool Success { get; init; }
    public int DurationMs { get; init; }
    public string? Error { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public sealed record AtlasConnectorTypeRecord
{
    public string Name { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public string? AuthorUrl { get; init; }
    public string? DocumentationUrl { get; init; }
    public string? RepositoryUrl { get; init; }
    public string Logo { get; init; } = string.Empty;
    public string? ToolsJson { get; init; }
    public string Category { get; init; } = "developer";
    public string? OauthProvider { get; init; }
    public string? OauthScopesJson { get; init; }
    public bool OauthConfigured { get; init; }
    public bool IsBuiltin { get; init; } = true;
    public IReadOnlyList<string> Entities { get; init; } = [];
}
