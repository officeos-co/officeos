using OffceOs.Common.Infrastructure.Security;
using OffceOs.Features.AgentHarness.Application;
using OffceOs.Features.AgentHarness.Application.Tools;
using OffceOs.Features.AgentHarness.Domain;
using OffceOs.Features.AgentRoutines.Domain;
using OffceOs.Features.Agents.Domain;

namespace OffceOs.Features.AgentHarness.Infrastructure;

internal sealed class GitHubCodeSessionService : ICodeSessionService
{
    private const string RepoPath = "/workspace/repo";
    private readonly IAgentSandbox _agentSandbox;
    private readonly IAgentRoutineCredentialRepository _agentRoutineCredentialRepository;
    private readonly IAgentSessionRepository _agentSessionRepository;
    private readonly CredentialProtector _credentialProtector;
    private readonly IHttpClientFactory _httpClientFactory;

    public GitHubCodeSessionService(
        IAgentSandbox agentSandbox,
        IAgentRoutineCredentialRepository agentRoutineCredentialRepository,
        IAgentSessionRepository agentSessionRepository,
        CredentialProtector credentialProtector,
        IHttpClientFactory httpClientFactory)
    {
        _agentSandbox = agentSandbox;
        _agentRoutineCredentialRepository = agentRoutineCredentialRepository;
        _agentSessionRepository = agentSessionRepository;
        _credentialProtector = credentialProtector;
        _httpClientFactory = httpClientFactory;
    }

    public async Task PrepareAsync(AgentSessionRecord session, string sandboxId, string serviceUrl, CancellationToken ct = default)
    {
        if (!session.HasRepository)
            return;

        var token = await GetGitHubTokenAsync(session, ct);
        var repository = ResolveRepository(session);
        var baseBranch = string.IsNullOrWhiteSpace(session.RepositoryBaseBranch)
            ? await GetDefaultBranchAsync(repository.FullName, token, ct)
            : session.RepositoryBaseBranch.Trim();
        var branch = $"officeos/session-{session.Id.ToString("N")[..12]}";
        session.RepositoryBaseBranch = baseBranch;

        await ExecuteRequiredAsync(sandboxId, serviceUrl, $"rm -rf {RepoPath}", TimeSpan.FromSeconds(60), ct);
        await ExecuteRequiredAsync(
            sandboxId,
            serviceUrl,
            $"git -c http.extraheader={ToolShell.Escape($"AUTHORIZATION: bearer {token}")} clone --branch {ToolShell.Escape(baseBranch)} --single-branch {ToolShell.Escape(repository.CloneUrl)} {RepoPath}",
            TimeSpan.FromMinutes(5),
            ct);
        await ExecuteRequiredAsync(
            sandboxId,
            serviceUrl,
            $"cd {RepoPath} && git checkout -b {ToolShell.Escape(branch)} && git config user.email officeos-agent@users.noreply.github.com && git config user.name 'OfficeOS Agent'",
            TimeSpan.FromSeconds(60),
            ct);

        session.RepositoryBranch = branch;
        await _agentSessionRepository.SaveAsync(session, ct);
    }

    public async Task<CodeSessionFinalizeResult?> FinalizeAsync(AgentSessionRecord session, string sandboxId, string serviceUrl, CancellationToken ct = default)
    {
        if (!session.HasRepository)
            return null;

        var token = await GetGitHubTokenAsync(session, ct);
        var repository = ResolveRepository(session);
        var branch = string.IsNullOrWhiteSpace(session.RepositoryBranch)
            ? $"officeos/session-{session.Id.ToString("N")[..12]}"
            : session.RepositoryBranch;
        var status = await ExecuteRequiredAsync(
            sandboxId,
            serviceUrl,
            $"cd {RepoPath} && git status --porcelain",
            TimeSpan.FromSeconds(60),
            ct);
        if (string.IsNullOrWhiteSpace(status.Output))
            return null;

        await ExecuteRequiredAsync(sandboxId, serviceUrl, $"cd {RepoPath} && git add -A", TimeSpan.FromSeconds(60), ct);
        await ExecuteRequiredAsync(
            sandboxId,
            serviceUrl,
            $"cd {RepoPath} && git commit -m {ToolShell.Escape($"OfficeOS session {session.Id.ToString("N")[..12]}")}",
            TimeSpan.FromMinutes(2),
            ct);
        var commit = await ExecuteRequiredAsync(sandboxId, serviceUrl, $"cd {RepoPath} && git rev-parse HEAD", TimeSpan.FromSeconds(30), ct);
        await ExecuteRequiredAsync(
            sandboxId,
            serviceUrl,
            $"cd {RepoPath} && git -c http.extraheader={ToolShell.Escape($"AUTHORIZATION: bearer {token}")} push origin {ToolShell.Escape(branch)}",
            TimeSpan.FromMinutes(5),
            ct);

        var pr = await OpenDraftPullRequestAsync(session, repository, branch, token, ct);
        return new CodeSessionFinalizeResult(branch, commit.Output.Trim(), pr.Url, pr.Number);
    }

