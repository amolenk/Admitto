using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy;

internal sealed record SetRegistrationPolicyCommand(
    TicketedEventId EventId,
    DateTimeOffset? RegistrationWindowOpensAt,
    DateTimeOffset? RegistrationWindowClosesAt,
    string? AllowedEmailDomain) : Command;
