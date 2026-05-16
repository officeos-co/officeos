using OffceOs.Features.ResourceLogs.Application;
using OffceOs.Features.ResourceLogs.Domain;
using OffceOs.Features.Providers.Infrastructure;
using OffceOs.Common.Domain.Primitives;
namespace OffceOs.Features.Providers.Application;

internal sealed class ProviderDispatchService : IProviderDispatchService
{
    private readonly IProviderService _providerService;
    private readonly LlmProviderDispatcher _llmProviderDispatcher;
    private readonly IResourceLogService _resourceLogService;

    public ProviderDispatchService(
        IProviderService providerService,
        LlmProviderDispatcher llmProviderDispatcher,
        IResourceLogService resourceLogService)
    {
        _providerService = providerService;
        _llmProviderDispatcher = llmProviderDispatcher;
        _resourceLogService = resourceLogService;
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
                await AppendProviderLogAsync(provider, workspaceId.Value, ResourceLogType.ErrorConfiguration, $"Provider '{provider}' has no authentication configured.", model, ct);
            return new AgentError(AgentErrorCategory.Configuration, $"Provider '{provider}' has no authentication configured.");
        }

        if (workspaceId.HasValue)
            await AppendProviderLogAsync(provider, workspaceId.Value, ResourceLogType.System, $"Dispatching LLM request to provider '{provider}' model '{model}'.", model, ct);

        var result = await _llmProviderDispatcher.DispatchAsync(provider, auth, model, requestBody, ct);
        if (result.IsFailure && workspaceId.HasValue)
            await AppendProviderLogAsync(provider, workspaceId.Value, result.Error.LogType, result.Error.Message, model, ct);

        return result;
    }

    private Task AppendProviderLogAsync(
        string provider,
        Guid workspaceId,
        ResourceLogType type,
        string content,
        string model,
        CancellationToken ct)
    {
        return _resourceLogService.AppendAsync(new ResourceLogRecord
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
