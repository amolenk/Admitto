using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CancellationPolicy.SetCancellationPolicy;

internal sealed record SetCancellationPolicyCommand(
    TicketedEventId EventId,
    DateTimeOffset LateCancellationCutoff) : Command;
