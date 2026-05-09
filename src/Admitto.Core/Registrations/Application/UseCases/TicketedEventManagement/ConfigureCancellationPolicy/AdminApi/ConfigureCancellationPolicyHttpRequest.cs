namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.ConfigureCancellationPolicy.AdminApi;

public sealed record ConfigureCancellationPolicyHttpRequest(
    DateTimeOffset? LateCancellationCutoff = null,
    uint? ExpectedVersion = null)
{
    internal ConfigureCancellationPolicyCommand ToCommand(Guid eventId) => new(
        eventId,
        ExpectedVersion,
        LateCancellationCutoff);
}
