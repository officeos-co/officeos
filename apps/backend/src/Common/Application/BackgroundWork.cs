namespace OffceOs.Application.Features;

/// <summary>
/// Runs a scoped async action on a background thread. Replaces the
/// Task.Run + IServiceScopeFactory + GetRequiredService boilerplate.
/// </summary>
internal static class BackgroundWork
{
    public static void Run<TService>(
        IServiceScopeFactory scopeFactory,
        Func<TService, Task> work,
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
                using var scope = scopeFactory.CreateScope();
                var resourceLogWriterService = scope.ServiceProvider.GetRequiredService<IResourceLogWriterService>();
                await resourceLogWriterService
                    .ForControlPlane()
                    .ErrorAsync(ex, "Background work failed for {Service}", typeof(TService).Name);
            }
        });
    }

    public static void Run<T1, T2>(
        IServiceScopeFactory scopeFactory,
        Func<T1, T2, Task> work,
        TimeSpan? delay = null)
        where T1 : notnull where T2 : notnull
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (delay.HasValue)
                    await Task.Delay(delay.Value);

                using var scope = scopeFactory.CreateScope();
                var s1 = scope.ServiceProvider.GetRequiredService<T1>();
                var s2 = scope.ServiceProvider.GetRequiredService<T2>();
                await work(s1, s2);
            }
            catch (Exception ex)
            {
                using var scope = scopeFactory.CreateScope();
                var resourceLogWriterService = scope.ServiceProvider.GetRequiredService<IResourceLogWriterService>();
                await resourceLogWriterService
                    .ForControlPlane()
                    .ErrorAsync(ex, "Background work failed for {Service1}+{Service2}",
                        [typeof(T1).Name, typeof(T2).Name]);
            }
        });
    }
}
