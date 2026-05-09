namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureReconfirmPolicy.AdminApi;

public sealed record ConfigureReconfirmPolicyHttpRequest(
    DateTimeOffset? OpensAt = null,
    DateTimeOffset? ClosesAt = null,
    int? CadenceDays = null,
    uint? ExpectedVersion = null)
{
    internal ConfigureReconfirmPolicyCommand ToCommand(Guid eventId) => new(
        eventId,
        ExpectedVersion,
        OpensAt,
        ClosesAt,
        CadenceDays);
}
