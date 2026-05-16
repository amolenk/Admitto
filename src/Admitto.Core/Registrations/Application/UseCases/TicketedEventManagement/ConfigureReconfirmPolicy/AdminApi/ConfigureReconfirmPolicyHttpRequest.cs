namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.ConfigureReconfirmPolicy.AdminApi;

public sealed record ConfigureReconfirmPolicyHttpRequest(
    DateTimeOffset? OpensAt = null,
    DateTimeOffset? ClosesAt = null,
    int? CadenceHours = null,
    int? MinEmailIntervalHours = null,
    uint? ExpectedVersion = null)
{
    internal ConfigureReconfirmPolicyCommand ToCommand(Guid eventId) => new(
        eventId,
        ExpectedVersion,
        OpensAt,
        ClosesAt,
        CadenceHours,
        MinEmailIntervalHours);
}
