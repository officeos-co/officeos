namespace EnterpriseAgentOs.Api.Features.Agents;

public sealed record CreateBrowserResourceInput(string? DisplayName);

public sealed record CreateMemoryStoreInput(string? DisplayName);

public sealed record UpsertMemoryStoreEntryInput(
    Guid MemoryStoreId,
    string Key,
    string Content);

public sealed record BrowserResourcePayload(
    Guid Id,
    Guid OwnerId,
    string DisplayName,
    Guid? CurrentAgentId,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record MemoryStorePayload(
    Guid Id,
    Guid OwnerId,
    string DisplayName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<MemoryStoreEntryPayload>? Entries = null);

public sealed record MemoryStoreEntryPayload(
    Guid Id,
    Guid MemoryStoreId,
    string Key,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record AgentSessionResourceAttachmentPayload(
    Guid Id,
    Guid AgentId,
    Guid SessionId,
    string ResourceType,
    Guid ResourceId,
    string AccessMode,
    string? Instructions,
    DateTime CreatedAt);
