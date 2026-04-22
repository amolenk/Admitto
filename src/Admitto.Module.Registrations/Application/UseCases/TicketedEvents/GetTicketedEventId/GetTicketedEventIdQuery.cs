using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventId;

internal record GetTicketedEventIdQuery(Guid TeamId, string EventSlug) : Query<Guid>;
