using System.Text.RegularExpressions;

namespace EnterpriseAgentOs.Application.Features.Agents;

internal static partial class ToolShell
{
    public static string Escape(string s) => "'" + s.Replace("'", "'\\''") + "'";
    public static string Base64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));

    [GeneratedRegex(@"(^|[;&|]\s*)(sudo\s+)?(rm\s+(-[^\s]*r[^\s]*f|-[^\s]*f[^\s]*r)|mkfs|dd\s+if=|:\(\)\s*\{|\bshutdown\b|\breboot\b|\bdrop\s+table\b)", RegexOptions.IgnoreCase)]
    public static partial Regex DestructiveCommandRegex();
}
