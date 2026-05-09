using Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.Registrations.RegisterAttendee;

internal sealed record RegisterAttendeeCommand(
    Guid EventId,
    string Email,
    string FirstName,
    string LastName,
    string[] TicketTypeSlugs,
    RegistrationMode Mode,
    string? CouponCode = null,
    string? EmailVerificationToken = null,
    IReadOnlyDictionary<string, string>? AdditionalDetails = null) : Command<Guid>;
