using Amolenk.Admitto.Module.Organization.Contracts.IntegrationEvents;
using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.MaterializeTicketedEvent.EventHandlers;

/// <summary>
/// Handles <see cref="TicketedEventCreationRequested"/> from the Organization module by
/// materialising the authoritative <see cref="TicketedEvent"/> aggregate and its
/// <see cref="TicketCatalog"/>. Publishes a <see cref="TicketedEventCreated"/>
/// integration event after successful creation.
/// </summary>
internal sealed class MaterializeTicketedEventIntegrationEventHandler(
    IRegistrationsWriteStore writeStore,
    [FromKeyedServices(RegistrationsModule.Key)] IIntegrationEventOutbox integrationEventOutbox)
    : IIntegrationEventHandler<TicketedEventCreationRequested>
{
    public async ValueTask HandleAsync(
        TicketedEventCreationRequested integrationEvent,
        CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(integrationEvent.TeamId);
        var ticketedEventId = TicketedEventId.New();
        var timeZone = TimeZoneId.From(integrationEvent.TimeZone);

        var ticketedEvent = TicketedEvent.Create(
            ticketedEventId,
            teamId,
            DisplayName.From(integrationEvent.Name),
            AbsoluteUrl.From(integrationEvent.WebsiteUrl),
            AbsoluteUrl.From(integrationEvent.BaseUrl),
            integrationEvent.StartsAt,
            integrationEvent.EndsAt,
            timeZone);

        var catalog = TicketCatalog.Create(ticketedEventId);

        writeStore.TicketedEvents.Add(ticketedEvent);
        writeStore.TicketCatalogs.Add(catalog);

        integrationEventOutbox.Enqueue(new TicketedEventCreated(
            integrationEvent.CreationRequestId,
            integrationEvent.TeamId,
            ticketedEventId.Value,
            timeZone.Value));
    }
}
