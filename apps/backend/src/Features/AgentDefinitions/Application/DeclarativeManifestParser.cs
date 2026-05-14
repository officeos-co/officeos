namespace OffceOs.Application.Features.AgentDefinitions;

internal sealed class DeclarativeManifestParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithAttemptingUnquotedStringTypeDeserialization()
        .Build();

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    public DeclarativeWorkspaceItem Parse(string manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest))
            throw new InvalidOperationException("Manifest is required.");

        var items = SplitDocuments(manifest)
            .Select(ParseOne)
            .ToList();

        if (items.Count == 0)
            throw new InvalidOperationException("Manifest is empty.");

        return new DeclarativeWorkspaceItem(items);
    }

    public string Serialize(DeclarativeWorkspaceItem config) =>
        string.Join($"{Environment.NewLine}---{Environment.NewLine}", config.Items.Select(item => YamlSerializer.Serialize(ToYamlObject(item)).TrimEnd()));

    private static DeclarativeResourceItem ParseOne(string document)
    {
        try
        {
            var json = NormalizeToJson(document);
            var parsed = JsonSerializer.Deserialize<DeclarativeResourceItem>(json, JsonOptions);
            if (parsed is null)
                throw new InvalidOperationException("Manifest document is empty.");

            return parsed;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Manifest is invalid: {ex.Message}", ex);
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new InvalidOperationException($"Manifest is invalid YAML: {ex.Message}", ex);
        }
    }

    private static IEnumerable<string> SplitDocuments(string manifest)
    {
        var builder = new StringBuilder();
        using var reader = new StringReader(manifest);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Trim() == "---")
            {
                var document = builder.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(document))
                    yield return document;
                builder.Clear();
                continue;
            }

            builder.AppendLine(line);
        }

        var last = builder.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(last))
            yield return last;
    }

    private static string NormalizeToJson(string config)
    {
        var trimmed = config.Trim();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
            return trimmed;

        var yaml = YamlDeserializer.Deserialize<object?>(trimmed);
        var node = ToJsonNode(yaml);
        return node?.ToJsonString(JsonOptions) ?? "{}";
    }

    private static JsonNode? ToJsonNode(object? value)
    {
        if (value is null)
            return null;

        if (value is IDictionary<object, object> objectMap)
        {
            var node = new JsonObject();
            foreach (var (key, mapValue) in objectMap)
                node[Convert.ToString(key) ?? string.Empty] = ToJsonNode(mapValue);
            return node;
        }

        if (value is IEnumerable<object> sequence && value is not string)
        {
            var array = new JsonArray();
            foreach (var item in sequence)
                array.Add(ToJsonNode(item));
            return array;
        }

        return value switch
        {
            string text => JsonValue.Create(text),
            bool boolean => JsonValue.Create(boolean),
            int integer => JsonValue.Create(integer),
            long integer => JsonValue.Create(integer),
            double number => JsonValue.Create(number),
            decimal number => JsonValue.Create(number),
            _ => JsonNode.Parse(JsonSerializer.Serialize(value, JsonOptions)),
        };
    }

    private static object ToYamlObject(DeclarativeResourceItem item)
    {
        var result = new Dictionary<string, object?>
        {
            ["apiVersion"] = item.ApiVersion,
            ["kind"] = item.Kind,
            ["metadata"] = item.Metadata is null
                ? null
                : new Dictionary<string, object?> { ["name"] = item.Metadata.Name },
            ["spec"] = item.Spec.HasValue ? JsonElementToObject(item.Spec.Value) : null,
        };

        return RemoveNulls(result);
    }

    private static object? JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(property => property.Name, property => JsonElementToObject(property.Value)),
            JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number when element.TryGetDecimal(out var number) => number,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    private static object RemoveNulls(object value)
    {
        if (value is Dictionary<string, object?> dictionary)
            return dictionary
                .Where(pair => pair.Value is not null)
                .ToDictionary(pair => pair.Key, pair => pair.Value is null ? null : RemoveNulls(pair.Value));

        if (value is IReadOnlyList<Dictionary<string, object?>> dictionaryList)
            return dictionaryList.Select(item => RemoveNulls(item)).ToList();

        if (value is IReadOnlyList<object?> objectList)
            return objectList.Where(item => item is not null).Select(item => RemoveNulls(item!)).ToList();

        return value;
    }
}

