using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.RegisterTicketedEventCreation;
using Amolenk.Admitto.Module.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement;

/// <summary>
/// Reacts to <see cref="TicketedEventCreatedDomainEvent"/> on behalf of the Team aggregate.
/// Translates the event into a <see cref="RegisterTicketedEventCreationCommand"/> and
/// dispatches it via the mediator.
/// </summary>
internal sealed class TicketedEventCreatedDomainEventHandler(IMediator mediator)
    : IDomainEventHandler<TicketedEventCreatedDomainEvent>
{
    public ValueTask HandleAsync(
        TicketedEventCreatedDomainEvent domainEvent,
        CancellationToken cancellationToken) =>
        mediator.SendAsync(new RegisterTicketedEventCreationCommand(domainEvent.TeamId), cancellationToken);
}
