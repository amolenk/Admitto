using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.CouponManagement.CreateCoupon;

internal sealed record CreateCouponCommand(
    Guid TeamId,
    Guid EventId,
    string Email,
    Guid[] AllowedTicketTypeIds,
    DateTimeOffset ExpiresAt,
    bool BypassRegistrationWindow) : Command<Guid>;