internal sealed record DeclarativeWorkspaceItem(IReadOnlyList<DeclarativeResourceItem> Items);

internal sealed record DeclarativeResourceItem(
    string ApiVersion,
    string Kind,
    DeclarativeMetadataItem? Metadata,
    JsonElement? Spec);

internal sealed record DeclarativeMetadataItem(string Name);

internal sealed record DeclarativeChannelSpecItem(
    string Type,
    string? DisplayName,
    bool? Enabled,
    string? Token,
    Dictionary<string, string>? Credentials);

internal sealed record DeclarativeProviderSpecItem(
    string Type,
    string? DisplayName,
    bool? Enabled,
    string? DefaultModel,
    IReadOnlyList<string>? Models,
    string? AuthKind,
    Dictionary<string, string>? Credentials);

internal sealed record DeclarativeIntegrationSpecItem(
    bool? Builtin,
    string? Provider,
    string? Title,
    string? Description,
    string? TransportType,
    string? Command,
    string? Args,
    string? Url,
    string? Category,
    string? Logo,
    string? CredentialFieldsJson,
    Dictionary<string, string>? Credentials);

internal sealed record DeclarativeMemoryStoreSpecItem(
    string? DisplayName,
    IReadOnlyList<DeclarativeMemoryStoreEntryItem>? Entries);

internal sealed record DeclarativeMemoryStoreEntryItem(string Key, string Content);

internal sealed record DeclarativeBrowserSpecItem(string? DisplayName);

internal sealed record DeclarativeAgentSpecItem(
    string Provider,
    string? Model,
    string? Description,
    string? System,
    DeclarativeAgentToolsItem? Tools,
    IReadOnlyList<DeclarativeAgentIntegrationRefItem>? Integrations,
    IReadOnlyList<DeclarativeAgentChannelRefItem>? Channels,
    IReadOnlyList<DeclarativeAgentResourceRefItem>? MemoryStores,
    IReadOnlyList<DeclarativeAgentResourceRefItem>? Browsers,
    JsonElement? Metadata);

internal sealed record DeclarativeAgentToolsItem(
    DeclarativeToolsetPolicyItem? Builtin,
    DeclarativeToolsetPolicyItem? Browser);

internal sealed record DeclarativeToolsetPolicyItem(DeclarativePermissionPolicyItem? PermissionPolicy);

internal sealed record DeclarativePermissionPolicyItem(string Type, IReadOnlyList<string>? Tools);

internal sealed record DeclarativeAgentIntegrationRefItem(
    string Ref,
    DeclarativePermissionPolicyItem? PermissionPolicy);

internal sealed record DeclarativeAgentChannelRefItem(
    string Ref,
    JsonElement? Config);

internal sealed record DeclarativeAgentResourceRefItem(
    string Ref,
    string? AccessMode,
    string? Instructions);

internal sealed record DeclarativeRoutineSpecItem(
    string AgentRef,
    string Prompt,
    IReadOnlyList<DeclarativeScheduleTriggerItem>? ScheduleTriggers,
    IReadOnlyList<DeclarativeApiTriggerItem>? ApiTriggers,
    IReadOnlyList<DeclarativeGitHubTriggerItem>? GitHubTriggers);

internal sealed record DeclarativeScheduleTriggerItem(string Name, string Expression);

internal sealed record DeclarativeApiTriggerItem(string Name);

internal sealed record DeclarativeGitHubTriggerItem(
    string Name,
    string Owner,
    string Repo,
    IReadOnlyList<string>? Events,
    string Secret);

internal sealed record DeclarativeEngineSpecItem(
    string Type,
    string? Version,
    string? Image,
    string? ExecutionMode,
    string? DefaultModel,
    string? PermissionProfile);
