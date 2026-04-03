using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.GetTicketedEvents;

internal sealed record GetTicketedEventsQuery(Guid TeamId) : Query<TicketedEventListItemDto[]>;
