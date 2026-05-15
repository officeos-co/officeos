using OffceOs.Application.Features.Agents;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Observability;
using OffceOs.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace OffceOs.Tests.Agents;

public sealed class OpenCodeAgentWorkServiceTests
{
    [Fact]
    public async Task ExecuteQueuedWorkAsync_invokes_opencode_with_prompt_and_working_directory()
    {
        var agentId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var work = Work(agentId, workspaceId, "Answer this bootstrap message.");
        var logs = new FakeAgentLogService();
        await logs.AppendAsync(work);
        var process = new RecordingOpenCodeProcessService();
        var service = new OpenCodeAgentWorkService(
            new FakeAgentRepository(new AgentRecord
            {
                Id = agentId,
                WorkspaceId = workspaceId,
                Name = "engineering-agent",
                Provider = "openai",
                Model = "gpt-4o-mini",
                Status = AgentStatus.Idle,
            }),
            logs,
            process,
            NullLogger<OpenCodeAgentWorkService>.Instance);

        await service.ExecuteQueuedWorkAsync(work);

        Assert.NotNull(process.Request);
        Assert.Equal("opencode", process.Request.FileName);
        Assert.DoesNotContain("--dir", process.Request.Arguments);
        Assert.Contains("--format", process.Request.Arguments);
        Assert.Contains("--print-logs", process.Request.Arguments);
        Assert.Contains("--log-level", process.Request.Arguments);
        Assert.Equal("WARN", process.Request.Arguments[Array.IndexOf(process.Request.Arguments.ToArray(), "--log-level") + 1]);
        Assert.Contains("--model", process.Request.Arguments);
        Assert.DoesNotContain("--agent", process.Request.Arguments);
        Assert.Contains("openai/gpt-5.2", process.Request.Arguments);
        Assert.Equal("Answer this bootstrap message.", process.Request.Arguments[^1]);
        Assert.Equal(process.Request.WorkingDirectory, process.Request.WorkingDirectory.Trim());
    }

    [Fact]
    public async Task ExecuteQueuedWorkAsync_records_opencode_stdout_and_stderr_lines()
    {
        var agentId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var work = Work(agentId, workspaceId, "Inspect repository.");
        var logs = new FakeAgentLogService();
        await logs.AppendAsync(work);
        var process = new RecordingOpenCodeProcessService
        {
            StdoutLines =
            [
                """{"type":"content","content":"Done."}""",
                """{"type":"message.part.updated","properties":{"part":{"id":"part-1","type":"tool","tool":"bash","state":{"status":"completed","input":{"command":"pwd"},"output":"/tmp"}}}}""",
            ],
            StderrLines =
            [
                """DEBUG 2026-05-14T21:02:14 service=bus type=message.part.updated publishing""",
            ],
        };
        var service = new OpenCodeAgentWorkService(
            new FakeAgentRepository(new AgentRecord
            {
                Id = agentId,
                WorkspaceId = workspaceId,
                Name = "engineering-agent",
                Provider = "openai",
                Model = "gpt-4o-mini",
                Status = AgentStatus.Idle,
            }),
            logs,
            process,
            NullLogger<OpenCodeAgentWorkService>.Instance);

        await service.ExecuteQueuedWorkAsync(work);

        Assert.Contains(logs.Records, log => log.Type == AgentLogType.MessageOut && log.Content == "Done.");
        Assert.Contains(logs.Records, log => log.Type == AgentLogType.ToolResult && log.Tool == "bash" && log.Content == "/tmp");
        Assert.Contains(logs.Records, log => log.Type == AgentLogType.System && log.Content.Contains("message.part.updated"));
    }

    [Fact]
    public async Task ExecuteQueuedWorkAsync_sends_bootstrap_prompt_and_logs_it()
    {
        var agentId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        const string BootstrapPrompt = "Bootstrap this agent with the configured instructions.";
        var work = Work(agentId, workspaceId, BootstrapPrompt, AgentWorkPurposeKinds.Bootstrap);
        var logs = new FakeAgentLogService();
        await logs.AppendAsync(work);
        var process = new RecordingOpenCodeProcessService();
        var service = new OpenCodeAgentWorkService(
            new FakeAgentRepository(new AgentRecord
            {
                Id = agentId,
                WorkspaceId = workspaceId,
                Name = "engineering-agent",
                Provider = "openai",
                Model = "gpt-4o-mini",
                Status = AgentStatus.Idle,
            }),
            logs,
            process,
            NullLogger<OpenCodeAgentWorkService>.Instance);

        await service.ExecuteQueuedWorkAsync(work);

        Assert.NotNull(process.Request);
        Assert.Contains("run", process.Request.Arguments);
        Assert.Contains("--format", process.Request.Arguments);
        Assert.Equal(BootstrapPrompt, process.Request.Arguments[^1]);
        Assert.Contains(logs.Records, log => log.Id == work.Id && log.WorkStatus == AgentWorkStatusKinds.Completed);
        Assert.Contains(logs.Records, log => log.Type == AgentLogType.MessageIn && log.Content == $"Bootstrap prompt: {BootstrapPrompt}");
    }

    private static AgentLogRecord Work(
        Guid agentId,
        Guid workspaceId,
        string content,
        string purpose = AgentWorkPurposeKinds.Manual) => new()
    {
        Id = Guid.NewGuid(),
        AgentId = agentId,
        WorkspaceId = workspaceId,
        Type = AgentLogType.MessageIn,
        WorkStatus = AgentWorkStatusKinds.Running,
        WorkPurpose = purpose,
        Content = content,
        CorrelationId = Guid.NewGuid().ToString("N"),
    };

    private sealed class RecordingOpenCodeProcessService : IOpenCodeProcessService
    {
        public ProcessRunRequest? Request { get; private set; }
        public IReadOnlyList<string> StdoutLines { get; init; } = [];
        public IReadOnlyList<string> StderrLines { get; init; } = [];

        public Task<ProcessRunResult> RunAsync(
            ProcessRunRequest request,
            Func<string, CancellationToken, Task> onStdoutLine,
            Func<string, CancellationToken, Task> onStderrLine,
            CancellationToken ct = default)
        {
            Request = request;
            foreach (var line in StdoutLines)
                onStdoutLine(line, ct).GetAwaiter().GetResult();
            foreach (var line in StderrLines)
                onStderrLine(line, ct).GetAwaiter().GetResult();
            return Task.FromResult(new ProcessRunResult(0, string.Empty));
        }
    }

}
