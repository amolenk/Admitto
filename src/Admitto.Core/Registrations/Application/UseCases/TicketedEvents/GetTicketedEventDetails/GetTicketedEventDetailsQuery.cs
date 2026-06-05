using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventDetails;

internal sealed record GetTicketedEventDetailsQuery(TicketedEventId EventId, TeamId TeamId)
    : Query<TicketedEventDetailsDto?>;
