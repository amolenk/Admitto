using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.GetPublicTicketTypes;

internal sealed record GetPublicTicketTypesQuery(TicketedEventId EventId, TeamId TeamId)
    : Query<IReadOnlyList<PublicTicketTypeDto>>;
