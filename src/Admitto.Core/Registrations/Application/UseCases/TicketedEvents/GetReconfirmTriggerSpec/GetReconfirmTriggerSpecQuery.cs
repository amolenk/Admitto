using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetReconfirmTriggerSpec;

internal sealed record GetReconfirmTriggerSpecQuery(Guid TeamId, Guid TicketedEventId)
    : Query<ReconfirmTriggerSpecDto?>;
