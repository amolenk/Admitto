using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureReconfirmPolicy.AdminApi;

public sealed record ConfigureReconfirmPolicyHttpRequest(
    DateTimeOffset? OpensAt = null,
    DateTimeOffset? ClosesAt = null,
    int? CadenceDays = null,
    uint? ExpectedVersion = null)
{
    internal ConfigureReconfirmPolicyCommand ToCommand(TicketedEventId eventId) => new(
        eventId,
        ExpectedVersion,
        OpensAt,
        ClosesAt,
        CadenceDays);
}
