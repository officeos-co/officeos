using OffceOs.Application.Features.Agents;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Agents;
using OffceOs.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace OffceOs.Tests.Agents;

public sealed class OpenCodeRunServiceTests
{
    [Fact]
    public async Task ExecuteQueuedRunAsync_invokes_opencode_with_prompt_and_working_directory()
    {
        var agentId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var run = new AgentRunRecord
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            WorkspaceId = workspaceId,
            Kind = "opencode",
            Status = "queued",
            Prompt = "Answer this bootstrap message.",
        };
        var process = new RecordingOpenCodeProcessService();
        var service = new OpenCodeRunService(
            new FakeAgentRepository(new AgentRecord
            {
                Id = agentId,
                WorkspaceId = workspaceId,
                Name = "engineering-agent",
                Provider = "openai",
                Model = "gpt-4o-mini",
                Status = AgentStatus.Idle,
            }),
            new RecordingRunRepository(run),
            new FakeAgentLogService(),
            process,
            NullLogger<OpenCodeRunService>.Instance);

        await service.ExecuteQueuedRunAsync(run);

        Assert.NotNull(process.Request);
        Assert.Equal("opencode", process.Request.FileName);
        Assert.DoesNotContain("--dir", process.Request.Arguments);
        Assert.Contains("--format", process.Request.Arguments);
        Assert.Contains("--model", process.Request.Arguments);
        Assert.Contains("--agent", process.Request.Arguments);
        Assert.Equal("Answer this bootstrap message.", process.Request.Arguments[^1]);
        Assert.Equal(process.Request.WorkingDirectory, process.Request.WorkingDirectory.Trim());
    }

    private sealed class RecordingOpenCodeProcessService : IOpenCodeProcessService
    {
        public ProcessRunRequest? Request { get; private set; }

        public Task<ProcessRunResult> RunAsync(
            ProcessRunRequest request,
            Func<string, CancellationToken, Task> onStdoutLine,
            CancellationToken ct = default)
        {
            Request = request;
            return Task.FromResult(new ProcessRunResult(0, string.Empty));
        }
    }

    private sealed class RecordingRunRepository : IAgentRunRepository
    {
        private readonly AgentRunRecord _run;

        public RecordingRunRepository(AgentRunRecord run) => _run = run;

        public Task<AgentRunRecord> CreateAsync(AgentRunRecord run, CancellationToken ct = default) =>
            Task.FromResult(run);

        public Task<AgentRunRecord?> GetByAsync(AgentRunFilter filter, CancellationToken ct = default) =>
            Task.FromResult<AgentRunRecord?>(_run);

        public Task<IReadOnlyList<AgentRunRecord>> ListAsync(AgentRunFilter filter, int limit = 100, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentRunRecord>>([_run]);

        public Task<IReadOnlyList<AgentRunRecord>> ListForAgentAsync(Guid agentId, Guid? parentRunId = null, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentRunRecord>>([_run]);

        public Task UpdateAsync(AgentRunRecord run, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
