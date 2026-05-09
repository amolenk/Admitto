using Amolenk.Admitto.Core.Organization.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.MaterializeTicketedEvent.EventHandlers;

/// <summary>
/// Handles <see cref="TicketedEventCreationRequestedIntegrationEvent"/> from the Organization module by
/// materialising the authoritative <see cref="TicketedEvent"/> aggregate and its
/// <see cref="TicketCatalog"/>. Publishes a <see cref="TicketedEventCreatedIntegrationEvent"/>
/// integration event after successful creation.
/// </summary>
internal sealed class TicketedEventCreationRequestedIntegrationEventHandler(
    IRegistrationsWriteStore writeStore,
    [FromKeyedServices(RegistrationsModule.Key)] IIntegrationEventOutbox integrationEventOutbox)
    : IIntegrationEventHandler<TicketedEventCreationRequestedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        TicketedEventCreationRequestedIntegrationEvent integrationEvent,
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

        integrationEventOutbox.Enqueue(new TicketedEventCreatedIntegrationEvent(
            integrationEvent.CreationRequestId,
            integrationEvent.TeamId,
            ticketedEventId.Value,
            timeZone.Value));
    }
}
