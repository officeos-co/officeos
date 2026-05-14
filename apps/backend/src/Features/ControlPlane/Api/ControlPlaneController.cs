namespace OffceOs.Api.Features.ControlPlane;

[ApiController]
[Route("api/control-plane/v1")]
public sealed class ControlPlaneController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult Me()
    {
        if (HttpContext.Items["User"] is not UserRecord user)
            return Unauthorized(new { error = "Unauthenticated." });

        return Ok(new { user.Id, user.Email, user.Name, user.DisplayName });
    }

    [HttpPost("auth/device/code")]
    public async Task<ActionResult<CliDeviceCodeResult>> CreateDeviceCode(
        [FromBody] CliDeviceCodeInput? input,
        [FromServices] ICliAuthService cliAuth,
        CancellationToken ct)
    {
        return Ok(await cliAuth.CreateDeviceCodeAsync(new CliDeviceCodeRequest(input?.RunnerName), ct));
    }

    [HttpPost("auth/device/token")]
    public async Task<ActionResult<CliDeviceTokenResult>> PollToken(
        [FromBody] CliDeviceTokenInput input,
        [FromServices] ICliAuthService cliAuth,
        CancellationToken ct)
    {
        try
        {
            return Ok(await cliAuth.PollTokenAsync(input.DeviceCode, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("auth/device/authorize")]
    public async Task<IActionResult> AuthorizeDeviceCode(
        [FromBody] CliDeviceAuthorizeInput input,
        [FromServices] ICliAuthService cliAuth,
        CancellationToken ct)
    {
        var user = RequireUser();
        if (user is null) return Unauthorized(new { error = "Sign in before authorizing the CLI." });

        try
        {
            await cliAuth.AuthorizeDeviceCodeAsync(input.UserCode, user.Id, ct);
            return Ok(new { ok = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("manifests/validate")]
    public async Task<IActionResult> ValidateManifest(
        [FromBody] DeclarativeManifestInput input,
        [FromServices] IDeclarativeAgentService manifests,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        return Ok(await manifests.ValidateAsync(input.Manifest, scope.Value.UserId, scope.Value.WorkspaceId, ct));
    }

    [HttpPost("manifests/diff")]
    public async Task<IActionResult> DiffManifest(
        [FromBody] DeclarativeManifestInput input,
        [FromServices] IDeclarativeAgentService manifests,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        try
        {
            return Ok(await manifests.DiffAsync(input.Manifest, scope.Value.UserId, scope.Value.WorkspaceId, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("manifests/apply")]
    public async Task<IActionResult> ApplyManifest(
        [FromBody] DeclarativeManifestInput input,
        [FromServices] IDeclarativeAgentService manifests,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        try
        {
            return Ok(await manifests.ApplyAsync(input.Manifest, scope.Value.UserId, scope.Value.WorkspaceId, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("resources/{kind}")]
    public async Task<IActionResult> ListResources(
        string kind,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentRepository agents,
        [FromServices] IAgentRunRepository runs,
        [FromServices] IChannelRepository channels,
        [FromServices] IAgentRoutineService routines,
        [FromServices] IMemoryStoreRepository memoryStores,
        [FromServices] IProviderResourceRepository providers,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        return NormalizeKind(kind) switch
        {
            "agents" => Ok((await agents.ListAsync(new AgentFilter { WorkspaceId = scope.Value.WorkspaceId }, ct)).Select(ToAgentResource)),
            "runs" => Ok((await runs.ListAsync(new AgentRunFilter { WorkspaceId = scope.Value.WorkspaceId }, 100, ct)).Select(ToRunResource)),
            "channels" => Ok((await channels.ListConnectionsAsync(new ChannelConnectionFilter { WorkspaceId = scope.Value.WorkspaceId }, ct)).Select(ToChannelResource)),
            "routines" => Ok((await routines.ListForOwnerAsync(scope.Value.UserId, scope.Value.WorkspaceId, ct)).Select(ToRoutineResource)),
            "memorystores" => Ok((await memoryStores.ListAsync(null, scope.Value.WorkspaceId, ct)).Select(ToMemoryStoreResource)),
            "providers" => Ok((await providers.ListAsync(scope.Value.WorkspaceId, ct)).Select(ToProviderResource)),
            "engines" => Ok(new[] { new { kind = "Engine", name = "opencode", type = "opencode" } }),
            _ => NotFound(new { error = $"Resource kind '{kind}' was not found." }),
        };
    }

    [HttpGet("resources/{kind}/{name}")]
    public async Task<IActionResult> DescribeResource(
        string kind,
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentRepository agents,
        [FromServices] IAgentRunRepository runs,
        [FromServices] IChannelRepository channels,
        [FromServices] IAgentRoutineService routines,
        [FromServices] IMemoryStoreRepository memoryStores,
        [FromServices] IProviderResourceRepository providers,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        if (NormalizeKind(kind) == "agents")
        {
            var agent = await FindAgentAsync(agents, name, scope.Value.WorkspaceId, ct);
            return agent is null
                ? NotFound(new { error = $"{kind}/{name} was not found." })
                : Ok(ToAgentDetailsResource(agent));
        }

        var list = await ListResources(kind, workspaces, agents, runs, channels, routines, memoryStores, providers, ct);
        if (list is not OkObjectResult ok || ok.Value is not IEnumerable<object> values)
            return list;

        var found = values.FirstOrDefault(value => ResourceName(value).Equals(name, StringComparison.OrdinalIgnoreCase));
        return found is null ? NotFound(new { error = $"{kind}/{name} was not found." }) : Ok(found);
    }

    [HttpDelete("resources/{kind}/{name}")]
    public async Task<IActionResult> DeleteResource(
        string kind,
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentDashboardService agents,
        [FromServices] IChannelRepository channels,
        [FromServices] IAgentRoutineService routines,
        [FromServices] IMemoryStoreRepository memoryStores,
        [FromServices] IProviderResourceRepository providers,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        var deleted = false;
        switch (NormalizeKind(kind))
        {
            case "agents":
                if (Guid.TryParse(name, out var agentId))
                    deleted = await agents.DeleteAsync(agentId, scope.Value.UserId, scope.Value.WorkspaceId, ct);
                break;
            case "channels":
                if (Guid.TryParse(name, out var channelId))
                    deleted = await channels.DeleteConnectionAsync(channelId, ct);
                break;
            case "routines":
                if (Guid.TryParse(name, out var routineId))
                    deleted = await routines.DeleteAsync(routineId, scope.Value.UserId, scope.Value.WorkspaceId, ct);
                break;
            case "memorystores":
                if (Guid.TryParse(name, out var memoryStoreId))
                    deleted = await memoryStores.DeleteAsync(memoryStoreId, null, scope.Value.WorkspaceId, ct);
                break;
            case "providers":
                deleted = await providers.DeleteAsync(scope.Value.WorkspaceId, name, ct);
                break;
            case "engines":
                return BadRequest(new { error = "The built-in OpenCode engine cannot be deleted." });
        }

        return deleted ? Ok(new { deleted = true }) : NotFound(new { error = $"{kind}/{name} was not found." });
    }

    [HttpPost("runs")]
    public async Task<IActionResult> CreateRun(
        [FromBody] ControlPlaneRunInput input,
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

    [HttpGet("providers")]
    public async Task<IActionResult> Providers(
        [FromServices] IProviderService providers,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        return Ok(await providers.ListForWorkspaceAsync(scope.Value.WorkspaceId, ct));
    }

    [HttpGet("models")]
    public async Task<IActionResult> Models(
        [FromServices] IProviderService providers,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        var rows = await providers.ListForWorkspaceAsync(scope.Value.WorkspaceId, ct);
        return Ok(rows.SelectMany(provider => provider.Models.Select(model => new
        {
            provider = provider.Name,
            model.Id,
            model.DisplayName,
            model.CostWeight,
            provider.Configured,
        })));
    }

    private async Task<(Guid UserId, Guid WorkspaceId)?> RequireScopeAsync(IWorkspaceService workspaces, CancellationToken ct)
    {
        var user = RequireUser();
        if (user is null)
            return null;

        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return (user.Id, workspace.Id);
    }

    private UserRecord? RequireUser() => HttpContext.Items["User"] as UserRecord;

    private static string NormalizeKind(string kind)
    {
        var value = kind.Trim().ToLowerInvariant();
        return value switch
        {
            "agent" or "agents" => "agents",
            "run" or "runs" => "runs",
            "channel" or "channels" => "channels",
            "routine" or "routines" => "routines",
            "memorystore" or "memorystores" or "memory-store" or "memory-stores" or "memory_store" or "memory_stores" => "memorystores",
            "provider" or "providers" => "providers",
            "engine" or "engines" => "engines",
            _ => value,
        };
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
        personalityFiles = agent.PersonalityFiles
            .OrderBy(file => file.CompositionOrder)
            .Select(file => new
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

    private static object ToChannelResource(ChannelConnectionRecord channel) => new
    {
        kind = "Channel",
        name = channel.Id.ToString(),
        id = channel.Id,
        type = channel.ChannelType.ToStorageString(),
        displayName = channel.DisplayName,
        enabled = channel.Enabled,
        createdAt = channel.CreatedAt,
    };

    private static object ToRoutineResource(AgentRoutineWithAgentRecord routine) => new
    {
        kind = "Routine",
        name = routine.Routine.Id.ToString(),
        id = routine.Routine.Id,
        routine.Routine.AgentId,
        agentName = routine.AgentName,
        routine.Routine.Enabled,
        routine.Routine.CreatedAt,
    };

    private static object ToMemoryStoreResource(MemoryStoreRecord store) => new
    {
        kind = "MemoryStore",
        name = store.Id.ToString(),
        id = store.Id,
        store.DisplayName,
        store.CreatedAt,
    };

    private static object ToProviderResource(ProviderResourceRecord provider) => new
    {
        kind = "Provider",
        name = provider.Name,
        id = provider.Id,
        type = provider.Type,
        displayName = provider.DisplayName,
        enabled = provider.Enabled,
        configured = provider.Enabled && !string.IsNullOrWhiteSpace(provider.EncryptedCredentialsJson),
        defaultModel = provider.DefaultModel,
        models = provider.Models,
        createdAt = provider.CreatedAt,
        updatedAt = provider.UpdatedAt,
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
        var match = matches.FirstOrDefault(agent =>
            agent.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        return match is null
            ? null
            : await agents.GetByAsync(new AgentFilter { Id = match.Id, WorkspaceId = workspaceId }, ct);
    }

    private static string ResourceName(object value)
    {
        var property = value.GetType().GetProperty("name");
        return property?.GetValue(value)?.ToString() ?? string.Empty;
    }
}

public sealed record ControlPlaneRunInput(
    string AgentRef,
    string Task,
    string? EngineRef,
    string? Repository,
    string? Ref,
    string? InputJson,
    bool Wait);

public sealed record DeclarativeManifestInput(string Manifest);

public sealed record CliDeviceCodeInput(string? RunnerName);

public sealed record CliDeviceAuthorizeInput(string UserCode);

public sealed record CliDeviceTokenInput(string DeviceCode);
