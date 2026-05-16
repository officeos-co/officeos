using OffceOs.Common.Domain.Primitives;

namespace OffceOs.Features.AgentHarness.Application.Tools;

/// <summary>Execute a shell command in the agent's OS.</summary>
internal sealed class ShellTool : IAgentTool
{
    private readonly ToolExecutionContext _toolExecutionContext;
    public ShellTool(ToolExecutionContext context) => _toolExecutionContext = context;

    public string Name => "shell";
    public AgentToolKind Kind => AgentToolKind.Execute;
    public ToolSchema Schema => new("shell",
        "Execute a shell command in the agent operating system. Include a short description of why the command is being run. Destructive commands require explicit user instruction.",
        new
        {
            type = "object",
            properties = new
            {
                command = new { type = "string", description = "The shell command to execute" },
                description = new { type = "string", description = "Brief reason for running this command" },
                timeout_secs = new { type = "integer", description = "Timeout in seconds (default 60, max 300)" }
            },
            required = new[] { "command" }
        });

    public Task<ToolValidationResult> ValidateAsync(JsonElement args, CancellationToken ct = default)
    {
        var command = args.TryGetProperty("command", out var c) ? c.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(command))
            return Task.FromResult(ToolValidationResult.Invalid("shell command is required."));

        if (ToolShell.DestructiveCommandRegex().IsMatch(command))
            return Task.FromResult(ToolValidationResult.Invalid("Potentially destructive shell command blocked. Ask the user for explicit confirmation before running it."));

        return Task.FromResult(ToolValidationResult.Valid);
    }

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var command = args.GetProperty("command").GetString() ?? "";
        var timeoutSecs = args.TryGetProperty("timeout_secs", out var t) ? Math.Clamp(t.GetInt32(), 1, 300) : 60;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSecs));

        var execResult = await _toolExecutionContext.Sandbox.ExecuteAsync(_toolExecutionContext.SandboxId, _toolExecutionContext.ServiceUrl, command, TimeSpan.FromSeconds(timeoutSecs), cts.Token);
        if (execResult.IsFailure)
            return new AgentError(AgentErrorCategory.ToolExecution, $"shell: {execResult.Error.Message}", execResult.Error.Detail);

        var (output, exitCode) = execResult.Value;
        return exitCode == 0
            ? new ToolResult(true, output)
            : new ToolResult(false, output, $"exit code {exitCode}");
    }
}
