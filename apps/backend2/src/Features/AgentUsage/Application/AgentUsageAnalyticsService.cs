namespace OffceOs.Application.Features.AgentUsage;

internal sealed class AgentUsageAnalyticsService : IAgentUsageAnalyticsService
{
    private readonly IAgentUsageRepository _agentUsageRepository;
    private readonly IAgentLogRepository _agentLogRepository;

    public AgentUsageAnalyticsService(
        IAgentUsageRepository agentUsageRepository,
        IAgentLogRepository agentLogRepository)
    {
        _agentUsageRepository = agentUsageRepository;
        _agentLogRepository = agentLogRepository;
    }

    public async Task<AgentUsageDashboardResult> GetDashboardAsync(Guid ownerId, AgentUsageAnalyticsRequest request, CancellationToken ct = default)
    {
        var (from, toExclusive) = NormalizeRange(request.From, request.To);
        var calls = await LoadCallsAsync(ownerId, request, from, toExclusive, ct);
        var logs = await LoadToolLogsAsync(ownerId, request, from, toExclusive, ct);

        return BuildDashboard(from, toExclusive, calls, logs);
    }

    public async Task<AgentUsageModelsResult> GetModelsAsync(Guid ownerId, AgentUsageAnalyticsRequest request, CancellationToken ct = default)
    {
        var (from, toExclusive) = NormalizeRange(request.From, request.To);
        var calls = await LoadCallsAsync(ownerId, request, from, toExclusive, ct);

        var models = calls
            .GroupBy(c => c.Model)
            .Select(g =>
            {
                var list = g.ToList();
                var input = list.Sum(c => c.InputTokens);
                var cacheRead = list.Sum(c => c.CacheReadTokens ?? 0);
                return new AgentUsageModelItem(
                    g.Key,
                    list.Count,
                    input,
                    list.Sum(c => c.OutputTokens),
                    cacheRead,
                    list.Sum(c => c.CacheWriteTokens ?? 0),
                    list.Sum(c => c.Credits),
                    Average(list, c => c.OutputTokens),
                    CacheHitRate(input, cacheRead),
                    BuildBreakdown(list, c => c.Activity, c => c.TotalTokens, c => c.Credits, c => c.CorrelationId));
            })
            .OrderByDescending(m => m.Credits)
            .ThenBy(m => m.Model)
            .ToList();

        return new AgentUsageModelsResult(models);
    }

    public async Task<AgentUsageCompareResult> CompareModelsAsync(Guid ownerId, AgentUsageCompareRequest request, CancellationToken ct = default)
    {
        var analyticsRequest = new AgentUsageAnalyticsRequest(
            request.From,
            request.To,
            request.WorkspaceId,
            request.AgentId,
            request.Provider);
        var (from, toExclusive) = NormalizeRange(request.From, request.To);
        var calls = await LoadCallsAsync(ownerId, analyticsRequest, from, toExclusive, ct);
        var logs = await LoadToolLogsAsync(ownerId, analyticsRequest, from, toExclusive, ct);

        var modelACalls = calls.Where(c => c.Model == request.ModelA).ToList();
        var modelBCalls = calls.Where(c => c.Model == request.ModelB).ToList();
        var categories = calls
            .Where(c => c.Model == request.ModelA || c.Model == request.ModelB)
            .GroupBy(c => c.Activity)
            .Select(g => new AgentUsageCompareCategoryItem(
                g.Key,
                OneShotRate(g.Where(c => c.Model == request.ModelA), logs),
                g.Count(c => c.Model == request.ModelA),
                OneShotRate(g.Where(c => c.Model == request.ModelB), logs),
                g.Count(c => c.Model == request.ModelB)))
            .OrderByDescending(c => c.ModelACalls + c.ModelBCalls)
            .ToList();

        return new AgentUsageCompareResult(
            request.ModelA,
            request.ModelB,
            BuildCompareModel(modelACalls, logs),
            BuildCompareModel(modelBCalls, logs),
            categories);
    }

