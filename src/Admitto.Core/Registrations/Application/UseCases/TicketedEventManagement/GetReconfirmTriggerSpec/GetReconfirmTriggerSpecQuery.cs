using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.GetReconfirmTriggerSpec;

internal sealed record GetReconfirmTriggerSpecQuery(Guid TicketedEventId)
    : Query<ReconfirmTriggerSpecDto?>;
