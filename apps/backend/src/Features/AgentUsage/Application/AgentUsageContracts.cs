namespace OffceOs.Application.Features.AgentUsage;

public sealed record AgentUsageToolCallRequest(string Name, string Arguments);

public sealed record AgentUsageResolveRequest(
    JsonElement RequestBody,
    string? AssistantContent,
    IReadOnlyList<AgentUsageToolCallRequest> ToolCalls,
    int? ReportedInputTokens,
    int? ReportedOutputTokens,
    int? CacheReadTokens = null,
    int? CacheWriteTokens = null,
    int? ReasoningTokens = null);

public sealed record AgentUsageResolutionResult(
    int InputTokens,
    int OutputTokens,
    int? CacheReadTokens,
    int? CacheWriteTokens,
    int? ReasoningTokens,
    bool EstimatedTokens,
    string Activity,
    IReadOnlyList<AgentUsageContextPartRecord> ContextParts)
{
    public long TotalTokens => (long)InputTokens + OutputTokens;
}

public sealed record AgentUsageRecordRequest(
    Guid AgentId,
    string CorrelationId,
    string Provider,
    string Model,
    int DurationMs,
    AgentUsageResolutionResult Usage,
    Guid? RunId,
    Guid? ParentRunId,
    string Outcome = AgentUsageOutcomeKinds.Success);

public sealed record AgentUsageAnalyticsRequest(
    DateTime From,
    DateTime To,
    Guid? WorkspaceId = null,
    Guid? AgentId = null,
    string? Provider = null,
    string? Model = null);

public sealed record AgentUsageDashboardResult(
    DateTime From,
    DateTime To,
    long TotalTokens,
    long InputTokens,
    long OutputTokens,
    long CacheReadTokens,
    long CacheWriteTokens,
    long ReasoningTokens,
    long TotalCredits,
    int Calls,
    int Sessions,
    double CacheHitRate,
    IReadOnlyList<AgentUsageDailyItem> Daily,
    IReadOnlyList<AgentUsageBreakdownItem> ByModel,
    IReadOnlyList<AgentUsageBreakdownItem> ByActivity,
    IReadOnlyList<AgentUsageBreakdownItem> CoreTools,
    IReadOnlyList<AgentUsageBreakdownItem> ShellCommands,
    IReadOnlyList<AgentUsageBreakdownItem> McpServers);

public sealed record AgentUsageDailyItem(
    DateTime Date,
    long Tokens,
    long InputTokens,
    long OutputTokens,
    long Credits,
    int Calls);

public sealed record AgentUsageBreakdownItem(
    string Name,
    long Tokens,
    long Credits,
    int Calls,
    double Share);

public sealed record AgentUsageModelsResult(IReadOnlyList<AgentUsageModelItem> Models);

public sealed record AgentUsageModelItem(
    string Model,
    int Calls,
    long InputTokens,
    long OutputTokens,
    long CacheReadTokens,
    long CacheWriteTokens,
    long Credits,
    double OutputTokensPerCall,
    double CacheHitRate,
    IReadOnlyList<AgentUsageBreakdownItem> Activities);

public sealed record AgentUsageCompareRequest(
    DateTime From,
    DateTime To,
    string ModelA,
    string ModelB,
    Guid? WorkspaceId = null,
    Guid? AgentId = null,
    string? Provider = null);

public sealed record AgentUsageCompareResult(
    string ModelA,
    string ModelB,
    AgentUsageCompareModelResult A,
    AgentUsageCompareModelResult B,
    IReadOnlyList<AgentUsageCompareCategoryItem> Categories);

public sealed record AgentUsageCompareModelResult(
    int Calls,
    int EditTurns,
    long TotalTokens,
    long InputTokens,
    long OutputTokens,
    long Credits,
    double OneShotRate,
    double RetryRate,
    double SelfCorrectionRate,
    double CreditsPerCall,
    double CreditsPerEdit,
    double OutputTokensPerCall,
    double CacheHitRate,
    double DelegationRate,
    double PlanningRate,
    double AverageToolsPerTurn);

public sealed record AgentUsageCompareCategoryItem(
    string Activity,
    double ModelAOneShotRate,
    int ModelACalls,
    double ModelBOneShotRate,
    int ModelBCalls);

public sealed record AgentUsageOptimizeResult(
    string SetupGrade,
    int SetupScore,
    long EstimatedTokenSavings,
    long EstimatedCreditSavings,
    IReadOnlyList<AgentUsageOptimizeFindingResult> Findings);

public sealed record AgentUsageOptimizeFindingResult(
    string Title,
    string Severity,
    string Description,
    long EstimatedTokenSavings,
    long EstimatedCreditSavings,
    string Recommendation);

public sealed record AgentUsageExport(
    AgentUsageDashboardResult Dashboard,
    IReadOnlyList<AgentUsageCallRecord> Calls);

public interface IAgentUsageService
{
    AgentUsageResolutionResult Resolve(AgentUsageResolveRequest request);
    Task<AgentUsageCallRecord> RecordCallAsync(AgentUsageRecordRequest request, CancellationToken ct = default);
}

public interface IAgentUsageAnalyticsService
{
    Task<AgentUsageDashboardResult> GetDashboardAsync(Guid ownerId, AgentUsageAnalyticsRequest request, CancellationToken ct = default);
    Task<AgentUsageModelsResult> GetModelsAsync(Guid ownerId, AgentUsageAnalyticsRequest request, CancellationToken ct = default);
    Task<AgentUsageCompareResult> CompareModelsAsync(Guid ownerId, AgentUsageCompareRequest request, CancellationToken ct = default);
    Task<AgentUsageOptimizeResult> GetOptimizeAsync(Guid ownerId, AgentUsageAnalyticsRequest request, CancellationToken ct = default);
    Task<AgentUsageExport> ExportAsync(Guid ownerId, AgentUsageAnalyticsRequest request, CancellationToken ct = default);
}
