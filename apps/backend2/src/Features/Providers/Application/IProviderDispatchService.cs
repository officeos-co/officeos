namespace OffceOs.Application.Features.Providers;

public interface IProviderDispatchService
{
    Task<AgentResult<LlmDispatchResponse>> DispatchAsync(
        string provider,
        Guid? workspaceId,
        string model,
        JsonElement requestBody,
        CancellationToken ct = default);
}
