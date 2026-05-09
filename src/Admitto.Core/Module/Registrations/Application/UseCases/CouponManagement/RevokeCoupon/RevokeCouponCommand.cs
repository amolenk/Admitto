using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.CouponManagement.RevokeCoupon;

internal sealed record RevokeCouponCommand(Guid EventId, Guid CouponId) : Command;
