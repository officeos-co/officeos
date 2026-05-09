namespace OffceOs.Application.Features.Agents;

internal sealed partial class IntegrationTool : IAgentTool
{
    private readonly McpClientTool _runtimeTool;
    private readonly McpClient _mcpClient;

    public string Name { get; }
    public ToolSchema Schema { get; }
    public string PermissionScope { get; }

    public IntegrationTool(IntegrationDiscoveredTool discovered)
    {
        // NativeHandle is IntegrationNativeHandle from Infrastructure
        dynamic handle = discovered.NativeHandle;
        _runtimeTool = (McpClientTool)handle.Tool;
        _mcpClient = (McpClient)handle.Client;

        Schema = CreateSchema(discovered);
        Name = Schema.Name;
        PermissionScope = $"{discovered.IntegrationName}:{discovered.Name}";
    }

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        try
        {
            var argsDict = args.ValueKind == JsonValueKind.Object
                ? args.Deserialize<Dictionary<string, object?>>() ?? new()
                : new Dictionary<string, object?>();

            var response = await _mcpClient.CallToolAsync(_runtimeTool.Name, argsDict, cancellationToken: ct);
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

    internal static ToolSchema CreateSchema(IntegrationDiscoveredTool discovered)
    {
        var slug = SlugRegex().Replace(discovered.IntegrationName, "_");
        var toolSlug = SlugRegex().Replace(discovered.Name, "_");
        var name = $"{slug}__{toolSlug}";
        var parameters = !string.IsNullOrEmpty(discovered.JsonSchema)
            ? JsonSerializer.Deserialize<JsonElement>(discovered.JsonSchema)
            : EmptyObjectSchema();

        return new ToolSchema(
            Name: name,
            Description: $"[{discovered.IntegrationName}] {discovered.Description ?? discovered.Name}",
            Parameters: parameters);
    }

    internal static JsonElement EmptyObjectSchema()
        => JsonSerializer.SerializeToElement(new { type = "object", properties = new { } });
}

internal sealed record IntegrationCatalogTool(string Name, string Description, JsonElement? Parameters);

internal interface IHydratableToolSchema
{
    Task<ToolSchema> HydrateSchemaAsync(CancellationToken ct);
}

internal sealed partial class LazyIntegrationTool : IAgentTool, IHydratableToolSchema
{
    private readonly string _runtimeToolName;
    private readonly LazyIntegrationConnection _connection;
    private ToolSchema _schema;

    public LazyIntegrationTool(IntegrationDefinitionRecord server, IntegrationCatalogTool catalogTool, LazyIntegrationConnection connection)
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
    public AgentToolKind Kind => AgentToolKind.Integration;

    public async Task<ToolSchema> HydrateSchemaAsync(CancellationToken ct)
    {
        var discovered = await GetDiscoveredToolAsync(ct);
        if (discovered is not null)
            _schema = IntegrationTool.CreateSchema(discovered);

        return _schema;
    }

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var discovered = await GetDiscoveredToolAsync(ct);
        if (discovered is null)
            return new AgentError(AgentErrorCategory.ToolExecution, $"integration tool '{_runtimeToolName}' was not discovered on server '{_connection.IntegrationName}'.");

        return await new IntegrationTool(discovered).ExecuteAsync(args, ct);
    }

    private async Task<IntegrationDiscoveredTool?> GetDiscoveredToolAsync(CancellationToken ct)
    {
        var connection = await _connection.EnsureConnectedAsync(ct);
        return connection.Tools.FirstOrDefault(t => string.Equals(t.Name, _runtimeToolName, StringComparison.Ordinal));
    }

    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    private static partial Regex SlugRegex();
}

internal sealed class LazyListIntegrationResourcesTool : IAgentTool
{
    private readonly IntegrationDefinitionRecord _server;
    private readonly LazyIntegrationConnection _connection;

    public LazyListIntegrationResourcesTool(IntegrationDefinitionRecord server, LazyIntegrationConnection connection)
    {
        _server = server;
        _connection = connection;
        Name = $"{Slug(server.Name)}__list_integration_resources";
        Schema = new ToolSchema(Name, $"[{server.Name}] List available integration resources.", new { type = "object", properties = new { } });
    }

