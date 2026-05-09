namespace OffceOs.Application.Features.Agents;

internal sealed class ToolSearchTool : IAgentTool
{
    private readonly IReadOnlyList<IAgentTool> _tools;
    public ToolSearchTool(IReadOnlyList<IAgentTool> tools) => _tools = tools;
    public IReadOnlyList<string> LastMatchedToolNames { get; private set; } = [];

    public string Name => "tool_search";
    public AgentToolKind Kind => AgentToolKind.Read;
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe => true;
    public ToolSchema Schema => new("tool_search",
        "Find available tools and return their full JSON schemas. Use when you need to discover integration, browser, cron, task, or less common tools.",
        new
        {
            type = "object",
            properties = new
            {
                query = new { type = "string", description = "Keyword search, or select:name1,name2 for exact names" },
                max_results = new { type = "integer", description = "Maximum matching tools (default 10)" }
            },
            required = new[] { "query" }
        });

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var query = args.GetProperty("query").GetString() ?? "";
        var max = args.TryGetProperty("max_results", out var m) ? Math.Clamp(m.GetInt32(), 1, 50) : 10;

        IEnumerable<IAgentTool> matches;
        if (query.StartsWith("select:", StringComparison.OrdinalIgnoreCase))
        {
            var names = query["select:".Length..]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            matches = _tools.Where(t => names.Contains(t.Name));
        }
        else
        {
            var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            matches = _tools.Where(t => terms.Length == 0 || terms.Any(term =>
                t.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || t.Schema.Description.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        var matched = matches
            .Where(t => t.Name != Name)
            .Take(max)
            .ToList();
        LastMatchedToolNames = matched.Select(t => t.Name).ToList();

        await Task.WhenAll(matched
            .OfType<IHydratableToolSchema>()
            .Select(t => HydrateBestEffortAsync(t, ct)));

        var payload = matched.Select(t => new
        {
            name = t.Name,
            description = t.Schema.Description,
            parameters = t.Schema.Parameters
        });

        return new ToolResult(true, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static async Task HydrateBestEffortAsync(IHydratableToolSchema tool, CancellationToken ct)
    {
        try
        {
            await tool.HydrateSchemaAsync(ct);
        }
        catch when (!ct.IsCancellationRequested)
        {
            // Keep tool_search useful even if one deferred integration is temporarily unavailable.
        }
    }
}

internal sealed class ListIntegrationResourcesTool : IAgentTool
{
    private readonly string _integrationName;
    private readonly object? _client;
    public ListIntegrationResourcesTool(string integrationName, object? client) { _integrationName = integrationName; _client = client; }
    public string Name => $"{Slug(_integrationName)}__list_integration_resources";
    public AgentToolKind Kind => AgentToolKind.Integration;
    public bool IsReadOnly => true;
    public ToolSchema Schema => new(Name, $"[{_integrationName}] List available integration resources.", new { type = "object", properties = new { } });

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        if (_client is null) return new ToolResult(false, "", "integration client is not connected.");
        try
        {
            var result = await InvokeIntegrationClientAsync(_client, ["ListResourcesAsync", "ListResourceAsync"], [], ct);
            return new ToolResult(true, FormatUnknown(result));
        }
        catch (Exception ex)
        {
            return new ToolResult(false, "", $"integration resource listing failed: {ex.Message}");
        }
    }

    internal static string Slug(string value) => new string(value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
    internal static string FormatUnknown(object? value)
    {
        if (value is null) return "";
        if (value is string text) return text;
        try
        {
            return JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return value.ToString() ?? "";
        }
    }

    internal static async Task<object?> InvokeIntegrationClientAsync(object client, string[] methodNames, object?[] args, CancellationToken ct)
    {
        var type = client.GetType();
        foreach (var name in methodNames)
        {
            var method = type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m => m.Name == name);
            if (method is null) continue;

            var parameters = method.GetParameters();
            var normalizedArgs = args.ToArray();
            if (parameters.Length > 0 && normalizedArgs.Length > 0 && normalizedArgs[0] is string s)
            {
                if (parameters[0].ParameterType == typeof(Uri))
                    normalizedArgs[0] = new Uri(s, UriKind.RelativeOrAbsolute);
            }

            var invokeArgs = parameters.Length switch
            {
                0 => [],
                1 when parameters[0].ParameterType == typeof(CancellationToken) => [ct],
                1 => normalizedArgs.Length > 0 ? [normalizedArgs[0]] : [],
                2 => normalizedArgs.Length > 0 ? [normalizedArgs[0], ct] : [null!, ct],
                _ => normalizedArgs.Concat([ct]).Take(parameters.Length).ToArray()
            };

            var raw = method.Invoke(client, invokeArgs);
            if (raw is Task task)
            {
                await task.ConfigureAwait(false);
                var resultProperty = task.GetType().GetProperty("Result");
                return resultProperty?.GetValue(task);
            }
            return raw;
        }

        throw new MissingMethodException("Connected integration client does not expose resource methods.");
    }
}

internal sealed class ReadIntegrationResourceTool : IAgentTool
{
    private readonly string _integrationName;
    private readonly object? _client;
    public ReadIntegrationResourceTool(string integrationName, object? client) { _integrationName = integrationName; _client = client; }
    public string Name => $"{ListIntegrationResourcesTool.Slug(_integrationName)}__read_integration_resource";
    public AgentToolKind Kind => AgentToolKind.Integration;
    public bool IsReadOnly => true;
    public ToolSchema Schema => new(Name, $"[{_integrationName}] Read a specific integration resource by URI.",
        new { type = "object", properties = new { uri = new { type = "string" } }, required = new[] { "uri" } });

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        if (_client is null) return new ToolResult(false, "", "integration client is not connected.");
        var uri = args.GetProperty("uri").GetString() ?? "";
        try
        {
            var result = await ListIntegrationResourcesTool.InvokeIntegrationClientAsync(_client, ["ReadResourceAsync", "GetResourceAsync"], [uri], ct);
            return new ToolResult(true, ListIntegrationResourcesTool.FormatUnknown(result));
        }
        catch (Exception ex)
        {
            return new ToolResult(false, "", $"integration resource read failed: {ex.Message}");
        }
    }
}
