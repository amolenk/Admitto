using Amolenk.Admitto.Core.Email.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Inbox;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.HandleReconfirmAutoExpired.EventHandlers;

internal sealed class ReconfirmAutoExpiredIntegrationEventHandler(IRegistrationsWriteStore writeStore)
    : IIntegrationEventHandler<ReconfirmAutoExpiredIntegrationEvent>
{
    public async ValueTask HandleAsync(
        ReconfirmAutoExpiredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var messageKey = integrationEvent.IntegrationEventId.ToString("N");
        var alreadyHandled = await writeStore.ProcessedMessages
            .AnyAsync(x => x.MessageKey == messageKey, cancellationToken);

        if (alreadyHandled)
            return;

        var teamId = TeamId.From(integrationEvent.TeamId);
        var ticketedEventId = TicketedEventId.From(integrationEvent.TicketedEventId);
        var eventIsActive = await writeStore.TicketedEvents
            .AsNoTracking()
            .AnyAsync(
                e => e.Id == ticketedEventId && e.TeamId == teamId && e.Status == EventLifecycleStatus.Active,
                cancellationToken);
        var catalog = await writeStore.TicketCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == ticketedEventId && c.TeamId == teamId && c.EventStatus == EventLifecycleStatus.Active,
                cancellationToken);

        if (!eventIsActive || catalog is null)
        {
            writeStore.ProcessedMessages.Add(
                ProcessedMessage.Create(messageKey, DateTimeOffset.UtcNow));
            return;
        }

        if (integrationEvent.RegistrationReferences is null)
        {
            writeStore.ProcessedMessages.Add(
                ProcessedMessage.Create(messageKey, DateTimeOffset.UtcNow));
            return;
        }

        foreach (var reference in integrationEvent.RegistrationReferences)
        {
            if (reference.RegistrationCycleId is null
                || reference.RegistrationVersion is null
                || reference.TicketCatalogVersion is null
                || reference.TicketTypeIds is null
                || catalog.Version != reference.TicketCatalogVersion.Value)
                continue;

            var registrationId = RegistrationId.From(reference.RegistrationId);
            var registrationCycleId = RegistrationCycleId.From(reference.RegistrationCycleId.Value);
            var registration = await writeStore.Registrations
                .FirstOrDefaultAsync(
                    r => r.Id == registrationId
                        && r.TeamId == teamId
                        && r.EventId == ticketedEventId
                        && r.RegistrationCycleId == registrationCycleId,
                    cancellationToken);

            if (registration is null
                || registration.Status != RegistrationStatus.Registered
                || registration.HasReconfirmed
                || !registration.Tickets.Select(t => t.Id.Value).ToHashSet()
                    .SetEquals(reference.TicketTypeIds))
            {
                continue;
            }

            // A harmless version advance (for example, an attendee detail edit)
            // must not permanently discard an otherwise valid terminal
            // cancellation. Cycle, status, ticket selection, and catalog-version
            // guards above still reject stale asynchronous evaluations.
            registration.Cancel(CancellationReason.ReconfirmAutoCancel);
        }

        writeStore.ProcessedMessages.Add(
            ProcessedMessage.Create(messageKey, DateTimeOffset.UtcNow));
    }
}
