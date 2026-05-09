using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.GetPublicTicketTypes;

internal sealed record GetPublicTicketTypesQuery(TicketedEventId EventId)
    : Query<IReadOnlyList<PublicTicketTypeDto>>;
