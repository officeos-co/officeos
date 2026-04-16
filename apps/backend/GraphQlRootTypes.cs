namespace EnterpriseAgentOs.Api;

/// <summary>
/// Root Query type for the dashboard GraphQL schema.
/// Per-domain query fields live in <c>Entities/{Domain}/{Domain}Queries.cs</c>
/// as <c>[ExtendObjectType(typeof(GraphQLQueries))]</c> classes. They are auto-registered
/// via <c>AddDomainTypeExtensions</c>.
/// </summary>
public class GraphQLQueries
{
    public string Ping() => "pong";
}

/// <summary>
/// Root Mutation type for the dashboard GraphQL schema.
/// Per-domain mutation fields live in <c>Entities/{Domain}/{Domain}Mutations.cs</c>
/// as <c>[ExtendObjectType(typeof(GraphQLMutations))]</c> classes. They are auto-registered
/// via <c>AddDomainTypeExtensions</c>.
/// </summary>
public class GraphQLMutations
{
    /// <summary>
    /// Placeholder to keep the Mutation type non-empty until the first domain extension registers.
    /// HotChocolate requires at least one field on a root type.
    /// </summary>
    public bool Noop() => true;
}

/// <summary>
/// Root Subscription type for the dashboard GraphQL schema.
/// Per-domain subscription fields live in <c>Entities/{Domain}/{Domain}Subscriptions.cs</c>
/// as <c>[ExtendObjectType(typeof(GraphQLSubscriptions))]</c> classes. They are auto-registered
/// via <c>AddDomainTypeExtensions</c>.
/// </summary>
public class GraphQLSubscriptions
{
    /// <summary>
    /// Placeholder heartbeat subscription so the type is non-empty before domain extensions register.
    /// </summary>
    [Subscribe(With = nameof(SubscribeHeartbeat))]
    public DateTime Heartbeat([EventMessage] DateTime at) => at;

    public async IAsyncEnumerable<DateTime> SubscribeHeartbeat(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return DateTime.UtcNow;
            try { await Task.Delay(TimeSpan.FromSeconds(30), ct); } catch { yield break; }
        }
    }
}
