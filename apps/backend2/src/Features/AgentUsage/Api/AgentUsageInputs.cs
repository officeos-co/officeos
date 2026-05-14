namespace OffceOs.Api.Features.AgentUsage;

public sealed record AgentUsageInput(
    DateTime From,
    DateTime To,
    Guid? WorkspaceId = null,
    Guid? AgentId = null,
    string? Provider = null,
    string? Model = null);

public sealed record AgentUsageCompareInput(
    DateTime From,
    DateTime To,
    string ModelA,
    string ModelB,
    Guid? WorkspaceId = null,
    Guid? AgentId = null,
    string? Provider = null);
