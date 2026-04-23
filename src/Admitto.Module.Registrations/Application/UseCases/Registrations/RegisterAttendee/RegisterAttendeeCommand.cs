using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterAttendee;

internal sealed record RegisterAttendeeCommand(
    TicketedEventId EventId,
    EmailAddress Email,
    string[] TicketTypeSlugs,
    RegistrationMode Mode,
    string? CouponCode = null,
    string? EmailVerificationToken = null,
    IReadOnlyDictionary<string, string>? AdditionalDetails = null) : Command<RegistrationId>;
