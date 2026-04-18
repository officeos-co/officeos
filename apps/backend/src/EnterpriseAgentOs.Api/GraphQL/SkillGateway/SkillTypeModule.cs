namespace EnterpriseAgentOs.Api.GraphQL.SkillGateway;

/// <summary>
/// HotChocolate HotChocolate.Execution.Configuration.ITypeModule that generates the entire GraphQL skill schema
/// dynamically from skill-runtime manifests. The backend has zero hardcoded
/// knowledge of specific skills — it reads manifests at startup and generates
/// query fields, argument types, and return types from them.
///
/// Each action becomes a query field named {skill}{PascalAction}
/// (e.g. notionSearch, githubListRepos, googleDriveSearch).
///
/// Resolvers call SkillRuntimeClient.ExecuteAsync() to dispatch
/// execution to the skill-runtime Node.js service.
/// </summary>
public sealed class SkillTypeModule : HotChocolate.Execution.Configuration.ITypeModule
{
    private readonly IServiceProvider _services;

    public SkillTypeModule(IServiceProvider services)
    {
        _services = services;
    }

    public event EventHandler<EventArgs>? TypesChanged;

    public async ValueTask<IReadOnlyCollection<ITypeSystemMember>> CreateTypesAsync(
        IDescriptorContext context,
        CancellationToken ct)
    {
        var types = new List<ITypeSystemMember>();

        using var scope = _services.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<ISkillCatalogRepository>();
        var records = await catalog.ListActiveAsync(ct);
        var queryExtDef = new ObjectTypeDefinition("Query");

        foreach (var record in records)
        {
            var actions = EnterpriseAgentOs.Application.Services.Skills.SkillService.DeserializeActions(record);
            if (actions.Count == 0) continue;
            foreach (var (actionName, action) in actions)
            {
                var pascalAction = SnakeToPascal(actionName);
                var fieldName = record.Name + pascalAction; // e.g. notionSearch

                // Build return type from manifest's returns schema
                var (returnTypeRef, returnObjectType) = BuildReturnType(
                    record.Name, pascalAction, action.Returns);
                if (returnObjectType is not null)
                {
                    types.Add(returnObjectType);
                }

                // Build arguments
                var argDefs = BuildArguments(action.Params);

                // Build the resolver
                var skillName = record.Name;
                var actionKey = actionName;
                var argNames = argDefs.Select(a => (graphql: a.Name, snake: CamelToSnake(a.Name))).ToList();

                FieldResolverDelegate resolver = async ctx =>
                {
                    // Collect arguments into a dictionary
                    var parameters = new Dictionary<string, object>();
                    foreach (var (graphql, snake) in argNames)
                    {
                        var val = ctx.ArgumentValue<object?>(graphql);
                        if (val is not null)
                        {
                            parameters[snake] = val;
                        }
                    }

                    // Get credentials for this skill
                    var svc = ctx.Service<ISkillService>();
                    var creds = await svc.GetDecryptedCredentialsAsync(skillName, ctx.RequestAborted);
                    if (creds is null)
                    {
                        throw new GraphQLException(
                            $"{skillName} skill not configured. Install and configure it on the Skills page.");
                    }

                    // Dispatch to skill runtime
                    var client = ctx.Service<SkillRuntimeClient>();
                    var result = await client.ExecuteAsync(
                        skillName, actionKey, parameters, creds, ct: ctx.RequestAborted);

                    if (!result.Success)
                    {
                        throw new GraphQLException(result.Error ?? "Skill execution failed");
                    }

                    return DeserializeResult(result.Result);
                };

                // Create the field definition
                var fieldDef = new ObjectFieldDefinition(
                    fieldName,
                    action.Description,
                    returnTypeRef,
                    resolver,
                    pureResolver: null);

                foreach (var arg in argDefs)
                {
                    fieldDef.Arguments.Add(arg);
                }

                queryExtDef.Fields.Add(fieldDef);
            }
        }

        types.Add(ObjectTypeExtension.CreateUnsafe(queryExtDef));
        return types;
    }

    /// <summary>
    /// Build a GraphQL return type from the manifest's returns JSON schema.
    /// </summary>
    private static (TypeReference, ITypeSystemMember?) BuildReturnType(
        string skill, string action, JsonElement? returns)
    {
        if (returns is null || returns.Value.ValueKind == JsonValueKind.Undefined)
        {
            return (TypeReference.Parse("String"), null);
        }

        var schema = returns.Value;
        var typeName = GetJsonString(schema, "type");

        if (typeName == "array")
        {
            if (schema.TryGetProperty("items", out var items)
                && GetJsonString(items, "type") == "object")
            {
                var objectTypeName = $"{Capitalize(skill)}{action}Result";
                var objectTypeDef = BuildObjectType(objectTypeName, items);
                return (TypeReference.Parse($"[{objectTypeName}]"), ObjectType.CreateUnsafe(objectTypeDef));
            }
            return (TypeReference.Parse("[String]"), null);
        }

        if (typeName == "object")
        {
            var objectTypeName = $"{Capitalize(skill)}{action}Result";
            var objectTypeDef = BuildObjectType(objectTypeName, schema);
            return (TypeReference.Parse(objectTypeName), ObjectType.CreateUnsafe(objectTypeDef));
        }

        return (TypeReference.Parse("String"), null);
    }

