using Amolenk.Admitto.Core.Module.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketedEvents.GetActiveReconfirmTriggerSpecs;

internal sealed record GetActiveReconfirmTriggerSpecsQuery
    : Query<IReadOnlyList<ReconfirmTriggerSpecDto>>;
