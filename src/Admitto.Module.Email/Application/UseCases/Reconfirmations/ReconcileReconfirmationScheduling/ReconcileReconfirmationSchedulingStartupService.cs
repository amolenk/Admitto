using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.Reconfirmations.ReconcileReconfirmationScheduling;

/// <summary>
/// Worker-startup hosted service that dispatches
/// <see cref="ReconcileReconfirmationSchedulingCommand"/> to heal Quartz
/// trigger state against the Registrations module's source of truth.
/// Failures are logged but do not crash worker startup; integration event
/// handlers will heal on the next policy-changed / time-zone-changed event.
/// </summary>
internal sealed class ReconcileReconfirmationSchedulingStartupService(
    IServiceScopeFactory scopeFactory,
    ILogger<ReconcileReconfirmationSchedulingStartupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            await mediator.SendAsync(
                new ReconcileReconfirmationSchedulingCommand(),
                stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Worker shutting down before reconciliation finished.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reconfirm trigger startup reconciliation failed.");
        }
    }
}
