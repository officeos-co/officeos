namespace OffceOs.Application.Features.Coding;

internal sealed class CliCodeService : ICliCodeService
{
    private const string DefaultProvider = "openai";

    private readonly IAgentDashboardService _agentDashboardService;

    public CliCodeService(IAgentDashboardService agentDashboardService)
    {
        _agentDashboardService = agentDashboardService;
    }

    public async Task<CliCodeSessionResult> CreateSessionAsync(
        CliCodeSessionRequest request,
        Guid userId,
        Guid workspaceId,
        CancellationToken ct = default)
    {
        var agent = await _agentDashboardService.CreateAsync(
            new CreateDashboardAgentRequest(
                $"OfficeOS Coding {DateTime.UtcNow:yyyyMMdd-HHmmss}",
                NormalizeProvider(request.Provider),
                string.IsNullOrWhiteSpace(request.Model) ? ProviderRegistry.DefaultModel : request.Model.Trim(),
                BuildSystemPrompt(request.Repository, NormalizeEffort(request.Effort)),
                null,
                null,
                null,
                null,
                null,
                null),
            userId,
            workspaceId,
            ct);

        return new CliCodeSessionResult(
            agent.Id,
            agent.Id,
            agent.Name,
            agent.Provider,
            agent.Model ?? ProviderRegistry.DefaultModel,
            NormalizeEffort(request.Effort));
    }

    private static string NormalizeProvider(string? provider)
        => string.IsNullOrWhiteSpace(provider) ? DefaultProvider : provider.Trim().ToLowerInvariant();

    public static string NormalizeEffort(string? effort)
    {
        var normalized = string.IsNullOrWhiteSpace(effort) ? "low" : effort.Trim().ToLowerInvariant();
        return normalized is "low" or "medium" or "high"
            ? normalized
            : throw new InvalidOperationException("Coding effort must be one of: low, medium, high.");
    }

    public static string WithEffort(string? prompt, string effort)
    {
        var normalized = NormalizeEffort(effort);
        var line = $"Coding effort: {normalized}";
        if (string.IsNullOrWhiteSpace(prompt))
            return line;

        var updated = Regex.Replace(
            prompt,
            @"Coding effort:\s*(low|medium|high)",
            line,
            RegexOptions.IgnoreCase);
        return updated == prompt ? $"{prompt.TrimEnd()}\n\n{line}" : updated;
    }

    private static string BuildSystemPrompt(CliCodeRepositoryRequest? repository, string effort)
    {
        var metadata = repository is null
            ? "No local repository metadata was provided."
            : $"""
            Local repository metadata:
            - root: {repository.Root ?? "unknown"}
            - remote: {repository.RemoteUrl ?? "unknown"}
            - branch: {repository.Branch ?? "unknown"}
            - commit: {repository.Commit ?? "unknown"}
            - local_changes: {repository.HasChanges}
            """;

        return $"""
        You are OfficeOS Coding, a cloud coding agent controlled from the OfficeOS CLI.

        The agent loop and all tools run in the cloud pod executor. Use the built-in shell, file, search, and HTTP tools to inspect and change the cloud workspace. Treat /workspace as the working directory.

        Repository loading policy:
        - If /workspace does not contain the target repository, initialize it from the GitHub remote when one is available.
        - If the repository is private or unavailable from the cloud runtime, clearly explain that a GitHub connection is required before continuing.
        - Prefer exact repository state from the provided commit and branch metadata.
        - Do not pretend local uncommitted files are present unless you can inspect them in the cloud workspace.

        Coding workflow:
        - Inspect before editing.
        - Keep changes focused on the user's request.
        - Run build, typecheck, lint, or focused tests when practical.
        - Summarize changed files and validation at the end.

        Coding effort: {effort}

        {metadata}
        """;
    }
}
