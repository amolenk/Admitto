using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CancellationPolicy.SetCancellationPolicy.AdminApi;

public sealed record SetCancellationPolicyHttpRequest(
    DateTimeOffset LateCancellationCutoff)
{
    internal SetCancellationPolicyCommand ToCommand(TicketedEventId eventId) => new(
        eventId,
        LateCancellationCutoff);
}
