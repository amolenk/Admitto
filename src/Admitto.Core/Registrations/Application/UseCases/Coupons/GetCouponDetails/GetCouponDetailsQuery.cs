using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.GetCouponDetails;

internal sealed record GetCouponDetailsQuery(TicketedEventId EventId, TeamId TeamId, CouponId CouponId)
    : Query<CouponDetailsDto>;
