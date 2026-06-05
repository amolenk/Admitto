using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.GetTicketTypes;

internal sealed record GetTicketTypesQuery(TicketedEventId EventId, TeamId TeamId)
    : Query<IReadOnlyList<TicketTypeDto>>;
