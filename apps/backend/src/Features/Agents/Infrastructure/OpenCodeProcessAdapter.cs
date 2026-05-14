namespace OffceOs.Infrastructure.Features.Agents;

internal sealed class OpenCodeProcessAdapter : IOpenCodeProcessService
{
    public async Task<ProcessRunResult> RunAsync(ProcessRunRequest request, Func<string, CancellationToken, Task> onStdoutLine, CancellationToken ct = default)
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

        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        while (!process.StandardOutput.EndOfStream)
        {
            var line = await process.StandardOutput.ReadLineAsync(ct);
            if (line is not null)
                await onStdoutLine(line, ct);
        }

        await process.WaitForExitAsync(ct);
        var stderr = await stderrTask;
        return new ProcessRunResult(process.ExitCode, stderr);
    }
}
