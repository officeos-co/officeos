namespace EnterpriseAgentOs.Infrastructure.Channels.Common;

/// <summary>
/// Splits text into platform-safe chunks respecting message size limits.
/// Splits on paragraph → newline → space → hard cut boundaries.
/// </summary>
public static class PlatformTextChunker
{
    public static readonly Dictionary<string, int> PlatformLimits = new(StringComparer.OrdinalIgnoreCase)
    {
        ["slack"] = 4000,
        ["discord"] = 2000,
        ["telegram"] = 4096,
        ["teams"] = 28000,
        ["google-chat"] = 4096,
        ["whatsapp"] = 4000,
    };

    public static IReadOnlyList<string> Chunk(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return new[] { text };

        var chunks = new List<string>();
        var remaining = text.AsSpan();

        while (remaining.Length > 0)
        {
            if (remaining.Length <= maxLength)
            {
                chunks.Add(remaining.ToString());
                break;
            }

            var cutPoint = FindCutPoint(remaining, maxLength);
            chunks.Add(remaining[..cutPoint].ToString().TrimEnd());
            remaining = remaining[cutPoint..].TrimStart();
        }

        return chunks;
    }

    public static IReadOnlyList<string> ChunkForPlatform(string text, string platform)
    {
        var limit = PlatformLimits.GetValueOrDefault(platform, 4000);
        return Chunk(text, limit);
    }

    private static int FindCutPoint(ReadOnlySpan<char> text, int maxLength)
    {
        var searchRegion = text[..maxLength];

        // Try paragraph boundary
        var idx = searchRegion.LastIndexOf("\n\n");
        if (idx > maxLength / 4) return idx + 2;

        // Try newline boundary
        idx = searchRegion.LastIndexOf('\n');
        if (idx > maxLength / 4) return idx + 1;

        // Try space boundary
        idx = searchRegion.LastIndexOf(' ');
        if (idx > maxLength / 4) return idx + 1;

        // Hard cut
        return maxLength;
    }
}
