namespace EnterpriseAgentOs.Api.GraphQL;

/// <summary>
/// Root Query type for the dashboard GraphQL schema.
/// Per-domain query fields live in <c>Entities/{Domain}/GraphQL/{Domain}Queries.cs</c>
/// as <c>[ExtendObjectType(typeof(GraphQLQueries))]</c> classes. They are auto-registered
/// via <c>AddTypeExtensionsFromAssembly</c>.
/// </summary>
public class GraphQLQueries
{
    public string Ping() => "pong";
}