    /// <summary>
    /// Build a HotChocolate ObjectTypeDefinition from a JSON schema object.
    /// </summary>
    private static ObjectTypeDefinition BuildObjectType(string name, JsonElement schema)
    {
        var typeDef = new ObjectTypeDefinition(name);

        if (!schema.TryGetProperty("properties", out var props))
        {
            typeDef.Fields.Add(new ObjectFieldDefinition(
                "_data",
                description: null,
                TypeReference.Parse("String"),
                resolver: null,
                pureResolver: ctx => JsonSerializer.Serialize(ctx.Parent<object>())));
            return typeDef;
        }

        foreach (var prop in props.EnumerateObject())
        {
            var fieldName = SnakeToCamel(prop.Name);
            var propType = GetJsonString(prop.Value, "type");

            var gqlType = propType switch
            {
                "string" => "String",
                "number" or "integer" => "Long",
                "boolean" => "Boolean",
                _ => "String",
            };

            var snakeName = prop.Name;
            typeDef.Fields.Add(new ObjectFieldDefinition(
                fieldName,
                description: null,
                TypeReference.Parse(gqlType),
                resolver: null,
                pureResolver: ctx =>
                {
                    var parent = ctx.Parent<Dictionary<string, object?>>();
                    if (parent.TryGetValue(snakeName, out var val)) return val;
                    if (parent.TryGetValue(fieldName, out val)) return val;
                    return null;
                }));
        }

        return typeDef;
    }

    /// <summary>
    /// Build GraphQL argument definitions from a JSON schema params object.
    /// </summary>
    private static List<ArgumentDefinition> BuildArguments(JsonElement? paramsSchema)
    {
        var args = new List<ArgumentDefinition>();
        if (paramsSchema is null) return args;
        var schema = paramsSchema.Value;

        if (!schema.TryGetProperty("properties", out var props)) return args;

        var requiredFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (schema.TryGetProperty("required", out var reqArr) && reqArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in reqArr.EnumerateArray())
            {
                var s = r.GetString();
                if (s is not null) requiredFields.Add(s);
            }
        }

        foreach (var prop in props.EnumerateObject())
        {
            var argName = SnakeToCamel(prop.Name);
            var propType = GetJsonString(prop.Value, "type");
            var isRequired = requiredFields.Contains(prop.Name);
            var description = GetJsonString(prop.Value, "description");

            var gqlType = propType switch
            {
                "string" => "String",
                "number" or "integer" => "Int",
                "boolean" => "Boolean",
                _ => "String",
            };
            if (isRequired) gqlType += "!";

            var argDef = new ArgumentDefinition();
            argDef.Name = argName;
            argDef.Type = TypeReference.Parse(gqlType);
            if (description is not null) argDef.Description = description;

            // Set default value if present
            if (prop.Value.TryGetProperty("default", out var defaultVal))
            {
                argDef.DefaultValue = defaultVal.ValueKind switch
                {
                    JsonValueKind.String => new StringValueNode(defaultVal.GetString()!),
                    JsonValueKind.Number => new IntValueNode(defaultVal.GetInt32()),
                    JsonValueKind.True => new BooleanValueNode(true),
                    JsonValueKind.False => new BooleanValueNode(false),
                    _ => null,
                };
            }

            args.Add(argDef);
        }

        return args;
    }

    /// <summary>
    /// Deserialize the runtime result JsonElement into dictionaries/lists
    /// that HotChocolate can resolve against the generated types.
    /// </summary>
    private static object? DeserializeResult(JsonElement? result)
    {
        if (result is null) return null;
        return DeserializeValue(result.Value);
    }

    private static object? DeserializeValue(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.Array => el.EnumerateArray()
                .Select(DeserializeValue)
                .ToList(),
            JsonValueKind.Object => el.EnumerateObject()
                .ToDictionary(p => p.Name, p => DeserializeValue(p.Value)),
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt64(out var l) ? (object)l : el.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    // --- String helpers ---

    private static string? GetJsonString(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static string SnakeToPascal(string s) =>
        string.Join("", s.Split('_').Select(Capitalize));

    private static string SnakeToCamel(string s)
    {
        var parts = s.Split('_');
        return parts[0] + string.Join("", parts.Skip(1).Select(Capitalize));
    }

    private static string CamelToSnake(string s)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            if (char.IsUpper(s[i]) && i > 0) sb.Append('_');
            sb.Append(char.ToLower(s[i]));
        }
        return sb.ToString();
    }

    private static string Capitalize(string s) =>
        s.Length == 0 ? s : char.ToUpper(s[0]) + s[1..];
}
