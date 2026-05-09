namespace OffceOs.Api.Common;

public class GraphQLQueries
{
    public string Ping() => "pong";
}
public class GraphQLMutations
{
    public bool Noop() => true;
}

public class GraphQLSubscriptions
{
    [Subscribe(With = nameof(SubscribeHeartbeat))]
    public DateTime Heartbeat([EventMessage] DateTime at) => at;

    public async IAsyncEnumerable<DateTime> SubscribeHeartbeat(
        [EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return DateTime.UtcNow;
            try { await Task.Delay(TimeSpan.FromSeconds(30), ct); } catch { yield break; }
        }
    }
}
