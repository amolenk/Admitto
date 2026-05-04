using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.GetPublicTicketTypes;

internal sealed record GetPublicTicketTypesQuery(TicketedEventId EventId)
    : Query<IReadOnlyList<PublicTicketTypeDto>>;
