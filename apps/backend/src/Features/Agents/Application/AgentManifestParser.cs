namespace OffceOs.Application.Features.Agents;

internal sealed class AgentManifestParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly JsonSerializerOptions DefinitionJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    private readonly AgentDefinitionParser _agentDefinitionParser;

    public AgentManifestParser(AgentDefinitionParser agentDefinitionParser)
    {
        _agentDefinitionParser = agentDefinitionParser;
    }

    public IReadOnlyList<AgentManifestItem> ParseMany(string manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest))
            throw new InvalidOperationException("Manifest is required.");

        var docs = SplitDocuments(manifest)
            .Select(ParseOne)
            .ToList();

        if (docs.Count == 0)
            throw new InvalidOperationException("Manifest is empty.");

        return docs;
    }

    public AgentDefinitionConfig ToDefinitionConfig(AgentManifestItem manifest)
    {
        if (!string.Equals(manifest.Kind, "Agent", StringComparison.Ordinal))
            throw new InvalidOperationException($"Unsupported manifest kind '{manifest.Kind}'.");

        var name = string.IsNullOrWhiteSpace(manifest.Metadata?.Name)
            ? throw new InvalidOperationException("Agent manifest metadata.name is required.")
            : manifest.Metadata.Name.Trim();
        var spec = manifest.Spec ?? throw new InvalidOperationException($"Agent manifest '{name}' requires spec.");

        return _agentDefinitionParser.Parse(JsonSerializer.Serialize(new AgentDefinitionConfig(
            name,
            spec.Description,
            spec.Model ?? ProviderRegistry.DefaultModel,
            spec.System,
            spec.McpServers?.Select(server => new AgentMcpServerConfig(server.Name, server.Type, server.Url)).ToList() ?? [],
            spec.Tools?.Select(tool => new AgentToolsetConfig(
                tool.Type,
                tool.McpServerName,
                tool.DefaultConfig is null
                    ? null
                    : new AgentToolsetDefaultConfig(tool.DefaultConfig.PermissionPolicy is null
                        ? null
                        : new AgentToolPermissionConfig(
                            tool.DefaultConfig.PermissionPolicy.Type,
                            tool.DefaultConfig.PermissionPolicy.Tools)))).ToList() ?? [],
            spec.Resources?.Select(resource => new AgentResourceAttachmentConfig(
                resource.Type,
                resource.ResourceId,
                resource.AccessMode,
                resource.Instructions)).ToList(),
            spec.Routines?.Select(routine => new AgentRoutineConfig(
                routine.Name,
                routine.Prompt,
                routine.ScheduleTriggers?.Select(trigger => new AgentRoutineScheduleTriggerConfig(trigger.Name, trigger.Expression)).ToList(),
                routine.ApiTriggers?.Select(trigger => new AgentRoutineApiTriggerConfig(trigger.Name)).ToList(),
                routine.GitHubTriggers?.Select(trigger => new AgentRoutineGitHubTriggerConfig(
                    trigger.Name,
                    trigger.Owner,
                    trigger.Repo,
                    trigger.Events ?? [],
                    trigger.Secret)).ToList())).ToList(),
            spec.Metadata), DefinitionJsonOptions));
    }

    public string Serialize(AgentManifestItem manifest) => YamlSerializer.Serialize(manifest).TrimEnd();

    public AgentManifestItem FromDefinition(string provider, AgentDefinitionConfig config) => new(
        "eaos.io/v1alpha1",
        "Agent",
        new AgentManifestMetadataItem(config.Name),
        new AgentManifestSpecItem(
            provider,
            config.Model,
            config.Description,
            config.System,
            config.McpServers.Select(server => new AgentManifestMcpServerItem(server.Name, server.Type, server.Url)).ToList(),
            config.Tools.Select(tool => new AgentManifestToolsetItem(
                tool.Type,
                tool.McpServerName,
                tool.DefaultConfig is null
                    ? null
                    : new AgentManifestToolsetDefaultItem(tool.DefaultConfig.PermissionPolicy is null
                        ? null
                        : new AgentManifestPermissionPolicyItem(
                            tool.DefaultConfig.PermissionPolicy.Type,
                            tool.DefaultConfig.PermissionPolicy.Tools)))).ToList(),
            config.Resources?.Select(resource => new AgentManifestResourceItem(resource.Type, resource.ResourceId, resource.AccessMode, resource.Instructions)).ToList(),
            config.Routines?.Select(routine => new AgentManifestRoutineItem(
                routine.Name,
                routine.Prompt,
                routine.ScheduleTriggers?.Select(trigger => new AgentManifestScheduleTriggerItem(trigger.Name, trigger.Expression)).ToList(),
                routine.ApiTriggers?.Select(trigger => new AgentManifestApiTriggerItem(trigger.Name)).ToList(),
                routine.GitHubTriggers?.Select(trigger => new AgentManifestGitHubTriggerItem(trigger.Name, trigger.Owner, trigger.Repo, trigger.Events, trigger.Secret)).ToList())).ToList(),
            config.Metadata));

    private static AgentManifestItem ParseOne(string document)
    {
        try
        {
            var json = NormalizeToJson(document);
            var parsed = JsonSerializer.Deserialize<AgentManifestItem>(json, JsonOptions);
            return parsed ?? throw new InvalidOperationException("Manifest document is empty.");
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
}

internal sealed record AgentManifestItem(
    string ApiVersion,
    string Kind,
    AgentManifestMetadataItem? Metadata,
    AgentManifestSpecItem? Spec);

internal sealed record AgentManifestMetadataItem(string Name);

internal sealed record AgentManifestSpecItem(
    string Provider,
    string? Model,
    string? Description,
    string? System,
    IReadOnlyList<AgentManifestMcpServerItem>? McpServers,
    IReadOnlyList<AgentManifestToolsetItem>? Tools,
    IReadOnlyList<AgentManifestResourceItem>? Resources,
    IReadOnlyList<AgentManifestRoutineItem>? Routines,
    JsonElement? Metadata);

internal sealed record AgentManifestMcpServerItem(string Name, string Type, string? Url);

internal sealed record AgentManifestToolsetItem(string Type, string? McpServerName, AgentManifestToolsetDefaultItem? DefaultConfig);

internal sealed record AgentManifestToolsetDefaultItem(AgentManifestPermissionPolicyItem? PermissionPolicy);

internal sealed record AgentManifestPermissionPolicyItem(string Type, IReadOnlyList<string>? Tools);

internal sealed record AgentManifestResourceItem(string Type, Guid ResourceId, string? AccessMode, string? Instructions);

internal sealed record AgentManifestRoutineItem(
    string Name,
    string Prompt,
    IReadOnlyList<AgentManifestScheduleTriggerItem>? ScheduleTriggers,
    IReadOnlyList<AgentManifestApiTriggerItem>? ApiTriggers,
    IReadOnlyList<AgentManifestGitHubTriggerItem>? GitHubTriggers);

internal sealed record AgentManifestScheduleTriggerItem(string Name, string Expression);

internal sealed record AgentManifestApiTriggerItem(string Name);

internal sealed record AgentManifestGitHubTriggerItem(string Name, string Owner, string Repo, IReadOnlyList<string>? Events, string Secret);
