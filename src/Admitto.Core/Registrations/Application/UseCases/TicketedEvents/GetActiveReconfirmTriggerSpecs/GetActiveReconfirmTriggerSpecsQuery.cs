using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetActiveReconfirmTriggerSpecs;

internal sealed record GetActiveReconfirmTriggerSpecsQuery
    : Query<IReadOnlyList<ReconfirmTriggerSpecDto>>;
