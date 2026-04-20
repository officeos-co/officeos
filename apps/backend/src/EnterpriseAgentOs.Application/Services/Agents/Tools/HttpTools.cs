using System.Text.Json;
using System.Text.RegularExpressions;

namespace EnterpriseAgentOs.Application.Services.Agents.Tools;

public sealed class HttpRequestTool : IAgentTool
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    public string Name => "http_request";
    public ToolSchema Schema => new("http_request",
        "Make an HTTP request. Supports GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS.",
        new
        {
            type = "object",
            properties = new
            {
                url = new { type = "string", description = "The URL to request" },
                method = new { type = "string", description = "HTTP method (default GET)" },
                headers = new { type = "object", description = "Request headers" },
                body = new { type = "string", description = "Request body" }
            },
            required = new[] { "url" }
        });

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var url = args.GetProperty("url").GetString() ?? "";
        var methodStr = args.TryGetProperty("method", out var m) ? m.GetString() ?? "GET" : "GET";
        var body = args.TryGetProperty("body", out var b) ? b.GetString() : null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
            return new ToolResult(false, "", "Only http:// and https:// URLs are allowed.");

        try
        {
            var request = new HttpRequestMessage(new HttpMethod(methodStr.ToUpperInvariant()), uri);

            if (args.TryGetProperty("headers", out var headers) && headers.ValueKind == JsonValueKind.Object)
            {
                foreach (var header in headers.EnumerateObject())
                    request.Headers.TryAddWithoutValidation(header.Name, header.Value.GetString());
            }

            if (body != null)
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await Http.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (responseBody.Length > 1_000_000)
                responseBody = responseBody[..1_000_000] + "\n[truncated]";

            return new ToolResult(response.IsSuccessStatusCode,
                $"HTTP {(int)response.StatusCode} {response.StatusCode}\n\n{responseBody}");
        }
        catch (TaskCanceledException)
        {
            return new AgentError(AgentErrorCategory.ToolExecution, "http_request: Request timed out (30s)");
        }
        catch (Exception ex)
        {
            return new AgentError(AgentErrorCategory.ToolExecution, $"http_request: {ex.Message}", ex.ToString());
        }
    }
}

public sealed class WebFetchTool : IAgentTool
{
    private static readonly HttpClient Http = new(new HttpClientHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 10,
    }) { Timeout = TimeSpan.FromSeconds(30) };

    public string Name => "web_fetch";
    public ToolSchema Schema => new("web_fetch",
        "Fetch a URL and convert to plain text. HTML is cleaned to readable text.",
        new
        {
            type = "object",
            properties = new
            {
                url = new { type = "string", description = "URL to fetch" }
            },
            required = new[] { "url" }
        });

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var url = args.GetProperty("url").GetString() ?? "";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
            return new ToolResult(false, "", "Only http:// and https:// URLs are allowed.");

        try
        {
            var response = await Http.GetAsync(uri, ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (content.Length > 1_000_000)
                content = content[..1_000_000] + "\n[truncated]";

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            if (contentType.Contains("html"))
            {
                content = Regex.Replace(content, "<script[^>]*>[\\s\\S]*?</script>", "", RegexOptions.IgnoreCase);
                content = Regex.Replace(content, "<style[^>]*>[\\s\\S]*?</style>", "", RegexOptions.IgnoreCase);
                content = Regex.Replace(content, "<[^>]+>", " ");
                content = System.Net.WebUtility.HtmlDecode(content);
                content = Regex.Replace(content, @"\s+", " ").Trim();
            }

            return new ToolResult(true, content);
        }
        catch (TaskCanceledException)
        {
            return new AgentError(AgentErrorCategory.ToolExecution, "web_fetch: Request timed out (30s)");
        }
        catch (Exception ex)
        {
            return new AgentError(AgentErrorCategory.ToolExecution, $"web_fetch: {ex.Message}", ex.ToString());
        }
    }
}
