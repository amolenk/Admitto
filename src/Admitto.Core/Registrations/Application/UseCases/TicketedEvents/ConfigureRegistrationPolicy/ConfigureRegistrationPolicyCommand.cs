using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureRegistrationPolicy;

internal sealed record ConfigureRegistrationPolicyCommand(
    Guid EventId,
    Guid TeamId,
    uint? ExpectedVersion,
    DateTimeOffset? OpensAt,
    DateTimeOffset? ClosesAt,
    string? AllowedEmailDomain) : Command;
