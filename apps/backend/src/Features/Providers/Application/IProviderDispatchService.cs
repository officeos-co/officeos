using OffceOs.Features.Providers.Infrastructure;
using OffceOs.Common.Domain.Primitives;
namespace OffceOs.Features.Providers.Application;

public interface IProviderDispatchService
{
    Task<AgentResult<LlmDispatchResponse>> DispatchAsync(
        string provider,
        Guid? workspaceId,
        string model,
        JsonElement requestBody,
        CancellationToken ct = default);
}
