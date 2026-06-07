using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.JoinWaitlist;

internal sealed class JoinWaitlistHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<JoinWaitlistCommand>
{
    public async ValueTask HandleAsync(
        JoinWaitlistCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        TeamId teamId = TeamId.From(command.TeamId);
        TicketTypeId ticketTypeId = TicketTypeId.From(command.TicketTypeId);

        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == eventId && tc.TeamId == teamId, cancellationToken);

        if (catalog is null)
            throw new BusinessRuleViolationException(Errors.EventNotFound);

        catalog.EnsureEventActive();

        var ticketType = catalog.GetTicketType(ticketTypeId);
        if (ticketType is null)
            throw new BusinessRuleViolationException(Errors.TicketTypeNotFound);

        if (!ticketType.WaitlistEnabled)
            throw new BusinessRuleViolationException(Errors.WaitlistNotEnabled);

        if (!ticketType.WaitlistMode)
            throw new BusinessRuleViolationException(Errors.WaitlistNotActive);

        var waitlist = await writeStore.Waitlists
            .Include(w => w.Entries)
            .FirstOrDefaultAsync(
                w => w.Id == ticketTypeId && w.EventId == eventId && w.TeamId == teamId,
                cancellationToken);

        if (waitlist is null)
            throw new BusinessRuleViolationException(Errors.WaitlistNotFound);

        waitlist.AddEntry(EmailAddress.From(command.Email), timeProvider.GetUtcNow());
    }

    internal static class Errors
    {
        public static readonly Error EventNotFound = new(
            "waitlist.event_not_found",
            "The ticketed event could not be found.",
            Type: ErrorType.NotFound);

        public static readonly Error TicketTypeNotFound = new(
            "waitlist.ticket_type_not_found",
            "The ticket type could not be found.",
            Type: ErrorType.NotFound);

        public static readonly Error WaitlistNotEnabled = new(
            "waitlist.not_enabled",
            "The waitlist is not enabled for this ticket type.",
            Type: ErrorType.Validation);

        public static readonly Error WaitlistNotActive = new(
            "waitlist.not_active",
            "The waitlist is not currently active for this ticket type.",
            Type: ErrorType.Validation);

        public static readonly Error WaitlistNotFound = new(
            "waitlist.not_found",
            "The waitlist could not be found.",
            Type: ErrorType.NotFound);
    }
}
