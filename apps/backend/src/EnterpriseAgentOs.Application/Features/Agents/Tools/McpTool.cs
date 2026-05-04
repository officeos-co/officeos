using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
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

        Schema = CreateSchema(discovered);
        Name = Schema.Name;
        PermissionScope = $"{discovered.ServerName}:{discovered.Name}";
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

    internal static ToolSchema CreateSchema(McpDiscoveredTool discovered)
    {
        var slug = SlugRegex().Replace(discovered.ServerName, "_");
        var toolSlug = SlugRegex().Replace(discovered.Name, "_");
        var name = $"{slug}__{toolSlug}";
        var parameters = !string.IsNullOrEmpty(discovered.JsonSchema)
            ? JsonSerializer.Deserialize<JsonElement>(discovered.JsonSchema)
            : EmptyObjectSchema();

        return new ToolSchema(
            Name: name,
            Description: $"[{discovered.ServerName}] {discovered.Description ?? discovered.Name}",
            Parameters: parameters);
    }

    internal static JsonElement EmptyObjectSchema()
        => JsonSerializer.SerializeToElement(new { type = "object", properties = new { } });
}

internal sealed record McpCatalogTool(string Name, string Description, JsonElement? Parameters);

internal interface IHydratableToolSchema
{
    Task<ToolSchema> HydrateSchemaAsync(CancellationToken ct);
}

internal sealed partial class LazyMcpTool : IAgentTool, IHydratableToolSchema
{
    private readonly string _runtimeToolName;
    private readonly LazyMcpServerConnection _connection;
    private ToolSchema _schema;

    public LazyMcpTool(McpServerRecord server, McpCatalogTool catalogTool, LazyMcpServerConnection connection)
    {
        _runtimeToolName = catalogTool.Name;
        _connection = connection;

        var slug = SlugRegex().Replace(server.Name, "_");
        var toolSlug = SlugRegex().Replace(catalogTool.Name, "_");
        Name = $"{slug}__{toolSlug}";
        PermissionScope = $"{server.Name}:{catalogTool.Name}";
        _schema = new ToolSchema(
            Name,
            $"[{server.Name}] {catalogTool.Description}",
            catalogTool.Parameters ?? JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new { },
                additionalProperties = true
            }));
    }

    public string Name { get; }
    public ToolSchema Schema => _schema;
    public string PermissionScope { get; }
    public AgentToolKind Kind => AgentToolKind.Mcp;

    public async Task<ToolSchema> HydrateSchemaAsync(CancellationToken ct)
    {
        var discovered = await GetDiscoveredToolAsync(ct);
        if (discovered is not null)
            _schema = McpTool.CreateSchema(discovered);

        return _schema;
    }

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var discovered = await GetDiscoveredToolAsync(ct);
        if (discovered is null)
            return new AgentError(AgentErrorCategory.ToolExecution, $"MCP tool '{_runtimeToolName}' was not discovered on server '{_connection.ServerName}'.");

        return await new McpTool(discovered).ExecuteAsync(args, ct);
    }

    private async Task<McpDiscoveredTool?> GetDiscoveredToolAsync(CancellationToken ct)
    {
        var connection = await _connection.EnsureConnectedAsync(ct);
        return connection.Tools.FirstOrDefault(t => string.Equals(t.Name, _runtimeToolName, StringComparison.Ordinal));
    }

    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    private static partial Regex SlugRegex();
}

internal sealed class LazyListMcpResourcesTool : IAgentTool
{
    private readonly McpServerRecord _server;
    private readonly LazyMcpServerConnection _connection;

    public LazyListMcpResourcesTool(McpServerRecord server, LazyMcpServerConnection connection)
    {
        _server = server;
        _connection = connection;
        Name = $"{Slug(server.Name)}__list_mcp_resources";
        Schema = new ToolSchema(Name, $"[{server.Name}] List available MCP resources.", new { type = "object", properties = new { } });
    }

    public string Name { get; }
    public AgentToolKind Kind => AgentToolKind.Mcp;
    public bool IsReadOnly => true;
    public ToolSchema Schema { get; }

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var connection = await _connection.EnsureConnectedAsync(ct);
        if (connection.NativeClient is null)
            return new ToolResult(false, "", "MCP client is not connected.");

