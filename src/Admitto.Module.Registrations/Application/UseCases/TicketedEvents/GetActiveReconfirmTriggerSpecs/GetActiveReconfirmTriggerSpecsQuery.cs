using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEvents.GetActiveReconfirmTriggerSpecs;

internal sealed record GetActiveReconfirmTriggerSpecsQuery
    : Query<IReadOnlyList<ReconfirmTriggerSpecDto>>;
