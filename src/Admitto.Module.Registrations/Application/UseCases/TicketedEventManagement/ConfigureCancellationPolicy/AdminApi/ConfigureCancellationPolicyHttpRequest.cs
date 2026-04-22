using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureCancellationPolicy.AdminApi;

public sealed record ConfigureCancellationPolicyHttpRequest(
    DateTimeOffset? LateCancellationCutoff = null,
    uint? ExpectedVersion = null)
{
    internal ConfigureCancellationPolicyCommand ToCommand(TicketedEventId eventId) => new(
        eventId,
        ExpectedVersion,
        LateCancellationCutoff);
}
