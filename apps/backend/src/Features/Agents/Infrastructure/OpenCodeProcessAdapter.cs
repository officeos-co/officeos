namespace OffceOs.Infrastructure.Features.Agents;

internal sealed class OpenCodeProcessAdapter : IOpenCodeProcessService
{
    public async Task<ProcessRunResult> RunAsync(
        ProcessRunRequest request,
        Func<string, CancellationToken, Task> onStdoutLine,
        Func<string, CancellationToken, Task> onStderrLine,
        CancellationToken ct = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = request.FileName,
            WorkingDirectory = request.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in request.Arguments)
            startInfo.ArgumentList.Add(argument);

        foreach (var (key, value) in request.Environment)
            startInfo.Environment[key] = value;

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stderr = new StringBuilder();
        var stdoutTask = ReadLinesAsync(process.StandardOutput, onStdoutLine, null, ct);
        var stderrTask = ReadLinesAsync(process.StandardError, onStderrLine, stderr, ct);
        await Task.WhenAll(stdoutTask, stderrTask);
        await process.WaitForExitAsync(ct);
        return new ProcessRunResult(process.ExitCode, stderr.ToString());
    }

    private static async Task ReadLinesAsync(
        StreamReader reader,
        Func<string, CancellationToken, Task> onLine,
        StringBuilder? capture,
        CancellationToken ct)
    {
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) continue;
            capture?.AppendLine(line);
            await onLine(line, ct);
        }
    }
}
