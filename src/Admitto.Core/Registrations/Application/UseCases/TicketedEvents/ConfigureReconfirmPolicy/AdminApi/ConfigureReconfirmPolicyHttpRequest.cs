namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureReconfirmPolicy.AdminApi;

public sealed record ConfigureReconfirmPolicyHttpRequest(
    DateTimeOffset? OpensAt = null,
    DateTimeOffset? ClosesAt = null,
    int? MinEmailIntervalHours = null,
    TimeOnly? QuietHoursStart = null,
    TimeOnly? QuietHoursEnd = null,
    uint? ExpectedVersion = null)
{
    internal ConfigureReconfirmPolicyCommand ToCommand(Guid eventId, Guid teamId) => new(
        eventId,
        teamId,
        ExpectedVersion,
        OpensAt,
        ClosesAt,
        MinEmailIntervalHours,
        QuietHoursStart,
        QuietHoursEnd);
}
