namespace OffceOs.Api.Features.Coding;

[ApiController]
[Route("api/cli/code")]
public sealed class CliCodeController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpPost("sessions")]
    public async Task<ActionResult<CliCodeSessionPayload>> CreateSession(
        [FromBody] CliCodeSessionInput input,
        [FromServices] UserContext user,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] ICliCodeService code,
        CancellationToken ct)
    {
        try
        {
            var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
            var session = await code.CreateSessionAsync(
                new CliCodeSessionRequest(
                    input.Provider,
                    input.Model,
                    input.Effort,
                    input.Repository is null
                        ? null
                        : new CliCodeRepositoryRequest(
                            input.Repository.Root,
                            input.Repository.RemoteUrl,
                            input.Repository.Branch,
                            input.Repository.Commit,
                            input.Repository.HasChanges)),
                user.Id,
                workspace.Id,
                ct);

            return Ok(new CliCodeSessionPayload(
                session.SessionId,
                session.AgentId,
                session.Name,
                session.Provider,
                session.Model,
                session.Effort));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("sessions/{sessionId:guid}/messages")]
    public async Task SendMessage(
        Guid sessionId,
        [FromBody] CliCodeMessageInput input,
        [FromServices] UserContext user,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentDashboardService agents,
        [FromServices] IAgentLogService logs,
        [FromServices] IAgentLogRepository logRepository,
        [FromServices] IServiceScopeFactory scopeFactory,
        CancellationToken ct)
    {
        Response.ContentType = "application/x-ndjson";

        if (string.IsNullOrWhiteSpace(input.Message))
        {
            await WriteEventAsync(new CliCodeStreamPayload("error", "Message is required."), ct);
            return;
        }

        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var agent = await agents.GetDashboardAgentAsync(sessionId, user.Id, workspace.Id, ct);
        if (agent is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            await WriteEventAsync(new CliCodeStreamPayload("error", "Coding session not found."), ct);
            return;
        }

        var correlationId = Guid.NewGuid().ToString("N");
        var messageRecord = await logs.AppendAsync(AgentLogRecord.MessageIn(sessionId, input.Message, correlationId), ct);
        await WriteEventAsync(new CliCodeStreamPayload("message_in", input.Message, CorrelationId: correlationId), ct);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, HttpContext.RequestAborted);
        var runTask = Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();
            var turnService = scope.ServiceProvider.GetRequiredService<AgentTurnService>();
            return await turnService.RunTurnAsync(sessionId, input.Message, correlationId, linkedCts.Token);
        }, linkedCts.Token);

        var emitted = new HashSet<Guid> { messageRecord.Id };
        while (!runTask.IsCompleted && !linkedCts.IsCancellationRequested)
        {
            await EmitNewLogsAsync(logRepository, sessionId, correlationId, emitted, linkedCts.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(500), linkedCts.Token);
        }

        await EmitNewLogsAsync(logRepository, sessionId, correlationId, emitted, linkedCts.Token);

        try
        {
            var result = await runTask;
            await EmitNewLogsAsync(logRepository, sessionId, correlationId, emitted, linkedCts.Token);
            await WriteEventAsync(new CliCodeStreamPayload(
                result.Success ? "done" : "error",
                result.Success ? null : result.Error,
                CorrelationId: correlationId), linkedCts.Token);
        }
        catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
        {
            await WriteEventAsync(new CliCodeStreamPayload("error", "Turn cancelled.", CorrelationId: correlationId), CancellationToken.None);
        }
        catch (Exception ex)
        {
            await WriteEventAsync(new CliCodeStreamPayload("error", ex.Message, CorrelationId: correlationId), CancellationToken.None);
        }
    }

    [HttpPatch("sessions/{sessionId:guid}/model")]
    public async Task<ActionResult<CliCodeModelSelectionPayload>> UpdateModel(
        Guid sessionId,
        [FromBody] CliCodeModelSelectionInput input,
        [FromServices] UserContext user,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentDashboardService agents,
        CancellationToken ct)
    {
        try
        {
            var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
            var updated = await agents.PatchAsync(
                sessionId,
                user.Id,
                workspace.Id,
                new PatchAgentRequest(
                    input.Provider,
                    input.Model,
                    Prompt: input.Effort is null
                        ? null
                        : CliCodeService.WithEffort(
                            (await agents.GetDashboardAgentAsync(sessionId, user.Id, workspace.Id, ct))?.Agent.Prompt,
                            input.Effort)),
                ct);

            if (updated is null)
                return NotFound(new { error = "Coding session not found." });

            return Ok(new CliCodeModelSelectionPayload(
                updated.Provider,
                updated.Model ?? ProviderRegistry.DefaultModel,
                input.Effort is null ? ExtractEffort(updated.Prompt) : CliCodeService.NormalizeEffort(input.Effort)));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("models")]
    public async Task<ActionResult<IReadOnlyList<CliCodeModelPayload>>> ListModels(
        [FromServices] UserContext user,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IProviderService providers,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var configured = await providers.ListForWorkspaceAsync(workspace.Id, ct);
        var configuredProviders = configured
            .Where(provider => provider.Configured)
            .OrderBy(provider => provider.Name)
            .ToList();
        var models = configuredProviders
            .SelectMany(provider => provider.Models.Select(model => new CliCodeModelPayload(
                provider.Name,
                provider.DisplayName,
                model.Id,
                model.DisplayName,
                model.CostWeight,
                false)))
            .OrderBy(model => model.Provider)
            .ThenBy(model => model.Model)
            .ToList();

        var defaultProvider = configuredProviders.FirstOrDefault();
        if (defaultProvider is not null)
        {
            models.Insert(0, new CliCodeModelPayload(
                defaultProvider.Name,
                defaultProvider.DisplayName,
                ProviderRegistry.DefaultModel,
                ProviderRegistry.GetDisplayName(ProviderRegistry.DefaultModel),
                0,
                true));
        }

        return Ok(models);
    }

    private async Task EmitNewLogsAsync(
        IAgentLogRepository logRepository,
        Guid agentId,
        string correlationId,
        HashSet<Guid> emitted,
        CancellationToken ct)
    {
        var records = await logRepository.ListAsync(
            new AgentLogFilter { AgentId = agentId, CorrelationId = correlationId },
            new AgentLogListOptions { Sort = AgentLogSort.TimeAscending, Limit = 200 },
            ct);

        foreach (var record in records)
        {
            if (!emitted.Add(record.Id)) continue;
            await WriteEventAsync(ToStreamEvent(record), ct);
        }
    }

    private Task WriteEventAsync(CliCodeStreamPayload value, CancellationToken ct)
        => WriteJsonLineAsync(Response, value, ct);

    private static async Task WriteJsonLineAsync(HttpResponse response, CliCodeStreamPayload value, CancellationToken ct)
    {
        await response.WriteAsync(JsonSerializer.Serialize(value, JsonOptions), ct);
        await response.WriteAsync("\n", ct);
        await response.Body.FlushAsync(ct);
    }

    private static CliCodeStreamPayload ToStreamEvent(AgentLogRecord record)
        => new(
            record.Type.ToString(),
            record.Content,
            record.Tool,
            record.Integration,
            record.CorrelationId,
            record.Time);

    private static string ExtractEffort(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return "low";
        var match = Regex.Match(prompt, @"Coding effort:\s*(low|medium|high)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.ToLowerInvariant() : "low";
    }
}

public sealed record CliCodeRepositoryInput(
    string? Root,
    string? RemoteUrl,
    string? Branch,
    string? Commit,
    bool HasChanges);

public sealed record CliCodeSessionInput(
    string? Provider,
    string? Model,
    string? Effort,
    CliCodeRepositoryInput? Repository);

public sealed record CliCodeMessageInput(string Message);

public sealed record CliCodeModelSelectionInput(string? Provider, string? Model, string? Effort);

public sealed record CliCodeSessionPayload(Guid SessionId, Guid AgentId, string Name, string Provider, string Model, string Effort);

public sealed record CliCodeModelSelectionPayload(string Provider, string Model, string Effort);

public sealed record CliCodeModelPayload(
    string Provider,
    string ProviderDisplayName,
    string Model,
    string ModelDisplayName,
    int CostWeight,
    bool IsDefault);

public sealed record CliCodeStreamPayload(
    string Type,
    string? Content,
    string? Tool = null,
    string? Integration = null,
    string? CorrelationId = null,
    DateTime? Time = null);
