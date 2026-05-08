using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureRegistrationPolicy;

internal sealed record ConfigureRegistrationPolicyCommand(
    Guid EventId,
    uint? ExpectedVersion,
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    string? AllowedEmailDomain) : Command;
