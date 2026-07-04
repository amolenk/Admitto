using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEvents;

internal sealed record GetTicketedEventsQuery(TeamId TeamId)
    : Query<IReadOnlyList<TicketedEventListItemDto>>;
