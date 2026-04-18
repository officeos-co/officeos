namespace EnterpriseAgentOs.Infrastructure.Adapters.LlmProviders;

/// <summary>
/// Translates between OpenAI chat-completions format and Anthropic Messages
/// API format. Handles both request translation (OpenAI → Anthropic) and
/// streaming response translation (Anthropic SSE → OpenAI SSE).
/// </summary>
public static class AnthropicTranslator
{
    /// <summary>
    /// Convert an OpenAI-format request body to an Anthropic Messages API body.
    /// Preserves <c>cache_control</c> annotations added by <see cref="PromptCacheInjector"/>:
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
        using var stream = new System.IO.MemoryStream();
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

        // tools field — preserve cache_control if present
        if (openAiBody.TryGetProperty("tools", out var toolsEl) && toolsEl.ValueKind == JsonValueKind.Array)
        {
            writer.WritePropertyName("tools");
            writer.WriteStartArray();
            foreach (var tool in toolsEl.EnumerateArray())
            {
                writer.WriteStartObject();
                foreach (var prop in tool.EnumerateObject())
                {
                    // Preserve all properties including cache_control
                    prop.WriteTo(writer);
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
        private readonly StreamReader _reader;
        private readonly MemoryStream _buffer = new();
        private bool _done;

        public TranslatingStream(Stream source)
        {
            _reader = new StreamReader(source, Encoding.UTF8);
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
            if (_buffer.Position < _buffer.Length)
            {
                return await _buffer.ReadAsync(buffer, offset, count, ct);
            }

            if (_done) return 0;

            // Read lines from Anthropic SSE until we produce an OpenAI event
            while (true)
            {
                var line = await _reader.ReadLineAsync(ct);
                if (line is null)
                {
                    // Stream ended — emit [DONE]
                    _done = true;
                    var doneBytes = Encoding.UTF8.GetBytes("data: [DONE]\n\n");
                    _buffer.SetLength(0);
                    _buffer.Position = 0;
                    await _buffer.WriteAsync(doneBytes, ct);
                    _buffer.Position = 0;
                    return await _buffer.ReadAsync(buffer, offset, count, ct);
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
                    case "content_block_delta":
                    {
                        var text = parsed.TryGetProperty("delta", out var delta)
                            && delta.TryGetProperty("text", out var txt)
                            ? txt.GetString()
                            : null;
                        if (text is not null)
                        {
                            openAiEvent = FormatOpenAiChunk(text);
                        }
                        break;
                    }
                    case "message_stop":
                    {
                        _done = true;
                        var bytes = Encoding.UTF8.GetBytes("data: [DONE]\n\n");
                        _buffer.SetLength(0);
                        _buffer.Position = 0;
                        await _buffer.WriteAsync(bytes, ct);
                        _buffer.Position = 0;
                        return await _buffer.ReadAsync(buffer, offset, count, ct);
                    }
                    case "message_delta":
                    {
                        // Contains stop_reason — we ignore it, message_stop follows
                        break;
                    }
                }

                if (openAiEvent is not null)
                {
                    var eventBytes = Encoding.UTF8.GetBytes(openAiEvent);
                    _buffer.SetLength(0);
                    _buffer.Position = 0;
                    await _buffer.WriteAsync(eventBytes, ct);
                    _buffer.Position = 0;
                    return await _buffer.ReadAsync(buffer, offset, count, ct);
                }
            }
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _reader.Dispose();
                _buffer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