    public string Name { get; }
    public AgentToolKind Kind => AgentToolKind.Integration;
    public bool IsReadOnly => true;
    public ToolSchema Schema { get; }

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var connection = await _connection.EnsureConnectedAsync(ct);
        if (connection.NativeClient is null)
            return new ToolResult(false, "", "integration client is not connected.");

        try
        {
            var result = await ListIntegrationResourcesTool.InvokeIntegrationClientAsync(connection.NativeClient, ["ListResourcesAsync", "ListResourceAsync"], [], ct);
            return new ToolResult(true, ListIntegrationResourcesTool.FormatUnknown(result));
        }
        catch (Exception ex)
        {
            return new ToolResult(false, "", $"integration resource listing failed for {_server.Name}: {ex.Message}");
        }
    }

    private static string Slug(string value) => new(value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
}

internal sealed class LazyReadIntegrationResourceTool : IAgentTool
{
    private readonly IntegrationDefinitionRecord _server;
    private readonly LazyIntegrationConnection _connection;

    public LazyReadIntegrationResourceTool(IntegrationDefinitionRecord server, LazyIntegrationConnection connection)
    {
        _server = server;
        _connection = connection;
        Name = $"{Slug(server.Name)}__read_integration_resource";
        Schema = new ToolSchema(Name, $"[{server.Name}] Read a specific integration resource by URI.",
            new { type = "object", properties = new { uri = new { type = "string" } }, required = new[] { "uri" } });
    }

    public string Name { get; }
    public AgentToolKind Kind => AgentToolKind.Integration;
    public bool IsReadOnly => true;
    public ToolSchema Schema { get; }

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var connection = await _connection.EnsureConnectedAsync(ct);
        if (connection.NativeClient is null)
            return new ToolResult(false, "", "integration client is not connected.");

        var uri = args.GetProperty("uri").GetString() ?? "";
        try
        {
            var result = await ListIntegrationResourcesTool.InvokeIntegrationClientAsync(connection.NativeClient, ["ReadResourceAsync", "GetResourceAsync"], [uri], ct);
            return new ToolResult(true, ListIntegrationResourcesTool.FormatUnknown(result));
        }
        catch (Exception ex)
        {
            return new ToolResult(false, "", $"integration resource read failed for {_server.Name}: {ex.Message}");
        }
    }

    private static string Slug(string value) => new(value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
}

internal sealed class LazyIntegrationConnection : IAsyncDisposable
{
    private readonly IntegrationDefinitionRecord _server;
    private readonly Func<string, Task<Dictionary<string, string>>> _credentialLoader;
    private readonly IIntegrationClientManager _integrationClientManager;
    private readonly TurnEventPublisher _turnEventPublisher;
    private readonly Guid _agentId;
    private readonly string _correlationId;
    private readonly object _gate = new();
    private Task<IntegrationConnectionResult>? _connectionTask;

    public LazyIntegrationConnection(
        IntegrationDefinitionRecord server,
        Func<string, Task<Dictionary<string, string>>> credentialLoader,
        IIntegrationClientManager integrationClientManager,
        TurnEventPublisher events,
        Guid agentId,
        string correlationId)
    {
        _server = server;
        _credentialLoader = credentialLoader;
        _integrationClientManager = integrationClientManager;
        _turnEventPublisher = events;
        _agentId = agentId;
        _correlationId = correlationId;
    }

    public string IntegrationName => _server.Name;

    public Task<IntegrationConnectionResult> EnsureConnectedAsync(CancellationToken ct)
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

    private async Task<IntegrationConnectionResult> ConnectAsync(CancellationToken ct)
    {
        var credentialStart = Stopwatch.GetTimestamp();
        var credentials = await _credentialLoader(_server.Name);
        await _turnEventPublisher.PublishDiagnosticAsync(
            _agentId,
            _correlationId,
            $"Tool setup: integration credentials loaded ({_server.Name})",
            ElapsedMs(credentialStart),
            ct);

        var connectStart = Stopwatch.GetTimestamp();
        var result = await _integrationClientManager.ConnectAsync(_server, credentials, ct);
        await _turnEventPublisher.PublishDiagnosticAsync(
            _agentId,
            _correlationId,
            $"Tool setup: integration connected ({_server.Name}, {result.Tools.Count} tools)",
            ElapsedMs(connectStart),
            ct);
        return result;
    }

    private static int ElapsedMs(long startTimestamp)
        => (int)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
}
