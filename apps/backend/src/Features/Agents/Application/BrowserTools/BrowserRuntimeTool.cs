namespace EnterpriseAgentOs.Application.Features.Agents;

internal abstract class BrowserRuntimeTool : IAgentTool
{
    private readonly BrowserToolDescriptor _descriptor;
    private readonly IBrowserService _browser;
    private readonly IBrowserRuntimeClient _runtime;
    private readonly Guid _agentId;

    protected BrowserRuntimeTool(
        string runtimeName,
        string description,
        IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors,
        IBrowserService browser,
        IBrowserRuntimeClient runtime,
        Guid agentId)
        : this(ResolveDescriptor(runtimeName, description, descriptors), browser, runtime, agentId)
    {
    }

    private BrowserRuntimeTool(
        BrowserToolDescriptor descriptor,
        IBrowserService browser,
        IBrowserRuntimeClient runtime,
        Guid agentId)
    {
        _descriptor = descriptor;
        _browser = browser;
        _runtime = runtime;
        _agentId = agentId;

        Name = ToToolName(descriptor.Name);
        Schema = new ToolSchema(
            Name,
            $"[Internal Browser] {descriptor.Description}",
            StripManagedFields(descriptor.InputSchema, descriptor.Name));
    }

    public string Name { get; }
    public ToolSchema Schema { get; }

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        try
        {
            var arguments = args.ValueKind == JsonValueKind.Object
                ? args.Deserialize<Dictionary<string, object?>>() ?? new()
                : new Dictionary<string, object?>();

            if (ShouldBindAgentSession(_descriptor))
            {
                var session = await _browser.GetOrCreateAsync(_agentId, ct);
                arguments["session_id"] = session.RuntimeSessionId;
            }
            if (_descriptor.Name == "browser.save_auth_profile")
                arguments["profile_name"] = BrowserService.AgentProfileName(_agentId);

            var result = await _runtime.CallToolAsync(_descriptor.Name, arguments, ct);
            return result.IsError
                ? new ToolResult(false, result.Output, result.Output)
                : new ToolResult(true, result.Output);
        }
        catch (Exception ex)
        {
            return new AgentError(AgentErrorCategory.ToolExecution, $"browser: {ex.Message}");
        }
    }

    public static string ToToolName(string runtimeName) => runtimeName.Replace(".", "__");

    private static BrowserToolDescriptor ResolveDescriptor(
        string runtimeName,
        string description,
        IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors)
        => descriptors.TryGetValue(runtimeName, out var descriptor)
            ? descriptor
            : new BrowserToolDescriptor(runtimeName, description, DefaultSchema());

    private static JsonElement DefaultSchema() => JsonSerializer.SerializeToElement(new
    {
        type = "object",
        properties = new
        {
            session_id = new { type = "string", description = "Browser session id. Managed by EAOS when called from an agent." }
        }
    });

    private static bool ShouldBindAgentSession(BrowserToolDescriptor descriptor)
    {
        if (descriptor.InputSchema.ValueKind != JsonValueKind.Object) return false;
        if (!descriptor.InputSchema.TryGetProperty("properties", out var properties)) return false;
        return properties.ValueKind == JsonValueKind.Object && properties.TryGetProperty("session_id", out _);
    }

    private static JsonElement StripManagedFields(JsonElement schema, string toolName)
    {
        if (schema.ValueKind != JsonValueKind.Object
            || !schema.TryGetProperty("properties", out var properties)
            || properties.ValueKind != JsonValueKind.Object)
            return schema;

        var managed = new HashSet<string> { "session_id" };
        if (toolName == "browser.save_auth_profile")
            managed.Add("profile_name");

        if (!properties.EnumerateObject().Any(p => managed.Contains(p.Name)))
            return schema;

        using var doc = JsonDocument.Parse(schema.GetRawText());
        var root = doc.RootElement;
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.NameEquals("properties"))
                {
                    writer.WritePropertyName("properties");
                    writer.WriteStartObject();
                    foreach (var child in prop.Value.EnumerateObject())
                    {
                        if (managed.Contains(child.Name)) continue;
                        child.WriteTo(writer);
                    }
                    writer.WriteEndObject();
                    continue;
                }

                if (prop.NameEquals("required") && prop.Value.ValueKind == JsonValueKind.Array)
                {
                    writer.WritePropertyName("required");
                    writer.WriteStartArray();
                    foreach (var item in prop.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String && managed.Contains(item.GetString() ?? ""))
                            continue;
                        item.WriteTo(writer);
                    }
                    writer.WriteEndArray();
                    continue;
                }

                prop.WriteTo(writer);
            }
            writer.WriteEndObject();
        }

        return JsonSerializer.Deserialize<JsonElement>(stream.ToArray());
    }
}
