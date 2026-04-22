using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureRegistrationPolicy.AdminApi;

public sealed record ConfigureRegistrationPolicyHttpRequest(
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    string? AllowedEmailDomain = null,
    uint? ExpectedVersion = null)
{
    internal ConfigureRegistrationPolicyCommand ToCommand(TicketedEventId eventId) => new(
        eventId,
        ExpectedVersion,
        OpensAt,
        ClosesAt,
        AllowedEmailDomain);
}
