using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.AddTicketType;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureRegistrationPolicy;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.UpdateTicketedEventDetails;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.Hosting;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Development;

internal sealed class LocalDemoSeedInitializer(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<LocalDemoSeedInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue<bool>("Development:LocalDemoSeed:Enabled"))
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await SeedRegistrationsAsync(stoppingToken))
                    return;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Local demo registrations seed will retry.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task<bool> SeedRegistrationsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var store = services.GetRequiredService<IRegistrationsWriteStore>();
        var demoSlug = Slug.From("admitto-demo");
        var ticketedEvent = await store.TicketedEvents.FirstOrDefaultAsync(
            item => item.PublicSlug == demoSlug, cancellationToken);

        // The event is deliberately discovered through the normal integration flow.
        if (ticketedEvent is null)
            return false;

        if (ticketedEvent.Status == EventLifecycleStatus.Archived)
        {
            logger.LogError(
                "Local demo seed stopped: event with public slug {Slug} is archived.", demoSlug.Value);
            return true;
        }

        var now = DateTimeOffset.UtcNow;
        var startsAt = now.AddDays(60);
        var endsAt = now.AddDays(61);
        var updateDetails = services.GetRequiredService<ICommandHandler<UpdateTicketedEventDetailsCommand>>();
        await updateDetails.HandleAsync(new UpdateTicketedEventDetailsCommand(
            ticketedEvent.Id.Value,
            ticketedEvent.TeamId.Value,
            ticketedEvent.Version,
            "Admitto Demo Event",
            "https://admitto-demo.local",
            "https://admitto-demo.local",
            "Europe/Amsterdam",
            startsAt,
            endsAt,
            demoSlug.Value), cancellationToken);

        var configurePolicy = services.GetRequiredService<ICommandHandler<ConfigureRegistrationPolicyCommand>>();
        await configurePolicy.HandleAsync(new ConfigureRegistrationPolicyCommand(
            ticketedEvent.Id.Value,
            ticketedEvent.TeamId.Value,
            ticketedEvent.Version,
            now.AddDays(-1),
            now.AddDays(30),
            null), cancellationToken);

        var catalog = await store.TicketCatalogs.FirstOrDefaultAsync(
            item => item.Id == ticketedEvent.Id && item.TeamId == ticketedEvent.TeamId, cancellationToken);
        if (catalog is null)
            return false;

        if (catalog.TicketTypes.All(ticket =>
                !string.Equals(ticket.Name.Value, "General Admission", StringComparison.OrdinalIgnoreCase)))
        {
            var addTicket = services.GetRequiredService<ICommandHandler<AddTicketTypeCommand, Guid>>();
            await addTicket.HandleAsync(new AddTicketTypeCommand(
                ticketedEvent.Id.Value,
                ticketedEvent.TeamId.Value,
                "General Admission",
                [],
                200,
                SelfServiceEnabled: true), cancellationToken);
        }

        var uow = services.GetRequiredKeyedService<IUnitOfWork>(RegistrationsModule.Key);
        await uow.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Configured local demo event {EventId}.", ticketedEvent.Id.Value);
        return true;
    }
}
