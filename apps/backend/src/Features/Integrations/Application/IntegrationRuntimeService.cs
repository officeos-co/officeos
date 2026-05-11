namespace OffceOs.Application.Features.Integrations;

internal sealed class IntegrationRuntimeService : IIntegrationRuntimeService
{
    private readonly IIntegrationDefinitionService _integrationDefinitionService;
    private readonly IIntegrationClientManager _integrationClientManager;

    public IntegrationRuntimeService(
        IIntegrationDefinitionService integrationDefinitionService,
        IIntegrationClientManager integrationClientManager)
    {
        _integrationDefinitionService = integrationDefinitionService;
        _integrationClientManager = integrationClientManager;
    }

    public async Task<ToolResult> ExecuteToolAsync(
        IntegrationDefinitionRecord integration,
        string toolName,
        JsonElement args,
        Guid? ownerId,
        Guid? workspaceId,
        CancellationToken ct = default)
    {
        var credentials = await _integrationDefinitionService.GetDecryptedCredentialAsync(integration.Name, ownerId, workspaceId, ct);
        if (credentials.Count == 0)
            return new ToolResult(false, "", $"integration '{integration.Name}' is not connected for this workspace.");

        await using var connection = await _integrationClientManager.ConnectAsync(integration, credentials, ct);
        var discovered = connection.Tools.FirstOrDefault(tool => string.Equals(tool.Name, toolName, StringComparison.Ordinal));
        if (discovered?.NativeHandle is null)
            return new ToolResult(false, "", $"integration tool '{toolName}' was not discovered on server '{integration.Name}'.");

        dynamic handle = discovered.NativeHandle;
        var runtimeTool = (McpClientTool)handle.Tool;
        var mcpClient = (McpClient)handle.Client;
        var argsDict = args.ValueKind == JsonValueKind.Object
            ? args.Deserialize<Dictionary<string, object?>>() ?? new()
            : new Dictionary<string, object?>();

        var response = await mcpClient.CallToolAsync(runtimeTool.Name, argsDict, cancellationToken: ct);
        var output = string.Join("\n", response.Content
            .OfType<ModelContextProtocol.Protocol.TextContentBlock>()
            .Select(c => c.Text ?? ""));

        return response.IsError == true
            ? new ToolResult(false, output, output)
            : new ToolResult(true, output);
    }
}
