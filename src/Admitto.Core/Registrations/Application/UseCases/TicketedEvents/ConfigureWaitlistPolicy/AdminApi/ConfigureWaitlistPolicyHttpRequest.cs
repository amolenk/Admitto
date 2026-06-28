namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureWaitlistPolicy.AdminApi;

public sealed record ConfigureWaitlistPolicyHttpRequest(
    TimeOnly QuietHoursStart,
    TimeOnly QuietHoursEnd,
    uint? ExpectedVersion = null)
{
    internal ConfigureWaitlistPolicyCommand ToCommand(Guid eventId, Guid teamId) => new(
        eventId,
        teamId,
        ExpectedVersion,
        QuietHoursStart,
        QuietHoursEnd);
}
