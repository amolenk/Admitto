using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.WithdrawWaitlistEntries.EventHandlers;

/// <summary>
/// Handles <see cref="RegistrationCancelledDomainEvent"/> by withdrawing the cancelled attendee
/// from every waitlist they may still be actively queued on, across all ticket types for the
/// event, so a cancelled attendee can never subsequently receive a promotion offer.
/// </summary>
internal sealed class RegistrationCancelledDomainEventHandler(
    ICommandHandler<WithdrawWaitlistEntriesCommand> withdrawWaitlistEntriesHandler)
    : IDomainEventHandler<RegistrationCancelledDomainEvent>
{
    public async ValueTask HandleAsync(
        RegistrationCancelledDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        await withdrawWaitlistEntriesHandler.HandleAsync(
            new WithdrawWaitlistEntriesCommand(
                domainEvent.TicketedEventId.Value,
                domainEvent.TeamId.Value,
                domainEvent.Email.Value),
            cancellationToken);
    }
}
