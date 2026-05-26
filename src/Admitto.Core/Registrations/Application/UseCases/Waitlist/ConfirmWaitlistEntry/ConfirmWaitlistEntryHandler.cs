using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.Security;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlist.ConfirmWaitlistEntry;

internal sealed class ConfirmWaitlistEntryHandler(
    IRegistrationsWriteStore writeStore,
    IVerificationTokenService verificationTokenService,
    TimeProvider timeProvider)
    : ICommandHandler<ConfirmWaitlistEntryCommand>
{
    public async ValueTask HandleAsync(
        ConfirmWaitlistEntryCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        TicketTypeId ticketTypeId = TicketTypeId.From(command.TicketTypeId);

        var claims = verificationTokenService.Validate(command.Token, eventId);
        if (claims is null)
            throw new BusinessRuleViolationException(Errors.InvalidToken);

        var waitlist = await writeStore.Waitlists
            .Include(w => w.Entries)
            .FirstOrDefaultAsync(w => w.Id == ticketTypeId, cancellationToken);

        if (waitlist is null)
            throw new BusinessRuleViolationException(Errors.WaitlistNotFound);

        waitlist.ConfirmEntry(claims.Email, timeProvider.GetUtcNow());
    }

    internal static class Errors
    {
        public static readonly Error InvalidToken = new(
            "waitlist.token_invalid",
            "The verification token is invalid or has expired.",
            Type: ErrorType.Validation);

        public static readonly Error WaitlistNotFound = new(
            "waitlist.not_found",
            "The waitlist could not be found.",
            Type: ErrorType.NotFound);
    }
}
