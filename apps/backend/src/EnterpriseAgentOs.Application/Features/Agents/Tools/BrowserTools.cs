namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class BrowserMcpTool : IAgentTool
{
    private readonly BrowserToolDescriptor _descriptor;
    private readonly IBrowserService _browser;
    private readonly IBrowserRuntimeClient _runtime;
    private readonly Guid _agentId;

    public BrowserMcpTool(
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

    public static string ToToolName(string mcpName) => mcpName.Replace(".", "__");

    public static bool ShouldExpose(BrowserToolDescriptor descriptor)
        => descriptor.Name.StartsWith("browser.", StringComparison.Ordinal)
           && descriptor.Name is not "browser.create_session"
           && descriptor.Name is not "browser.close_session"
           && descriptor.Name is not "browser.list_sessions"
           && descriptor.Name is not "browser.fork_session"
           && descriptor.Name is not "browser.share_session";

    public static IReadOnlyList<BrowserToolDescriptor> DefaultCatalog()
    {
        var tools = new[]
        {
            ("browser.get_session", "Get one browser session summary."),
            ("browser.observe", "Capture the current browser observation with screenshot, interactables, and perception summary."),
            ("browser.screenshot", "Capture a lightweight screenshot for one session without the full observe payload."),
            ("browser.get_console", "Read recent browser console messages for an active session."),
            ("browser.get_page_errors", "Read recent uncaught page errors for an active session."),
            ("browser.get_request_failures", "Read recent failed network requests for an active session."),
            ("browser.stop_trace", "Finalize the current Playwright trace for an active session and return its artifact path."),
            ("browser.list_auth_profiles", "List reusable saved auth profiles that can be loaded into a new session."),
            ("browser.get_auth_profile", "Inspect one saved auth profile and its storage-state metadata."),
            ("browser.list_downloads", "List files captured from browser downloads for one session."),
            ("browser.list_tabs", "List currently open tabs/pages for one session."),
            ("browser.activate_tab", "Switch the active session page to one tab index."),
            ("browser.close_tab", "Close one tab index if more than one tab is open."),
            ("browser.execute_action", "Execute one browser action using the shared internal action schema."),
            ("browser.save_auth_state", "Save session storage state to the per-session auth-state root."),
            ("browser.save_auth_profile", "Save the current session storage state into a reusable named auth profile."),
            ("browser.request_human_takeover", "Ask for a human to take over the shared browser desktop."),
            ("browser.get_network_log", "Return captured HTTP request/response entries for a session."),
            ("browser.eval_js", "Execute a JavaScript expression in the current page context and return the result."),
            ("browser.wait_for_selector", "Wait for a CSS selector to reach a specific state."),
            ("browser.get_html", "Get the HTML source of the current page, optionally as plain text."),
            ("browser.find_elements", "Find all elements matching a CSS selector and return text, href, value, bounding box, and visibility."),
            ("browser.drag_drop", "Drag from one element or coordinate to another."),
            ("browser.set_viewport", "Resize the browser viewport to the specified width and height."),
            ("browser.get_cookies", "Get all cookies for the current session context."),
            ("browser.set_cookies", "Set one or more cookies in the current session context."),
            ("browser.get_local_storage", "Read localStorage or sessionStorage in the current page context."),
            ("browser.set_local_storage", "Write localStorage or sessionStorage in the current page context."),
            ("browser.export_script", "Export the current session's recorded actions as a runnable Playwright Python script."),
            ("browser.cdp_attach", "Attach to an already-running Chrome instance via CDP URL."),
            ("browser.find_by_vision", "Find an element from a natural language description and return click coordinates."),
        };

        return tools.Select(t => new BrowserToolDescriptor(t.Item1, t.Item2, DefaultSchema())).ToList();
    }

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
