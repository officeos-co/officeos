namespace OffceOs.Application.Features.Agents;

internal sealed class BrowserService : IBrowserService
{
    private readonly IBrowserSessionRepository _browserSessionRepository;
    private readonly IBrowserRuntimeClient _browserRuntimeClient;
    private readonly ILogger<BrowserService> _logger;

    public BrowserService(
        IBrowserSessionRepository sessions,
        IBrowserRuntimeClient runtime,
        ILogger<BrowserService> logger)
    {
        _browserSessionRepository = sessions;
        _browserRuntimeClient = runtime;
        _logger = logger;
    }

    public async Task<BrowserSessionState> GetOrCreateAsync(Guid agentId, CancellationToken ct = default)
    {
        var existing = await _browserSessionRepository.GetByAsync(new BrowserSessionFilter { AgentId = agentId }, ct);
        if (existing is not null)
        {
            var state = await _browserRuntimeClient.GetSessionAsync(agentId, existing.RuntimeSessionId, ct);
            if (state is not null)
            {
                await _browserSessionRepository.UpsertAsync(agentId, existing.RuntimeSessionId, existing.CookiesJson, ct);
                return state with { LastAccessedAt = DateTime.UtcNow };
            }
        }

        var profileName = AgentProfileName(agentId);
        var created = await _browserRuntimeClient.CreateSessionAsync(agentId, $"agent-{agentId:N}", profileName, ct);
        if (string.IsNullOrWhiteSpace(created.RuntimeSessionId))
            throw new InvalidOperationException("Browser runtime did not return a session id.");

        await _browserSessionRepository.UpsertAsync(agentId, created.RuntimeSessionId, null, ct);
        return created;
    }

    public async Task<BrowserSessionState?> GetStateAsync(Guid agentId, CancellationToken ct = default)
    {
        var existing = await _browserSessionRepository.GetByAsync(new BrowserSessionFilter { AgentId = agentId }, ct);
        if (existing is null)
            return new BrowserSessionState(agentId, null, "not_started", null, null, null, null, null, null);

        var state = await _browserRuntimeClient.GetSessionAsync(agentId, existing.RuntimeSessionId, ct);
        return state ?? new BrowserSessionState(
            agentId,
            existing.RuntimeSessionId,
            "unavailable",
            null,
            null,
            null,
            null,
            existing.CreatedAt,
            existing.LastAccessedAt);
    }

    public async Task<BrowserSessionState> RestartAsync(Guid agentId, CancellationToken ct = default)
    {
        await StopAsync(agentId, ct);
        return await GetOrCreateAsync(agentId, ct);
    }

    public async Task StopAsync(Guid agentId, CancellationToken ct = default)
    {
        var existing = await _browserSessionRepository.GetByAsync(new BrowserSessionFilter { AgentId = agentId }, ct);
        if (existing is null) return;

        try
        {
            await _browserRuntimeClient.CloseSessionAsync(existing.RuntimeSessionId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to close browser session {RuntimeSessionId} for agent {AgentId}", existing.RuntimeSessionId, agentId);
        }

        await _browserSessionRepository.DeleteByAgentAsync(agentId, ct);
    }

    public async Task<string?> GetViewUrlAsync(Guid agentId, CancellationToken ct = default)
    {
        var state = await GetOrCreateAsync(agentId, ct);
        return state.TakeoverUrl;
    }

    internal static string AgentProfileName(Guid agentId) => $"agent-{agentId:N}";
}
