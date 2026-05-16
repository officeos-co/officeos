using OffceOs.Domain.Features.AgentDefinitions;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.AgentRoutines;
namespace OffceOs.Application.Features.AgentDefinitions;

internal sealed class AgentDefinitionParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    public AgentDefinitionConfig Parse(string config)
    {
        if (string.IsNullOrWhiteSpace(config))
            throw new InvalidOperationException("Agent definition config is required.");

        AgentDefinitionConfig? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<AgentDefinitionConfig>(NormalizeToJson(config), JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Agent definition config is invalid: {ex.Message}", ex);
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new InvalidOperationException($"Agent definition config is invalid YAML: {ex.Message}", ex);
        }

        if (parsed is null)
            throw new InvalidOperationException("Agent definition config is empty.");

        return NormalizeAndValidate(parsed);
    }

    public string Serialize(AgentDefinitionConfig config)
        => JsonSerializer.Serialize(NormalizeAndValidate(config), JsonOptions);

    public string SerializeYaml(AgentDefinitionConfig config)
        => YamlSerializer.Serialize(ToYamlObject(NormalizeAndValidate(config))).TrimEnd();

    public AgentDefinitionConfig CreateDefaultConfig(
        string name,
        string model,
        string? system,
        IReadOnlyList<string>? integrationNames)
    {
        var mcpServers = (integrationNames ?? [])
            .Select(NormalizeIntegrationName)
            .Where(integrationName => !string.IsNullOrWhiteSpace(integrationName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(integrationName => new AgentMcpServerConfig(integrationName, "registered", null))
            .ToList();

        var tools = new List<AgentToolsetConfig>
        {
            new(
                AgentToolsetKinds.Builtin,
                null,
                new AgentToolsetDefaultConfig(new AgentToolPermissionConfig(AgentToolPermissionKinds.AlwaysAllow, null))),
        };

        tools.AddRange(mcpServers.Select(server => new AgentToolsetConfig(
            AgentToolsetKinds.Mcp,
            server.Name,
            new AgentToolsetDefaultConfig(new AgentToolPermissionConfig(AgentToolPermissionKinds.AlwaysAllow, null)))));

        return NormalizeAndValidate(new AgentDefinitionConfig(
            name,
            null,
            model,
            system,
            mcpServers,
            tools,
            null,
            null,
            null));
    }

    public AgentDefinitionRecord CreateRecord(
        Guid agentId,
        int version,
        AgentDefinitionConfig config,
        string provider,
        Guid? createdBy)
    {
        var configJson = Serialize(config);
        return new AgentDefinitionRecord
        {
            AgentId = agentId,
            Version = version,
            Name = config.Name,
            Description = config.Description,
            Provider = provider,
            Model = config.Model,
            SystemPrompt = config.System,
            ConfigJson = configJson,
            ConfigHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(configJson))).ToLowerInvariant(),
            CreatedBy = createdBy,
        };
    }

    private static AgentDefinitionConfig NormalizeAndValidate(AgentDefinitionConfig config)
    {
        var name = RequireTrimmed(config.Name, "Agent name is required.");
        var model = RequireTrimmed(config.Model, "Agent model is required.");
        var system = string.IsNullOrWhiteSpace(config.System) ? null : config.System;
        var description = string.IsNullOrWhiteSpace(config.Description) ? null : config.Description.Trim();

        var mcpServers = (config.McpServers ?? [])
            .Select(server => new AgentMcpServerConfig(
                RequireTrimmed(server.Name, "MCP server name is required."),
                RequireTrimmed(server.Type, "MCP server type is required.").ToLowerInvariant(),
                string.IsNullOrWhiteSpace(server.Url) ? null : server.Url.Trim()))
            .ToList();

        foreach (var server in mcpServers.Where(server => server.Type == "url" && string.IsNullOrWhiteSpace(server.Url)))
            throw new InvalidOperationException($"MCP server '{server.Name}' requires a url.");

        var serverNames = mcpServers.Select(server => server.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tools = (config.Tools ?? []).Select(tool =>
        {
            var type = RequireTrimmed(tool.Type, "Toolset type is required.").ToLowerInvariant();
            if (type is not AgentToolsetKinds.Builtin and not AgentToolsetKinds.Mcp and not AgentToolsetKinds.Browser)
                throw new InvalidOperationException($"Toolset type '{tool.Type}' is not supported.");

            var mcpServerName = string.IsNullOrWhiteSpace(tool.McpServerName) ? null : tool.McpServerName.Trim();
            if (type == AgentToolsetKinds.Mcp)
            {
                if (mcpServerName is null)
                    throw new InvalidOperationException("MCP toolsets require mcp_server_name.");
                if (!serverNames.Contains(mcpServerName))
                    throw new InvalidOperationException($"MCP toolset references unknown MCP server '{mcpServerName}'.");
            }

            var policy = NormalizePolicy(tool.DefaultConfig?.PermissionPolicy);
            return new AgentToolsetConfig(type, mcpServerName, new AgentToolsetDefaultConfig(policy));
        }).ToList();

        if (tools.Count == 0)
        {
            tools.Add(new AgentToolsetConfig(
                AgentToolsetKinds.Builtin,
                null,
                new AgentToolsetDefaultConfig(new AgentToolPermissionConfig(AgentToolPermissionKinds.AlwaysAllow, null))));
        }

        var resources = (config.Resources ?? [])
            .Select(resource =>
            {
                var type = RequireTrimmed(resource.Type, "Resource attachment type is required.").ToLowerInvariant();
                if (type is not AgentResourceKinds.Browser and not AgentResourceKinds.MemoryStore and not AgentResourceKinds.Channel)
                    throw new InvalidOperationException($"Resource attachment type '{resource.Type}' is not supported.");

                return new AgentResourceAttachmentConfig(
                    type,
                    resource.ResourceId == Guid.Empty ? throw new InvalidOperationException("Resource attachment resource_id is required.") : resource.ResourceId,
                    NormalizeAccessMode(resource.AccessMode),
                    string.IsNullOrWhiteSpace(resource.Instructions) ? null : resource.Instructions.Trim());
            })
            .ToList();

        var routines = (config.Routines ?? [])
            .Select(routine => new AgentRoutineConfig(
                RequireTrimmed(routine.Name, "Routine name is required."),
                RequireTrimmed(routine.Prompt, "Routine prompt is required."),
                (routine.ScheduleTriggers ?? [])
                    .Select(trigger => new AgentRoutineScheduleTriggerConfig(
                        RequireTrimmed(trigger.Name, "Schedule routine trigger name is required."),
                        RequireTrimmed(trigger.Expression, "Schedule routine trigger expression is required.")))
                    .ToList(),
                (routine.ApiTriggers ?? [])
                    .Select(trigger => new AgentRoutineApiTriggerConfig(
                        RequireTrimmed(trigger.Name, "API routine trigger name is required.")))
                    .ToList(),
                (routine.GitHubTriggers ?? [])
                    .Select(trigger => new AgentRoutineGitHubTriggerConfig(
                        RequireTrimmed(trigger.Name, "GitHub routine trigger name is required."),
                        GitHubRepositoryRecord.Parse(RequireTrimmed(trigger.Repo, "GitHub routine trigger repo is required.")).Url,
                        (trigger.Events ?? []).Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                        string.IsNullOrWhiteSpace(trigger.AuthRef) ? null : trigger.AuthRef.Trim(),
                        string.IsNullOrWhiteSpace(trigger.Secret) ? null : trigger.Secret.Trim(),
                        GitHubRoutineTriggerModes.Normalize(trigger.Mode),
                        trigger.PollIntervalSeconds))
                    .ToList()))
            .ToList();

        foreach (var routine in routines.Where(routine =>
            (routine.ScheduleTriggers?.Count ?? 0) == 0
            && (routine.ApiTriggers?.Count ?? 0) == 0
            && (routine.GitHubTriggers?.Count ?? 0) == 0))
            throw new InvalidOperationException($"Routine '{routine.Name}' requires at least one trigger.");

        foreach (var trigger in routines.SelectMany(routine => routine.GitHubTriggers ?? []))
        {
            if (trigger.Events.Count == 0)
                throw new InvalidOperationException($"GitHub routine trigger '{trigger.Name}' requires at least one event.");
            if (GitHubRoutineTriggerModes.Normalize(trigger.Mode) == GitHubRoutineTriggerModes.Poll
                && string.IsNullOrWhiteSpace(trigger.AuthRef))
                throw new InvalidOperationException($"GitHub routine trigger '{trigger.Name}' requires auth_ref for polling mode.");
        }

        return new AgentDefinitionConfig(
            name,
            description,
            model,
            system,
            mcpServers,
            tools,
            resources,
            routines,
            config.Metadata);
    }

    private static AgentToolPermissionConfig NormalizePolicy(AgentToolPermissionConfig? policy)
    {
        if (policy is null)
            return new AgentToolPermissionConfig(AgentToolPermissionKinds.AlwaysAllow, null);

        var type = RequireTrimmed(policy.Type, "Permission policy type is required.").ToLowerInvariant();
        if (type is not AgentToolPermissionKinds.AlwaysAllow
            and not AgentToolPermissionKinds.AlwaysDeny
            and not AgentToolPermissionKinds.AllowList
            and not AgentToolPermissionKinds.DenyList)
            throw new InvalidOperationException($"Permission policy type '{policy.Type}' is not supported.");

        var tools = (policy.Tools ?? [])
            .Where(tool => !string.IsNullOrWhiteSpace(tool))
            .Select(tool => tool.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (type is AgentToolPermissionKinds.AllowList or AgentToolPermissionKinds.DenyList && tools.Count == 0)
            throw new InvalidOperationException($"Permission policy '{type}' requires at least one tool.");

        return new AgentToolPermissionConfig(type, tools.Count == 0 ? null : tools);
    }

    private static string NormalizeIntegrationName(string value)
    {
        var key = ToolKey.Parse(value);
        return key.SkillName is "builtin" or "builtins" or "agent_toolset" or "browser" or "internal_browser"
            ? string.Empty
            : value.Contains(':', StringComparison.Ordinal) ? key.SkillName : value.Trim();
    }

    private static string NormalizeAccessMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return AgentResourceAccessModes.ReadWrite;

        var normalized = value.Trim().ToLowerInvariant();
        return normalized is AgentResourceAccessModes.ReadOnly or AgentResourceAccessModes.ReadWrite
            ? normalized
            : throw new InvalidOperationException($"Resource attachment access_mode '{value}' is not supported.");
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

        if (value is IDictionary<string, object> stringMap)
        {
            var node = new JsonObject();
            foreach (var (key, mapValue) in stringMap)
                node[key] = ToJsonNode(mapValue);
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
            DateTime dateTime => JsonValue.Create(dateTime),
            _ => JsonNode.Parse(JsonSerializer.Serialize(value, JsonOptions)),
        };
    }

    private static object ToYamlObject(AgentDefinitionConfig config)
    {
        var result = new Dictionary<string, object?>
        {
            ["name"] = config.Name,
            ["description"] = config.Description,
            ["model"] = config.Model,
            ["system"] = config.System,
            ["mcp_servers"] = config.McpServers.Select(server => new Dictionary<string, object?>
            {
                ["name"] = server.Name,
                ["type"] = server.Type,
                ["url"] = server.Url,
            }).ToList(),
            ["tools"] = config.Tools.Select(tool => new Dictionary<string, object?>
            {
                ["type"] = tool.Type,
                ["mcp_server_name"] = tool.McpServerName,
                ["default_config"] = tool.DefaultConfig is null
                    ? null
                    : new Dictionary<string, object?>
                    {
                        ["permission_policy"] = tool.DefaultConfig.PermissionPolicy is null
                            ? null
                            : new Dictionary<string, object?>
                            {
                                ["type"] = tool.DefaultConfig.PermissionPolicy.Type,
                                ["tools"] = tool.DefaultConfig.PermissionPolicy.Tools,
                            },
                    },
            }).ToList(),
            ["resources"] = config.Resources?.Select(resource => new Dictionary<string, object?>
            {
                ["type"] = resource.Type,
                ["resource_id"] = resource.ResourceId,
                ["access_mode"] = resource.AccessMode,
                ["instructions"] = resource.Instructions,
            }).ToList(),
            ["routines"] = config.Routines?.Select(routine => new Dictionary<string, object?>
            {
                ["name"] = routine.Name,
                ["prompt"] = routine.Prompt,
                ["schedule_triggers"] = routine.ScheduleTriggers?.Select(trigger => new Dictionary<string, object?>
                {
                    ["name"] = trigger.Name,
                    ["expression"] = trigger.Expression,
                }).ToList(),
                ["api_triggers"] = routine.ApiTriggers?.Select(trigger => new Dictionary<string, object?>
                {
                    ["name"] = trigger.Name,
                }).ToList(),
                ["github_triggers"] = routine.GitHubTriggers?.Select(trigger => new Dictionary<string, object?>
                {
                    ["name"] = trigger.Name,
                    ["repo"] = trigger.Repo,
                    ["events"] = trigger.Events,
                    ["auth_ref"] = trigger.AuthRef,
                    ["secret"] = trigger.Secret,
                    ["mode"] = trigger.Mode,
                    ["poll_interval_seconds"] = trigger.PollIntervalSeconds,
                }).ToList(),
            }).ToList(),
            ["metadata"] = config.Metadata is null ? null : JsonElementToObject(config.Metadata.Value),
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

    private static string RequireTrimmed(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(message);

        return value.Trim();
    }
}
