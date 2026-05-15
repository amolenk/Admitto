using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets;

internal sealed class ChangeAttendeeTicketsHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<ChangeAttendeeTicketsCommand>
{
    public async ValueTask HandleAsync(
        ChangeAttendeeTicketsCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        RegistrationId registrationId = RegistrationId.From(command.RegistrationId);

        // 1. Load registration; reject if not found.
        var registration = await writeStore.Registrations
            .FirstOrDefaultAsync(
                r => r.Id == registrationId && r.EventId == eventId,
                cancellationToken);

        if (registration is null)
            throw new BusinessRuleViolationException(
                NotFoundError.Create<Registration>(registrationId.Value));

        // 2. Reject cancelled registrations.
        if (registration.Status == RegistrationStatus.Cancelled)
            throw new BusinessRuleViolationException(Errors.RegistrationIsCancelled);

        // 3. Load event; reject if not Active.
        var ticketedEvent = await writeStore.TicketedEvents
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (ticketedEvent is null || !ticketedEvent.IsActive)
            throw new BusinessRuleViolationException(Errors.EventNotActive);

        // 3b. For self-service, also enforce registration window.
        if (command.Mode == ChangeMode.SelfService)
        {
            var now = timeProvider.GetUtcNow();
            var policy = ticketedEvent.RegistrationPolicy;
            if (policy is null || now < policy.OpensAt || now >= policy.ClosesAt)
                throw new BusinessRuleViolationException(Errors.RegistrationWindowClosed);
        }

        // 4. Load catalog.
        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == eventId, cancellationToken);

        if (catalog is null)
            throw new BusinessRuleViolationException(Errors.NoTicketTypesConfigured);

        // 5. Validate the full new selection (duplicates, unknown, cancelled, time slot conflicts).
        var newTicketTypeIds = command.TicketTypeIds.Select(TicketTypeId.From).ToList();
        catalog.ValidateSelection(newTicketTypeIds);

        // 6. Compute delta: toRelease = current ∖ new, toClaim = new ∖ current.
        var currentIds = registration.Tickets.Select(t => t.Id.Value).ToHashSet();
        var newIdsSet = command.TicketTypeIds.ToHashSet();

        var toRelease = currentIds.Except(newIdsSet).Select(TicketTypeId.From).ToList();
        var toClaim = newTicketTypeIds.Where(id => !currentIds.Contains(id.Value)).ToList();

        // 7. Release freed capacity.
        catalog.Release(toRelease);

        // 8. Claim added capacity (enforce for self-service, unenforced for admin).
        catalog.Claim(toClaim, enforce: command.Mode == ChangeMode.SelfService);

        // 9. Build new ticket snapshots.
        var newTickets = newTicketTypeIds
            .Select(id =>
            {
                var ticketType = catalog.GetTicketType(id);
                var timeSlots = ticketType?.TimeSlots ?? [];
                var name = ticketType?.Name ?? TicketTypeName.From(id.Value.ToString());
                return new TicketTypeSnapshot(id, name, timeSlots);
            })
            .ToList();

        // 10. Apply the change to the registration.
        registration.ChangeTickets(newTickets, timeProvider.GetUtcNow());
    }

    internal static class Errors
    {
        public static readonly Error RegistrationWindowClosed = new(
            "change_tickets.registration_window_closed",
            "The registration window is not open for this event.",
            Type: ErrorType.Validation);

        public static readonly Error RegistrationIsCancelled = new(
            "registration.is_cancelled",
            "Registration is cancelled.",
            Type: ErrorType.Conflict);

        public static readonly Error EventNotActive = new(
            "change_tickets.event_not_active",
            "Cannot change tickets on a cancelled or archived event.",
            Type: ErrorType.Validation);

        public static readonly Error NoTicketTypesConfigured = new(
            "change_tickets.no_ticket_types",
            "No ticket types have been configured for this event.",
            Type: ErrorType.Validation);
    }
}
