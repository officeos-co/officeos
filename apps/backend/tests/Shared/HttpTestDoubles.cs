namespace OffceOs.Tests.Shared;

public sealed record CapturedRequest(
    string RequestUri,
    string Body,
    string? AuthorizationScheme,
    string? AuthorizationParameter,
    string? ApiKeyHeaderName,
    string? ApiKeyHeaderValue);

public sealed class CapturingHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    public CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

    public List<CapturedRequest> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        request.Headers.TryGetValues("x-api-key", out var apiKeys);
        Requests.Add(new CapturedRequest(
            request.RequestUri?.ToString() ?? string.Empty,
            body,
            request.Headers.Authorization?.Scheme,
            request.Headers.Authorization?.Parameter,
            apiKeys is null ? null : "x-api-key",
            apiKeys?.SingleOrDefault()));

        return _respond(request);
    }
}

public sealed class RecordingHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        _respond = respond;
    }

    public List<HttpRequestMessage> Requests { get; } = [];
    public List<string> Bodies { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
        return _respond(request);
    }
}

public sealed class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public FakeHttpClientFactory(HttpMessageHandler handler) => _handler = handler;

    public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
}
