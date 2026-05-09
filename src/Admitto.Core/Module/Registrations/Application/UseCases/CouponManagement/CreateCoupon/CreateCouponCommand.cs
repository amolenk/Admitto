using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.CouponManagement.CreateCoupon;

internal sealed record CreateCouponCommand(
    Guid EventId,
    string Email,
    string[] AllowedTicketTypeSlugs,
    DateTimeOffset ExpiresAt,
    bool BypassRegistrationWindow) : Command<Guid>;
