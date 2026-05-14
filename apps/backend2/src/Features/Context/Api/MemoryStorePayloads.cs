namespace OffceOs.Api.Features.Context;

public sealed record CreateMemoryStoreInput(string? DisplayName);

public sealed record UpsertMemoryStoreEntryInput(
    Guid MemoryStoreId,
    string Key,
    string Content);

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
