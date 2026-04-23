namespace EnterpriseAgentOs.Application.Features;

/// <summary>
/// Runs a scoped async action on a background thread. Replaces the
/// Task.Run + IServiceScopeFactory + GetRequiredService boilerplate.
/// </summary>
internal static class BackgroundWork
{
    public static void Run<TService>(
        IServiceScopeFactory scopeFactory,
        Func<TService, Task> work,
        ILogger logger,
        TimeSpan? delay = null) where TService : notnull
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (delay.HasValue)
                    await Task.Delay(delay.Value);

                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<TService>();
                await work(service);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background work failed for {Service}", typeof(TService).Name);
            }
        });
    }
}
