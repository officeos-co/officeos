namespace OffceOs.Application.Features.Agents;

internal sealed class AgentDefinitionParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public AgentDefinitionConfig Parse(string configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson))
            throw new InvalidOperationException("Agent definition config is required.");

        AgentDefinitionConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<AgentDefinitionConfig>(configJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Agent definition config is invalid JSON: {ex.Message}", ex);
        }

        if (config is null)
            throw new InvalidOperationException("Agent definition config is empty.");

        return NormalizeAndValidate(config);
    }

    public string Serialize(AgentDefinitionConfig config)
        => JsonSerializer.Serialize(NormalizeAndValidate(config), JsonOptions);

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

        return new AgentDefinitionConfig(
            name,
            description,
            model,
            system,
            mcpServers,
            tools,
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

    private static string RequireTrimmed(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(message);

        return value.Trim();
    }
}
