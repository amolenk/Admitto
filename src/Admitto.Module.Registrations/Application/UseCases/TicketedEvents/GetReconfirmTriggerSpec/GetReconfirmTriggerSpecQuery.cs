using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEvents.GetReconfirmTriggerSpec;

internal sealed record GetReconfirmTriggerSpecQuery(Guid TicketedEventId)
    : Query<ReconfirmTriggerSpecDto?>;
