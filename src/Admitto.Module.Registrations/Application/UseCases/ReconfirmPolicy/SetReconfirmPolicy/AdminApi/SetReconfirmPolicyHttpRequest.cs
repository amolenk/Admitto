using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.ReconfirmPolicy.SetReconfirmPolicy.AdminApi;

public sealed record SetReconfirmPolicyHttpRequest(
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    int CadenceDays)
{
    internal SetReconfirmPolicyCommand ToCommand(TicketedEventId eventId) => new(
        eventId,
        OpensAt,
        ClosesAt,
        TimeSpan.FromDays(CadenceDays));
}
