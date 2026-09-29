using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.WithdrawWaitlistEntries;

/// <summary>
/// Withdraws an attendee from every waitlist they may still be actively queued on,
/// across all ticket types for the event, so they can never receive a promotion offer. Each withdrawn entry leaves
/// the ticket catalog's queued count in the same unit of work.
/// </summary>
internal sealed class WithdrawWaitlistEntriesHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<WithdrawWaitlistEntriesCommand>
{
    public async ValueTask HandleAsync(
        WithdrawWaitlistEntriesCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);
        var email = EmailAddress.From(command.Email);

        var waitlists = await writeStore.Waitlists
            .Where(w => w.EventId == eventId && w.TeamId == teamId)
            .ToListAsync(cancellationToken);

        var queuedWaitlists = waitlists.Where(w => w.HasActiveEntry(email)).ToList();
        if (queuedWaitlists.Count == 0)
            return;

        var catalog = await writeStore.TicketCatalogs.GetAsync(
            c => c.Id == eventId && c.TeamId == teamId,
            cancellationToken);

        foreach (var waitlist in queuedWaitlists)
        {
            waitlist.RemoveEntry(email, catalog);
        }
    }
}
