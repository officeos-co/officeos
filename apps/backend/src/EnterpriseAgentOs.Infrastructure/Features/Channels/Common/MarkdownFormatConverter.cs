using System.Text.RegularExpressions;

namespace EnterpriseAgentOs.Infrastructure.Features.Channels.Common;

/// <summary>
/// Converts standard Markdown to platform-specific formatting.
/// </summary>
public static partial class MarkdownFormatConverter
{
    public static string Convert(string markdown, string targetPlatform)
    {
        if (string.IsNullOrEmpty(markdown)) return markdown;

        return targetPlatform.ToLowerInvariant() switch
        {
            "slack" => ToSlack(markdown),
            "telegram" => ToTelegramMarkdownV2(markdown),
            "whatsapp" => ToWhatsApp(markdown),
            // Discord, Teams, Google Chat support standard Markdown
            _ => markdown,
        };
    }

    /// <summary>
    /// Slack uses mrkdwn: *bold*, _italic_, ~strike~, `code`, ```pre```, &lt;url|text&gt;
    /// </summary>
    private static string ToSlack(string md)
    {
        // Bold: **text** → *text*
        md = BoldRegex().Replace(md, "*$1*");
        // Italic: *text* (single) or _text_ → _text_
        // Note: after bold conversion, single * is now our bold marker, so handle _italic_ only
        // Strikethrough: ~~text~~ → ~text~
        md = StrikethroughRegex().Replace(md, "~$1~");
        // Links: [text](url) → <url|text>
        md = LinkRegex().Replace(md, "<$2|$1>");
        return md;
    }

    /// <summary>
    /// Telegram MarkdownV2: must escape special chars outside of entities.
    /// Bold: *text*, italic: _text_, strikethrough: ~text~, code: `code`, pre: ```pre```
    /// Special chars that must be escaped: _ * [ ] ( ) ~ ` > # + - = | { } . !
    /// </summary>
    private static string ToTelegramMarkdownV2(string md)
    {
        // First, convert markdown constructs, then escape remaining special chars

        // Protect code blocks and inline code
        var codeBlocks = new List<string>();
        md = CodeBlockRegex().Replace(md, m => { codeBlocks.Add(m.Value); return $"\x00CB{codeBlocks.Count - 1}\x00"; });
        var inlineCodes = new List<string>();
        md = InlineCodeRegex().Replace(md, m => { inlineCodes.Add(m.Value); return $"\x00IC{inlineCodes.Count - 1}\x00"; });

        // Convert bold: **text** → *text* (Telegram uses single *)
        md = BoldRegex().Replace(md, "*$1*");
        // Strikethrough: ~~text~~ → ~text~
        md = StrikethroughRegex().Replace(md, "~$1~");

        // Escape special chars in plain text (not inside formatting markers)
        md = TelegramEscapeRegex().Replace(md, @"\$0");

        // Restore code blocks
        for (int i = 0; i < codeBlocks.Count; i++)
            md = md.Replace($"\x00CB{i}\x00", codeBlocks[i]);
        for (int i = 0; i < inlineCodes.Count; i++)
            md = md.Replace($"\x00IC{i}\x00", inlineCodes[i]);

        return md;
    }

    /// <summary>
    /// WhatsApp uses: *bold*, _italic_, ~strikethrough~, ```monospace```
    /// </summary>
    private static string ToWhatsApp(string md)
    {
        // Bold: **text** → *text*
        md = BoldRegex().Replace(md, "*$1*");
        // Strikethrough: ~~text~~ → ~text~
        md = StrikethroughRegex().Replace(md, "~$1~");
        // Inline code: `text` stays the same in WhatsApp
        // Links: [text](url) → text (url) — WhatsApp auto-links URLs
        md = LinkRegex().Replace(md, "$1 ($2)");
        return md;
    }

    [GeneratedRegex(@"\*\*(.+?)\*\*", RegexOptions.Singleline)]
    private static partial Regex BoldRegex();

    [GeneratedRegex(@"~~(.+?)~~", RegexOptions.Singleline)]
    private static partial Regex StrikethroughRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\(([^)]+)\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"```[\s\S]*?```", RegexOptions.Singleline)]
    private static partial Regex CodeBlockRegex();

    [GeneratedRegex(@"`[^`]+`")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"(?<!\\)([_\[\]()>#+\-=|{}.!])")]
    private static partial Regex TelegramEscapeRegex();
}
