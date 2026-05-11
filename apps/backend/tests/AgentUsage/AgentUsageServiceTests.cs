using System.Text.Json;
using OffceOs.Application.Features.AgentUsage;
using OffceOs.Domain.Features.AgentUsage;
using Xunit;

namespace OffceOs.Tests.AgentUsage;

public sealed class AgentUsageServiceTests
{
    [Fact]
    public void Resolve_prefers_reported_tokens_and_breaks_down_context_parts()
    {
        var requestBody = JsonSerializer.SerializeToElement(new
        {
            model = "gpt-4o-mini",
            messages = new object[]
            {
                new { role = "system", content = "You are a coding agent." },
                new { role = "user", content = "Implement the feature." },
                new { role = "tool", content = "file contents here" },
            },
            tools = new object[]
            {
                new { type = "function", function = new { name = "file_read", description = "Read a file" } },
            },
            stream = true,
        });
        var service = new AgentUsageService(new NullAgentUsageRepository());

        var result = service.Resolve(new AgentUsageResolveRequest(
            requestBody,
            "Done",
            [new AgentUsageToolCallRequest("file_edit", """{"path":"src/App.cs"}""")],
            120,
            30,
            CacheReadTokens: 80,
            ReasoningTokens: 4));

        Assert.Equal(120, result.InputTokens);
        Assert.Equal(30, result.OutputTokens);
        Assert.Equal(80, result.CacheReadTokens);
        Assert.Equal(4, result.ReasoningTokens);
        Assert.False(result.EstimatedTokens);
        Assert.Equal(AgentUsageActivityKinds.FeatureDevelopment, result.Activity);
        Assert.Contains(result.ContextParts, p => p.Kind == AgentUsageContextPartKinds.SystemPrompt);
        Assert.Contains(result.ContextParts, p => p.Kind == AgentUsageContextPartKinds.ToolSchema && p.Tool == "file_read");
        Assert.Contains(result.ContextParts, p => p.Kind == AgentUsageContextPartKinds.ToolResult);
        Assert.True(result.ContextParts.Sum(p => p.Tokens) >= 120);
    }

    private sealed class NullAgentUsageRepository : IAgentUsageRepository
    {
        public IQueryable<AgentUsageCallRecord> Query(AgentUsageFilter filter) => Enumerable.Empty<AgentUsageCallRecord>().AsQueryable();
        public Task<List<AgentUsageCallRecord>> ListAsync(AgentUsageFilter filter, CancellationToken ct = default) => Task.FromResult(new List<AgentUsageCallRecord>());
        public Task<AgentUsageCallRecord> SaveAsync(AgentUsageCallRecord record, CancellationToken ct = default) => Task.FromResult(record);
    }
}