    public async Task<AgentUsageOptimizeResult> GetOptimizeAsync(Guid ownerId, AgentUsageAnalyticsRequest request, CancellationToken ct = default)
    {
        var (from, toExclusive) = NormalizeRange(request.From, request.To);
        var calls = await LoadCallsAsync(ownerId, request, from, toExclusive, ct);
        var logs = await LoadToolLogsAsync(ownerId, request, from, toExclusive, ct);
        var findings = new List<AgentUsageOptimizeFindingResult>();

        AddReadEditFinding(calls, logs, findings);
        AddRepeatedReadFinding(logs, findings);
        AddToolSchemaFinding(calls, findings);
        AddRetryFinding(calls, logs, findings);
        AddLowOutputFinding(calls, findings);
        AddShellOutputFinding(logs, findings);

        var orderedFindings = findings
            .OrderByDescending(f => SeverityRank(f.Severity))
            .ThenByDescending(f => f.EstimatedCreditSavings)
            .ThenByDescending(f => f.EstimatedTokenSavings)
            .ToList();

        var score = Math.Max(0, 100 - orderedFindings.Sum(f => SeverityRank(f.Severity) * 8));
        return new AgentUsageOptimizeResult(
            Grade(score),
            score,
            orderedFindings.Sum(f => f.EstimatedTokenSavings),
            orderedFindings.Sum(f => f.EstimatedCreditSavings),
            orderedFindings);
    }

    public async Task<AgentUsageExport> ExportAsync(Guid ownerId, AgentUsageAnalyticsRequest request, CancellationToken ct = default)
    {
        var (from, toExclusive) = NormalizeRange(request.From, request.To);
        var calls = await LoadCallsAsync(ownerId, request, from, toExclusive, ct);
        var logs = await LoadToolLogsAsync(ownerId, request, from, toExclusive, ct);
        return new AgentUsageExport(BuildDashboard(from, toExclusive, calls, logs), calls);
    }

    private async Task<List<AgentUsageCallRecord>> LoadCallsAsync(
        Guid ownerId,
        AgentUsageAnalyticsRequest request,
        DateTime from,
        DateTime toExclusive,
        CancellationToken ct)
    {
        return await _agentUsageRepository.ListAsync(new AgentUsageFilter
        {
            OwnerId = ownerId,
            WorkspaceId = request.WorkspaceId,
            AgentId = request.AgentId,
            Provider = request.Provider,
            Model = request.Model,
            FromInclusive = from,
            ToExclusive = toExclusive,
        }, ct);
    }

    private async Task<List<AgentLogRecord>> LoadToolLogsAsync(
        Guid ownerId,
        AgentUsageAnalyticsRequest request,
        DateTime from,
        DateTime toExclusive,
        CancellationToken ct)
    {
        return await _agentLogRepository.ListAsync(new AgentLogFilter
        {
            OwnerId = ownerId,
            WorkspaceId = request.WorkspaceId,
            AgentId = request.AgentId,
            Types = [AgentLogType.ToolCall, AgentLogType.ToolResult, AgentLogType.Error, AgentLogType.System],
            FromInclusive = from,
            ToExclusive = toExclusive,
        }, new AgentLogListOptions
        {
            Sort = AgentLogSort.TimeAscending,
        }, ct);
    }

