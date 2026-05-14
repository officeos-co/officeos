namespace OffceOs.Application.Features.Providers;

internal sealed class ProviderDispatchService : IProviderDispatchService
{
    private readonly IProviderService _providerService;
    private readonly LlmProviderDispatcher _llmProviderDispatcher;
    private readonly IAgentLogService _agentLogService;

    public ProviderDispatchService(
        IProviderService providerService,
        LlmProviderDispatcher llmProviderDispatcher,
        IAgentLogService agentLogService)
    {
        _providerService = providerService;
        _llmProviderDispatcher = llmProviderDispatcher;
        _agentLogService = agentLogService;
    }

    public async Task<AgentResult<LlmDispatchResponse>> DispatchAsync(
        string provider,
        Guid? workspaceId,
        string model,
        JsonElement requestBody,
        CancellationToken ct = default)
    {
        var auth = await _providerService.GetAuthForDispatchAsync(provider, workspaceId, ct);
        if (auth is null)
        {
            if (workspaceId.HasValue)
                await AppendProviderLogAsync(provider, workspaceId.Value, AgentLogType.ErrorConfiguration, $"Provider '{provider}' has no authentication configured.", model, ct);
            return new AgentError(AgentErrorCategory.Configuration, $"Provider '{provider}' has no authentication configured.");
        }

        if (workspaceId.HasValue)
            await AppendProviderLogAsync(provider, workspaceId.Value, AgentLogType.System, $"Dispatching LLM request to provider '{provider}' model '{model}'.", model, ct);

        var result = await _llmProviderDispatcher.DispatchAsync(provider, auth, model, requestBody, ct);
        if (result.IsFailure && workspaceId.HasValue)
            await AppendProviderLogAsync(provider, workspaceId.Value, result.Error.LogType, result.Error.Message, model, ct);

        return result;
    }

    private Task AppendProviderLogAsync(
        string provider,
        Guid workspaceId,
        AgentLogType type,
        string content,
        string model,
        CancellationToken ct)
    {
        return _agentLogService.AppendAsync(new AgentLogRecord
        {
            WorkspaceId = workspaceId,
            ResourceKind = ResourceLogKinds.Provider,
            ResourceName = provider.Trim().ToLowerInvariant(),
            Type = type,
            Tool = model,
            Content = content,
            MetadataJson = JsonSerializer.Serialize(new { provider, model }),
        }, ct);
    }
}
