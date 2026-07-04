using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.RevokeCoupon;

internal sealed record RevokeCouponCommand(Guid EventId, Guid TeamId, Guid CouponId) : Command;
