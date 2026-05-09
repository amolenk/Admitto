using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.GetTicketTypes;

internal sealed record GetTicketTypesQuery(TicketedEventId EventId)
    : Query<IReadOnlyList<TicketTypeDto>>;
