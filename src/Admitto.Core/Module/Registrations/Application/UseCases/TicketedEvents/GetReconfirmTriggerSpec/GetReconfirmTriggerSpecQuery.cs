using Amolenk.Admitto.Core.Module.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketedEvents.GetReconfirmTriggerSpec;

internal sealed record GetReconfirmTriggerSpecQuery(Guid TicketedEventId)
    : Query<ReconfirmTriggerSpecDto?>;
