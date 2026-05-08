using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureCancellationPolicy;

internal sealed record ConfigureCancellationPolicyCommand(
    Guid EventId,
    uint? ExpectedVersion,
    DateTimeOffset? LateCancellationCutoff) : Command;
