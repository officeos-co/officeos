namespace OffceOs.Api.Features.Agents;

[ApiController]
[Route("api/control-plane/v1")]
public sealed class AgentResourceController : ControllerBase
{
    [HttpGet("resources/agents")]
    [HttpGet("resources/agent")]
    public async Task<IActionResult> ListAgents(
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentRepository agents,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        return Ok((await agents.ListAsync(new AgentFilter { WorkspaceId = scope.Value.WorkspaceId }, ct)).Select(ToAgentResource));
    }

    [HttpGet("resources/agents/{name}")]
    [HttpGet("resources/agent/{name}")]
    public async Task<IActionResult> DescribeAgent(
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentRepository agents,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        var agent = await FindAgentAsync(agents, name, scope.Value.WorkspaceId, ct);
        return agent is null
            ? NotFound(new { error = $"agents/{name} was not found." })
            : Ok(ToAgentDetailsResource(agent));
    }

    [HttpDelete("resources/agents/{name}")]
    [HttpDelete("resources/agent/{name}")]
    public async Task<IActionResult> DeleteAgent(
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentDashboardService agents,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        return Guid.TryParse(name, out var agentId) &&
            await agents.DeleteAsync(agentId, scope.Value.UserId, scope.Value.WorkspaceId, ct)
            ? Ok(new { deleted = true })
            : NotFound(new { error = $"agents/{name} was not found." });
    }

    [HttpGet("resources/runs")]
    [HttpGet("resources/run")]
    public async Task<IActionResult> ListRunResources(
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentRunRepository runs,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        return Ok((await runs.ListAsync(new AgentRunFilter { WorkspaceId = scope.Value.WorkspaceId }, 100, ct)).Select(ToRunResource));
    }

    [HttpGet("resources/runs/{name}")]
    [HttpGet("resources/run/{name}")]
    public async Task<IActionResult> DescribeRunResource(
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentRunRepository runs,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        if (!Guid.TryParse(name, out var runId))
            return NotFound(new { error = $"runs/{name} was not found." });

        var run = await runs.GetByAsync(new AgentRunFilter { Id = runId, WorkspaceId = scope.Value.WorkspaceId }, ct);
        return run is null ? NotFound(new { error = $"runs/{name} was not found." }) : Ok(ToRunResource(run));
    }

    [HttpGet("resources/engines")]
    [HttpGet("resources/engine")]
    public IActionResult ListEngines() => Ok(new[] { new { kind = "Engine", name = "opencode", type = "opencode" } });

    [HttpGet("resources/engines/{name}")]
    [HttpGet("resources/engine/{name}")]
    public IActionResult DescribeEngine(string name) =>
        name.Equals("opencode", StringComparison.OrdinalIgnoreCase)
            ? Ok(new { kind = "Engine", name = "opencode", type = "opencode" })
            : NotFound(new { error = $"engines/{name} was not found." });

    [HttpDelete("resources/engines/{name}")]
    [HttpDelete("resources/engine/{name}")]
    public IActionResult DeleteEngine(string name) =>
        name.Equals("opencode", StringComparison.OrdinalIgnoreCase)
            ? BadRequest(new { error = "The built-in OpenCode engine cannot be deleted." })
            : NotFound(new { error = $"engines/{name} was not found." });

