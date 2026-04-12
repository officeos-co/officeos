using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.LlmProxy;

/// <summary>
/// Injects Anthropic prompt-caching breakpoints into an OpenAI-format request body.
/// Anthropic charges $0.30/M tokens for cache reads vs $3/M for regular input (10x savings).
/// Cache is activated by adding <c>cache_control: {type: "ephemeral"}</c> to specific parts
/// of the request. LiteLLM transparently passes <c>cache_control</c> through to Anthropic's
/// Messages API.
/// </summary>
public static class PromptCacheInjector
{
    /// <summary>
    /// Injects Anthropic cache_control breakpoints into an OpenAI-format request body.
    /// Only modifies the body when the resolved model starts with "claude".
    /// Returns the body unchanged for all other models.
    /// LiteLLM transparently passes cache_control through to Anthropic's Messages API.
    /// </summary>
    public static JsonElement Inject(JsonElement body, string model)
    {
        if (!model.StartsWith("claude", StringComparison.OrdinalIgnoreCase))
            return body;

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();

        foreach (var prop in body.EnumerateObject())
        {
            if (prop.Name == "messages")
            {
                writer.WritePropertyName("messages");
                WriteMessagesWithCache(writer, prop.Value);
            }
            else if (prop.Name == "tools")
            {
                writer.WritePropertyName("tools");
                WriteToolsWithCache(writer, prop.Value);
            }
            else
            {
                prop.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        return JsonDocument.Parse(stream.ToArray()).RootElement.Clone();
    }

    private static void WriteMessagesWithCache(Utf8JsonWriter writer, JsonElement messages)
    {
        writer.WriteStartArray();

        foreach (var msg in messages.EnumerateArray())
        {
            var role = msg.TryGetProperty("role", out var r) ? r.GetString() : null;

            if (role == "system")
            {
                writer.WriteStartObject();

                foreach (var prop in msg.EnumerateObject())
                {
                    if (prop.Name == "content")
                    {
                        writer.WritePropertyName("content");
                        WriteSystemContentWithCache(writer, prop.Value);
                    }
                    else
                    {
                        prop.WriteTo(writer);
                    }
                }

                writer.WriteEndObject();
            }
            else
            {
                // Non-system messages (user, assistant) — pass through unchanged
                msg.WriteTo(writer);
            }
        }

        writer.WriteEndArray();
    }

    private static void WriteSystemContentWithCache(Utf8JsonWriter writer, JsonElement content)
    {
        if (content.ValueKind == JsonValueKind.String)
        {
            // Wrap string content in a content array with cache_control on the single element
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WriteString("type", "text");
            writer.WriteString("text", content.GetString());
            WriteCacheControl(writer);
            writer.WriteEndObject();
            writer.WriteEndArray();
        }
        else if (content.ValueKind == JsonValueKind.Array)
        {
            // Add cache_control to the last element of the existing array
            var elements = content.EnumerateArray().ToList();
            writer.WriteStartArray();

            for (var i = 0; i < elements.Count; i++)
            {
                var elem = elements[i];
                var isLast = i == elements.Count - 1;

                if (isLast)
                {
                    // Re-write the last element with cache_control appended
                    writer.WriteStartObject();
                    foreach (var prop in elem.EnumerateObject())
                    {
                        prop.WriteTo(writer);
                    }
                    WriteCacheControl(writer);
                    writer.WriteEndObject();
                }
                else
                {
                    elem.WriteTo(writer);
                }
            }

            writer.WriteEndArray();
        }
        else
        {
            // Unexpected content kind — pass through unchanged
            content.WriteTo(writer);
        }
    }

    private static void WriteToolsWithCache(Utf8JsonWriter writer, JsonElement tools)
    {
        if (tools.ValueKind != JsonValueKind.Array)
        {
            tools.WriteTo(writer);
            return;
        }

        var toolList = tools.EnumerateArray().ToList();
        writer.WriteStartArray();

        for (var i = 0; i < toolList.Count; i++)
        {
            var tool = toolList[i];
            var isLast = i == toolList.Count - 1;

            if (isLast)
            {
                // Add cache_control to the last tool
                writer.WriteStartObject();
                foreach (var prop in tool.EnumerateObject())
                {
                    prop.WriteTo(writer);
                }
                WriteCacheControl(writer);
                writer.WriteEndObject();
            }
            else
            {
                tool.WriteTo(writer);
            }
        }

        writer.WriteEndArray();
    }

    private static void WriteCacheControl(Utf8JsonWriter writer)
    {
        writer.WritePropertyName("cache_control");
        writer.WriteStartObject();
        writer.WriteString("type", "ephemeral");
        writer.WriteEndObject();
    }
}
