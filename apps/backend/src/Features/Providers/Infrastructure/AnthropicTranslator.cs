namespace OffceOs.Features.Providers.Infrastructure;

/// <summary>
/// Translates between OpenAI chat-completions format and Anthropic Messages
/// API format. Handles both request translation (OpenAI → Anthropic) and
/// streaming response translation (Anthropic SSE → OpenAI SSE).
/// </summary>
public static class AnthropicTranslator
{
    /// <summary>
    /// Convert an OpenAI-format request body to an Anthropic Messages API body.
/// Preserves <c>cache_control</c> annotations in the request body:
    /// system messages with array content are passed as-is to the Anthropic <c>system</c> field,
    /// and tool-level <c>cache_control</c> is preserved in translated tool definitions.
    /// </summary>
    public static string TranslateRequest(JsonElement openAiBody, string model)
    {
        var messages = new List<object>();
        // systemContent holds either a plain string or a pre-built array JsonElement for cache_control
        string? systemPromptString = null;
        JsonElement? systemPromptArray = null;

        if (openAiBody.TryGetProperty("messages", out var msgsEl) && msgsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var msg in msgsEl.EnumerateArray())
            {
                var role = msg.TryGetProperty("role", out var r) ? r.GetString() : "user";

                if (role == "system")
                {
                    if (msg.TryGetProperty("content", out var c))
                    {
                        if (c.ValueKind == JsonValueKind.Array)
                        {
                            // Array content with potential cache_control — pass through as JsonElement
                            systemPromptArray = c.Clone();
                        }
                        else
                        {
                            systemPromptString = c.GetString();
                        }
                    }
                    continue;
                }

                var contentValue = msg.TryGetProperty("content", out var cv)
                    ? cv.ValueKind == JsonValueKind.String ? cv.GetString() : cv.GetRawText()
                    : "";

                messages.Add(new
                {
                    role = role == "assistant" ? "assistant" : "user",
                    content = contentValue ?? "",
                });
            }
        }

        var maxTokens = 4096;
        if (openAiBody.TryGetProperty("max_tokens", out var mt) && mt.ValueKind == JsonValueKind.Number)
        {
            maxTokens = mt.GetInt32();
        }

        var temperature = 0.7;
        if (openAiBody.TryGetProperty("temperature", out var temp) && temp.ValueKind == JsonValueKind.Number)
        {
            temperature = temp.GetDouble();
        }

        // Build the request using Utf8JsonWriter so we can embed raw JsonElement values
        // (needed to preserve cache_control arrays in system and tools fields).
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        writer.WriteString("model", model);
        writer.WriteNumber("max_tokens", maxTokens);
        writer.WriteBoolean("stream", true);
        writer.WriteNumber("temperature", temperature);

        // system field
        if (systemPromptArray.HasValue)
        {
            // Anthropic accepts a content array in the system field, which preserves cache_control
            writer.WritePropertyName("system");
            systemPromptArray.Value.WriteTo(writer);
        }
        else if (systemPromptString is not null)
        {
            writer.WriteString("system", systemPromptString);
        }

        // messages field
        writer.WritePropertyName("messages");
        writer.WriteStartArray();
        foreach (var m in messages)
        {
            // Serialize each message object via JSON round-trip
            var msgJson = JsonSerializer.Serialize(m);
            using var msgDoc = JsonDocument.Parse(msgJson);
            msgDoc.RootElement.WriteTo(writer);
        }
        writer.WriteEndArray();

