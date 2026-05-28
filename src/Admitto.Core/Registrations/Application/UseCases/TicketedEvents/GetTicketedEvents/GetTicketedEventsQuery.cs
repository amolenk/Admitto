using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEvents;

internal sealed record GetTicketedEventsQuery(TeamId TeamId)
    : Query<IReadOnlyList<TicketedEventListItemDto>>;
