namespace EnterpriseAgentOs.Api.Middleware;

public sealed class RequestResponseLoggingMiddleware
{
    private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/healthz",
        "/api/health"
    };

    private const int MaxBodyLogSize = 64 * 1024;

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (ExcludedPaths.Contains(path))
        {
            await _next(context);
            return;
        }

        var requestBody = await ReadRequestBodyAsync(context.Request);

        _logger.LogDebug("HTTP {Method} {Path}{QueryString} RequestBody: {RequestBody}",
            context.Request.Method,
            path,
            context.Request.QueryString,
            requestBody);

        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        await _next(context);

        responseBodyStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await ReadStreamAsync(responseBodyStream);

        _logger.LogDebug("HTTP {Method} {Path} responded {StatusCode} ResponseBody: {ResponseBody}",
            context.Request.Method,
            path,
            context.Response.StatusCode,
            responseBody);

        responseBodyStream.Seek(0, SeekOrigin.Begin);
        await responseBodyStream.CopyToAsync(originalBodyStream);
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength is null or 0)
            return "";

        request.EnableBuffering();
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        return Truncate(body);
    }

    private static async Task<string> ReadStreamAsync(MemoryStream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        return Truncate(body);
    }

    private static string Truncate(string value)
    {
        if (value.Length <= MaxBodyLogSize)
            return value;

        return string.Concat(value.AsSpan(0, MaxBodyLogSize), "... [truncated]");
    }
}
