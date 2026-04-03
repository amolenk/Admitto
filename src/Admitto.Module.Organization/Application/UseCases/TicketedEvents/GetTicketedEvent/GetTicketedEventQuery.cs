using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.GetTicketedEvent;

internal sealed record GetTicketedEventQuery(Guid TeamId, string EventSlug) : Query<TicketedEventDto?>;
