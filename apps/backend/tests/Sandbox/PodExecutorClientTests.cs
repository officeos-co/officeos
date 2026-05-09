using System.Net;
using System.Net.Http.Json;
using OffceOs.Infrastructure.Features.Agents;
using Xunit;

namespace OffceOs.Tests.Sandbox;

public sealed class PodExecutorClientTests
{
    [Fact]
    public void BuildEndpointUri_builds_rest_endpoint_from_http_base_url()
    {
        var uri = PodExecutorClient.BuildEndpointUri(
            "http://eaos-agent-12345678.default.svc.cluster.local:42617",
            "process/execute");

        Assert.Equal(
            "http://eaos-agent-12345678.default.svc.cluster.local:42617/process/execute",
            uri.ToString());
    }

    [Fact]
    public void BuildEndpointUri_accepts_legacy_websocket_service_url()
    {
        var uri = PodExecutorClient.BuildEndpointUri(
            "ws://eaos-agent-12345678.default.svc.cluster.local:42617/ws",
            "files/download?path=%2Fworkspace%2Ffile.txt");

        Assert.Equal(
            "http://eaos-agent-12345678.default.svc.cluster.local:42617/files/download?path=%2Fworkspace%2Ffile.txt",
            uri.AbsoluteUri);
    }

    [Fact]
    public async Task ExecuteAsync_posts_daytona_like_process_request()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { result = "ok", exitCode = 3 }),
        });
        var client = new PodExecutorClient(new HttpClient(handler));

        var result = await client.ExecuteAsync(
            "eaos-agent-12345678",
            "http://pod-executor:42617",
            "echo ok",
            TimeSpan.FromSeconds(9),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Value.Output);
        Assert.Equal(3, result.Value.ExitCode);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("http://pod-executor:42617/process/execute", request.RequestUri!.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("eaos-agent-12345678", request.Headers.Authorization.Parameter);
        Assert.Contains("\"command\":\"echo ok\"", handler.Bodies.Single());
        Assert.Contains("\"cwd\":\"/workspace\"", handler.Bodies.Single());
        Assert.Contains("\"timeout\":9", handler.Bodies.Single());
    }

    [Fact]
    public async Task WriteFileAsync_creates_folder_then_uploads_multipart_file()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { ok = true }),
        });
        var client = new PodExecutorClient(new HttpClient(handler));

        var result = await client.WriteFileAsync(
            "eaos-agent-12345678",
            "http://pod-executor:42617",
            "/workspace/nested/file.txt",
            "hello",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("http://pod-executor:42617/files/folder?path=%2Fworkspace%2Fnested&mode=0755", handler.Requests[0].RequestUri!.AbsoluteUri);
        Assert.Equal("http://pod-executor:42617/files/upload?path=%2Fworkspace%2Fnested%2Ffile.txt", handler.Requests[1].RequestUri!.AbsoluteUri);
        Assert.All(handler.Requests, request => Assert.Equal("eaos-agent-12345678", request.Headers.Authorization!.Parameter));
        Assert.Contains("hello", handler.Bodies[1]);
    }

    private sealed class RecordingHandler : HttpMessageHandler
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
}
