using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.ConfigureRegistrationPolicy;

internal sealed record ConfigureRegistrationPolicyCommand(
    Guid EventId,
    uint? ExpectedVersion,
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    string? AllowedEmailDomain) : Command;
