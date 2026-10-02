using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.Shared;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

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

        var catalog = await writeStore.TicketCatalogs.GetAsync(
            tc => tc.Id == ticketedEventId && tc.TeamId == teamId,
            cancellationToken);

        catalog.EnsureEventActive();

        var waitlist = await writeStore.Waitlists
            .Include(w => w.Entries)
            .Include(w => w.Coupons)
            .FirstOrDefaultAsync(w => w.Id == ticketTypeId && w.EventId == ticketedEventId && w.TeamId == teamId, cancellationToken);

        if (waitlist is null)
            throw new BusinessRuleViolationException(Errors.WaitlistNotFound);

        var entry = waitlist.Entries.FirstOrDefault(e => e.Id == entryId);
        if (entry is null)
            throw new BusinessRuleViolationException(Errors.WaitlistNotFound);

        var registrationId = entry.RegistrationId;

        var withdrawnCouponId = waitlist.RemoveEntry(entryId, catalog);
        if (withdrawnCouponId is { } couponId)
        {
            var offer = await writeStore.Coupons.FirstOrDefaultAsync(
                c => c.Id == couponId, cancellationToken);
            offer?.Expire(DateTimeOffset.UtcNow);
        }

        var allEventWaitlists = await writeStore.Waitlists
            .Where(w => w.EventId == ticketedEventId && w.TeamId == teamId)
            .ToListAsync(cancellationToken);

        var registration = await writeStore.Registrations.FirstOrDefaultAsync(
            r => r.Id == registrationId, cancellationToken);
        if (registration is not null)
        {
            RegistrationCouponHelpers.CancelIfExhausted(
                registration, allEventWaitlists, CancellationReason.TicketTypesRemoved);
        }
    }

    internal static class Errors
    {
        public static readonly Error WaitlistNotFound = new(
            "waitlist.not_found",
            "The waitlist could not be found.",
            Type: ErrorType.NotFound);
    }
}
