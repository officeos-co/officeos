namespace OffceOs.Application.Features.Integrations;

public interface IIntegrationRuntimeService
{
    Task<ToolResult> ExecuteToolAsync(
        IntegrationDefinitionRecord integration,
        string toolName,
        JsonElement args,
        Guid? ownerId,
        Guid? workspaceId,
        CancellationToken ct = default);
}
