using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.RemoveWaitlistEntry;

internal sealed class RemoveWaitlistEntryHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<RemoveWaitlistEntryCommand>
{
    public async ValueTask HandleAsync(
        RemoveWaitlistEntryCommand command,
        CancellationToken cancellationToken)
    {
        TicketTypeId ticketTypeId = TicketTypeId.From(command.TicketTypeId);
        TicketedEventId ticketedEventId = TicketedEventId.From(command.EventId);
        TeamId teamId = TeamId.From(command.TeamId);
        WaitlistEntryId entryId = WaitlistEntryId.From(command.EntryId);

        var waitlist = await writeStore.Waitlists
            .Include(w => w.Entries)
            .Include(w => w.Coupons)
            .FirstOrDefaultAsync(w => w.Id == ticketTypeId && w.EventId == ticketedEventId && w.TeamId == teamId, cancellationToken);

        if (waitlist is null)
            throw new BusinessRuleViolationException(Errors.WaitlistNotFound);

        waitlist.RemoveEntry(entryId);
    }

    internal static class Errors
    {
        public static readonly Error WaitlistNotFound = new(
            "waitlist.not_found",
            "The waitlist could not be found.",
            Type: ErrorType.NotFound);
    }
}
