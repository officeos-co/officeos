namespace OffceOs.Application.Features.Agents;

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
        var toolCalls = new Dictionary<int, ToolCallAccumulator>();
        int? inputTokens = null;
        int? outputTokens = null;
        int? cacheReadTokens = null;
        int? cacheWriteTokens = null;
        int? reasoningTokens = null;

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

                if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
                {
                    if (usage.TryGetProperty("prompt_tokens", out var promptTokens))
                        inputTokens = promptTokens.GetInt32();
                    if (usage.TryGetProperty("completion_tokens", out var completionTokens))
                        outputTokens = completionTokens.GetInt32();
                    cacheReadTokens = ReadFirstInt(usage, "cache_read_input_tokens", "cached_tokens") ?? cacheReadTokens;
                    cacheWriteTokens = ReadFirstInt(usage, "cache_creation_input_tokens", "cache_write_input_tokens") ?? cacheWriteTokens;
                    reasoningTokens = ReadFirstInt(usage, "reasoning_tokens") ?? reasoningTokens;

                    if (usage.TryGetProperty("prompt_tokens_details", out var promptDetails) &&
                        promptDetails.ValueKind == JsonValueKind.Object)
                    {
                        cacheReadTokens = ReadFirstInt(promptDetails, "cached_tokens", "cache_read_tokens") ?? cacheReadTokens;
                    }

                    if (usage.TryGetProperty("completion_tokens_details", out var completionDetails) &&
                        completionDetails.ValueKind == JsonValueKind.Object)
                    {
                        reasoningTokens = ReadFirstInt(completionDetails, "reasoning_tokens") ?? reasoningTokens;
                    }
                }

                if (!root.TryGetProperty("choices", out var choices) ||
                    choices.ValueKind != JsonValueKind.Array ||
                    choices.GetArrayLength() == 0)
                    continue;

                var firstChoice = choices[0];
                if (firstChoice.ValueKind != JsonValueKind.Object ||
                    !firstChoice.TryGetProperty("delta", out var delta) ||
                    delta.ValueKind != JsonValueKind.Object)
                    continue;

                if (delta.TryGetProperty("content", out var text) && text.ValueKind == JsonValueKind.String)
                    content.Append(text.GetString());

                if (delta.TryGetProperty("tool_calls", out var streamedToolCalls) &&
                    streamedToolCalls.ValueKind == JsonValueKind.Array)
                {
                    foreach (var toolCall in streamedToolCalls.EnumerateArray())
                    {
                        if (toolCall.ValueKind != JsonValueKind.Object ||
                            !toolCall.TryGetProperty("index", out var indexElement) ||
                            indexElement.ValueKind != JsonValueKind.Number ||
                            !indexElement.TryGetInt32(out var index))
                            continue;

                        if (!toolCalls.TryGetValue(index, out var accumulator))
                        {
                            accumulator = new ToolCallAccumulator();
                            toolCalls[index] = accumulator;
                        }

                        if (toolCall.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                        {
                            accumulator.Id = id.GetString() ?? "";
                        }

                        if (toolCall.TryGetProperty("function", out var function) &&
                            function.ValueKind == JsonValueKind.Object)
                        {
                            if (function.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                                accumulator.Name = name.GetString() ?? "";

                            if (function.TryGetProperty("arguments", out var arguments) &&
                                arguments.ValueKind == JsonValueKind.String)
                                accumulator.Args.Append(arguments.GetString());
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
            outputTokens,
            cacheReadTokens,
            cacheWriteTokens,
            reasoningTokens);
    }

    private static int? ReadFirstInt(JsonElement element, params string[] properties)
    {
        foreach (var property in properties)
        {
            if (element.TryGetProperty(property, out var value) &&
                value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt32(out var result))
            {
                return result;
            }
        }

        return null;
    }

    private sealed class ToolCallAccumulator
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public StringBuilder Args { get; } = new();
    }
}
