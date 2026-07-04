using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeWithCoupon;

internal sealed record RegisterAttendeeWithCouponCommand(
    Guid EventId,
    Guid TeamId,
    string Email,
    string FirstName,
    string LastName,
    Guid[] TicketTypeIds,
    Guid CouponCode,
    IReadOnlyDictionary<string, string>? AdditionalDetails = null) : Command<Guid>;
