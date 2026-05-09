namespace OffceOs.Application.Features.Agents;

/// <summary>
/// Detects repetitive tool call patterns to prevent infinite loops.
/// Ported from agent-core's LoopDetector.
///
/// Patterns detected:
/// 1. Exact repeat: same tool + args 3+ consecutive times (3=warn, 4=block, 5+=break)
/// 2. Ping-pong: two tools alternating A→B→A→B for 4+ cycles
/// 3. No progress: same tool 5+ times with identical output hash
/// </summary>
internal sealed class LoopDetector
{
    private const int WindowSize = 20;
    private readonly List<ToolCallRecord> _window = new();

    public LoopDetectionResult Record(string toolName, string args, string output)
    {
        var outputHash = ComputeHash(output);
        _window.Add(new ToolCallRecord(toolName, args, outputHash));
        if (_window.Count > WindowSize)
            _window.RemoveAt(0);

        // Pattern 1: Exact repeat (most severe)
        var exactRepeatResult = CheckExactRepeat();
        if (exactRepeatResult != LoopDetectionResult.Ok)
            return exactRepeatResult;

        // Pattern 2: Ping-pong
        var pingPongResult = CheckPingPong();
        if (pingPongResult != LoopDetectionResult.Ok)
            return pingPongResult;

        // Pattern 3: No progress
        return CheckNoProgress();
    }

    private LoopDetectionResult CheckExactRepeat()
    {
        if (_window.Count < 3) return LoopDetectionResult.Ok;

        var last = _window[^1];
        var consecutiveCount = 1;
        for (var i = _window.Count - 2; i >= 0; i--)
        {
            if (_window[i].ToolName == last.ToolName && _window[i].Args == last.Args)
                consecutiveCount++;
            else
                break;
        }

        return consecutiveCount switch
        {
            >= 5 => LoopDetectionResult.Break($"Tool '{last.ToolName}' called {consecutiveCount} times with identical arguments — terminating turn."),
            4 => LoopDetectionResult.Block($"Tool '{last.ToolName}' called 4 times with identical arguments — call blocked."),
            3 => LoopDetectionResult.Warning($"Tool '{last.ToolName}' called 3 times with identical arguments — try a different approach."),
            _ => LoopDetectionResult.Ok,
        };
    }

    private LoopDetectionResult CheckPingPong()
    {
        if (_window.Count < 8) return LoopDetectionResult.Ok;

        var a = _window[^2].ToolName;
        var b = _window[^1].ToolName;
        if (a == b) return LoopDetectionResult.Ok;

        var cycles = 0;
        for (var i = _window.Count - 1; i >= 1; i -= 2)
        {
            if (_window[i].ToolName == b && _window[i - 1].ToolName == a)
                cycles++;
            else
                break;
        }

        return cycles >= 4
            ? LoopDetectionResult.Warning($"Ping-pong detected: '{a}' and '{b}' alternating for {cycles} cycles.")
            : LoopDetectionResult.Ok;
    }

    private LoopDetectionResult CheckNoProgress()
    {
        if (_window.Count < 5) return LoopDetectionResult.Ok;

        var last = _window[^1];
        var sameToolSameOutput = 0;
        for (var i = _window.Count - 2; i >= 0; i--)
        {
            if (_window[i].ToolName == last.ToolName && _window[i].OutputHash == last.OutputHash)
                sameToolSameOutput++;
            else
                break;
        }

        return sameToolSameOutput >= 4
            ? LoopDetectionResult.Warning($"Tool '{last.ToolName}' called {sameToolSameOutput + 1} times with different args but identical output — no progress.")
            : LoopDetectionResult.Ok;
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16]; // First 8 bytes as hex
    }

    private record ToolCallRecord(string ToolName, string Args, string OutputHash);
}

/// <summary>Result of loop detection analysis.</summary>
public abstract record LoopDetectionResult
{
    public static readonly LoopDetectionResult Ok = new OkResult();
    public static LoopDetectionResult Warning(string message) => new WarningResult(message);
    public static LoopDetectionResult Block(string message) => new BlockResult(message);
    public static LoopDetectionResult Break(string message) => new BreakResult(message);

    public sealed record OkResult : LoopDetectionResult;
    public sealed record WarningResult(string Message) : LoopDetectionResult;
    public sealed record BlockResult(string Message) : LoopDetectionResult;
    public sealed record BreakResult(string Message) : LoopDetectionResult;
}
