using System.Net;
using System.Net.Http.Json;
using EnterpriseAgentOs.Domain.Features.Agents;
using EnterpriseAgentOs.Infrastructure.Common.Configuration;
using EnterpriseAgentOs.Infrastructure.Features.Agents;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EnterpriseAgentOs.Api.Tests.Sandbox;

public sealed class DaytonaSandboxProviderTests
{
    private const string ToolboxUrl = "http://proxy.local/toolbox";

    [Fact]
    public async Task CreateAsync_maps_request_and_response()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { id = "sbx-123", toolboxProxyUrl = ToolboxUrl }),
        });
        var provider = CreateProvider(handler);

        var deployment = await provider.CreateAsync(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            null,
            new Dictionary<string, string> { ["FOO"] = "bar" },
            new Dictionary<string, string> { ["eaos.agent_id"] = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa" });

        Assert.Equal("sbx-123", deployment.SandboxId);
        Assert.Equal(ToolboxUrl, deployment.ServiceUrl);
        Assert.Equal(HttpMethod.Post, handler.Requests.Single().Method);
        Assert.Equal("http://daytona.local/api/sandbox", handler.Requests.Single().RequestUri!.ToString());
        Assert.Contains("\"target\":\"us\"", handler.Bodies.Single());
        Assert.Contains("\"snapshot\":\"daytonaio/sandbox:0.5.0-slim\"", handler.Bodies.Single());
        Assert.Contains("\"env\"", handler.Bodies.Single());
        Assert.Contains("\"FOO\":\"bar\"", handler.Bodies.Single());
        Assert.Contains("\"EAOS_EAOS_AGENT_ID\"", handler.Bodies.Single());
    }

    [Fact]
    public async Task Requests_include_bearer_token()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { id = "sbx-123", toolboxProxyUrl = ToolboxUrl }),
        });
        var provider = CreateProvider(handler);

        await provider.CreateAsync(Guid.NewGuid(), null, new Dictionary<string, string>(), new Dictionary<string, string>());

        Assert.Equal("Bearer", handler.Requests.Single().Headers.Authorization!.Scheme);
        Assert.Equal("test-daytona-key", handler.Requests.Single().Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task ExecuteAsync_posts_to_toolbox_and_maps_exit_code()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { result = "ok", exitCode = 0 }),
        });
        var provider = CreateProvider(handler);

        var result = await provider.ExecuteAsync("sbx-123", ToolboxUrl, "echo ok", TimeSpan.FromSeconds(5));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.ExitCode);
        Assert.Equal("ok", result.Value.Output);
        Assert.Equal("http://proxy.local/toolbox/sbx-123/process/execute", handler.Requests.Single().RequestUri!.ToString());
        Assert.Equal("test-daytona-key", handler.Requests.Single().Headers.Authorization!.Parameter);
        Assert.Contains("\"command\":\"echo ok\"", handler.Bodies.Single());
        Assert.Contains("\"cwd\":\"/workspace\"", handler.Bodies.Single());
        Assert.Contains("\"timeout\":5", handler.Bodies.Single());
    }

    [Fact]
    public async Task ExecuteAsync_maps_non_zero_exit()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { result = "failed", exitCode = 2 }),
        });
        var provider = CreateProvider(handler);

        var result = await provider.ExecuteAsync("sbx-123", ToolboxUrl, "false", TimeSpan.FromSeconds(5));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.ExitCode);
        Assert.Equal("failed", result.Value.Output);
    }

    [Fact]
    public async Task ReadFileAsync_returns_failure_for_missing_file()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("missing"),
        });
        var provider = CreateProvider(handler);

        var result = await provider.ReadFileAsync("sbx-123", ToolboxUrl, "/tmp/missing.txt");

        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Error.Message);
    }

    [Fact]
    public async Task ReadFileAsync_downloads_text_from_toolbox()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("hello"),
        });
        var provider = CreateProvider(handler);

        var result = await provider.ReadFileAsync("sbx-123", ToolboxUrl, "/workspace/hello.txt");

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
        Assert.Equal("http://proxy.local/toolbox/sbx-123/files/download?path=%2Fworkspace%2Fhello.txt", handler.Requests.Single().RequestUri!.ToString());
    }

    [Fact]
    public async Task WriteFileAsync_creates_parent_directory_and_uploads_multipart_file()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var provider = CreateProvider(handler);

        var result = await provider.WriteFileAsync("sbx-123", ToolboxUrl, "/workspace/a/b.txt", "hello");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("http://proxy.local/toolbox/sbx-123/files/folder?path=%2Fworkspace%2Fa&mode=0755", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal("http://proxy.local/toolbox/sbx-123/files/upload?path=%2Fworkspace%2Fa%2Fb.txt", handler.Requests[1].RequestUri!.ToString());
        Assert.All(handler.Requests, request => Assert.Equal("test-daytona-key", request.Headers.Authorization!.Parameter));
        Assert.StartsWith("multipart/form-data", handler.Requests[1].Content!.Headers.ContentType!.MediaType);
        Assert.Contains("hello", handler.Bodies[1]);
    }

    [Fact]
    public async Task TerminateAsync_is_idempotent_for_not_found()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var provider = CreateProvider(handler);

        var result = await provider.TerminateAsync("sbx-123");

        Assert.True(result);
        Assert.Equal("http://daytona.local/api/sandbox/sbx-123", handler.Requests.Single().RequestUri!.ToString());
    }

    private static DaytonaSandboxProvider CreateProvider(RecordingHandler handler)
    {
        var client = new HttpClient(handler, disposeHandler: false);
        var config = new DaytonaConfig
        {
            ApiUrl = "http://daytona.local/api",
            ApiKey = "test-daytona-key",
            Target = "us",
            Snapshot = "daytonaio/sandbox:0.5.0-slim",
            Workdir = "/workspace",
        };
        return new DaytonaSandboxProvider(
            client,
            config,
            NullLogger<DaytonaSandboxProvider>.Instance);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _response;

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> response)
        {
            _response = response;
        }

        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            return _response(request);
        }
    }
}
