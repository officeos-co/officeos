namespace EnterpriseAgentOs.Application.Services.Agents.Tools;

/// <summary>
/// Parses CLI-like skill_exec commands into (skill, action, args).
/// Handles quoted values, escaped quotes, --key=value syntax.
/// </summary>
public static class SkillExecParser
{
    public static (string Skill, string Action, Dictionary<string, object> Args)? Parse(string command)
    {
        var tokens = Tokenize(command);
        if (tokens.Count < 2) return null;

        var args = new Dictionary<string, object>();
        for (var i = 2; i < tokens.Count; i++)
        {
            if (tokens[i].StartsWith("--") && tokens[i].Length > 2)
            {
                var keyPart = tokens[i][2..];

                // Handle --key=value syntax
                var eqIdx = keyPart.IndexOf('=');
                if (eqIdx >= 0)
                {
                    var key = keyPart[..eqIdx];
                    var val = keyPart[(eqIdx + 1)..];
                    args[key] = CoerceValue(val);
                    continue;
                }

                if (i + 1 < tokens.Count)
                {
                    args[keyPart] = CoerceValue(tokens[++i]);
                }
                // else: flag at end with no value — skip (don't crash)
            }
        }

        return (tokens[0], tokens[1], args);
    }

    private static object CoerceValue(string value)
    {
        if (int.TryParse(value, out var intVal)) return intVal;
        if (bool.TryParse(value, out var boolVal)) return boolVal;
        return value;
    }

    /// <summary>
    /// Shell-like tokenizer: splits on whitespace, respects double and single quotes,
    /// handles backslash escapes inside quotes, preserves empty quoted strings.
    /// </summary>
    private static List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        char quote = '\0';
        var hasContent = false; // tracks whether we've seen quote or char content for this token

        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];

            if (quote != '\0')
            {
                // Inside a quoted region
                if (c == '\\' && i + 1 < input.Length)
                {
                    var next = input[i + 1];
                    if (next == quote || next == '\\')
                    {
                        current.Append(next);
                        i++; // skip escaped char
                        continue;
                    }
                }
                if (c == quote)
                {
                    quote = '\0'; // close quote
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c is '"' or '\'')
            {
                quote = c;
                hasContent = true; // an empty "" should still produce a token
            }
            else if (char.IsWhiteSpace(c))
            {
                if (current.Length > 0 || hasContent)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                    hasContent = false;
                }
            }
            else
            {
                current.Append(c);
                hasContent = true;
            }
        }

        if (current.Length > 0 || hasContent)
            tokens.Add(current.ToString());

        return tokens;
    }
}
