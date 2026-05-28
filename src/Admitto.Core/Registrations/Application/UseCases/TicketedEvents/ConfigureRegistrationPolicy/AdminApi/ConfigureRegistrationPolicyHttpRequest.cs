namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureRegistrationPolicy.AdminApi;

public sealed record ConfigureRegistrationPolicyHttpRequest(
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    string? AllowedEmailDomain = null,
    uint? ExpectedVersion = null)
{
    internal ConfigureRegistrationPolicyCommand ToCommand(Guid eventId, Guid teamId) => new(
        eventId,
        teamId,
        ExpectedVersion,
        OpensAt,
        ClosesAt,
        AllowedEmailDomain);
}
