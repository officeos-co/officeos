namespace OffceOs.Api.Features.Analytics;

public sealed record GlobalLogFiltersInput(
    string? Search = null,
    string? AgentName = null,
    AgentLogType? Type = null,
    int Skip = 0,
    int Limit = 50);

public sealed record AppendAgentLogInput(
    Guid AgentId,
    AgentLogType Type,
    string Content,
    string? Tool = null,
    string? Integration = null,
    string? Channel = null,
    string? CorrelationId = null);