        // tools field — translate from OpenAI format to Anthropic format
        // OpenAI: {"type":"function","function":{"name":"x","description":"y","parameters":{...}}}
        // Anthropic: {"name":"x","description":"y","input_schema":{...}}
        if (openAiBody.TryGetProperty("tools", out var toolsEl) && toolsEl.ValueKind == JsonValueKind.Array)
        {
            writer.WritePropertyName("tools");
            writer.WriteStartArray();
            foreach (var tool in toolsEl.EnumerateArray())
            {
                writer.WriteStartObject();

                if (tool.TryGetProperty("function", out var fn))
                {
                    // Translate OpenAI function format to Anthropic format
                    if (fn.TryGetProperty("name", out var nameEl))
                        writer.WriteString("name", nameEl.GetString());
                    if (fn.TryGetProperty("description", out var descEl))
                        writer.WriteString("description", descEl.GetString());
                    if (fn.TryGetProperty("parameters", out var paramsEl))
                    {
                        writer.WritePropertyName("input_schema");
                        paramsEl.WriteTo(writer);
                    }
                }
                else
                {
                    // Already in Anthropic format or unknown — pass through
                    foreach (var prop in tool.EnumerateObject())
                        prop.WriteTo(writer);
                }

                // Preserve cache_control if present at tool level
                if (tool.TryGetProperty("cache_control", out var cc))
                {
                    writer.WritePropertyName("cache_control");
                    cc.WriteTo(writer);
                }

                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Wraps an Anthropic SSE stream into an OpenAI-compatible SSE stream.
    /// Reads Anthropic events and emits OpenAI-format <c>data: {...}</c> lines.
    /// </summary>
    public static Stream TranslateStream(Stream anthropicStream)
    {
        var pipe = new TranslatingStream(anthropicStream);
        return pipe;
    }

    /// <summary>
    /// A readable Stream that reads from the Anthropic SSE source, translates
    /// events to OpenAI format, and buffers them for the consumer.
    /// </summary>
    private sealed class TranslatingStream : Stream
    {
        private readonly StreamReader _streamReader;
        private readonly MemoryStream _memoryStream = new();
        private int? _inputTokens;
        private int? _outputTokens;
        private bool _done;

        public TranslatingStream(Stream source)
        {
            _streamReader = new StreamReader(source, Encoding.UTF8);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) =>
            ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        {
            // If we have buffered output, return that first
            if (_memoryStream.Position < _memoryStream.Length)
            {
                return await _memoryStream.ReadAsync(buffer, offset, count, ct);
            }

            if (_done) return 0;

            // Read lines from Anthropic SSE until we produce an OpenAI event
            while (true)
            {
                var line = await _streamReader.ReadLineAsync(ct);
                if (line is null)
                {
                    return await EmitDoneAsync(buffer, offset, count, ct);
                }

                if (!line.StartsWith("data: ", StringComparison.Ordinal)) continue;
                var data = line.Substring(6);
                if (string.IsNullOrWhiteSpace(data)) continue;

                JsonElement parsed;
                try
                {
                    parsed = JsonDocument.Parse(data).RootElement;
                }
                catch
                {
                    continue;
                }

                var type = parsed.TryGetProperty("type", out var t) ? t.GetString() : null;

                string? openAiEvent = null;

                switch (type)
                {
                    case "message_start":
                    {
                        CaptureUsage(parsed);
                        break;
                    }
                    case "content_block_start":
                    {
                        // Tool use blocks start here with id, name
                        if (parsed.TryGetProperty("index", out var idxEl)
                            && parsed.TryGetProperty("content_block", out var block)
                            && block.TryGetProperty("type", out var blockType)
                            && blockType.GetString() == "tool_use")
                        {
                            var idx = idxEl.GetInt32();
                            var id = block.TryGetProperty("id", out var idProp) ? idProp.GetString() : "";
                            var name = block.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "";
                            openAiEvent = FormatOpenAiToolCallStart(idx, id ?? "", name ?? "");
                        }
                        break;
                    }
                    case "content_block_delta":
                    {
                        if (parsed.TryGetProperty("delta", out var delta))
                        {
                            var deltaType = delta.TryGetProperty("type", out var dt) ? dt.GetString() : null;

                            if (deltaType == "text_delta" || deltaType is null)
                            {
                                var text = delta.TryGetProperty("text", out var txt) ? txt.GetString() : null;
                                if (text is not null)
                                    openAiEvent = FormatOpenAiChunk(text);
                            }
                            else if (deltaType == "input_json_delta")
                            {
                                var json = delta.TryGetProperty("partial_json", out var pj) ? pj.GetString() : null;
                                if (json is not null)
                                {
                                    var idx = parsed.TryGetProperty("index", out var idxEl) ? idxEl.GetInt32() : 0;
                                    openAiEvent = FormatOpenAiToolCallDelta(idx, json);
                                }
                            }
                        }
                        break;
                    }
                    case "message_stop":
                    {
                        return await EmitDoneAsync(buffer, offset, count, ct);
                    }
                    case "message_delta":
                    {
                        CaptureUsage(parsed);
                        break;
                    }
                }

                if (openAiEvent is not null)
                {
                    var eventBytes = Encoding.UTF8.GetBytes(openAiEvent);
                    _memoryStream.SetLength(0);
                    _memoryStream.Position = 0;
                    await _memoryStream.WriteAsync(eventBytes, ct);
                    _memoryStream.Position = 0;
                    return await _memoryStream.ReadAsync(buffer, offset, count, ct);
                }
            }
        }

        private async Task<int> EmitDoneAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        {
            _done = true;
            var doneBytes = Encoding.UTF8.GetBytes(FormatOpenAiUsage() + "data: [DONE]\n\n");
            _memoryStream.SetLength(0);
            _memoryStream.Position = 0;
            await _memoryStream.WriteAsync(doneBytes, ct);
            _memoryStream.Position = 0;
            return await _memoryStream.ReadAsync(buffer, offset, count, ct);
        }

        private void CaptureUsage(JsonElement parsed)
        {
            if (parsed.TryGetProperty("message", out var message)
                && message.TryGetProperty("usage", out var messageUsage))
            {
                CaptureUsageFields(messageUsage);
            }

            if (parsed.TryGetProperty("usage", out var usage))
                CaptureUsageFields(usage);
        }

        private void CaptureUsageFields(JsonElement usage)
        {
            if (usage.TryGetProperty("input_tokens", out var input) && input.ValueKind == JsonValueKind.Number)
                _inputTokens = input.GetInt32();

            if (usage.TryGetProperty("output_tokens", out var output) && output.ValueKind == JsonValueKind.Number)
                _outputTokens = output.GetInt32();
        }

        private string FormatOpenAiUsage()
        {
            if (!_inputTokens.HasValue && !_outputTokens.HasValue) return string.Empty;

            var chunk = new
            {
                choices = Array.Empty<object>(),
                usage = new
                {
                    prompt_tokens = _inputTokens ?? 0,
                    completion_tokens = _outputTokens ?? 0,
                },
            };
            return $"data: {JsonSerializer.Serialize(chunk)}\n\n";
        }

        private static string FormatOpenAiChunk(string content)
        {
            var chunk = new
            {
                choices = new[]
                {
                    new
                    {
                        index = 0,
                        delta = new { content },
                    },
                },
            };
            return $"data: {JsonSerializer.Serialize(chunk)}\n\n";
        }

        private static string FormatOpenAiToolCallStart(int index, string id, string name)
        {
            var chunk = new
            {
                choices = new object[]
                {
                    new
                    {
                        index = 0,
                        delta = new
                        {
                            tool_calls = new object[]
                            {
                                new { index, id, type = "function", function = new { name, arguments = "" } }
                            }
                        },
                    },
                },
            };
            return $"data: {JsonSerializer.Serialize(chunk)}\n\n";
        }

        private static string FormatOpenAiToolCallDelta(int index, string arguments)
        {
            var chunk = new
            {
                choices = new object[]
                {
                    new
                    {
                        index = 0,
                        delta = new
                        {
                            tool_calls = new object[]
                            {
                                new { index, function = new { arguments } }
                            }
                        },
                    },
                },
            };
            return $"data: {JsonSerializer.Serialize(chunk)}\n\n";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _streamReader.Dispose();
                _memoryStream.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
