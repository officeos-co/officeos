namespace OffceOs.Application.Features.Agents;

public sealed record AgentHealthResult(
    string Status,
    string State,
    string Reason,
    string Message,
    Guid? LastBootstrapRunId,
    DateTime? LastBootstrapAt,
    DateTime? LastSuccessfulBootstrapAt);

internal static class AgentHealthProjection
{
    private static readonly TimeSpan IdleAfter = TimeSpan.FromSeconds(5);

    public static AgentHealthResult From(AgentRecord agent, IReadOnlyList<AgentRunRecord> runs)
        => From(agent, runs, DateTime.UtcNow);

    internal static AgentHealthResult From(AgentRecord agent, IReadOnlyList<AgentRunRecord> runs, DateTime now)
    {
        var ordered = runs
            .Where(run => run.AgentId == agent.Id)
            .OrderByDescending(run => run.CreatedAt)
            .ToList();

        var bootstrapRuns = ordered
            .Where(run => run.Purpose == AgentRunPurposeKinds.Bootstrap)
            .ToList();
        var activeBootstrapRuns = agent.ActiveDefinitionId.HasValue
            ? bootstrapRuns.Where(run => run.DefinitionId == agent.ActiveDefinitionId).ToList()
            : bootstrapRuns;
        var latestBootstrap = activeBootstrapRuns.FirstOrDefault();
        var latestAnyBootstrap = bootstrapRuns.FirstOrDefault();
        var lastSuccessfulBootstrap = activeBootstrapRuns
            .FirstOrDefault(run => IsCompleted(run.Status));

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
                    latestAnyBootstrap.CreatedAt,
                    lastSuccessfulBootstrap?.CompletedAt);
            }

            return new AgentHealthResult(
                "Pending",
                "orange",
                "BootstrapMissing",
                "No bootstrap run exists for this agent.",
                null,
                null,
                null);
        }

        if (IsQueued(latestBootstrap.Status))
        {
            return new AgentHealthResult(
                "Pending",
                "orange",
                "BootstrapQueued",
                "Bootstrap run is queued.",
                latestBootstrap.Id,
                latestBootstrap.CreatedAt,
                lastSuccessfulBootstrap?.CompletedAt);
        }

        if (IsRunning(latestBootstrap.Status))
        {
            return new AgentHealthResult(
                "Pending",
                "orange",
                "BootstrapRunning",
                "Bootstrap run is running.",
                latestBootstrap.Id,
                latestBootstrap.CreatedAt,
                lastSuccessfulBootstrap?.CompletedAt);
        }

        if (IsFailed(latestBootstrap.Status))
        {
            return new AgentHealthResult(
                "Failed",
                "red",
                "BootstrapFailed",
                string.IsNullOrWhiteSpace(latestBootstrap.Error)
                    ? "Latest bootstrap run failed."
                    : $"Latest bootstrap failed: {latestBootstrap.Error}",
                latestBootstrap.Id,
                latestBootstrap.CreatedAt,
                lastSuccessfulBootstrap?.CompletedAt);
        }

        var newerFailedRun = ordered.FirstOrDefault(run =>
            run.Purpose != AgentRunPurposeKinds.Bootstrap
            && run.CreatedAt > latestBootstrap.CreatedAt
            && IsFailed(run.Status));
        if (newerFailedRun is not null)
        {
            return new AgentHealthResult(
                "Degraded",
                "orange",
                "RecentRunFailed",
                string.IsNullOrWhiteSpace(newerFailedRun.Error)
                    ? "A recent non-bootstrap run failed."
                    : $"Recent run failed: {newerFailedRun.Error}",
                latestBootstrap.Id,
                latestBootstrap.CreatedAt,
                latestBootstrap.CompletedAt);
        }

        var latestActivityAt = ordered.Count == 0
            ? latestBootstrap.CompletedAt ?? latestBootstrap.UpdatedAt
            : ordered
                .Select(ActivityAt)
                .DefaultIfEmpty(latestBootstrap.CompletedAt ?? latestBootstrap.UpdatedAt)
                .Max();

        if (now - latestActivityAt >= IdleAfter)
        {
            return new AgentHealthResult(
                "Idle",
                "idle",
                "AgentIdle",
                $"No agent run activity for at least {IdleAfter.TotalMinutes:0} minutes.",
                latestBootstrap.Id,
                latestBootstrap.CreatedAt,
                latestBootstrap.CompletedAt);
        }

        return new AgentHealthResult(
            "Healthy",
            "green",
            "BootstrapSucceeded",
            "Latest bootstrap completed successfully.",
            latestBootstrap.Id,
            latestBootstrap.CreatedAt,
            latestBootstrap.CompletedAt);
    }

    private static DateTime ActivityAt(AgentRunRecord run) =>
        run.CompletedAt ?? run.UpdatedAt;

    private static bool IsQueued(string status) =>
        status.Equals("queued", StringComparison.OrdinalIgnoreCase);

    private static bool IsRunning(string status) =>
        status.Equals("running", StringComparison.OrdinalIgnoreCase);

    private static bool IsCompleted(string status) =>
        status.Equals("completed", StringComparison.OrdinalIgnoreCase)
        || status.Equals("succeeded", StringComparison.OrdinalIgnoreCase);

    private static bool IsFailed(string status) =>
        status.Equals("failed", StringComparison.OrdinalIgnoreCase)
        || status.Equals("canceled", StringComparison.OrdinalIgnoreCase)
        || status.Equals("cancelled", StringComparison.OrdinalIgnoreCase);
}
