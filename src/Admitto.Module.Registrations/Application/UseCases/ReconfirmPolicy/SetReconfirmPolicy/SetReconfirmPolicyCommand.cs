using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.ReconfirmPolicy.SetReconfirmPolicy;

internal sealed record SetReconfirmPolicyCommand(
    TicketedEventId EventId,
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    TimeSpan Cadence) : Command;
