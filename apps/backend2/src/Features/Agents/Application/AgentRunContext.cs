namespace OffceOs.Application.Features.Agents;

internal static class AgentRunContext
{
    private static readonly AsyncLocal<(Guid? RunId, Guid? ParentRunId)> Current = new();

    public static Guid? RunId => Current.Value.RunId;
    public static Guid? ParentRunId => Current.Value.ParentRunId;

    public static IDisposable Begin(Guid? runId, Guid? parentRunId)
    {
        var previous = Current.Value;
        Current.Value = (runId, parentRunId);
        return new Scope(previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly (Guid? RunId, Guid? ParentRunId) _previous;
        public Scope((Guid? RunId, Guid? ParentRunId) previous) => _previous = previous;
        public void Dispose() => Current.Value = _previous;
    }
}
