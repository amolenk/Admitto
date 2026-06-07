using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.GetPublicCouponDetails;

internal sealed record GetPublicCouponDetailsQuery(TicketedEventId EventId, TeamId TeamId, CouponCode Code)
    : Query<PublicCouponDetailsDto>;
