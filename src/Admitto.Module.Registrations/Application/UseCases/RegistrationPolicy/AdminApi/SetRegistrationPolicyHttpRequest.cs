using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.AdminApi;

public sealed record SetRegistrationPolicyHttpRequest(
    DateTimeOffset? RegistrationWindowOpensAt,
    DateTimeOffset? RegistrationWindowClosesAt,
    string? AllowedEmailDomain)
{
    internal SetRegistrationPolicyCommand ToCommand(TicketedEventId eventId) => new(
        eventId,
        RegistrationWindowOpensAt,
        RegistrationWindowClosesAt,
        AllowedEmailDomain);
}