    [HttpPost("runs")]
    public async Task<IActionResult> CreateRun(
        [FromBody] AgentRunInput input,
        [FromServices] IControlPlaneRunService runs,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        try
        {
            return Ok(await runs.CreateAsync(new CreateControlPlaneRunRequest(
                input.AgentRef,
                input.Task,
                input.EngineRef,
                input.Repository,
                input.Ref,
                input.InputJson,
                input.Wait), scope.Value.UserId, scope.Value.WorkspaceId, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("runs")]
    public async Task<IActionResult> ListRuns(
        [FromServices] IControlPlaneRunService runs,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        return Ok(await runs.ListAsync(scope.Value.UserId, scope.Value.WorkspaceId, ct));
    }

    [HttpGet("runs/{runId:guid}")]
    public async Task<IActionResult> GetRun(
        Guid runId,
        [FromServices] IControlPlaneRunService runs,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        var run = await runs.GetAsync(runId, scope.Value.UserId, scope.Value.WorkspaceId, ct);
        return run is null ? NotFound(new { error = "Run not found." }) : Ok(run);
    }

    [HttpPost("runs/{runId:guid}/cancel")]
    public async Task<IActionResult> CancelRun(
        Guid runId,
        [FromServices] IControlPlaneRunService runs,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        return await runs.CancelAsync(runId, scope.Value.UserId, scope.Value.WorkspaceId, ct)
            ? Ok(new { canceled = true })
            : NotFound(new { error = "Run not found." });
    }

    [HttpGet("runs/{runId:guid}/logs")]
    public async Task<IActionResult> GetRunLogs(
        Guid runId,
        [FromServices] IControlPlaneRunService runs,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        return Ok(await runs.LogsAsync(runId, scope.Value.UserId, scope.Value.WorkspaceId, ct));
    }

    private async Task<(Guid UserId, Guid WorkspaceId)?> RequireScopeAsync(IWorkspaceService workspaces, CancellationToken ct)
    {
        if (HttpContext.Items["User"] is not UserRecord user)
            return null;

        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return (user.Id, workspace.Id);
    }

    private static object ToAgentResource(AgentRecord agent) => new
    {
        kind = "Agent",
        name = agent.Name,
        id = agent.Id,
        provider = agent.Provider,
        model = agent.Model,
        status = agent.Status.ToString(),
        createdAt = agent.CreatedAt,
    };

    private static object ToAgentDetailsResource(AgentRecord agent) => new
    {
        kind = "Agent",
        name = agent.Name,
        id = agent.Id,
        provider = agent.Provider,
        model = agent.Model,
        status = agent.Status.ToString(),
        prompt = agent.Prompt,
        systemPrompt = SystemPromptComposer.Compose(agent),
        podName = agent.PodName,
        serviceUrl = agent.ServiceUrl,
        activeDefinitionId = agent.ActiveDefinitionId,
        workspaceId = agent.WorkspaceId,
        createdAt = agent.CreatedAt,
        personalityFiles = agent.PersonalityFiles.OrderBy(file => file.CompositionOrder).Select(file => new
        {
            file.FileName,
            file.Content,
            file.CreatedAt,
            file.UpdatedAt,
        }),
        memories = agent.Memories.Select(memory => new
        {
            memory.Key,
            memory.Content,
            memory.CreatedAt,
            memory.UpdatedAt,
        }),
        channelBindings = agent.ChannelBindings.Select(binding => new
        {
            binding.Id,
            binding.ChannelConnectionId,
            binding.Enabled,
            binding.Config,
            binding.CreatedAt,
        }),
        activeSession = agent.ActiveSession is null
            ? null
            : new
            {
                agent.ActiveSession.Id,
                status = agent.ActiveSession.Status.ToString(),
                agent.ActiveSession.MessageCount,
                agent.ActiveSession.LastActivityAt,
                agent.ActiveSession.CreatedAt,
                agent.ActiveSession.EndedAt,
            },
    };

    private static object ToRunResource(AgentRunRecord run) => new
    {
        kind = "Run",
        name = run.Id.ToString(),
        id = run.Id,
        agentId = run.AgentId,
        engine = run.Kind,
        phase = run.Status,
        createdAt = run.CreatedAt,
        completedAt = run.CompletedAt,
    };

    private static async Task<AgentRecord?> FindAgentAsync(
        IAgentRepository agents,
        string name,
        Guid workspaceId,
        CancellationToken ct)
    {
        if (Guid.TryParse(name, out var id))
            return await agents.GetByAsync(new AgentFilter { Id = id, WorkspaceId = workspaceId }, ct);

        var matches = await agents.ListAsync(new AgentFilter { WorkspaceId = workspaceId }, ct);
        var match = matches.FirstOrDefault(agent => agent.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return match is null
            ? null
            : await agents.GetByAsync(new AgentFilter { Id = match.Id, WorkspaceId = workspaceId }, ct);
    }
}

public sealed record AgentRunInput(
    string AgentRef,
    string Task,
    string? EngineRef,
    string? Repository,
    string? Ref,
    string? InputJson,
    bool Wait);
