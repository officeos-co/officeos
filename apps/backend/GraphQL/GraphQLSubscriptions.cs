namespace EnterpriseAgentOs.Api.GraphQL;

/// <summary>
/// Root Subscription type for the dashboard GraphQL schema.
/// Per-domain subscription fields live in <c>Entities/{Domain}/GraphQL/{Domain}Subscriptions.cs</c>
/// as <c>[ExtendObjectType(typeof(GraphQLSubscriptions))]</c> classes. They are auto-registered
/// via <c>AddTypeExtensionsFromAssembly</c>.
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
