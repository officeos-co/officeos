namespace EnterpriseAgentOs.Api.GraphQL;

/// <summary>
/// Root Mutation type for the dashboard GraphQL schema.
/// Per-domain mutation fields live in <c>Entities/{Domain}/GraphQL/{Domain}Mutations.cs</c>
/// as <c>[ExtendObjectType(typeof(GraphQLMutations))]</c> classes. They are auto-registered
/// via <c>AddTypeExtensionsFromAssembly</c>.
/// </summary>
public class GraphQLMutations
{
    /// <summary>
    /// Placeholder to keep the Mutation type non-empty until the first domain extension registers.
    /// HotChocolate requires at least one field on a root type.
    /// </summary>
    public bool Noop() => true;
}
