using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.ReconfirmPolicy.GetReconfirmPolicy;

internal sealed record GetReconfirmPolicyQuery(TicketedEventId EventId)
    : Query<ReconfirmPolicyDto?>;
