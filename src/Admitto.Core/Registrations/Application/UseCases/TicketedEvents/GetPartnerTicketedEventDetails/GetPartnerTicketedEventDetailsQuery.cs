using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetPartnerTicketedEventDetails;

internal sealed record GetPartnerTicketedEventDetailsQuery(TicketedEventId EventId, TeamId TeamId)
    : Query<PartnerTicketedEventDetailsDto?>;
