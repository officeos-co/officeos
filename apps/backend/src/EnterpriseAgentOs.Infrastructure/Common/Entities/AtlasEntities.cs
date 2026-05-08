namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class IntegrationConnectionEntity
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string WorkspaceName { get; set; } = "default";
    public string DisplayName { get; set; } = string.Empty;
    public string RepositoriesJson { get; set; } = "[]";
    public string EntitiesJson { get; set; } = "[]";
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
    public Guid CreatedById { get; set; }
    public UserEntity? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class IntegrationIndexEntityStatusEntity
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public IntegrationConnectionEntity? Connection { get; set; }
    public string Entity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public string? Error { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class IntegrationIndexJobEntity
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public IntegrationConnectionEntity? Connection { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
    public int RecordsIndexed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public sealed class IntegrationIndexedRecordEntity
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public IntegrationConnectionEntity? Connection { get; set; }
    public string Entity { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public string RawJson { get; set; } = "{}";
    public DateTime? ExternalUpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class IntegrationActivityEntity
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public IntegrationConnectionEntity? Connection { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Entity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string DetailsJson { get; set; } = "{}";
    public bool Success { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class IntegrationRequestHistoryEntity
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public IntegrationConnectionEntity? Connection { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ParamsJson { get; set; } = "{}";
    public bool Success { get; set; }
    public int DurationMs { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
