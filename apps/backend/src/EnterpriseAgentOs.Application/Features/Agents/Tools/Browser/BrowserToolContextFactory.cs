namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed record BrowserToolContext(
    IBrowserService BrowserService,
    IBrowserRuntimeClient BrowserRuntime,
    IReadOnlyDictionary<string, BrowserToolDescriptor> Descriptors);

internal interface IBrowserToolContextFactory
{
    Task<BrowserToolContext?> CreateForTurnAsync(CancellationToken ct = default);
    Task<BrowserToolContext> CreateCatalogAsync(CancellationToken ct = default);
}

internal sealed class BrowserToolContextFactory : IBrowserToolContextFactory
{
    private readonly IBrowserService _browserService;
    private readonly IBrowserRuntimeClient _browserRuntime;

    public BrowserToolContextFactory(IBrowserService browserService, IBrowserRuntimeClient browserRuntime)
    {
        _browserService = browserService;
        _browserRuntime = browserRuntime;
    }

    public async Task<BrowserToolContext?> CreateForTurnAsync(CancellationToken ct = default)
    {
        if (!await _browserRuntime.IsAvailableAsync(ct))
            return null;

        return new BrowserToolContext(
            _browserService,
            _browserRuntime,
            ToDescriptorMap(await _browserRuntime.ListToolsAsync(ct)));
    }

    public async Task<BrowserToolContext> CreateCatalogAsync(CancellationToken ct = default)
    {
        try
        {
            if (await _browserRuntime.IsAvailableAsync(ct))
            {
                return new BrowserToolContext(
                    _browserService,
                    _browserRuntime,
                    ToDescriptorMap(await _browserRuntime.ListToolsAsync(ct)));
            }
        }
        catch
        {
            // Catalog rendering should still show stable browser tool metadata
            // when the browser runtime is temporarily unavailable.
        }

        return new BrowserToolContext(
            _browserService,
            _browserRuntime,
            new Dictionary<string, BrowserToolDescriptor>(StringComparer.Ordinal));
    }

    private static IReadOnlyDictionary<string, BrowserToolDescriptor> ToDescriptorMap(
        IReadOnlyList<BrowserToolDescriptor> runtimeTools)
        => runtimeTools
            .Where(t => t.Name.StartsWith("browser.", StringComparison.Ordinal))
            .GroupBy(t => t.Name, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
}
