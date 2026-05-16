using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.ResourceLogs;
namespace OffceOs.Application.Features.Agents;

public sealed record AgentHealthResult(
    string Status,
    string State,
    string Reason,
    string Message,
    Guid? LastBootstrapWorkLogId,
    DateTime? LastBootstrapAt,
    DateTime? LastSuccessfulBootstrapAt);

internal static class AgentHealthProjection
{
    private static readonly TimeSpan IdleAfter = TimeSpan.FromSeconds(5);

    public static AgentHealthResult From(AgentRecord agent, IReadOnlyList<ResourceLogRecord> logs)
        => From(agent, logs, DateTime.UtcNow);

    internal static AgentHealthResult From(AgentRecord agent, IReadOnlyList<ResourceLogRecord> logs, DateTime now)
    {
        var ordered = logs
            .Where(log => log.AgentId == agent.Id)
            .OrderByDescending(log => ActivityAt(log))
            .ToList();

        var bootstrapWork = ordered
            .Where(log => log.WorkPurpose == AgentWorkPurposeKinds.Bootstrap)
            .ToList();
        var activeBootstrapRuns = agent.ActiveDefinitionId.HasValue
            ? bootstrapWork.Where(log => log.DefinitionId == agent.ActiveDefinitionId).ToList()
            : bootstrapWork;
        var latestBootstrap = activeBootstrapRuns.FirstOrDefault();
        var latestAnyBootstrap = bootstrapWork.FirstOrDefault();
        var lastSuccessfulBootstrap = activeBootstrapRuns
            .FirstOrDefault(log => IsCompleted(log.WorkStatus));

        if (latestBootstrap is null)
        {
            if (latestAnyBootstrap is not null && latestAnyBootstrap.DefinitionId != agent.ActiveDefinitionId)
            {
                return new AgentHealthResult(
                    "Pending",
                    "orange",
                    "DefinitionChangedNeedsBootstrap",
                    "Agent definition changed and has not bootstrapped successfully yet.",
                    latestAnyBootstrap.Id,
                    latestAnyBootstrap.Time,
                    lastSuccessfulBootstrap?.CompletedAt);
            }

            return new AgentHealthResult(
                "Pending",
                "orange",
                "BootstrapMissing",
                "No bootstrap work exists for this agent.",
                null,
                null,
                null);
        }

        if (IsQueued(latestBootstrap.WorkStatus))
        {
            return new AgentHealthResult(
                "Pending",
                "orange",
                "BootstrapQueued",
                "Bootstrap work is queued.",
                latestBootstrap.Id,
                latestBootstrap.Time,
                lastSuccessfulBootstrap?.CompletedAt);
        }

        if (IsRunning(latestBootstrap.WorkStatus))
        {
            return new AgentHealthResult(
                "Pending",
                "orange",
                "BootstrapRunning",
                "Bootstrap work is running.",
                latestBootstrap.Id,
                latestBootstrap.Time,
                lastSuccessfulBootstrap?.CompletedAt);
        }

        if (IsFailed(latestBootstrap.WorkStatus))
        {
            return new AgentHealthResult(
                "Failed",
                "red",
                "BootstrapFailed",
                string.IsNullOrWhiteSpace(latestBootstrap.WorkError)
                    ? "Latest bootstrap work failed."
                    : $"Latest bootstrap failed: {latestBootstrap.WorkError}",
                latestBootstrap.Id,
                latestBootstrap.Time,
                lastSuccessfulBootstrap?.CompletedAt);
        }

        var newerFailedWork = ordered.FirstOrDefault(log =>
            log.WorkPurpose != AgentWorkPurposeKinds.Bootstrap
            && log.Time > latestBootstrap.Time
            && IsFailed(log.WorkStatus));
        if (newerFailedWork is not null)
        {
            return new AgentHealthResult(
                "Degraded",
                "orange",
                "RecentWorkFailed",
                string.IsNullOrWhiteSpace(newerFailedWork.WorkError)
                    ? "A recent non-bootstrap work item failed."
                    : $"Recent work failed: {newerFailedWork.WorkError}",
                latestBootstrap.Id,
                latestBootstrap.Time,
                latestBootstrap.CompletedAt);
        }

        var latestActivityAt = ordered.Count == 0
            ? latestBootstrap.CompletedAt ?? latestBootstrap.Time
            : ordered
                .Select(ActivityAt)
                .DefaultIfEmpty(latestBootstrap.CompletedAt ?? latestBootstrap.Time)
                .Max();

        if (now - latestActivityAt >= IdleAfter)
        {
            return new AgentHealthResult(
                "Idle",
                "idle",
                "AgentIdle",
                $"No agent work activity for at least {IdleAfter.TotalMinutes:0} minutes.",
                latestBootstrap.Id,
                latestBootstrap.Time,
                latestBootstrap.CompletedAt);
        }

        return new AgentHealthResult(
            "Healthy",
            "green",
            "BootstrapSucceeded",
            "Latest bootstrap completed successfully.",
            latestBootstrap.Id,
            latestBootstrap.Time,
            latestBootstrap.CompletedAt);
    }

    private static DateTime ActivityAt(ResourceLogRecord log) =>
        log.CompletedAt ?? log.StartedAt ?? log.Time;

    private static bool IsQueued(string? status) =>
        status?.Equals(AgentWorkStatusKinds.Queued, StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsRunning(string? status) =>
        status?.Equals(AgentWorkStatusKinds.Running, StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsCompleted(string? status) =>
        status?.Equals(AgentWorkStatusKinds.Completed, StringComparison.OrdinalIgnoreCase) == true
        || status?.Equals("succeeded", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsFailed(string? status) =>
        status?.Equals(AgentWorkStatusKinds.Failed, StringComparison.OrdinalIgnoreCase) == true
        || status?.Equals(AgentWorkStatusKinds.Canceled, StringComparison.OrdinalIgnoreCase) == true
        || status?.Equals("cancelled", StringComparison.OrdinalIgnoreCase) == true;
}
