using Amolenk.Admitto.Core.Organization.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.MaterializeTicketedEvent.EventHandlers;

/// <summary>
/// Handles <see cref="TicketedEventCreationRequestedIntegrationEvent"/> from the Organization module by
/// dispatching a <see cref="MaterializeTicketedEventCommand"/>. The command handler creates the
/// <c>TicketedEvent</c> aggregate, which raises <c>TicketedEventCreatedDomainEvent</c>;
/// <c>RegistrationsIntegrationEventPublisher</c> then converts that into the outbound
/// <c>TicketedEventCreatedIntegrationEvent</c>.
/// </summary>
internal sealed class TicketedEventCreationRequestedIntegrationEventHandler(
    ICommandHandler<MaterializeTicketedEventCommand> materializeHandler)
    : IIntegrationEventHandler<TicketedEventCreationRequestedIntegrationEvent>
{
    public ValueTask HandleAsync(
        TicketedEventCreationRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        return materializeHandler.HandleAsync(
            new MaterializeTicketedEventCommand(
                integrationEvent.CreationRequestId,
                integrationEvent.TeamId,
                integrationEvent.Name,
                integrationEvent.WebsiteUrl,
                integrationEvent.BaseUrl,
                integrationEvent.StartsAt,
                integrationEvent.EndsAt,
                integrationEvent.TimeZone),
            cancellationToken);
    }
}
