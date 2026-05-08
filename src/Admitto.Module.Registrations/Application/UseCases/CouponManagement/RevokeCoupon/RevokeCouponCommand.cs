using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.RevokeCoupon;

internal sealed record RevokeCouponCommand(Guid EventId, Guid CouponId) : Command;