    private static AgentUsageDashboardResult BuildDashboard(
        DateTime from,
        DateTime toExclusive,
        IReadOnlyList<AgentUsageCallRecord> calls,
        IReadOnlyList<AgentLogRecord> logs)
    {
        var totalInput = calls.Sum(c => c.InputTokens);
        var cacheRead = calls.Sum(c => c.CacheReadTokens ?? 0);
        return new AgentUsageDashboardResult(
            from,
            toExclusive,
            calls.Sum(c => c.TotalTokens),
            totalInput,
            calls.Sum(c => c.OutputTokens),
            cacheRead,
            calls.Sum(c => c.CacheWriteTokens ?? 0),
            calls.Sum(c => c.ReasoningTokens ?? 0),
            calls.Sum(c => c.Credits),
            calls.Count,
            calls.Select(c => c.RunId?.ToString("N") ?? c.CorrelationId).Distinct(StringComparer.Ordinal).Count(),
            CacheHitRate(totalInput, cacheRead),
            BuildDaily(from, toExclusive, calls),
            BuildBreakdown(calls, c => c.Model, c => c.TotalTokens, c => c.Credits, c => c.CorrelationId),
            BuildBreakdown(calls, c => c.Activity, c => c.TotalTokens, c => c.Credits, c => c.CorrelationId),
            BuildToolBreakdown(logs, IsCoreTool),
            BuildShellCommandBreakdown(logs),
            BuildToolBreakdown(logs, IsMcpOrIntegrationTool));
    }

    private static IReadOnlyList<AgentUsageDailyItem> BuildDaily(DateTime from, DateTime toExclusive, IReadOnlyList<AgentUsageCallRecord> calls)
    {
        var byDate = calls.GroupBy(c => c.Time.Date).ToDictionary(g => g.Key, g => g.ToList());
        return Enumerable.Range(0, (int)Math.Ceiling((toExclusive - from).TotalDays))
            .Select(offset =>
            {
                var date = from.Date.AddDays(offset);
                var rows = byDate.TryGetValue(date, out var list) ? list : [];
                return new AgentUsageDailyItem(
                    date,
                    rows.Sum(c => c.TotalTokens),
                    rows.Sum(c => c.InputTokens),
                    rows.Sum(c => c.OutputTokens),
                    rows.Sum(c => c.Credits),
                    rows.Count);
            })
            .ToList();
    }

    private static IReadOnlyList<AgentUsageBreakdownItem> BuildToolBreakdown(
        IReadOnlyList<AgentLogRecord> logs,
        Func<string, bool> predicate)
    {
        var toolCalls = logs
            .Where(l => l.Type == AgentLogType.ToolCall && !string.IsNullOrWhiteSpace(l.Tool) && predicate(l.Tool))
            .ToList();

        return BuildBreakdown(toolCalls, l => l.Tool!, _ => 0, _ => 0, l => l.CorrelationId ?? l.Id.ToString("N"));
    }

    private static IReadOnlyList<AgentUsageBreakdownItem> BuildShellCommandBreakdown(IReadOnlyList<AgentLogRecord> logs)
    {
        var shellCalls = logs
            .Where(l => l.Type == AgentLogType.ToolCall && l.Tool == "shell")
            .Select(l => FirstShellCommandToken(l.Content))
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!)
            .ToList();

