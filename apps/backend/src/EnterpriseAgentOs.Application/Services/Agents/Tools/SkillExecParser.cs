namespace EnterpriseAgentOs.Application.Services.Agents.Tools;

/// <summary>
/// Parses CLI-like skill_exec commands into (skill, action, args).
/// Handles quoted values: --arg "value with spaces" and --arg 'value'.
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
            if (tokens[i].StartsWith("--") && i + 1 < tokens.Count)
            {
                var key = tokens[i][2..];
                var value = tokens[++i];
                if (int.TryParse(value, out var intVal))
                    args[key] = intVal;
                else if (bool.TryParse(value, out var boolVal))
                    args[key] = boolVal;
                else
                    args[key] = value;
            }
        }

        return (tokens[0], tokens[1], args);
    }

    /// <summary>
    /// Shell-like tokenizer: splits on whitespace, respects double and single quotes.
    /// Strips outer quotes from tokens.
    /// </summary>
    private static List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        char quote = '\0';

        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];

            if (quote != '\0')
            {
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
                quote = c; // open quote
            }
            else if (char.IsWhiteSpace(c))
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return tokens;
    }
}
