using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.GetCouponDetails;

internal sealed record GetCouponDetailsQuery(TicketedEventId EventId, CouponId CouponId)
    : Query<CouponDetailsDto>;
