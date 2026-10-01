using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendee;

internal sealed record RegisterAttendeeCommand(
    Guid EventId,
    Guid TeamId,
    string Email,
    string FirstName,
    string LastName,
    Guid[] RegisterTicketTypeIds,
    Guid[] WaitlistTicketTypeIds,
    IReadOnlyDictionary<string, string>? AdditionalDetails = null,
    Guid? CouponCode = null) : Command<RegisterAttendeeResult>;

internal sealed record RegisterAttendeeResult(
    Guid RegistrationId,
    Guid[] RegisteredTicketTypeIds,
    Guid[] WaitlistedTicketTypeIds);
