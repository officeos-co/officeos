namespace OffceOs.Application.Features.AgentHarness;

internal sealed class BrowserNavigateTool : IAgentTool
{
    private readonly IBrowserService _browserService;
    private readonly IBrowserRuntimeClient _browserRuntimeClient;
    private readonly Guid _agentId;

    public BrowserNavigateTool(IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    {
        _browserService = browser;
        _browserRuntimeClient = runtime;
        _agentId = agentId;
    }

    public string Name => "browser__navigate";
    public bool? ShouldDeferOverride => false;
    public bool IsReadOnly => true;
    public ToolSchema Schema => new(
        Name,
        "[Internal Browser] Navigate the active browser session to an http:// or https:// URL.",
        new
        {
            type = "object",
            properties = new
            {
                url = new { type = "string", description = "The http:// or https:// URL to open in the browser." },
                reason = new { type = "string", description = "Brief reason for the navigation." }
            },
            required = new[] { "url" }
        });

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var url = args.GetProperty("url").GetString() ?? "";
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return new ToolResult(false, "", "Only http:// and https:// URLs are allowed.");

        var reason = args.TryGetProperty("reason", out var reasonElement) && reasonElement.ValueKind == JsonValueKind.String
            ? reasonElement.GetString()
            : null;

        try
        {
            var session = await _browserService.GetOrCreateAsync(_agentId, ct);
            var result = await _browserRuntimeClient.CallToolAsync(
                "browser.execute_action",
                new Dictionary<string, object?>
                {
                    ["session_id"] = session.RuntimeSessionId,
                    ["action"] = new Dictionary<string, object?>
                    {
                        ["action"] = "navigate",
                        ["reason"] = string.IsNullOrWhiteSpace(reason) ? "Open the requested URL in the browser." : reason,
                        ["risk_category"] = "read",
                        ["url"] = uri.ToString(),
                    }
                },
                ct);

            return result.IsError
                ? new ToolResult(false, result.Output, result.Output)
                : new ToolResult(true, result.Output);
        }
        catch (Exception ex)
        {
            return new AgentError(AgentErrorCategory.ToolExecution, $"browser: {ex.Message}");
        }
    }
}
