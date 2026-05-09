namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureRegistrationPolicy.AdminApi;

public sealed record ConfigureRegistrationPolicyHttpRequest(
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    string? AllowedEmailDomain = null,
    uint? ExpectedVersion = null)
{
    internal ConfigureRegistrationPolicyCommand ToCommand(Guid eventId) => new(
        eventId,
        ExpectedVersion,
        OpensAt,
        ClosesAt,
        AllowedEmailDomain);
}
