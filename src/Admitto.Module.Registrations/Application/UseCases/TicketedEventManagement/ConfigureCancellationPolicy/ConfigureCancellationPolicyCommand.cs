using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureCancellationPolicy;

internal sealed record ConfigureCancellationPolicyCommand(
    TicketedEventId EventId,
    uint? ExpectedVersion,
    DateTimeOffset? LateCancellationCutoff) : Command;
