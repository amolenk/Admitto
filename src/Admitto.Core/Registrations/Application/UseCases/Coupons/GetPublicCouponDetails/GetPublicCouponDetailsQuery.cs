using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.GetPublicCouponDetails;

internal sealed record GetPublicCouponDetailsQuery(TicketedEventId EventId, CouponCode Code)
    : Query<PublicCouponDetailsDto>;
