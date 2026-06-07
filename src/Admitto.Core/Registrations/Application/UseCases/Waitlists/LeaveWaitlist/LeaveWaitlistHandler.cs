using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.LeaveWaitlist;

internal sealed class LeaveWaitlistHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<LeaveWaitlistCommand>
{
    public async ValueTask HandleAsync(
        LeaveWaitlistCommand command,
        CancellationToken cancellationToken)
    {
        var emailResult = EmailAddress.TryFrom(command.Email);
        if (!emailResult.IsSuccess)
            throw new BusinessRuleViolationException(Errors.InvalidEmail);

        var email = emailResult.ValueObject;
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        TicketTypeId ticketTypeId = TicketTypeId.From(command.TicketTypeId);

        var catalog = await writeStore.TicketCatalogs.GetUntrackedAsync(
            tc => tc.Id == eventId,
            cancellationToken);

        catalog.EnsureEventActive();

        var waitlist = await writeStore.Waitlists
            .FirstOrDefaultAsync(w => w.Id == ticketTypeId && w.EventId == eventId, cancellationToken);

        if (waitlist is null)
            throw new BusinessRuleViolationException(Errors.WaitlistNotFound);

        waitlist.RemoveEntry(email);
    }

    internal static class Errors
    {
        public static readonly Error InvalidEmail = new(
            "waitlist.invalid_email",
            "The provided email address is not valid.",
            Type: ErrorType.Validation);

        public static readonly Error WaitlistNotFound = new(
            "waitlist.not_found",
            "The waitlist could not be found.",
            Type: ErrorType.NotFound);
    }
}
