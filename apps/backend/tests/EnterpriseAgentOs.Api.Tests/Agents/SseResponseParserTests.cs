using System.Net;
using System.Text;
using EnterpriseAgentOs.Application.Features.Agents;
using Xunit;

namespace EnterpriseAgentOs.Api.Tests.Agents;

public sealed class SseResponseParserTests
{
    [Fact]
    public async Task ParseAsync_ignores_null_usage_and_delta_chunks()
    {
        var parser = new SseResponseParser();
        using var response = SseResponse("""
            data: {"choices":[{"index":0,"delta":{"content":"Hello"}}],"usage":null}

            data: {"choices":[{"index":0,"delta":null,"finish_reason":"stop"}],"usage":null}

            data: {"choices":[],"usage":{"prompt_tokens":12,"completion_tokens":4}}

            data: [DONE]

            """);

        var result = await parser.ParseAsync(response, CancellationToken.None);

        Assert.Equal("Hello", result.Content);
        Assert.Empty(result.ToolCalls);
        Assert.Equal(12, result.InputTokens);
        Assert.Equal(4, result.OutputTokens);
    }

    [Fact]
    public async Task ParseAsync_assembles_streamed_tool_calls_with_null_usage_chunks()
    {
        var parser = new SseResponseParser();
        using var response = SseResponse("""
            data: {"choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"id":"call_123","function":{"name":"google_docs_create","arguments":""}}]}}],"usage":null}

            data: {"choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"function":{"arguments":"{\"title\""}}]}}],"usage":null}

            data: {"choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"function":{"arguments":":\"Poem\"}"}}]}}],"usage":null}

            data: {"choices":[{"index":0,"delta":null,"finish_reason":"tool_calls"}],"usage":null}

            data: {"choices":[],"usage":{"prompt_tokens":21,"completion_tokens":9}}

            data: [DONE]

            """);

        var result = await parser.ParseAsync(response, CancellationToken.None);

        var toolCall = Assert.Single(result.ToolCalls);
        Assert.Equal("call_123", toolCall.Id);
        Assert.Equal("google_docs_create", toolCall.Name);
        Assert.Equal("""{"title":"Poem"}""", toolCall.Arguments);
        Assert.Equal(21, result.InputTokens);
        Assert.Equal(9, result.OutputTokens);
    }

    private static HttpResponseMessage SseResponse(string content) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(content, Encoding.UTF8, "text/event-stream"),
    };
}