        try
        {
            var result = await ListMcpResourcesTool.InvokeMcpAsync(connection.NativeClient, ["ListResourcesAsync", "ListResourceAsync"], [], ct);
            return new ToolResult(true, ListMcpResourcesTool.FormatUnknown(result));
        }
        catch (Exception ex)
        {
            return new ToolResult(false, "", $"MCP resource listing failed for {_server.Name}: {ex.Message}");
        }
    }

    private static string Slug(string value) => new(value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
}

internal sealed class LazyReadMcpResourceTool : IAgentTool
{
    private readonly McpServerRecord _server;
    private readonly LazyMcpServerConnection _connection;

    public LazyReadMcpResourceTool(McpServerRecord server, LazyMcpServerConnection connection)
    {
        _server = server;
        _connection = connection;
        Name = $"{Slug(server.Name)}__read_mcp_resource";
        Schema = new ToolSchema(Name, $"[{server.Name}] Read a specific MCP resource by URI.",
            new { type = "object", properties = new { uri = new { type = "string" } }, required = new[] { "uri" } });
    }

    public string Name { get; }
    public AgentToolKind Kind => AgentToolKind.Mcp;
    public bool IsReadOnly => true;
    public ToolSchema Schema { get; }

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var connection = await _connection.EnsureConnectedAsync(ct);
        if (connection.NativeClient is null)
            return new ToolResult(false, "", "MCP client is not connected.");

        var uri = args.GetProperty("uri").GetString() ?? "";
        try
        {
            var result = await ListMcpResourcesTool.InvokeMcpAsync(connection.NativeClient, ["ReadResourceAsync", "GetResourceAsync"], [uri], ct);
            return new ToolResult(true, ListMcpResourcesTool.FormatUnknown(result));
        }
        catch (Exception ex)
        {
            return new ToolResult(false, "", $"MCP resource read failed for {_server.Name}: {ex.Message}");
        }
    }

    private static string Slug(string value) => new(value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
}

internal sealed class LazyMcpServerConnection : IAsyncDisposable
{
    private readonly McpServerRecord _server;
    private readonly Func<string, Task<Dictionary<string, string>>> _credentialLoader;
    private readonly IMcpClientManager _mcpClientManager;
    private readonly TurnEventPublisher _events;
    private readonly Guid _agentId;
    private readonly string _correlationId;
    private readonly object _gate = new();
    private Task<McpConnectionResult>? _connectionTask;

    public LazyMcpServerConnection(
        McpServerRecord server,
        Func<string, Task<Dictionary<string, string>>> credentialLoader,
        IMcpClientManager mcpClientManager,
        TurnEventPublisher events,
        Guid agentId,
        string correlationId)
    {
        _server = server;
        _credentialLoader = credentialLoader;
        _mcpClientManager = mcpClientManager;
        _events = events;
        _agentId = agentId;
        _correlationId = correlationId;
    }

    public string ServerName => _server.Name;

    public Task<McpConnectionResult> EnsureConnectedAsync(CancellationToken ct)
    {
        lock (_gate)
        {
            _connectionTask ??= ConnectAsync(ct);
            return _connectionTask;
        }
    }

    public async ValueTask DisposeAsync()
    {
        var task = _connectionTask;
        if (task is { IsCompletedSuccessfully: true })
            await task.Result.DisposeAsync();
    }

    private async Task<McpConnectionResult> ConnectAsync(CancellationToken ct)
    {
        var credentialStart = Stopwatch.GetTimestamp();
        var credentials = await _credentialLoader(_server.Name);
        await _events.PublishDiagnosticAsync(
            _agentId,
            _correlationId,
            $"Tool setup: MCP credentials loaded ({_server.Name})",
            ElapsedMs(credentialStart),
            ct);

        var connectStart = Stopwatch.GetTimestamp();
        var result = await _mcpClientManager.ConnectAsync(_server, credentials, ct);
        await _events.PublishDiagnosticAsync(
            _agentId,
            _correlationId,
            $"Tool setup: MCP connected ({_server.Name}, {result.Tools.Count} tools)",
            ElapsedMs(connectStart),
            ct);
        return result;
    }

    private static int ElapsedMs(long startTimestamp)
        => (int)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
}
