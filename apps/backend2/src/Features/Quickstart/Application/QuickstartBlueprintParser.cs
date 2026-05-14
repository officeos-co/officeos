namespace OffceOs.Application.Features.Quickstart;

internal sealed class QuickstartBlueprintParser
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

    private readonly AgentDefinitionParser _agentDefinitionParser;

    public QuickstartBlueprintParser(AgentDefinitionParser agentDefinitionParser)
    {
        _agentDefinitionParser = agentDefinitionParser;
    }

    public QuickstartParsedBlueprintResult Parse(IReadOnlyList<QuickstartFileRequest> files)
    {
        if (files.Count == 0)
            throw new InvalidOperationException("At least one quickstart YAML file is required.");

        var normalizedFiles = NormalizeFiles(files);
        var workspaceFiles = normalizedFiles
            .Where(file => file.Path.Equals("workspace.yaml", StringComparison.OrdinalIgnoreCase)
                || file.Path.Equals("workspace.yml", StringComparison.OrdinalIgnoreCase)
                || ReadKind(file.Content).Equals("workspace", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (workspaceFiles.Count > 1)
            throw new InvalidOperationException("Only one workspace YAML file is supported.");

        if (workspaceFiles.Count == 0)
        {
            var singleFileAgents = normalizedFiles
                .Select(file => new QuickstartParsedAgentResult(file.Path, ParseAgent(file.Content)))
                .ToList();
            ValidateAgentKeys(singleFileAgents);
            return new QuickstartParsedBlueprintResult(null, [], singleFileAgents);
        }

        var workspaceFile = workspaceFiles[0];
        var workspace = ParseWorkspace(workspaceFile.Content);
        var resources = ParseWorkspaceResources(workspace);
        var agentRefs = workspace.Agents is { Count: > 0 }
            ? workspace.Agents
            : throw new InvalidOperationException("Workspace YAML must declare at least one agent file.");

        var fileMap = normalizedFiles.ToDictionary(file => file.Path, StringComparer.OrdinalIgnoreCase);
        var agents = new List<QuickstartParsedAgentResult>();
        foreach (var agentRef in agentRefs)
        {
            var filePath = RequirePath(agentRef.File, "Workspace agent file is required.");
            if (!fileMap.TryGetValue(filePath, out var agentFile))
                throw new InvalidOperationException($"Workspace references missing agent file '{filePath}'.");

            var agent = ParseAgent(agentFile.Content);
            agents.Add(new QuickstartParsedAgentResult(filePath, agent));
        }

        ValidateAgentKeys(agents);
        return new QuickstartParsedBlueprintResult(workspace, resources, agents);
    }

    public AgentDefinitionConfig ResolveAgentConfig(
        QuickstartAgentBlueprintConfig agent,
        IReadOnlyDictionary<string, QuickstartCreatedResourceResult> resources)
    {
        var resolvedResources = (agent.Resources ?? [])
            .Select(resource => ResolveResource(resource, resources))
            .ToList();

        return _agentDefinitionParser.Parse(_agentDefinitionParser.Serialize(new AgentDefinitionConfig(
            agent.Name,
            agent.Description,
            agent.Model,
            agent.System,
            agent.McpServers ?? [],
            agent.Tools ?? [],
            resolvedResources,
            agent.Routines,
            agent.Metadata)));
    }

    private static List<QuickstartNormalizedFileResult> NormalizeFiles(IReadOnlyList<QuickstartFileRequest> files)
    {
        var normalized = new List<QuickstartNormalizedFileResult>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var path = NormalizePath(file.Path);
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Quickstart file path is required.");
            if (string.IsNullOrWhiteSpace(file.Content))
                throw new InvalidOperationException($"Quickstart file '{path}' is empty.");
            if (!seen.Add(path))
                throw new InvalidOperationException($"Duplicate quickstart file path '{path}'.");

            normalized.Add(new QuickstartNormalizedFileResult(path, file.Content.Trim()));
        }

        return normalized;
    }

    private static QuickstartWorkspaceConfig ParseWorkspace(string content)
    {
        try
        {
            return JsonSerializer.Deserialize<QuickstartWorkspaceConfig>(NormalizeToJson(content), JsonOptions)
                ?? throw new InvalidOperationException("Workspace YAML is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Workspace YAML is invalid: {ex.Message}", ex);
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new InvalidOperationException($"Workspace YAML is invalid: {ex.Message}", ex);
        }
    }

    private static QuickstartAgentBlueprintConfig ParseAgent(string content)
    {
        try
        {
            var agent = JsonSerializer.Deserialize<QuickstartAgentBlueprintConfig>(NormalizeToJson(content), JsonOptions)
                ?? throw new InvalidOperationException("Agent YAML is empty.");
            if (!string.IsNullOrWhiteSpace(agent.Kind)
                && !agent.Kind.Equals("agent", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Unsupported quickstart file kind '{agent.Kind}'.");

            return agent;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Agent YAML is invalid: {ex.Message}", ex);
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new InvalidOperationException($"Agent YAML is invalid: {ex.Message}", ex);
        }
    }

    private static IReadOnlyList<QuickstartWorkspaceResourceResult> ParseWorkspaceResources(QuickstartWorkspaceConfig workspace)
    {
        var resources = new List<QuickstartWorkspaceResourceResult>();
        foreach (var browser in workspace.Resources?.Browsers ?? [])
        {
            var key = RequireKey(browser.Key, "Workspace browser resource key is required.");
            resources.Add(new QuickstartWorkspaceResourceResult(
                key,
                AgentResourceKinds.Browser,
                string.IsNullOrWhiteSpace(browser.DisplayName) ? "Browser" : browser.DisplayName.Trim()));
        }

        foreach (var memoryStore in workspace.Resources?.MemoryStores ?? [])
        {
            var key = RequireKey(memoryStore.Key, "Workspace memory store resource key is required.");
            resources.Add(new QuickstartWorkspaceResourceResult(
                key,
                AgentResourceKinds.MemoryStore,
                string.IsNullOrWhiteSpace(memoryStore.DisplayName) ? "Memory Store" : memoryStore.DisplayName.Trim()));
        }

        var duplicate = resources
            .GroupBy(resource => resource.Key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Duplicate workspace resource key '{duplicate.Key}'.");

        return resources;
    }

    private static AgentResourceAttachmentConfig ResolveResource(
        QuickstartAgentResourceConfig resource,
        IReadOnlyDictionary<string, QuickstartCreatedResourceResult> resources)
    {
        var type = RequireKey(resource.Type, "Agent resource type is required.").ToLowerInvariant();
        if (type is not AgentResourceKinds.Browser and not AgentResourceKinds.MemoryStore and not AgentResourceKinds.Channel)
            throw new InvalidOperationException($"Agent resource type '{resource.Type}' is not supported.");

        if (resource.ResourceId is { } resourceId && resourceId != Guid.Empty)
        {
            return new AgentResourceAttachmentConfig(
                type,
                resourceId,
                resource.AccessMode,
                string.IsNullOrWhiteSpace(resource.Instructions) ? null : resource.Instructions.Trim());
        }

        var reference = RequireKey(resource.Ref, $"Agent {type} resource must declare ref or resource_id.");
        if (!resources.TryGetValue(reference, out var createdResource))
            throw new InvalidOperationException($"Agent resource references unknown workspace resource '{reference}'.");
        if (!createdResource.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Agent resource '{reference}' is a {createdResource.Type}, not a {type}.");

        return new AgentResourceAttachmentConfig(
            type,
            createdResource.Id,
            resource.AccessMode,
            string.IsNullOrWhiteSpace(resource.Instructions) ? null : resource.Instructions.Trim());
    }

    private static void ValidateAgentKeys(IReadOnlyList<QuickstartParsedAgentResult> agents)
    {
        var duplicate = agents
            .Select(agent => agent.Agent.Key)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .GroupBy(key => key!, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Duplicate agent key '{duplicate.Key}'.");
    }

    private static string ReadKind(string content)
    {
        try
        {
            var json = JsonNode.Parse(NormalizeToJson(content));
            return json?["kind"]?.GetValue<string>()?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
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

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return path.Trim().Replace('\\', '/').TrimStart('/');
    }

    private static string RequirePath(string? value, string message)
    {
        var path = NormalizePath(value);
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException(message);

        return path;
    }

    private static string RequireKey(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(message);

        return value.Trim();
    }
}

internal sealed record QuickstartParsedBlueprintResult(
    QuickstartWorkspaceConfig? Workspace,
    IReadOnlyList<QuickstartWorkspaceResourceResult> Resources,
    IReadOnlyList<QuickstartParsedAgentResult> Agents);

internal sealed record QuickstartWorkspaceResourceResult(
    string Key,
    string Type,
    string DisplayName);

internal sealed record QuickstartParsedAgentResult(
    string FilePath,
    QuickstartAgentBlueprintConfig Agent);

internal sealed record QuickstartCreatedResourceResult(
    string Key,
    string Type,
    Guid Id);

internal sealed record QuickstartNormalizedFileResult(
    string Path,
    string Content);
