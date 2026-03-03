using Amolenk.Admitto.Shared.Application.Messaging;

namespace Amolenk.Admitto.Organization.Application.UseCases.TicketedEvents.GetTicketedEventId;

internal record GetTicketedEventIdQuery(Guid TeamId, string EventSlug) : Query<Guid>;