    private async Task<string> GetGitHubTokenAsync(AgentSessionRecord session, CancellationToken ct)
    {
        if (session.WorkspaceId is null)
            throw new InvalidOperationException("GitHub code sessions require a workspace.");
        if (string.IsNullOrWhiteSpace(session.RepositoryCredentialRef))
            throw new InvalidOperationException("GitHub code sessions require a repository credential ref.");

        var credential = await _agentRoutineCredentialRepository.GetByNameAsync(session.WorkspaceId.Value, session.RepositoryCredentialRef, ct)
            ?? throw new InvalidOperationException($"GitHub credential '{session.RepositoryCredentialRef}' was not found.");
        var credentials = _credentialProtector.Unprotect(credential.EncryptedSecret);
        if (!credentials.TryGetValue("GITHUB_PERSONAL_ACCESS_TOKEN", out var token) || string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException($"GitHub credential '{session.RepositoryCredentialRef}' has no GitHub token.");

        await _agentRoutineCredentialRepository.MarkUsedAsync(credential.Id, DateTime.UtcNow, ct);
        return token;
    }

    private async Task<AgentSandboxCommandResult> ExecuteRequiredAsync(
        string sandboxId,
        string serviceUrl,
        string command,
        TimeSpan timeout,
        CancellationToken ct)
    {
        var result = await _agentSandbox.ExecuteAsync(sandboxId, serviceUrl, command, timeout, ct);
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error.Message);
        if (result.Value.ExitCode != 0)
            throw new InvalidOperationException(result.Value.Output);
        return result.Value;
    }

    private async Task<string> GetDefaultBranchAsync(string repositoryFullName, string token, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("github-api");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"repos/{repositoryFullName}");
        AddGitHubHeaders(request, token);
        using var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GitHub repository lookup failed with {(int)response.StatusCode}: {body}");
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("default_branch").GetString() ?? "main";
    }

    private async Task<(string Url, int? Number)> OpenDraftPullRequestAsync(
        AgentSessionRecord session,
        GitHubSessionRepository repository,
        string branch,
        string token,
        CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("github-api");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"repos/{repository.FullName}/pulls");
        AddGitHubHeaders(request, token);
        request.Content = JsonContent.Create(new
        {
            title = BuildPullRequestTitle(session),
            head = branch,
            @base = string.IsNullOrWhiteSpace(session.RepositoryBaseBranch) ? "main" : session.RepositoryBaseBranch,
            body = BuildPullRequestBody(session, branch),
            draft = true,
        });
        using var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GitHub pull request creation failed with {(int)response.StatusCode}: {body}");
        using var document = JsonDocument.Parse(body);
        var url = document.RootElement.GetProperty("html_url").GetString() ?? string.Empty;
        var number = document.RootElement.TryGetProperty("number", out var numberElement) ? numberElement.GetInt32() : (int?)null;
        return (url, number);
    }

    private static void AddGitHubHeaders(HttpRequestMessage request, string token)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    private static GitHubSessionRepository ResolveRepository(AgentSessionRecord session)
    {
        if (!string.IsNullOrWhiteSpace(session.RepositoryFullName))
        {
            var parsed = GitHubRepositoryRecord.Parse(session.RepositoryFullName);
            return new GitHubSessionRepository(parsed.FullName, parsed.Url);
        }

        var fromClone = GitHubRepositoryRecord.Parse(session.RepositoryCloneUrl ?? string.Empty);
        return new GitHubSessionRepository(fromClone.FullName, fromClone.Url);
    }

    private static string BuildPullRequestTitle(AgentSessionRecord session)
    {
        var summary = session.Input.ReplaceLineEndings(" ").Trim();
        if (summary.Length > 72)
            summary = summary[..72].Trim();
        return string.IsNullOrWhiteSpace(summary)
            ? $"OfficeOS session {session.Id.ToString("N")[..12]}"
            : $"OfficeOS: {summary}";
    }

    private static string BuildPullRequestBody(AgentSessionRecord session, string branch)
    {
        var builder = new StringBuilder()
            .AppendLine("Created by OfficeOS.")
            .AppendLine()
            .AppendLine($"Session: `{session.Id}`")
            .AppendLine($"Agent: `{session.AgentId}`")
            .AppendLine($"Source: `{session.Source}`")
            .AppendLine($"Purpose: `{session.Purpose}`")
            .AppendLine($"Branch: `{branch}`");
        if (session.RoutineId.HasValue)
            builder.AppendLine($"Routine: `{session.RoutineId}`");
        if (session.TriggerId.HasValue)
            builder.AppendLine($"Trigger: `{session.TriggerId}`");
        return builder.ToString();
    }

    private sealed record GitHubSessionRepository(string FullName, string CloneUrl);
}
