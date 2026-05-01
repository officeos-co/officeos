using System.Text.Json;
using System.Text.RegularExpressions;
using ModelContextProtocol.Client;

namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed partial class McpTool : IAgentTool
{
    private readonly McpClientTool _mcpTool;
    private readonly McpClient _client;

    public string Name { get; }
    public ToolSchema Schema { get; }
    public string PermissionScope { get; }

    public McpTool(McpDiscoveredTool discovered)
    {
        // NativeHandle is McpNativeHandle from Infrastructure
        dynamic handle = discovered.NativeHandle;
        _mcpTool = (McpClientTool)handle.Tool;
        _client = (McpClient)handle.Client;

        var slug = SlugRegex().Replace(discovered.ServerName, "_");
        var toolSlug = SlugRegex().Replace(discovered.Name, "_");
        Name = $"{slug}__{toolSlug}";
        PermissionScope = $"{discovered.ServerName}:{discovered.Name}";

        var parameters = !string.IsNullOrEmpty(discovered.JsonSchema)
            ? JsonSerializer.Deserialize<JsonElement>(discovered.JsonSchema)
            : JsonSerializer.SerializeToElement(new { type = "object", properties = new { } });

        Schema = new ToolSchema(
            Name: Name,
            Description: $"[{discovered.ServerName}] {discovered.Description ?? discovered.Name}",
            Parameters: parameters);
    }

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        try
        {
            var argsDict = args.ValueKind == JsonValueKind.Object
                ? args.Deserialize<Dictionary<string, object?>>() ?? new()
                : new Dictionary<string, object?>();

            var response = await _client.CallToolAsync(_mcpTool.Name, argsDict, cancellationToken: ct);
            var output = string.Join("\n", response.Content
                .OfType<ModelContextProtocol.Protocol.TextContentBlock>()
                .Select(c => c.Text ?? ""));

            return response.IsError == true
                ? new ToolResult(false, output, output)
                : new ToolResult(true, output);
        }
        catch (Exception ex)
        {
            return new AgentError(AgentErrorCategory.ToolExecution, ex.Message);
        }
    }

    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    private static partial Regex SlugRegex();
}
