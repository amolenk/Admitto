using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CancelUnreconfirmedRegistrations;

internal sealed class CancelUnreconfirmedRegistrationsHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<CancelUnreconfirmedRegistrationsCommand>
{
    public async ValueTask HandleAsync(
        CancelUnreconfirmedRegistrationsCommand command,
        CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(command.TeamId);
        var ticketedEventId = TicketedEventId.From(command.TicketedEventId);
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

        if (!eventIsActive || catalog is null || command.RegistrationReferences is null)
            return;

        foreach (var reference in command.RegistrationReferences)
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
    }
}
