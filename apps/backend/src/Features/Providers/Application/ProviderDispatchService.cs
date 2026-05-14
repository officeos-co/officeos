namespace OffceOs.Application.Features.Providers;

internal sealed class ProviderDispatchService : IProviderDispatchService
{
    private readonly IProviderService _providerService;
    private readonly LlmProviderDispatcher _llmProviderDispatcher;

    public ProviderDispatchService(
        IProviderService providerService,
        LlmProviderDispatcher llmProviderDispatcher)
    {
        _providerService = providerService;
        _llmProviderDispatcher = llmProviderDispatcher;
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
            return new AgentError(AgentErrorCategory.Configuration, $"Provider '{provider}' has no authentication configured.");

        return await _llmProviderDispatcher.DispatchAsync(provider, auth, model, requestBody, ct);
    }
}