        var total = Math.Max(1, shellCalls.Count);
        return shellCalls
            .GroupBy(c => c)
            .Select(g => new AgentUsageBreakdownItem(g.Key, 0, 0, g.Count(), (double)g.Count() / total))
            .OrderByDescending(i => i.Calls)
            .ThenBy(i => i.Name)
            .Take(12)
            .ToList();
    }

    private static IReadOnlyList<AgentUsageBreakdownItem> BuildBreakdown<T>(
        IEnumerable<T> rows,
        Func<T, string> name,
        Func<T, long> tokens,
        Func<T, long> credits,
        Func<T, string?> callKey)
    {
        var materialized = rows.ToList();
        var totalTokens = Math.Max(1, materialized.Sum(tokens));
        return materialized
            .GroupBy(name)
            .Select(g => new AgentUsageBreakdownItem(
                g.Key,
                g.Sum(tokens),
                g.Sum(credits),
                g.Select(callKey).Where(k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.Ordinal).Count(),
                (double)g.Sum(tokens) / totalTokens))
            .OrderByDescending(i => i.Credits)
            .ThenByDescending(i => i.Tokens)
            .ThenByDescending(i => i.Calls)
            .ThenBy(i => i.Name)
            .Take(12)
            .ToList();
    }

    private static AgentUsageCompareModelResult BuildCompareModel(
        IReadOnlyList<AgentUsageCallRecord> calls,
        IReadOnlyList<AgentLogRecord> logs)
    {
        var input = calls.Sum(c => c.InputTokens);
        var output = calls.Sum(c => c.OutputTokens);
        var credits = calls.Sum(c => c.Credits);
        var cacheRead = calls.Sum(c => c.CacheReadTokens ?? 0);
        var editTurns = CountTurnsWithTools(calls, logs, "file_edit", "file_write");
        var toolCallCount = CountToolCalls(calls, logs);

        return new AgentUsageCompareModelResult(
            calls.Count,
            editTurns,
            input + output,
            input,
            output,
            credits,
            OneShotRate(calls, logs),
            RetryRate(calls, logs),
            SelfCorrectionRate(calls, logs),
            calls.Count == 0 ? 0 : (double)credits / calls.Count,
            editTurns == 0 ? 0 : (double)credits / editTurns,
            calls.Count == 0 ? 0 : (double)output / calls.Count,
            CacheHitRate(input, cacheRead),
            Rate(calls, c => c.Activity == AgentUsageActivityKinds.Delegation),
            Rate(calls, c => c.Activity == AgentUsageActivityKinds.Planning),
            calls.Count == 0 ? 0 : (double)toolCallCount / calls.Count);
    }

    private static double OneShotRate(IEnumerable<AgentUsageCallRecord> calls, IReadOnlyList<AgentLogRecord> logs)
    {
        var list = calls.ToList();
        if (list.Count == 0) return 0;
        var retryCorrelationIds = RetryCorrelationIds(logs);
        var successful = list.Count(c => !retryCorrelationIds.Contains(c.CorrelationId));
        return (double)successful / list.Count;
    }

    private static double RetryRate(IEnumerable<AgentUsageCallRecord> calls, IReadOnlyList<AgentLogRecord> logs)
    {
        var list = calls.ToList();
        if (list.Count == 0) return 0;
        var retryCorrelationIds = RetryCorrelationIds(logs);
        return (double)list.Count(c => retryCorrelationIds.Contains(c.CorrelationId)) / list.Count;
    }

    private static double SelfCorrectionRate(IEnumerable<AgentUsageCallRecord> calls, IReadOnlyList<AgentLogRecord> logs)
    {
        var list = calls.ToList();
        if (list.Count == 0) return 0;
        var errorCorrelationIds = logs
            .Where(l => l.Type.ToString().StartsWith("Error", StringComparison.Ordinal) || l.Type == AgentLogType.Error)
            .Select(l => l.CorrelationId)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!)
            .ToHashSet(StringComparer.Ordinal);
        return (double)list.Count(c => errorCorrelationIds.Contains(c.CorrelationId)) / list.Count;
    }

    private static void AddReadEditFinding(
        IReadOnlyList<AgentUsageCallRecord> calls,
        IReadOnlyList<AgentLogRecord> logs,
        List<AgentUsageOptimizeFindingResult> findings)
    {
        var reads = logs.Count(l => l.Type == AgentLogType.ToolCall && l.Tool is "file_read" or "content_search" or "glob_search");
        var edits = logs.Count(l => l.Type == AgentLogType.ToolCall && l.Tool is "file_edit" or "file_write");
        if (edits == 0) return;

        var ratio = (double)reads / edits;
        if (ratio >= 4) return;

        var savings = calls.Sum(c => c.InputTokens) / 20;
        findings.Add(new AgentUsageOptimizeFindingResult(
            "Agents edit with too little context",
            ratio < 2 ? "High" : "Medium",
            $"{reads} read/search calls and {edits} edit/write calls. A healthy editing loop usually reads several times before writing.",
            savings,
            ProviderRegistry.ToCredits(calls.OrderByDescending(c => c.Credits).FirstOrDefault()?.Model ?? ProviderRegistry.DefaultModel, savings),
            "Read the target file and nearby callers before editing; prefer precise file and line references in prompts."));
    }

    private static void AddRepeatedReadFinding(IReadOnlyList<AgentLogRecord> logs, List<AgentUsageOptimizeFindingResult> findings)
    {
        var repeated = logs
            .Where(l => l.Type == AgentLogType.ToolCall && l.Tool == "file_read")
            .Select(l => ReadJsonString(l.Content, "path"))
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .GroupBy(p => p!)
            .Where(g => g.Count() > 2)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .ToList();

        if (repeated.Count == 0) return;

        var redundantReads = repeated.Sum(g => g.Count() - 1);
        findings.Add(new AgentUsageOptimizeFindingResult(
            "Agents re-read the same files",
            redundantReads > 10 ? "High" : "Medium",
            $"Detected {redundantReads} redundant reads. Top repeats: {string.Join(", ", repeated.Select(g => $"{g.Key} ({g.Count()}x)"))}.",
            redundantReads * 800,
            redundantReads * 800,
            "Point agents at exact file ranges and keep relevant snippets in the task context when follow-up work is expected."));
    }

    private static void AddToolSchemaFinding(IReadOnlyList<AgentUsageCallRecord> calls, List<AgentUsageOptimizeFindingResult> findings)
    {
        var schemaTokens = calls.SelectMany(c => c.ContextParts)
            .Where(p => p.Kind == AgentUsageContextPartKinds.ToolSchema || p.Kind == AgentUsageContextPartKinds.DeferredToolCatalog)
            .Sum(p => p.Tokens);
        var inputTokens = calls.Sum(c => c.InputTokens);
        if (inputTokens == 0 || schemaTokens < inputTokens / 4) return;

        findings.Add(new AgentUsageOptimizeFindingResult(
            "Tool schemas dominate context",
            schemaTokens > inputTokens / 2 ? "High" : "Medium",
            $"Tool metadata accounts for about {schemaTokens} of {inputTokens} input tokens.",
            schemaTokens / 3,
            schemaTokens / 3,
            "Preload fewer tools by default and rely on deferred discovery for rarely used integration and MCP tools."));
    }

    private static void AddRetryFinding(
        IReadOnlyList<AgentUsageCallRecord> calls,
        IReadOnlyList<AgentLogRecord> logs,
        List<AgentUsageOptimizeFindingResult> findings)
    {
        var retryRate = RetryRate(calls, logs);
        if (retryRate < 0.25) return;

        var savings = calls.Sum(c => c.TotalTokens) / 10;
        findings.Add(new AgentUsageOptimizeFindingResult(
            "Retry loops are expensive",
            retryRate > 0.45 ? "High" : "Medium",
            $"{retryRate:P0} of calls are in turns with repeated tool use or errors.",
            savings,
            savings,
            "Tighten tool validation failures, surface actionable tool errors, and prefer smaller targeted edits."));
    }

    private static void AddLowOutputFinding(IReadOnlyList<AgentUsageCallRecord> calls, List<AgentUsageOptimizeFindingResult> findings)
    {
        var lowYield = calls.Where(c => c.InputTokens > 4000 && c.OutputTokens < 200).ToList();
        if (lowYield.Count == 0) return;

        var savings = lowYield.Sum(c => c.InputTokens) / 5;
        findings.Add(new AgentUsageOptimizeFindingResult(
            "Large context produces little output",
            lowYield.Count > 5 ? "High" : "Medium",
            $"{lowYield.Count} calls used large input context but produced short responses.",
            savings,
            savings,
            "Trim older tool output and summarize inactive context before asking for small answers."));
    }

    private static void AddShellOutputFinding(IReadOnlyList<AgentLogRecord> logs, List<AgentUsageOptimizeFindingResult> findings)
    {
        var noisyShell = logs.Where(l => l.Type == AgentLogType.ToolResult && l.Tool == "shell" && l.Content.Length > 8000).ToList();
        if (noisyShell.Count == 0) return;

        findings.Add(new AgentUsageOptimizeFindingResult(
            "Shell output is too large",
            noisyShell.Count > 3 ? "High" : "Medium",
            $"{noisyShell.Count} shell results exceeded 8k characters.",
            noisyShell.Sum(l => l.Content.Length / 4) / 2,
            noisyShell.Sum(l => l.Content.Length / 4) / 2,
            "Use targeted commands with line limits, filters, or summary output instead of dumping full logs."));
    }

    private static HashSet<string> RetryCorrelationIds(IReadOnlyList<AgentLogRecord> logs)
    {
        return logs
            .Where(l => l.Type == AgentLogType.ToolCall && !string.IsNullOrWhiteSpace(l.CorrelationId))
            .GroupBy(l => l.CorrelationId!)
            .Where(g => g.GroupBy(l => $"{l.Tool}:{l.Content}").Any(toolGroup => toolGroup.Count() > 1))
            .Select(g => g.Key)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static int CountTurnsWithTools(IReadOnlyList<AgentUsageCallRecord> calls, IReadOnlyList<AgentLogRecord> logs, params string[] tools)
    {
        var toolSet = tools.ToHashSet(StringComparer.Ordinal);
        var correlationIds = logs
            .Where(l => l.Type == AgentLogType.ToolCall && l.Tool is not null && toolSet.Contains(l.Tool))
            .Select(l => l.CorrelationId)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!)
            .ToHashSet(StringComparer.Ordinal);

        return calls.Count(c => correlationIds.Contains(c.CorrelationId));
    }

    private static int CountToolCalls(IReadOnlyList<AgentUsageCallRecord> calls, IReadOnlyList<AgentLogRecord> logs)
    {
        var correlationIds = calls.Select(c => c.CorrelationId).ToHashSet(StringComparer.Ordinal);
        return logs.Count(l => l.Type == AgentLogType.ToolCall && l.CorrelationId is not null && correlationIds.Contains(l.CorrelationId));
    }

    private static bool IsCoreTool(string tool) => tool is
        "shell" or "file_read" or "file_write" or "file_edit" or "content_search" or "glob_search" or
        "memory_store" or "memory_recall" or "memory_forget" or "http_request" or "web_fetch" or
        "tool_search" or "task_create" or "task_list" or "task_get" or "task_update" or "agent_spawn";

    private static bool IsMcpOrIntegrationTool(string tool) => !IsCoreTool(tool)
        || tool.Contains("__", StringComparison.Ordinal);

    private static string? FirstShellCommandToken(string content)
    {
        var command = ReadJsonString(content, "command") ?? content;
        var trimmed = command.Trim();
        if (trimmed.Length == 0) return null;
        var first = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return first?.Split('/').LastOrDefault();
    }

    private static string? ReadJsonString(string json, string property)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(property, out var value)
                && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static (DateTime From, DateTime ToExclusive) NormalizeRange(DateTime from, DateTime to)
    {
        var start = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(to.Date, DateTimeKind.Utc).AddDays(1);
        if (end <= start)
            end = start.AddDays(1);

        var maxEnd = start.AddDays(366);
        if (end > maxEnd)
            end = maxEnd;

        return (start, end);
    }

    private static double CacheHitRate(long inputTokens, long cacheReadTokens)
    {
        var denominator = inputTokens + cacheReadTokens;
        return denominator <= 0 ? 0 : (double)cacheReadTokens / denominator;
    }

    private static double Average(IReadOnlyList<AgentUsageCallRecord> calls, Func<AgentUsageCallRecord, long> value) =>
        calls.Count == 0 ? 0 : (double)calls.Sum(value) / calls.Count;

    private static double Rate(IReadOnlyList<AgentUsageCallRecord> calls, Func<AgentUsageCallRecord, bool> predicate) =>
        calls.Count == 0 ? 0 : (double)calls.Count(predicate) / calls.Count;

    private static int SeverityRank(string severity) => severity switch
    {
        "High" => 3,
        "Medium" => 2,
        "Low" => 1,
        _ => 0,
    };

    private static string Grade(int score) => score switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 70 => "C",
        >= 60 => "D",
        _ => "F",
    };
}
