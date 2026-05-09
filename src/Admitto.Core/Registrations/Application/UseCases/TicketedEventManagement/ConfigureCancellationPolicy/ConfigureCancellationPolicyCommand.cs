using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.ConfigureCancellationPolicy;

internal sealed record ConfigureCancellationPolicyCommand(
    Guid EventId,
    uint? ExpectedVersion,
    DateTimeOffset? LateCancellationCutoff) : Command;
