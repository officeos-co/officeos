namespace OffceOs.Api.Features.Agents;

public sealed record CreateBrowserResourceInput(string? DisplayName);

public sealed record BrowserResourcePayload(
    Guid Id,
    Guid OwnerId,
    string DisplayName,
    Guid? CurrentAgentId,
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
