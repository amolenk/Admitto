using Amolenk.Admitto.Module.Organization.Contracts.IntegrationEvents;
using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.MaterializeTicketedEvent.EventHandlers;

/// <summary>
/// Handles <see cref="TicketedEventCreationRequested"/> from the Organization module by
/// materialising the authoritative <see cref="TicketedEvent"/> aggregate and its
/// <see cref="TicketCatalog"/>. Publishes a <see cref="TicketedEventCreated"/> or
/// <see cref="TicketedEventCreationRejected"/> integration event depending on the outcome.
/// </summary>
/// <remarks>
/// At-least-once delivery: a redelivered request for a slug that already resolved to a
/// <see cref="TicketedEvent"/> is reported as <c>duplicate_slug</c>. Organization ignores
/// outcome events for creation requests in a terminal state, so the spurious rejection is
/// harmless.
/// </remarks>
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
        var teamSlug = Slug.From(integrationEvent.TeamSlug);
        var slug = Slug.From(integrationEvent.Slug);

        var slugAlreadyUsed = await writeStore.TicketedEvents
            .AnyAsync(te => te.TeamId == teamId && te.Slug == slug, cancellationToken);

        if (slugAlreadyUsed)
        {
            integrationEventOutbox.Enqueue(new TicketedEventCreationRejected(
                integrationEvent.CreationRequestId,
                integrationEvent.TeamId,
                "duplicate_slug"));
            return;
        }

        var ticketedEventId = TicketedEventId.New();

        var timeZone = TimeZoneId.From(integrationEvent.TimeZone);

        var ticketedEvent = TicketedEvent.Create(
            ticketedEventId,
            teamId,
            teamSlug,
            slug,
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
            integrationEvent.Slug,
            timeZone.Value));
    }
}
