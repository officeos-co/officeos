namespace EnterpriseAgentOs.Application.Features.Agents;

/// <summary>
/// Parses streamed OpenAI-compatible server-sent events into assistant output and tool calls.
/// </summary>
/// <remarks>
/// <para><strong>Responsible for:</strong> reading SSE lines, collecting assistant text, assembling
/// streamed tool-call arguments, and extracting provider-reported token usage.</para>
/// <para><strong>Responsible only for:</strong> stream parsing. It does not dispatch providers, estimate
/// usage, record billing, publish events, or execute tools.</para>
/// <para><strong>Acceptance criteria:</strong> this class should change only when provider stream format,
/// OpenAI-compatible SSE parsing, or usage field extraction changes.</para>
/// </remarks>
internal sealed class SseResponseParser
{
    public async Task<SseResult> ParseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var content = new StringBuilder();
        var toolCalls = new Dictionary<int, (string Id, string Name, StringBuilder Args)>();
        int? inputTokens = null;
        int? outputTokens = null;

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (!line.StartsWith("data: ", StringComparison.Ordinal)) continue;
            var data = line[6..];
            if (data == "[DONE]") break;

            try
            {
                using var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;

                if (root.TryGetProperty("usage", out var usage))
                {
                    if (usage.TryGetProperty("prompt_tokens", out var promptTokens))
                        inputTokens = promptTokens.GetInt32();
                    if (usage.TryGetProperty("completion_tokens", out var completionTokens))
                        outputTokens = completionTokens.GetInt32();
                }

                if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                    continue;
                var delta = choices[0].GetProperty("delta");

                if (delta.TryGetProperty("content", out var text) && text.ValueKind == JsonValueKind.String)
                    content.Append(text.GetString());

                if (delta.TryGetProperty("tool_calls", out var streamedToolCalls))
                {
                    foreach (var toolCall in streamedToolCalls.EnumerateArray())
                    {
                        var index = toolCall.GetProperty("index").GetInt32();
                        if (!toolCalls.ContainsKey(index))
                        {
                            var id = toolCall.GetProperty("id").GetString() ?? "";
                            var name = toolCall.GetProperty("function").GetProperty("name").GetString() ?? "";
                            toolCalls[index] = (id, name, new StringBuilder());
                        }

                        if (toolCall.TryGetProperty("function", out var function) &&
                            function.TryGetProperty("arguments", out var arguments) &&
                            arguments.ValueKind == JsonValueKind.String)
                        {
                            toolCalls[index].Args.Append(arguments.GetString());
                        }
                    }
                }
            }
            catch (JsonException) { }
        }

        return new SseResult(
            content.Length > 0 ? content.ToString() : null,
            toolCalls.Values.Select(tc => new ParsedToolCall(tc.Id, tc.Name, tc.Args.ToString())).ToList(),
            inputTokens,
            outputTokens);
    }
}
