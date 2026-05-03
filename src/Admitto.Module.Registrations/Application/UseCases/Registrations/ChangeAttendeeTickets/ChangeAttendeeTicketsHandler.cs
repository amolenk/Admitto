using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets;

internal sealed class ChangeAttendeeTicketsHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<ChangeAttendeeTicketsCommand>
{
    public async ValueTask HandleAsync(
        ChangeAttendeeTicketsCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Load registration; reject if not found.
        var registration = await writeStore.Registrations
            .FirstOrDefaultAsync(
                r => r.Id == command.RegistrationId && r.EventId == command.EventId,
                cancellationToken);

        if (registration is null)
            throw new BusinessRuleViolationException(
                NotFoundError.Create<Registration>(command.RegistrationId.Value));

        // 2. Reject cancelled registrations.
        if (registration.Status == RegistrationStatus.Cancelled)
            throw new BusinessRuleViolationException(Errors.RegistrationIsCancelled);

        // 3. Load event; reject if not Active.
        var ticketedEvent = await writeStore.TicketedEvents
            .FirstOrDefaultAsync(e => e.Id == command.EventId, cancellationToken);

        if (ticketedEvent is null || !ticketedEvent.IsActive)
            throw new BusinessRuleViolationException(Errors.EventNotActive);

        // 4. Load catalog.
        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == command.EventId, cancellationToken);

        if (catalog is null)
            throw new BusinessRuleViolationException(Errors.NoTicketTypesConfigured);

        // 5. Validate the full new selection (duplicates, unknown, cancelled, time slot conflicts).
        catalog.ValidateSelection(command.TicketTypeSlugs);

        // 6. Compute delta: toRelease = current ∖ new, toClaim = new ∖ current.
        var currentSlugs = registration.Tickets.Select(t => t.Slug).ToHashSet();
        var newSlugsSet = command.TicketTypeSlugs.ToHashSet();

        var toRelease = currentSlugs.Except(newSlugsSet).ToList();
        var toClaim = command.TicketTypeSlugs.Where(s => !currentSlugs.Contains(s)).ToList();

        // 7. Release freed capacity.
        catalog.Release(toRelease);

        // 8. Claim added capacity (unenforced — admin path, validation runs inside Claim).
        catalog.Claim(toClaim, enforce: false);

        // 9. Build new ticket snapshots.
        var newTickets = command.TicketTypeSlugs
            .Select(slug =>
            {
                var ticketType = catalog.GetTicketType(slug);
                var timeSlots = ticketType?.TimeSlots.Select(ts => ts.Slug.Value).ToArray()
                    ?? [];
                var name = ticketType?.Name.Value ?? slug;
                return new TicketTypeSnapshot(slug, name, timeSlots);
            })
            .ToList();

        // 10. Apply the change to the registration.
        registration.ChangeTickets(newTickets, timeProvider.GetUtcNow());
    }

    internal static class Errors
    {
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
