namespace OffceOs.Application.Features.AgentRoutines;

internal sealed class AgentRoutineGitHubPollerService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(30);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<AgentRoutineGitHubPollerService> _logger;

    public AgentRoutineGitHubPollerService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<AgentRoutineGitHubPollerService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AgentRoutineGitHubPollerService started");
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var pollingService = scope.ServiceProvider.GetRequiredService<AgentRoutineGitHubPollingService>();
                await pollingService.PollAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "GitHub routine poller tick failed");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }
    }

    internal sealed class AgentRoutineGitHubPollingService
    {
        private readonly IAgentRoutineRepository _agentRoutineRepository;
        private readonly IAgentRoutineExecutionService _agentRoutineExecutionService;
        private readonly IIntegrationDefinitionService _integrationDefinitionService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AgentRoutineGitHubPollingService> _logger;

        public AgentRoutineGitHubPollingService(
            IAgentRoutineRepository agentRoutineRepository,
            IAgentRoutineExecutionService executionService,
            IIntegrationDefinitionService integrationDefinitionService,
            IHttpClientFactory httpClientFactory,
            ILogger<AgentRoutineGitHubPollingService> logger)
        {
            _agentRoutineRepository = agentRoutineRepository;
            _agentRoutineExecutionService = executionService;
            _integrationDefinitionService = integrationDefinitionService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task PollAsync(CancellationToken ct)
        {
            var routines = await _agentRoutineRepository.ListAllEnabledForExecutionAsync(ct);
            foreach (var routine in routines)
            {
                foreach (var trigger in routine.Routine.Triggers.Where(IsPollingGitHubTrigger))
                {
                    var config = DeserializeConfig(trigger.ConfigJson);
                    foreach (var configuredEvent in config.Events)
                        await PollTriggerEventAsync(routine, trigger, config, configuredEvent, ct);
                }
            }
        }

        private async Task PollTriggerEventAsync(
            AgentRoutineExecutionRecord routine,
            AgentRoutineTriggerRecord trigger,
            GitHubRoutineTriggerConfig config,
            string configuredEvent,
            CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var cursor = await _agentRoutineRepository.GetPollCursorAsync(trigger.Id, configuredEvent, ct);
            if (cursor is null)
            {
                await _agentRoutineRepository.UpsertPollCursorAsync(new AgentRoutinePollCursorRecord
                {
                    TriggerId = trigger.Id,
                    Event = configuredEvent,
                    CursorAt = now,
                    LastPolledAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                }, ct);
                return;
            }

            var interval = TimeSpan.FromSeconds(Math.Max(15, config.PollIntervalSeconds));
            if (cursor.LastPolledAt.HasValue && cursor.LastPolledAt.Value.Add(interval) > now)
                return;

            var credentials = await _integrationDefinitionService.GetDecryptedCredentialAsync("github", routine.OwnerId, routine.WorkspaceId, ct);
            if (!credentials.TryGetValue("GITHUB_PERSONAL_ACCESS_TOKEN", out var token) || string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("GitHub polling routine trigger {TriggerId} skipped because workspace {WorkspaceId} has no GitHub OAuth credential", trigger.Id, routine.WorkspaceId);
                cursor.LastPolledAt = now;
                cursor.UpdatedAt = now;
                await _agentRoutineRepository.UpsertPollCursorAsync(cursor, ct);
                return;
            }

            var events = await FetchEventsAsync(config, configuredEvent, cursor.CursorAt, token, ct);
            var nextCursor = cursor.CursorAt;
            foreach (var item in events.OrderBy(item => item.UpdatedAt))
            {
                if (item.UpdatedAt <= cursor.CursorAt)
                    continue;

                var payload = JsonSerializer.Serialize(new
                {
                    source = "github_poll",
                    @event = configuredEvent,
                    action = item.Action,
                    repository = new
                    {
                        full_name = config.Repository,
                        html_url = $"https://github.com/{config.Repository}",
                        clone_url = config.RepositoryUrl,
                    },
                    item = item.Payload,
                }, JsonOptions);

                await _agentRoutineExecutionService.ExecuteGitHubPollTriggerAsync(trigger.Id, payload, ct);
                if (item.UpdatedAt > nextCursor)
                    nextCursor = item.UpdatedAt;
            }

            cursor.CursorAt = nextCursor;
            cursor.LastPolledAt = now;
            cursor.UpdatedAt = now;
            await _agentRoutineRepository.UpsertPollCursorAsync(cursor, ct);
        }

        private async Task<IReadOnlyList<GitHubPolledEvent>> FetchEventsAsync(
            GitHubRoutineTriggerConfig config,
            string configuredEvent,
            DateTime since,
            string token,
            CancellationToken ct)
        {
            var baseEvent = configuredEvent.Split('.', 2, StringSplitOptions.TrimEntries)[0].ToLowerInvariant();
            return baseEvent switch
            {
                "pull_request" => await FetchPullRequestsAsync(config, configuredEvent, since, token, ct),
                "issue_comment" => await FetchIssueCommentsAsync(config, since, token, ct),
                "issues" => await FetchIssuesAsync(config, since, token, ct),
                "push" => await FetchCommitsAsync(config, since, token, ct),
                "workflow_run" => await FetchWorkflowRunsAsync(config, since, token, ct),
                _ => [],
            };
        }

        private async Task<IReadOnlyList<GitHubPolledEvent>> FetchPullRequestsAsync(
            GitHubRoutineTriggerConfig config,
            string configuredEvent,
            DateTime since,
            string token,
            CancellationToken ct)
        {
            var json = await SendGitHubAsync($"repos/{config.Repository}/pulls?state=all&sort=updated&direction=desc&per_page=50", token, ct);
            var events = new List<GitHubPolledEvent>();
            foreach (var item in json.EnumerateArray())
            {
                var updatedAt = item.GetProperty("updated_at").GetDateTime();
                if (updatedAt <= since)
                    continue;

                var createdAt = item.TryGetProperty("created_at", out var created) ? created.GetDateTime() : updatedAt;
                var action = createdAt > since ? "opened" : "updated";
                if (configuredEvent.Contains('.', StringComparison.Ordinal) && !configuredEvent.EndsWith($".{action}", StringComparison.OrdinalIgnoreCase))
                    continue;

                events.Add(new GitHubPolledEvent(action, updatedAt, item.Clone()));
            }

            return events;
        }

        private async Task<IReadOnlyList<GitHubPolledEvent>> FetchIssueCommentsAsync(
            GitHubRoutineTriggerConfig config,
            DateTime since,
            string token,
            CancellationToken ct)
        {
            var json = await SendGitHubAsync($"repos/{config.Repository}/issues/comments?since={Uri.EscapeDataString(since.ToString("O"))}&per_page=50", token, ct);
            return json.EnumerateArray()
                .Select(item => new GitHubPolledEvent("created_or_updated", item.GetProperty("updated_at").GetDateTime(), item.Clone()))
                .ToList();
        }

        private async Task<IReadOnlyList<GitHubPolledEvent>> FetchIssuesAsync(
            GitHubRoutineTriggerConfig config,
            DateTime since,
            string token,
            CancellationToken ct)
        {
            var json = await SendGitHubAsync($"repos/{config.Repository}/issues?state=all&since={Uri.EscapeDataString(since.ToString("O"))}&per_page=50", token, ct);
            return json.EnumerateArray()
                .Where(item => !item.TryGetProperty("pull_request", out _))
                .Select(item => new GitHubPolledEvent("created_or_updated", item.GetProperty("updated_at").GetDateTime(), item.Clone()))
                .ToList();
        }

        private async Task<IReadOnlyList<GitHubPolledEvent>> FetchCommitsAsync(
            GitHubRoutineTriggerConfig config,
            DateTime since,
            string token,
            CancellationToken ct)
        {
            var json = await SendGitHubAsync($"repos/{config.Repository}/commits?since={Uri.EscapeDataString(since.ToString("O"))}&per_page=50", token, ct);
            return json.EnumerateArray()
                .Select(item => new GitHubPolledEvent("committed", ReadCommitDate(item), item.Clone()))
                .ToList();
        }

        private async Task<IReadOnlyList<GitHubPolledEvent>> FetchWorkflowRunsAsync(
            GitHubRoutineTriggerConfig config,
            DateTime since,
            string token,
            CancellationToken ct)
        {
            var json = await SendGitHubAsync($"repos/{config.Repository}/actions/runs?per_page=50", token, ct);
            if (!json.TryGetProperty("workflow_runs", out var runs) || runs.ValueKind != JsonValueKind.Array)
                return [];

            return runs.EnumerateArray()
                .Select(item => new GitHubPolledEvent("updated", item.GetProperty("updated_at").GetDateTime(), item.Clone()))
                .Where(item => item.UpdatedAt > since)
                .ToList();
        }

        private async Task<JsonElement> SendGitHubAsync(string requestUri, string token, CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("github-api");
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"GitHub API request '{requestUri}' failed with {(int)response.StatusCode}: {body}");

            using var document = JsonDocument.Parse(body);
            return document.RootElement.Clone();
        }

        private static bool IsPollingGitHubTrigger(AgentRoutineTriggerRecord trigger)
        {
            if (!trigger.Enabled || trigger.Kind != AgentRoutineTriggerKinds.GitHub)
                return false;

            var config = DeserializeConfig(trigger.ConfigJson);
            return config.Mode.Equals(GitHubRoutineTriggerModes.Poll, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(config.Repository)
                && config.Events.Count > 0;
        }

        private static GitHubRoutineTriggerConfig DeserializeConfig(string configJson) =>
            JsonSerializer.Deserialize<GitHubRoutineTriggerConfig>(configJson) ?? new GitHubRoutineTriggerConfig();

        private static DateTime ReadCommitDate(JsonElement item)
        {
            if (item.TryGetProperty("commit", out var commit)
                && commit.TryGetProperty("committer", out var committer)
                && committer.TryGetProperty("date", out var date))
                return date.GetDateTime();

            return DateTime.UtcNow;
        }
    }

    private sealed record GitHubPolledEvent(string Action, DateTime UpdatedAt, JsonElement Payload);
}
