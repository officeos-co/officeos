namespace OffceOs.Application.Features.AgentHarness;

internal sealed partial class IntegrationTool : IAgentTool
{
    private readonly McpClientTool _mcpClientTool;
    private readonly McpClient _mcpClient;

    public IntegrationTool(IntegrationDiscoveredTool discovered)
    {
        dynamic handle = discovered.NativeHandle;
        _mcpClientTool = (McpClientTool)handle.Tool;
        _mcpClient = (McpClient)handle.Client;

        Schema = CreateSchema(discovered);
        Name = Schema.Name;
        PermissionScopeOverride = $"{discovered.IntegrationName}:{discovered.Name}";
    }

    public string Name { get; }
    public ToolSchema Schema { get; }
    public string PermissionScopeOverride { get; }
    public AgentToolKind Kind => AgentToolKind.Integration;

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        try
        {
            var argsDict = args.ValueKind == JsonValueKind.Object
                ? args.Deserialize<Dictionary<string, object?>>() ?? new()
                : new Dictionary<string, object?>();

            var response = await _mcpClient.CallToolAsync(_mcpClientTool.Name, argsDict, cancellationToken: ct);
            var output = string.Join("\n", response.Content
                .OfType<ModelContextProtocol.Protocol.TextContentBlock>()
                .Select(content => content.Text ?? string.Empty));

            return response.IsError == true
                ? new ToolResult(false, output, output)
                : new ToolResult(true, output);
        }
        catch (Exception ex)
        {
            return new AgentError(AgentErrorCategory.ToolExecution, ex.Message);
        }
    }

    internal static ToolSchema CreateSchema(IntegrationDiscoveredTool discovered)
    {
        var slug = SlugRegex().Replace(discovered.IntegrationName, "_");
        var toolSlug = SlugRegex().Replace(discovered.Name, "_");
        var name = $"{slug}__{toolSlug}";

        return new ToolSchema(
            name,
            $"[{discovered.IntegrationName}] {discovered.Description ?? discovered.Name}",
            discovered.Parameters ?? EmptyObjectSchema());
    }

    internal static JsonElement EmptyObjectSchema()
        => JsonSerializer.SerializeToElement(new { type = "object", properties = new { } });

    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    private static partial Regex SlugRegex();
}

internal interface IHydratableToolSchema
{
    Task<ToolSchema> HydrateSchemaAsync(CancellationToken ct);
}

internal sealed partial class UnavailableIntegrationTool : IAgentTool
{
    private readonly IntegrationDefinitionRecord _server;
    private readonly IntegrationCatalogToolRecord _catalogTool;

    public UnavailableIntegrationTool(IntegrationDefinitionRecord server, IntegrationCatalogToolRecord catalogTool)
    {
        _server = server;
        _catalogTool = catalogTool;
        var slug = SlugRegex().Replace(server.Name, "_");
        var toolSlug = SlugRegex().Replace(catalogTool.Name, "_");
        Name = $"{slug}__{toolSlug}";
        PermissionScopeOverride = $"{server.Name}:{catalogTool.Name}";
        Schema = new ToolSchema(
            Name,
            $"[{server.Name}] {catalogTool.Description}",
            catalogTool.Parameters ?? JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new { },
                additionalProperties = true,
            }));
    }

    public string Name { get; }
    public ToolSchema Schema { get; }
    public string PermissionScopeOverride { get; }
    public AgentToolKind Kind => AgentToolKind.Integration;

    public Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
        => Task.FromResult<AgentResult<ToolResult>>(new ToolResult(
            false,
            string.Empty,
            $"Integration '{_server.Name}' is not connected; tool '{_catalogTool.Name}' cannot execute."));

    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    private static partial Regex SlugRegex();
}
