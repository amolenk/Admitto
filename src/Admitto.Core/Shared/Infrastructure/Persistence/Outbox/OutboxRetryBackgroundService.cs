using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;

internal sealed class OutboxRetryBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<OutboxRetryOptions> options,
    ILogger<OutboxRetryBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.CurrentValue.PollingInterval);

        do
        {
            await DispatchOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task DispatchOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var registrations = scope.ServiceProvider.GetServices<OutboxDbContextRegistration>().ToList();
        var sender = scope.ServiceProvider.GetRequiredService<IOutboxMessageSender>();

        foreach (var registration in registrations)
        {
            try
            {
                var dbContext = (IOutboxDbContext)scope.ServiceProvider.GetRequiredService(registration.DbContextType);
                var dispatcher = new OutboxDispatcher(dbContext, sender);
                await dispatcher.DispatchOrphanedAsync(
                    options.CurrentValue.BatchSize,
                    options.CurrentValue.MinimumAge,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to dispatch pending outbox rows for module {ModuleKey}.",
                    registration.ModuleKey);
            }
        }
    }
}
